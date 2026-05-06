using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sixram.Api.Configuration;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Reporting;
using Sixram.Api.Entities;

namespace Sixram.Api.Services;

public interface IComplianceService
{
    Task<ComplianceSummaryDto> GetSummaryAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ComplianceIssueDto>> GetIssuesAsync(ComplianceIssueQueryDto query, string? actorUserId, CancellationToken cancellationToken = default);
}

public class ComplianceService : IComplianceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserAccessService _userAccessService;
    private readonly IAttendanceCalculationService _attendanceCalculationService;
    private readonly EmployeeDocumentOptions _documentOptions;

    public ComplianceService(
        ApplicationDbContext dbContext,
        IUserAccessService userAccessService,
        IAttendanceCalculationService attendanceCalculationService,
        IOptions<EmployeeDocumentOptions> documentOptions)
    {
        _dbContext = dbContext;
        _userAccessService = userAccessService;
        _attendanceCalculationService = attendanceCalculationService;
        _documentOptions = documentOptions.Value;
    }

    public async Task<ComplianceSummaryDto> GetSummaryAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessCompliance(actor);

        var issues = await BuildIssuesAsync(actor, cancellationToken);
        return new ComplianceSummaryDto
        {
            OpenIssueCount = issues.Count,
            CriticalIssueCount = issues.Count(item => item.Severity == ComplianceSeverityLevels.Critical),
            HighIssueCount = issues.Count(item => item.Severity == ComplianceSeverityLevels.High),
            MissingRequiredDocumentCount = issues.Count(item => item.IssueType == ComplianceIssueTypes.MissingRequiredDocument),
            ExpiredDocumentCount = issues.Count(item => item.IssueType == ComplianceIssueTypes.ExpiredDocument),
            ExpiringSoonDocumentCount = issues.Count(item => item.IssueType == ComplianceIssueTypes.ExpiringSoonDocument),
            MissingGovernmentIdCount = issues.Count(item => item.IssueType == ComplianceIssueTypes.MissingGovernmentId),
            MissingScheduleAssignmentCount = issues.Count(item => item.IssueType == ComplianceIssueTypes.MissingScheduleAssignment),
            MissingCompensationProfileCount = issues.Count(item => item.IssueType == ComplianceIssueTypes.MissingCompensationProfile),
            IncompleteAttendanceCount = issues.Count(item => item.IssueType == ComplianceIssueTypes.IncompleteAttendance)
        };
    }

    public async Task<PagedResultDto<ComplianceIssueDto>> GetIssuesAsync(ComplianceIssueQueryDto query, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessCompliance(actor);

        var items = await BuildIssuesAsync(actor, cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(item =>
                item.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                item.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                item.EmployeeCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                item.EmployeeFullName.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (query.EmployeeId is not null)
        {
            items = items.Where(item => item.EmployeeId == query.EmployeeId.Value).ToList();
        }

        if (query.DepartmentId is not null)
        {
            items = items.Where(item => item.DepartmentId == query.DepartmentId.Value).ToList();
        }

        if (query.BranchId is not null)
        {
            items = items.Where(item => item.BranchId == query.BranchId.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.IssueType))
        {
            items = items.Where(item => item.IssueType == query.IssueType.Trim().ToLowerInvariant()).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Severity))
        {
            items = items.Where(item => item.Severity == query.Severity.Trim().ToLowerInvariant()).ToList();
        }

        items = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("employee", false) => items.OrderBy(item => item.EmployeeFullName).ThenBy(item => item.Title).ToList(),
            ("employee", true) => items.OrderByDescending(item => item.EmployeeFullName).ThenBy(item => item.Title).ToList(),
            ("detected", false) => items.OrderBy(item => item.DetectedAtUtc).ThenBy(item => item.EmployeeFullName).ToList(),
            ("detected", true) => items.OrderByDescending(item => item.DetectedAtUtc).ThenBy(item => item.EmployeeFullName).ToList(),
            (_, false) => items.OrderBy(item => SeverityRank(item.Severity)).ThenBy(item => item.EmployeeFullName).ThenBy(item => item.Title).ToList(),
            _ => items.OrderByDescending(item => SeverityRank(item.Severity)).ThenBy(item => item.EmployeeFullName).ThenBy(item => item.Title).ToList()
        };

        var totalCount = items.Count;
        var pageItems = items
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResultDto<ComplianceIssueDto>
        {
            Items = pageItems,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    private async Task<List<ComplianceIssueDto>> BuildIssuesAsync(PortalActorContext actor, CancellationToken cancellationToken)
    {
        var today = _attendanceCalculationService.GetBusinessToday();
        var expiringSoonCutoff = today.AddDays(Math.Max(1, _documentOptions.ExpiringSoonDays));
        var scopedEmployees = await BuildScopedEmployeesQuery(actor)
            .Include(record => record.Department)
            .Include(record => record.Branch)
            .ToListAsync(cancellationToken);

        var employeeIds = scopedEmployees.Select(record => record.Id).ToArray();
        if (employeeIds.Length == 0)
        {
            return [];
        }

        var documents = await _dbContext.EmployeeDocuments
            .AsNoTracking()
            .Include(record => record.DocumentType)
            .Where(record => employeeIds.Contains(record.EmployeeId))
            .ToListAsync(cancellationToken);

        var requiredDocumentTypes = await _dbContext.DocumentTypes
            .AsNoTracking()
            .Where(record => record.IsActive && record.IsRequired)
            .ToListAsync(cancellationToken);

        var activeAssignments = await _dbContext.EmployeeScheduleAssignments
            .AsNoTracking()
            .Where(record =>
                employeeIds.Contains(record.EmployeeId) &&
                record.IsActive &&
                record.EffectiveStartDate <= today &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= today))
            .ToListAsync(cancellationToken);

        var activeCompensations = await _dbContext.CompensationProfiles
            .AsNoTracking()
            .Where(record =>
                employeeIds.Contains(record.EmployeeId) &&
                record.IsActive &&
                record.EffectiveStartDate <= today &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= today))
            .ToListAsync(cancellationToken);

        var incompleteAttendance = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(record =>
                employeeIds.Contains(record.EmployeeId) &&
                record.AttendanceDate >= today.AddDays(-14) &&
                record.AttendanceDate <= today &&
                record.Status == AttendanceStatuses.Incomplete)
            .ToListAsync(cancellationToken);

        var pendingProfileChanges = await _dbContext.EmployeeProfileChangeRequests
            .AsNoTracking()
            .Where(record => employeeIds.Contains(record.EmployeeId) && record.Status == RequestStatuses.Pending)
            .ToListAsync(cancellationToken);

        var pendingAttendanceAdjustments = await _dbContext.AttendanceAdjustmentRequests
            .AsNoTracking()
            .Where(record => employeeIds.Contains(record.EmployeeId) && record.Status == RequestStatuses.Pending)
            .ToListAsync(cancellationToken);

        var pendingLeaveRequests = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(record => employeeIds.Contains(record.EmployeeId) && record.Status == LeaveRequestStatuses.Pending)
            .ToListAsync(cancellationToken);

        var documentsByEmployee = documents.GroupBy(record => record.EmployeeId).ToDictionary(group => group.Key, group => group.ToList());
        var assignmentsByEmployee = activeAssignments.GroupBy(record => record.EmployeeId).ToDictionary(group => group.Key, group => group.ToList());
        var compensationByEmployee = activeCompensations.GroupBy(record => record.EmployeeId).ToDictionary(group => group.Key, group => group.ToList());
        var attendanceByEmployee = incompleteAttendance.GroupBy(record => record.EmployeeId).ToDictionary(group => group.Key, group => group.ToList());
        var profileChangesByEmployee = pendingProfileChanges.GroupBy(record => record.EmployeeId).ToDictionary(group => group.Key, group => group.ToList());
        var attendanceAdjustmentsByEmployee = pendingAttendanceAdjustments.GroupBy(record => record.EmployeeId).ToDictionary(group => group.Key, group => group.ToList());
        var leaveRequestsByEmployee = pendingLeaveRequests.GroupBy(record => record.EmployeeId).ToDictionary(group => group.Key, group => group.ToList());

        var issues = new List<ComplianceIssueDto>();

        foreach (var employee in scopedEmployees)
        {
            var employeeDocuments = documentsByEmployee.GetValueOrDefault(employee.Id) ?? [];
            var activeEmployeeDocuments = employeeDocuments.Where(record => !record.IsArchived).ToList();

            foreach (var documentType in requiredDocumentTypes)
            {
                var hasDocument = activeEmployeeDocuments.Any(record => record.DocumentTypeId == documentType.Id);
                if (!hasDocument)
                {
                    issues.Add(CreateIssue(
                        ComplianceIssueTypes.MissingRequiredDocument,
                        ComplianceSeverityLevels.High,
                        employee,
                        $"Missing required document: {documentType.Name}",
                        $"{employee.EmployeeCode} does not have an active {documentType.Name} document on file.",
                        "document_type",
                        documentType.Id.ToString(),
                        $"/admin/employees/{employee.Id}"));
                }
            }

            foreach (var document in activeEmployeeDocuments.Where(record => record.ExpiryDate is not null && record.ExpiryDate < today))
            {
                issues.Add(CreateIssue(
                    ComplianceIssueTypes.ExpiredDocument,
                    ComplianceSeverityLevels.Critical,
                    employee,
                    $"Expired document: {document.Title}",
                    $"{document.DocumentType?.Name ?? "Document"} expired on {document.ExpiryDate:yyyy-MM-dd}.",
                    "employee_document",
                    document.Id.ToString(),
                    actor.IsManager && !actor.IsAdministrator && !actor.IsHumanResources ? string.Empty : $"/admin/employees/{employee.Id}"));
            }

            foreach (var document in activeEmployeeDocuments.Where(record => record.ExpiryDate is not null && record.ExpiryDate >= today && record.ExpiryDate <= expiringSoonCutoff))
            {
                issues.Add(CreateIssue(
                    ComplianceIssueTypes.ExpiringSoonDocument,
                    ComplianceSeverityLevels.Medium,
                    employee,
                    $"Document expiring soon: {document.Title}",
                    $"{document.DocumentType?.Name ?? "Document"} will expire on {document.ExpiryDate:yyyy-MM-dd}.",
                    "employee_document",
                    document.Id.ToString(),
                    actor.IsManager && !actor.IsAdministrator && !actor.IsHumanResources ? string.Empty : $"/admin/employees/{employee.Id}"));
            }

            if (string.IsNullOrWhiteSpace(employee.SssNumber) ||
                string.IsNullOrWhiteSpace(employee.PhilHealthNumber) ||
                string.IsNullOrWhiteSpace(employee.PagIbigNumber) ||
                string.IsNullOrWhiteSpace(employee.TinNumber))
            {
                issues.Add(CreateIssue(
                    ComplianceIssueTypes.MissingGovernmentId,
                    ComplianceSeverityLevels.High,
                    employee,
                    "Missing government ID details",
                    "One or more government ID fields are missing from the employee master profile.",
                    "employee",
                    employee.Id.ToString(),
                    $"/admin/employees/{employee.Id}"));
            }

            if (string.IsNullOrWhiteSpace(employee.EmergencyContactName) || string.IsNullOrWhiteSpace(employee.EmergencyContactPhone))
            {
                issues.Add(CreateIssue(
                    ComplianceIssueTypes.MissingEmergencyContact,
                    ComplianceSeverityLevels.Medium,
                    employee,
                    "Missing emergency contact details",
                    "Emergency contact information is incomplete.",
                    "employee",
                    employee.Id.ToString(),
                    $"/admin/employees/{employee.Id}"));
            }

            if (!assignmentsByEmployee.ContainsKey(employee.Id))
            {
                issues.Add(CreateIssue(
                    ComplianceIssueTypes.MissingScheduleAssignment,
                    ComplianceSeverityLevels.High,
                    employee,
                    "No active schedule assignment",
                    "The employee does not have an active work schedule assignment for today.",
                    "employee",
                    employee.Id.ToString(),
                    $"/admin/attendance/assignments"));
            }

            if (!compensationByEmployee.ContainsKey(employee.Id))
            {
                issues.Add(CreateIssue(
                    ComplianceIssueTypes.MissingCompensationProfile,
                    ComplianceSeverityLevels.High,
                    employee,
                    "No active compensation profile",
                    "The employee does not have an active compensation profile for current payroll preparation.",
                    "employee",
                    employee.Id.ToString(),
                    $"/admin/payroll/compensation"));
            }

            foreach (var attendance in attendanceByEmployee.GetValueOrDefault(employee.Id) ?? [])
            {
                issues.Add(CreateIssue(
                    ComplianceIssueTypes.IncompleteAttendance,
                    ComplianceSeverityLevels.High,
                    employee,
                    $"Incomplete attendance log: {attendance.AttendanceDate:yyyy-MM-dd}",
                    "The attendance record has incomplete timekeeping data that should be corrected.",
                    "attendance_record",
                    attendance.Id.ToString(),
                    actor.IsManager && !actor.IsAdministrator && !actor.IsHumanResources ? "/manager/attendance" : "/admin/attendance"));
            }

            foreach (var request in profileChangesByEmployee.GetValueOrDefault(employee.Id) ?? [])
            {
                issues.Add(CreateIssue(
                    ComplianceIssueTypes.PendingProfileChange,
                    ComplianceSeverityLevels.Low,
                    employee,
                    "Pending profile change request",
                    "A self-service profile update is awaiting review.",
                    ApprovableTypes.ProfileChangeRequest,
                    request.Id.ToString(),
                    "/approvals"));
            }

            foreach (var request in attendanceAdjustmentsByEmployee.GetValueOrDefault(employee.Id) ?? [])
            {
                issues.Add(CreateIssue(
                    ComplianceIssueTypes.PendingAttendanceAdjustment,
                    ComplianceSeverityLevels.Medium,
                    employee,
                    "Pending attendance adjustment",
                    "An attendance correction request is awaiting review.",
                    ApprovableTypes.AttendanceAdjustmentRequest,
                    request.Id.ToString(),
                    "/approvals"));
            }

            foreach (var request in leaveRequestsByEmployee.GetValueOrDefault(employee.Id) ?? [])
            {
                issues.Add(CreateIssue(
                    ComplianceIssueTypes.PendingLeaveRequest,
                    ComplianceSeverityLevels.Low,
                    employee,
                    "Pending leave request",
                    "A leave request is still waiting for approval.",
                    ApprovableTypes.LeaveRequest,
                    request.Id.ToString(),
                    "/approvals"));
            }
        }

        return issues;
    }

    private IQueryable<Employee> BuildScopedEmployeesQuery(PortalActorContext actor)
    {
        var source = _dbContext.Employees.AsNoTracking().AsQueryable();
        if (actor.IsAdministrator || actor.IsHumanResources)
        {
            return source;
        }

        return source.Where(record => actor.ManagedEmployeeIds.Contains(record.Id));
    }

    private static ComplianceIssueDto CreateIssue(
        string issueType,
        string severity,
        Employee employee,
        string title,
        string description,
        string referenceType,
        string referenceId,
        string linkPath)
    {
        return new ComplianceIssueDto
        {
            Id = $"{issueType}:{referenceId}:{employee.Id}",
            IssueType = issueType,
            Severity = severity,
            EmployeeId = employee.Id,
            DepartmentId = employee.DepartmentId,
            BranchId = employee.BranchId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = string.Join(" ", new[]
            {
                employee.FirstName.Trim(),
                employee.MiddleName.Trim(),
                employee.LastName.Trim(),
                employee.Suffix.Trim()
            }.Where(part => !string.IsNullOrWhiteSpace(part))),
            DepartmentName = employee.Department?.Name ?? string.Empty,
            BranchName = employee.Branch?.Name ?? string.Empty,
            Title = title,
            Description = description,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            LinkPath = linkPath,
            DetectedAtUtc = DateTime.UtcNow
        };
    }

    private static int SeverityRank(string severity)
    {
        return severity switch
        {
            ComplianceSeverityLevels.Critical => 4,
            ComplianceSeverityLevels.High => 3,
            ComplianceSeverityLevels.Medium => 2,
            _ => 1
        };
    }
}
