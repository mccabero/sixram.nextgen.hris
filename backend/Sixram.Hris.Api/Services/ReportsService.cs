using System.Text;
using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.DTOs.Reporting;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public sealed record ReportExportFile(byte[] Content, string FileName, string ContentType);

public interface IReportsService
{
    Task<ReportsCenterDto> GetRegistryAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<ReportOptionsDto> GetOptionsAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<ReportResultDto> RunReportAsync(string reportKey, ReportQueryDto query, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ReportExportFile> ExportCsvAsync(string reportKey, ReportQueryDto query, string? actorUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedReportDto>> GetSavedReportsAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<SavedReportDto> CreateSavedReportAsync(SaveSavedReportDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<SavedReportDto> UpdateSavedReportAsync(Guid savedReportId, SaveSavedReportDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task DeleteSavedReportAsync(Guid savedReportId, string? actorUserId, CancellationToken cancellationToken = default);
}

public class ReportsService : IReportsService
{
    private static readonly IReadOnlySet<string> DocumentComplianceReportIssueTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ComplianceIssueTypes.MissingRequiredDocument,
        ComplianceIssueTypes.ExpiredDocument,
        ComplianceIssueTypes.ExpiringSoonDocument
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IUserAccessService _userAccessService;
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceCalculationService _attendanceCalculationService;
    private readonly IComplianceService _complianceService;
    private readonly IAuditLogService _auditLogService;

    public ReportsService(
        ApplicationDbContext dbContext,
        IUserAccessService userAccessService,
        IAttendanceService attendanceService,
        IAttendanceCalculationService attendanceCalculationService,
        IComplianceService complianceService,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _userAccessService = userAccessService;
        _attendanceService = attendanceService;
        _attendanceCalculationService = attendanceCalculationService;
        _complianceService = complianceService;
        _auditLogService = auditLogService;
    }

    public async Task<ReportsCenterDto> GetRegistryAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessReports(actor);

        return new ReportsCenterDto
        {
            Reports = GetDefinitions()
                .Where(definition => CanRunReport(actor, definition.Key))
                .ToList()
        };
    }

    public async Task<ReportOptionsDto> GetOptionsAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessReports(actor);

        var employees = await BuildScopedEmployeeQuery(actor, includeInactive: false)
            .Include(record => record.Department)
            .Include(record => record.Branch)
            .OrderBy(record => record.LastName)
            .ThenBy(record => record.FirstName)
            .Select(record => new EmployeeAttendanceOptionDto
            {
                Id = record.Id,
                EmployeeCode = record.EmployeeCode,
                FullName = BuildFullName(record.FirstName, record.MiddleName, record.LastName, record.Suffix),
                DepartmentName = record.Department != null ? record.Department.Name : string.Empty,
                BranchName = record.Branch != null ? record.Branch.Name : string.Empty,
                IsActive = record.IsActive
            })
            .ToListAsync(cancellationToken);

        var canAccessPayroll = actor.IsAdministrator || actor.IsPayrollOfficer;

        return new ReportOptionsDto
        {
            Employees = employees,
            Departments = await _dbContext.Departments.AsNoTracking().OrderBy(record => record.Name).Select(record => new LookupOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                IsActive = record.IsActive
            }).ToListAsync(cancellationToken),
            Branches = await _dbContext.Branches.AsNoTracking().OrderBy(record => record.Name).Select(record => new LookupOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                IsActive = record.IsActive
            }).ToListAsync(cancellationToken),
            EmploymentTypes = await _dbContext.EmploymentTypes.AsNoTracking().OrderBy(record => record.Name).Select(record => new LookupOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                IsActive = record.IsActive
            }).ToListAsync(cancellationToken),
            EmploymentStatuses = await _dbContext.EmploymentStatuses.AsNoTracking().OrderBy(record => record.Name).Select(record => new LookupOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                IsActive = record.IsActive
            }).ToListAsync(cancellationToken),
            LeaveTypes = await _dbContext.LeaveTypes.AsNoTracking().OrderBy(record => record.Name).Select(record => new LookupOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                IsActive = record.IsActive
            }).ToListAsync(cancellationToken),
            DocumentTypes = await _dbContext.DocumentTypes.AsNoTracking().OrderBy(record => record.Name).Select(record => new LookupOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                IsActive = record.IsActive
            }).ToListAsync(cancellationToken),
            PayPeriods = canAccessPayroll
                ? await _dbContext.PayPeriods.AsNoTracking().OrderByDescending(record => record.PayrollDate).Select(record => new LookupOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    IsActive = record.Status != PayPeriodStatuses.Cancelled
                }).ToListAsync(cancellationToken)
                : [],
            PayrollRuns = canAccessPayroll
                ? await _dbContext.PayrollRuns.AsNoTracking().OrderByDescending(record => record.GeneratedAtUtc).Select(record => new LookupOptionDto
                {
                    Id = record.Id,
                    Code = record.ReferenceNumber,
                    Name = record.Name,
                    IsActive = record.Status != PayrollRunStatuses.Cancelled
                }).ToListAsync(cancellationToken)
                : []
        };
    }

    public async Task<ReportResultDto> RunReportAsync(string reportKey, ReportQueryDto query, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessReports(actor);
        EnsureCanRunReport(actor, reportKey);

        return reportKey switch
        {
            ReportKeys.EmployeeMasterList => await BuildEmployeeMasterListAsync(actor, query, cancellationToken),
            ReportKeys.EmployeeProfileCompleteness => await BuildEmployeeProfileCompletenessAsync(actor, query, cancellationToken),
            ReportKeys.DepartmentHeadcount => await BuildDepartmentHeadcountAsync(actor, query, cancellationToken),
            ReportKeys.BranchHeadcount => await BuildBranchHeadcountAsync(actor, query, cancellationToken),
            ReportKeys.DocumentComplianceIssues => await BuildDocumentComplianceReportAsync(actor, query, actorUserId, cancellationToken),
            ReportKeys.AttendanceDaily => await BuildAttendanceDailyReportAsync(actor, query, cancellationToken),
            ReportKeys.AttendanceSummary => await BuildAttendanceSummaryReportAsync(actor, query, cancellationToken),
            ReportKeys.LeaveUsage => await BuildLeaveUsageReportAsync(actor, query, cancellationToken),
            ReportKeys.LeaveBalances => await BuildLeaveBalanceReportAsync(actor, query, cancellationToken),
            ReportKeys.PayrollRegister => await BuildPayrollRegisterReportAsync(actor, query, cancellationToken),
            ReportKeys.ApprovalAging => await BuildApprovalAgingReportAsync(actor, query, cancellationToken),
            ReportKeys.AuditActivity => await BuildAuditActivityReportAsync(query, actorUserId, cancellationToken),
            _ => throw new NotFoundException($"Report '{reportKey}' was not found.")
        };
    }

    public async Task<ReportExportFile> ExportCsvAsync(string reportKey, ReportQueryDto query, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var exportQuery = new ReportQueryDto
        {
            Search = query.Search,
            EmployeeId = query.EmployeeId,
            DepartmentId = query.DepartmentId,
            BranchId = query.BranchId,
            EmploymentTypeId = query.EmploymentTypeId,
            EmploymentStatusId = query.EmploymentStatusId,
            LeaveTypeId = query.LeaveTypeId,
            DocumentTypeId = query.DocumentTypeId,
            PayPeriodId = query.PayPeriodId,
            PayrollRunId = query.PayrollRunId,
            Status = query.Status,
            Source = query.Source,
            IssueType = query.IssueType,
            Severity = query.Severity,
            EntityType = query.EntityType,
            Action = query.Action,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            Year = query.Year,
            Month = query.Month,
            IncludeInactive = query.IncludeInactive,
            SortBy = query.SortBy,
            Descending = query.Descending,
            PageNumber = 1,
            PageSize = 5000
        };

        var report = await RunReportAsync(reportKey, exportQuery, actorUserId, cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", report.Columns.Select(column => EscapeCsv(column.Label))));

        foreach (var row in report.Rows)
        {
            builder.AppendLine(string.Join(",", report.Columns.Select(column => EscapeCsv(row.Values.GetValueOrDefault(column.Key) ?? string.Empty))));
        }

        await _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Action = "export",
                EntityType = AuditEntityTypes.Report,
                EntityId = report.ReportKey,
                Remarks = $"Exported CSV for report '{report.Title}'."
            },
            cancellationToken);

        return new ReportExportFile(
            Encoding.UTF8.GetBytes(builder.ToString()),
            $"{report.ReportKey}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv",
            "text/csv");
    }

    public async Task<IReadOnlyList<SavedReportDto>> GetSavedReportsAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessReports(actor);

        return await _dbContext.SavedReports
            .AsNoTracking()
            .Where(record => record.UserId == actor.UserId)
            .OrderByDescending(record => record.IsDefault)
            .ThenBy(record => record.Name)
            .Select(record => new SavedReportDto
            {
                Id = record.Id,
                ReportKey = record.ReportKey,
                Name = record.Name,
                FiltersJson = record.FiltersJson,
                IsDefault = record.IsDefault,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<SavedReportDto> CreateSavedReportAsync(SaveSavedReportDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessReports(actor);
        EnsureCanRunReport(actor, request.ReportKey);

        if (request.IsDefault)
        {
            await ClearDefaultSavedReportsAsync(actor.UserId, request.ReportKey, cancellationToken);
        }

        var record = new SavedReport
        {
            UserId = actor.UserId,
            ReportKey = request.ReportKey.Trim(),
            Name = request.Name.Trim(),
            FiltersJson = string.IsNullOrWhiteSpace(request.FiltersJson) ? "{}" : request.FiltersJson,
            IsDefault = request.IsDefault,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.SavedReports.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SavedReportDto
        {
            Id = record.Id,
            ReportKey = record.ReportKey,
            Name = record.Name,
            FiltersJson = record.FiltersJson,
            IsDefault = record.IsDefault,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    public async Task<SavedReportDto> UpdateSavedReportAsync(Guid savedReportId, SaveSavedReportDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessReports(actor);
        EnsureCanRunReport(actor, request.ReportKey);

        var record = await _dbContext.SavedReports.SingleOrDefaultAsync(item => item.Id == savedReportId && item.UserId == actor.UserId, cancellationToken)
            ?? throw new NotFoundException($"Saved report '{savedReportId}' was not found.");

        if (request.IsDefault)
        {
            await ClearDefaultSavedReportsAsync(actor.UserId, request.ReportKey, cancellationToken);
        }

        record.ReportKey = request.ReportKey.Trim();
        record.Name = request.Name.Trim();
        record.FiltersJson = string.IsNullOrWhiteSpace(request.FiltersJson) ? "{}" : request.FiltersJson;
        record.IsDefault = request.IsDefault;
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SavedReportDto
        {
            Id = record.Id,
            ReportKey = record.ReportKey,
            Name = record.Name,
            FiltersJson = record.FiltersJson,
            IsDefault = record.IsDefault,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    public async Task DeleteSavedReportAsync(Guid savedReportId, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessReports(actor);

        var record = await _dbContext.SavedReports.SingleOrDefaultAsync(item => item.Id == savedReportId && item.UserId == actor.UserId, cancellationToken)
            ?? throw new NotFoundException($"Saved report '{savedReportId}' was not found.");

        _dbContext.SavedReports.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ReportResultDto> BuildEmployeeMasterListAsync(PortalActorContext actor, ReportQueryDto query, CancellationToken cancellationToken)
    {
        var source = ApplyCommonEmployeeFilters(
            BuildScopedEmployeeQuery(actor, includeInactive: query.IncludeInactive == true)
                .Include(record => record.Department)
                .Include(record => record.Position)
                .Include(record => record.Branch)
                .Include(record => record.EmploymentType)
                .Include(record => record.EmploymentStatus)
                .Include(record => record.Manager),
            query,
            includeInactiveByDefault: false);

        var totalCount = await source.CountAsync(cancellationToken);
        var activeCount = await source.CountAsync(record => record.IsActive, cancellationToken);
        var inactiveCount = totalCount - activeCount;
        source = ApplyEmployeeSorting(source, query.SortBy, query.Descending);

        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var rows = records
            .Select(record => new ReportRowDto
            {
                Id = record.Id.ToString(),
                LinkPath = $"/admin/employees/{record.Id}",
                Values = new Dictionary<string, string>
                {
                    ["employeeCode"] = record.EmployeeCode,
                    ["fullName"] = BuildFullName(record.FirstName, record.MiddleName, record.LastName, record.Suffix),
                    ["departmentName"] = record.Department != null ? record.Department.Name : string.Empty,
                    ["positionName"] = record.Position != null ? record.Position.Name : string.Empty,
                    ["branchName"] = record.Branch != null ? record.Branch.Name : string.Empty,
                    ["employmentTypeName"] = record.EmploymentType != null ? record.EmploymentType.Name : string.Empty,
                    ["employmentStatusName"] = record.EmploymentStatus != null ? record.EmploymentStatus.Name : string.Empty,
                    ["dateHired"] = record.DateHired.HasValue ? record.DateHired.Value.ToString("yyyy-MM-dd") : string.Empty,
                    ["managerName"] = record.Manager != null ? BuildFullName(record.Manager.FirstName, record.Manager.MiddleName, record.Manager.LastName, record.Manager.Suffix) : string.Empty,
                    ["email"] = record.Email,
                    ["mobileNumber"] = record.MobileNumber,
                    ["active"] = record.IsActive ? "Active" : "Inactive"
                }
            })
            .ToList();

        return BuildReport(
            ReportKeys.EmployeeMasterList,
            "Employee Master List",
            "Employee roster with organization placement and contact references.",
            query,
            totalCount,
            GetEmployeeMasterColumns(),
            rows,
            [
                Metric("total", "Total employees", totalCount),
                Metric("active", "Active employees", activeCount, "success"),
                Metric("inactive", "Inactive employees", inactiveCount, inactiveCount > 0 ? "warning" : "default")
            ]);
    }

    private async Task<ReportResultDto> BuildEmployeeProfileCompletenessAsync(PortalActorContext actor, ReportQueryDto query, CancellationToken cancellationToken)
    {
        var employees = await ApplyCommonEmployeeFilters(
                BuildScopedEmployeeQuery(actor, includeInactive: query.IncludeInactive == true)
                    .Include(record => record.Department)
                    .Include(record => record.Branch),
                query,
                includeInactiveByDefault: false)
            .ToListAsync(cancellationToken);

        var rows = employees.Select(record =>
        {
            var completeness = CalculateProfileCompletion(record);
            var missingFields = GetMissingProfileFields(record);
            return new
            {
                Employee = record,
                Completeness = completeness,
                MissingFields = missingFields
            };
        }).ToList();

        rows = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("completion", false) => rows.OrderBy(item => item.Completeness).ThenBy(item => item.Employee.LastName).ToList(),
            ("completion", true) => rows.OrderByDescending(item => item.Completeness).ThenBy(item => item.Employee.LastName).ToList(),
            (_, true) => rows.OrderByDescending(item => BuildFullName(item.Employee.FirstName, item.Employee.MiddleName, item.Employee.LastName, item.Employee.Suffix)).ToList(),
            _ => rows.OrderBy(item => BuildFullName(item.Employee.FirstName, item.Employee.MiddleName, item.Employee.LastName, item.Employee.Suffix)).ToList()
        };

        var totalCount = rows.Count;
        var pageRows = rows
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new ReportRowDto
            {
                Id = item.Employee.Id.ToString(),
                LinkPath = $"/admin/employees/{item.Employee.Id}",
                Values = new Dictionary<string, string>
                {
                    ["employeeCode"] = item.Employee.EmployeeCode,
                    ["fullName"] = BuildFullName(item.Employee.FirstName, item.Employee.MiddleName, item.Employee.LastName, item.Employee.Suffix),
                    ["departmentName"] = item.Employee.Department?.Name ?? string.Empty,
                    ["branchName"] = item.Employee.Branch?.Name ?? string.Empty,
                    ["completion"] = $"{item.Completeness}%",
                    ["missingFields"] = string.Join(", ", item.MissingFields),
                    ["missingFieldCount"] = item.MissingFields.Count.ToString()
                }
            })
            .ToList();

        var averageCompletion = totalCount == 0 ? 0 : (int)Math.Round(rows.Average(item => item.Completeness));
        return BuildReport(
            ReportKeys.EmployeeProfileCompleteness,
            "Employee Profile Completeness",
            "Employees with missing profile details, emergency contacts, or government identifiers.",
            query,
            totalCount,
            [
                new ReportColumnDto { Key = "employeeCode", Label = "Employee Code", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "fullName", Label = "Employee", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "departmentName", Label = "Department", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "branchName", Label = "Branch", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "completion", Label = "Completion", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "missingFieldCount", Label = "Missing Fields", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "missingFields", Label = "Missing Details", Alignment = ReportColumnAlignment.Left }
            ],
            pageRows,
            [
                Metric("employees", "Employees reviewed", totalCount),
                Metric("average", "Average completion", $"{averageCompletion}%"),
                Metric("needsAttention", "Below 80%", rows.Count(item => item.Completeness < 80), rows.Any(item => item.Completeness < 80) ? "warning" : "success")
            ]);
    }

    private async Task<ReportResultDto> BuildDepartmentHeadcountAsync(PortalActorContext actor, ReportQueryDto query, CancellationToken cancellationToken)
    {
        var employees = await ApplyCommonEmployeeFilters(
                BuildScopedEmployeeQuery(actor, includeInactive: query.IncludeInactive == true)
                    .Include(record => record.Department),
                query,
                includeInactiveByDefault: false)
            .ToListAsync(cancellationToken);

        var grouped = employees
            .GroupBy(record => new
            {
                record.DepartmentId,
                DepartmentCode = record.Department != null ? record.Department.Code : "UNASSIGNED",
                DepartmentName = record.Department != null ? record.Department.Name : "Unassigned"
            })
            .Select(group =>
            {
                var managerIds = group
                    .Where(item => item.ManagerId.HasValue)
                    .Select(item => item.ManagerId!.Value)
                    .ToHashSet();

                return new
                {
                    group.Key.DepartmentId,
                    group.Key.DepartmentCode,
                    group.Key.DepartmentName,
                    Total = group.Count(),
                    Active = group.Count(record => record.IsActive),
                    Inactive = group.Count(record => !record.IsActive),
                    ManagerCount = group.Count(record => managerIds.Contains(record.Id))
                };
            })
            .ToList();

        var ordered = query.Descending
            ? grouped.OrderByDescending(item => item.Total).ThenBy(item => item.DepartmentName).ToList()
            : grouped.OrderBy(item => item.DepartmentName).ToList();

        var totalCount = ordered.Count;
        var rows = ordered
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new ReportRowDto
            {
                Id = item.DepartmentId?.ToString() ?? item.DepartmentCode,
                Values = new Dictionary<string, string>
                {
                    ["code"] = item.DepartmentCode,
                    ["name"] = item.DepartmentName,
                    ["total"] = item.Total.ToString(),
                    ["active"] = item.Active.ToString(),
                    ["inactive"] = item.Inactive.ToString(),
                    ["managerCount"] = item.ManagerCount.ToString()
                }
            })
            .ToList();

        return BuildReport(
            ReportKeys.DepartmentHeadcount,
            "Department Headcount",
            "Headcount distribution by department.",
            query,
            totalCount,
            [
                new ReportColumnDto { Key = "code", Label = "Code", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "name", Label = "Department", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "total", Label = "Headcount", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "active", Label = "Active", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "inactive", Label = "Inactive", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "managerCount", Label = "Managers", Alignment = ReportColumnAlignment.Right }
            ],
            rows,
            [
                Metric("departments", "Departments", totalCount),
                Metric("employees", "Employees", ordered.Sum(item => item.Total)),
                Metric("active", "Active", ordered.Sum(item => item.Active), "success")
            ]);
    }

    private async Task<ReportResultDto> BuildBranchHeadcountAsync(PortalActorContext actor, ReportQueryDto query, CancellationToken cancellationToken)
    {
        var grouped = await ApplyCommonEmployeeFilters(
                BuildScopedEmployeeQuery(actor, includeInactive: query.IncludeInactive == true)
                    .Include(record => record.Branch),
                query,
                includeInactiveByDefault: false)
            .GroupBy(record => new
            {
                record.BranchId,
                BranchCode = record.Branch != null ? record.Branch.Code : "UNASSIGNED",
                BranchName = record.Branch != null ? record.Branch.Name : "Unassigned"
            })
            .Select(group => new
            {
                group.Key.BranchId,
                group.Key.BranchCode,
                group.Key.BranchName,
                Total = group.Count(),
                Active = group.Count(record => record.IsActive),
                Inactive = group.Count(record => !record.IsActive)
            })
            .ToListAsync(cancellationToken);

        var ordered = query.Descending
            ? grouped.OrderByDescending(item => item.Total).ThenBy(item => item.BranchName).ToList()
            : grouped.OrderBy(item => item.BranchName).ToList();

        var totalCount = ordered.Count;
        var rows = ordered
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new ReportRowDto
            {
                Id = item.BranchId?.ToString() ?? item.BranchCode,
                Values = new Dictionary<string, string>
                {
                    ["code"] = item.BranchCode,
                    ["name"] = item.BranchName,
                    ["total"] = item.Total.ToString(),
                    ["active"] = item.Active.ToString(),
                    ["inactive"] = item.Inactive.ToString()
                }
            })
            .ToList();

        return BuildReport(
            ReportKeys.BranchHeadcount,
            "Branch Headcount",
            "Headcount distribution by branch or location.",
            query,
            totalCount,
            [
                new ReportColumnDto { Key = "code", Label = "Code", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "name", Label = "Branch", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "total", Label = "Headcount", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "active", Label = "Active", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "inactive", Label = "Inactive", Alignment = ReportColumnAlignment.Right }
            ],
            rows,
            [
                Metric("branches", "Branches", totalCount),
                Metric("employees", "Employees", ordered.Sum(item => item.Total)),
                Metric("active", "Active", ordered.Sum(item => item.Active), "success")
            ]);
    }

    private async Task<ReportResultDto> BuildDocumentComplianceReportAsync(PortalActorContext actor, ReportQueryDto query, string? actorUserId, CancellationToken cancellationToken)
    {
        var complianceIssues = await _complianceService.GetIssuesAsync(new ComplianceIssueQueryDto
        {
            Search = query.Search,
            EmployeeId = query.EmployeeId,
            DepartmentId = query.DepartmentId,
            BranchId = query.BranchId,
            IssueType = query.IssueType,
            Severity = query.Severity,
            PageNumber = 1,
            PageSize = 5000,
            SortBy = "severity",
            Descending = true
        }, actorUserId, cancellationToken);

        var items = complianceIssues.Items
            .Where(item => DocumentComplianceReportIssueTypes.Contains(item.IssueType))
            .ToList();

        var totalCount = items.Count;
        var rows = items
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new ReportRowDto
            {
                Id = item.Id,
                LinkPath = item.LinkPath,
                Values = new Dictionary<string, string>
                {
                    ["employeeCode"] = item.EmployeeCode,
                    ["employeeFullName"] = item.EmployeeFullName,
                    ["departmentName"] = item.DepartmentName,
                    ["branchName"] = item.BranchName,
                    ["issueType"] = ToTitle(item.IssueType.Replace('_', ' ')),
                    ["severity"] = ToTitle(item.Severity),
                    ["title"] = item.Title,
                    ["description"] = item.Description
                }
            })
            .ToList();

        return BuildReport(
            ReportKeys.DocumentComplianceIssues,
            "Document Compliance Issues",
            "Missing, expired, or expiring employee document requirements.",
            query,
            totalCount,
            [
                new ReportColumnDto { Key = "employeeCode", Label = "Employee Code", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "employeeFullName", Label = "Employee", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "departmentName", Label = "Department", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "branchName", Label = "Branch", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "issueType", Label = "Issue Type", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "severity", Label = "Severity", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "title", Label = "Title", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "description", Label = "Description", Alignment = ReportColumnAlignment.Left }
            ],
            rows,
            [
                Metric("issues", "Issues", totalCount),
                Metric("expired", "Expired", items.Count(item => item.IssueType == ComplianceIssueTypes.ExpiredDocument), items.Any(item => item.IssueType == ComplianceIssueTypes.ExpiredDocument) ? "danger" : "default"),
                Metric("expiringSoon", "Expiring soon", items.Count(item => item.IssueType == ComplianceIssueTypes.ExpiringSoonDocument), items.Any(item => item.IssueType == ComplianceIssueTypes.ExpiringSoonDocument) ? "warning" : "default"),
                Metric("missing", "Missing required", items.Count(item => item.IssueType == ComplianceIssueTypes.MissingRequiredDocument), items.Any(item => item.IssueType == ComplianceIssueTypes.MissingRequiredDocument) ? "warning" : "default")
            ]);
    }

    private async Task<ReportResultDto> BuildAttendanceDailyReportAsync(PortalActorContext actor, ReportQueryDto query, CancellationToken cancellationToken)
    {
        var (dateFrom, dateTo) = NormalizeDateRange(query.DateFrom, query.DateTo, _attendanceCalculationService.GetBusinessToday());
        var attendance = await _attendanceService.GetAttendanceRecordsAsync(new AttendanceRecordListQueryDto
        {
            Search = query.Search,
            EmployeeId = query.EmployeeId,
            EmployeeIds = actor.IsManager && !actor.IsAdministrator && !actor.IsHumanResources ? actor.ManagedEmployeeIds : [],
            DepartmentId = query.DepartmentId,
            BranchId = query.BranchId,
            Status = query.Status,
            Source = query.Source,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = 1,
            PageSize = 5000,
            SortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "date" : query.SortBy,
            Descending = query.Descending
        }, cancellationToken);

        var items = attendance.Items.ToList();
        var totalCount = items.Count;
        var pageRows = items
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new ReportRowDto
            {
                Id = item.AttendanceRecordId?.ToString() ?? $"{item.EmployeeId}:{item.AttendanceDate:yyyyMMdd}",
                LinkPath = actor.IsManager && !actor.IsAdministrator && !actor.IsHumanResources ? "/manager/attendance" : "/admin/attendance",
                Values = new Dictionary<string, string>
                {
                    ["employeeCode"] = item.EmployeeCode,
                    ["employeeFullName"] = item.EmployeeFullName,
                    ["departmentName"] = item.DepartmentName,
                    ["branchName"] = item.BranchName,
                    ["attendanceDate"] = item.AttendanceDate.ToString("yyyy-MM-dd"),
                    ["status"] = item.Status,
                    ["scheduledTime"] = BuildScheduleLabel(item.ScheduledStartTime, item.ScheduledEndTime),
                    ["actualTime"] = BuildScheduleLabel(item.ActualTimeIn, item.ActualTimeOut),
                    ["lateMinutes"] = item.LateMinutes.ToString(),
                    ["undertimeMinutes"] = item.UndertimeMinutes.ToString(),
                    ["overtimeMinutes"] = item.OvertimeMinutes.ToString(),
                    ["source"] = item.Source
                }
            })
            .ToList();

        return BuildReport(
            ReportKeys.AttendanceDaily,
            "Daily Attendance",
            "Daily attendance records with calculated late, undertime, and overtime values.",
            query,
            totalCount,
            [
                new ReportColumnDto { Key = "employeeCode", Label = "Employee Code", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "employeeFullName", Label = "Employee", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "departmentName", Label = "Department", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "branchName", Label = "Branch", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "attendanceDate", Label = "Date", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "status", Label = "Status", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "scheduledTime", Label = "Scheduled", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "actualTime", Label = "Actual", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "lateMinutes", Label = "Late", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "undertimeMinutes", Label = "Under", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "overtimeMinutes", Label = "OT", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "source", Label = "Source", Alignment = ReportColumnAlignment.Left }
            ],
            pageRows,
            [
                Metric("present", "Present", items.Count(item => item.Status == AttendanceStatuses.Present), "success"),
                Metric("late", "Late", items.Count(item => item.Status == AttendanceStatuses.Late), items.Any(item => item.Status == AttendanceStatuses.Late) ? "warning" : "default"),
                Metric("absent", "Absent", items.Count(item => item.Status == AttendanceStatuses.Absent), items.Any(item => item.Status == AttendanceStatuses.Absent) ? "danger" : "default"),
                Metric("incomplete", "Incomplete", items.Count(item => item.Status == AttendanceStatuses.Incomplete), items.Any(item => item.Status == AttendanceStatuses.Incomplete) ? "warning" : "default")
            ]);
    }

    private async Task<ReportResultDto> BuildAttendanceSummaryReportAsync(PortalActorContext actor, ReportQueryDto query, CancellationToken cancellationToken)
    {
        var (dateFrom, dateTo) = NormalizeDateRange(query.DateFrom, query.DateTo, _attendanceCalculationService.GetBusinessToday());
        var attendance = await _attendanceService.GetAttendanceRecordsAsync(new AttendanceRecordListQueryDto
        {
            Search = query.Search,
            EmployeeId = query.EmployeeId,
            EmployeeIds = actor.IsManager && !actor.IsAdministrator && !actor.IsHumanResources ? actor.ManagedEmployeeIds : [],
            DepartmentId = query.DepartmentId,
            BranchId = query.BranchId,
            Status = query.Status,
            Source = query.Source,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = 1,
            PageSize = 5000,
            SortBy = "date",
            Descending = true
        }, cancellationToken);

        var grouped = attendance.Items
            .GroupBy(item => item.DepartmentName)
            .Select(group => new
            {
                DepartmentName = string.IsNullOrWhiteSpace(group.Key) ? "Unassigned" : group.Key,
                Present = group.Count(item => item.Status == AttendanceStatuses.Present),
                Late = group.Count(item => item.Status == AttendanceStatuses.Late),
                Absent = group.Count(item => item.Status == AttendanceStatuses.Absent),
                Incomplete = group.Count(item => item.Status == AttendanceStatuses.Incomplete),
                AvgLate = group.Any() ? Math.Round(group.Average(item => item.LateMinutes), 2) : 0
            })
            .OrderByDescending(item => item.Present + item.Late + item.Absent + item.Incomplete)
            .ToList();

        var totalCount = grouped.Count;
        var rows = grouped
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new ReportRowDto
            {
                Id = item.DepartmentName,
                Values = new Dictionary<string, string>
                {
                    ["departmentName"] = item.DepartmentName,
                    ["present"] = item.Present.ToString(),
                    ["late"] = item.Late.ToString(),
                    ["absent"] = item.Absent.ToString(),
                    ["incomplete"] = item.Incomplete.ToString(),
                    ["avgLate"] = item.AvgLate.ToString("0.##")
                }
            })
            .ToList();

        return BuildReport(
            ReportKeys.AttendanceSummary,
            "Attendance Summary by Department",
            "Department-level summary of present, late, absent, and incomplete attendance results.",
            query,
            totalCount,
            [
                new ReportColumnDto { Key = "departmentName", Label = "Department", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "present", Label = "Present", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "late", Label = "Late", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "absent", Label = "Absent", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "incomplete", Label = "Incomplete", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "avgLate", Label = "Avg Late (min)", Alignment = ReportColumnAlignment.Right }
            ],
            rows,
            [
                Metric("departments", "Departments", totalCount),
                Metric("present", "Present", grouped.Sum(item => item.Present), "success"),
                Metric("late", "Late", grouped.Sum(item => item.Late), grouped.Any(item => item.Late > 0) ? "warning" : "default"),
                Metric("absent", "Absent", grouped.Sum(item => item.Absent), grouped.Any(item => item.Absent > 0) ? "danger" : "default")
            ]);
    }

    private async Task<ReportResultDto> BuildLeaveUsageReportAsync(PortalActorContext actor, ReportQueryDto query, CancellationToken cancellationToken)
    {
        var source = _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.LeaveType)
            .AsQueryable();

        source = ApplyLeaveScopeAndFilters(source, actor, query);

        var totalCount = await source.CountAsync(cancellationToken);
        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("start", false) => source.OrderBy(record => record.StartDate).ThenBy(record => record.Employee!.LastName),
            ("start", true) => source.OrderByDescending(record => record.StartDate).ThenBy(record => record.Employee!.LastName),
            ("days", false) => source.OrderBy(record => record.TotalLeaveDays).ThenBy(record => record.Employee!.LastName),
            ("days", true) => source.OrderByDescending(record => record.TotalLeaveDays).ThenBy(record => record.Employee!.LastName),
            _ => source.OrderByDescending(record => record.StartDate).ThenBy(record => record.Employee!.LastName)
        };

        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var rows = records.Select(record => new ReportRowDto
        {
            Id = record.Id.ToString(),
            LinkPath = actor.IsManager && !actor.IsAdministrator && !actor.IsHumanResources ? "/manager/leave" : "/admin/leave",
            Values = new Dictionary<string, string>
            {
                ["employeeCode"] = record.Employee?.EmployeeCode ?? string.Empty,
                ["employeeFullName"] = record.Employee == null ? string.Empty : BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
                ["departmentName"] = record.Employee?.Department?.Name ?? string.Empty,
                ["branchName"] = record.Employee?.Branch?.Name ?? string.Empty,
                ["leaveTypeName"] = record.LeaveType?.Name ?? string.Empty,
                ["paidFlag"] = record.LeaveType?.IsPaid == true ? "Paid" : "Unpaid",
                ["dateRange"] = $"{record.StartDate:yyyy-MM-dd} to {record.EndDate:yyyy-MM-dd}",
                ["days"] = record.TotalLeaveDays.ToString("0.##"),
                ["status"] = record.Status
            }
        }).ToList();

        var totals = await source
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalDays = group.Sum(item => item.TotalLeaveDays),
                PaidDays = group.Where(item => item.LeaveType != null && item.LeaveType.IsPaid).Sum(item => item.TotalLeaveDays),
                UnpaidDays = group.Where(item => item.LeaveType != null && !item.LeaveType.IsPaid).Sum(item => item.TotalLeaveDays),
                PendingCount = group.Count(item => item.Status == LeaveRequestStatuses.Pending)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return BuildReport(
            ReportKeys.LeaveUsage,
            "Leave Usage",
            "Leave requests by employee, type, status, and paid or unpaid classification.",
            query,
            totalCount,
            [
                new ReportColumnDto { Key = "employeeCode", Label = "Employee Code", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "employeeFullName", Label = "Employee", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "departmentName", Label = "Department", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "branchName", Label = "Branch", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "leaveTypeName", Label = "Leave Type", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "paidFlag", Label = "Paid / Unpaid", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "dateRange", Label = "Date Range", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "days", Label = "Days", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "status", Label = "Status", Alignment = ReportColumnAlignment.Left }
            ],
            rows,
            [
                Metric("totalDays", "Total days", totals?.TotalDays ?? 0),
                Metric("paidDays", "Paid days", totals?.PaidDays ?? 0, "success"),
                Metric("unpaidDays", "Unpaid days", totals?.UnpaidDays ?? 0, "warning"),
                Metric("pending", "Pending requests", totals?.PendingCount ?? 0, (totals?.PendingCount ?? 0) > 0 ? "warning" : "default")
            ]);
    }

    private async Task<ReportResultDto> BuildLeaveBalanceReportAsync(PortalActorContext actor, ReportQueryDto query, CancellationToken cancellationToken)
    {
        var targetYear = query.Year ?? _attendanceCalculationService.GetBusinessToday().Year;
        var source = _dbContext.EmployeeLeaveBalances
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.LeaveType)
            .Where(record => record.PeriodYear == targetYear)
            .AsQueryable();

        source = ApplyLeaveBalanceScopeAndFilters(source, actor, query);
        var totalCount = await source.CountAsync(cancellationToken);
        source = query.Descending
            ? source.OrderByDescending(record => record.AvailableBalance).ThenBy(record => record.Employee!.LastName)
            : source.OrderBy(record => record.Employee!.LastName).ThenBy(record => record.LeaveType!.Name);

        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var rows = records
            .Select(record => new ReportRowDto
            {
                Id = record.Id.ToString(),
                LinkPath = actor.IsManager && !actor.IsAdministrator && !actor.IsHumanResources ? "/manager/leave" : "/admin/leave",
                Values = new Dictionary<string, string>
                {
                    ["employeeCode"] = record.Employee!.EmployeeCode,
                    ["employeeFullName"] = BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
                    ["departmentName"] = record.Employee.Department != null ? record.Employee.Department.Name : string.Empty,
                    ["leaveTypeName"] = record.LeaveType!.Name,
                    ["opening"] = record.OpeningBalance.ToString("0.##"),
                    ["used"] = record.Used.ToString("0.##"),
                    ["pending"] = record.Pending.ToString("0.##"),
                    ["available"] = record.AvailableBalance.ToString("0.##")
                }
            })
            .ToList();

        var summary = await source
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Available = group.Sum(item => item.AvailableBalance),
                Low = group.Count(item => item.AvailableBalance >= 0m && item.AvailableBalance <= 2m),
                Negative = group.Count(item => item.AvailableBalance < 0m)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return BuildReport(
            ReportKeys.LeaveBalances,
            $"Leave Balances ({targetYear})",
            "Employee leave balance snapshots for the selected period year.",
            query,
            totalCount,
            [
                new ReportColumnDto { Key = "employeeCode", Label = "Employee Code", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "employeeFullName", Label = "Employee", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "departmentName", Label = "Department", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "leaveTypeName", Label = "Leave Type", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "opening", Label = "Opening", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "used", Label = "Used", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "pending", Label = "Pending", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "available", Label = "Available", Alignment = ReportColumnAlignment.Right }
            ],
            rows,
            [
                Metric("available", "Available balance", summary?.Available ?? 0),
                Metric("low", "Low balances", summary?.Low ?? 0, (summary?.Low ?? 0) > 0 ? "warning" : "default"),
                Metric("negative", "Negative balances", summary?.Negative ?? 0, (summary?.Negative ?? 0) > 0 ? "danger" : "default")
            ]);
    }

    private async Task<ReportResultDto> BuildPayrollRegisterReportAsync(PortalActorContext actor, ReportQueryDto query, CancellationToken cancellationToken)
    {
        _userAccessService.EnsureCanAccessPayrollReports(actor);

        var source = _dbContext.PayrollRunItems
            .AsNoTracking()
            .Include(record => record.PayrollRun)
                .ThenInclude(run => run!.PayPeriod)
            .Include(record => record.Employee)
            .AsQueryable();

        if (query.PayPeriodId is not null)
        {
            source = source.Where(record => record.PayrollRun != null && record.PayrollRun.PayPeriodId == query.PayPeriodId.Value);
        }

        if (query.PayrollRunId is not null)
        {
            source = source.Where(record => record.PayrollRunId == query.PayrollRunId.Value);
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim().ToLowerInvariant() || (record.PayrollRun != null && record.PayrollRun.Status == query.Status.Trim().ToLowerInvariant()));
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(record => record.PayrollRun != null && record.PayrollRun.PayPeriod != null && record.PayrollRun.PayPeriod.PayrollDate >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(record => record.PayrollRun != null && record.PayrollRun.PayPeriod != null && record.PayrollRun.PayPeriod.PayrollDate <= query.DateTo.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.BranchId == query.BranchId.Value);
        }

        if (query.EmploymentTypeId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.EmploymentTypeId == query.EmploymentTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.EmployeeCodeSnapshot.Contains(search) ||
                record.EmployeeNameSnapshot.Contains(search) ||
                record.DepartmentSnapshot.Contains(search) ||
                record.BranchSnapshot.Contains(search));
        }

        var totalCount = await source.CountAsync(cancellationToken);
        source = query.Descending
            ? source.OrderByDescending(record => record.NetPay).ThenBy(record => record.EmployeeNameSnapshot)
            : source.OrderBy(record => record.EmployeeNameSnapshot);

        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var rows = records
            .Select(record => new ReportRowDto
            {
                Id = record.Id.ToString(),
                LinkPath = $"/admin/payroll/payslips/{record.Id}",
                Values = new Dictionary<string, string>
                {
                    ["employeeCode"] = record.EmployeeCodeSnapshot,
                    ["employeeName"] = record.EmployeeNameSnapshot,
                    ["departmentName"] = record.DepartmentSnapshot,
                    ["branchName"] = record.BranchSnapshot,
                    ["status"] = record.Status,
                    ["grossPay"] = record.GrossPay.ToString("0.00"),
                    ["totalDeductions"] = record.TotalDeductions.ToString("0.00"),
                    ["netPay"] = record.NetPay.ToString("0.00"),
                    ["lateMinutes"] = record.LateMinutes.ToString(),
                    ["undertimeMinutes"] = record.UndertimeMinutes.ToString(),
                    ["overtimeMinutes"] = record.OvertimeMinutes.ToString()
                }
            })
            .ToList();

        var summary = await source
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Gross = group.Sum(item => item.GrossPay),
                Deductions = group.Sum(item => item.TotalDeductions),
                Net = group.Sum(item => item.NetPay),
                Held = group.Count(item => item.Status == PayrollItemStatuses.Held)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return BuildReport(
            ReportKeys.PayrollRegister,
            "Payroll Register",
            "Payroll snapshot data sourced from finalized payroll run items.",
            query,
            totalCount,
            [
                new ReportColumnDto { Key = "employeeCode", Label = "Employee Code", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "employeeName", Label = "Employee", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "departmentName", Label = "Department", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "branchName", Label = "Branch", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "status", Label = "Item Status", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "grossPay", Label = "Gross Pay", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "totalDeductions", Label = "Deductions", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "netPay", Label = "Net Pay", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "lateMinutes", Label = "Late", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "undertimeMinutes", Label = "Under", Alignment = ReportColumnAlignment.Right },
                new ReportColumnDto { Key = "overtimeMinutes", Label = "OT", Alignment = ReportColumnAlignment.Right }
            ],
            rows,
            [
                Metric("gross", "Gross pay", (summary?.Gross ?? 0m).ToString("0.00")),
                Metric("deductions", "Deductions", (summary?.Deductions ?? 0m).ToString("0.00")),
                Metric("net", "Net pay", (summary?.Net ?? 0m).ToString("0.00"), "success"),
                Metric("held", "Held items", summary?.Held ?? 0, (summary?.Held ?? 0) > 0 ? "warning" : "default")
            ]);
    }

    private async Task<ReportResultDto> BuildApprovalAgingReportAsync(PortalActorContext actor, ReportQueryDto query, CancellationToken cancellationToken)
    {
        var requests = new List<(string Type, Guid EmployeeId, string EmployeeCode, string EmployeeName, string DepartmentName, string BranchName, string Title, string Status, DateTime SubmittedAtUtc, DateTime LastUpdatedAtUtc, string CurrentApprover)>();

        var employeeScope = actor.IsAdministrator || actor.IsHumanResources || actor.IsPayrollOfficer
            ? BuildScopedEmployeeQuery(actor, includeInactive: true).Select(record => record.Id)
            : _dbContext.Employees.AsNoTracking().Where(record => actor.ManagedEmployeeIds.Contains(record.Id)).Select(record => record.Id);
        var employeeIds = await employeeScope.ToListAsync(cancellationToken);

        var leaveRequests = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.CurrentApproverUser)
            .Where(record => employeeIds.Contains(record.EmployeeId))
            .ToListAsync(cancellationToken);

        requests.AddRange(leaveRequests.Select(record => (
            ApprovableTypes.LeaveRequest,
            record.EmployeeId,
            record.Employee?.EmployeeCode ?? string.Empty,
            record.Employee == null ? string.Empty : BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
            record.Employee?.Department?.Name ?? string.Empty,
            record.Employee?.Branch?.Name ?? string.Empty,
            $"{record.LeaveTypeId}: {record.StartDate:yyyy-MM-dd} to {record.EndDate:yyyy-MM-dd}",
            record.Status,
            record.SubmittedAtUtc ?? record.CreatedAtUtc,
            record.UpdatedAtUtc ?? record.CreatedAtUtc,
            BuildUserDisplayName(record.CurrentApproverUser))));

        var attendanceRequests = await _dbContext.AttendanceAdjustmentRequests
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.CurrentApproverUser)
            .Where(record => employeeIds.Contains(record.EmployeeId))
            .ToListAsync(cancellationToken);

        requests.AddRange(attendanceRequests.Select(record => (
            ApprovableTypes.AttendanceAdjustmentRequest,
            record.EmployeeId,
            record.Employee?.EmployeeCode ?? string.Empty,
            record.Employee == null ? string.Empty : BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
            record.Employee?.Department?.Name ?? string.Empty,
            record.Employee?.Branch?.Name ?? string.Empty,
            $"{record.AttendanceDate:yyyy-MM-dd} attendance correction",
            record.Status,
            record.CreatedAtUtc,
            record.UpdatedAtUtc ?? record.CreatedAtUtc,
            BuildUserDisplayName(record.CurrentApproverUser))));

        var profileRequests = await _dbContext.EmployeeProfileChangeRequests
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Where(record => employeeIds.Contains(record.EmployeeId))
            .ToListAsync(cancellationToken);

        requests.AddRange(profileRequests.Select(record => (
            ApprovableTypes.ProfileChangeRequest,
            record.EmployeeId,
            record.Employee?.EmployeeCode ?? string.Empty,
            record.Employee == null ? string.Empty : BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
            record.Employee?.Department?.Name ?? string.Empty,
            record.Employee?.Branch?.Name ?? string.Empty,
            "Personal profile update",
            record.Status,
            record.CreatedAtUtc,
            record.UpdatedAtUtc ?? record.CreatedAtUtc,
            "HR Review")));

        if (actor.IsAdministrator || actor.IsPayrollOfficer)
        {
            var payrollAdjustments = await _dbContext.PayrollAdjustments
                .AsNoTracking()
                .Include(record => record.Employee)
                    .ThenInclude(employee => employee!.Department)
                .Include(record => record.Employee)
                    .ThenInclude(employee => employee!.Branch)
                .Include(record => record.ApprovedByUser)
                .Where(record => employeeIds.Contains(record.EmployeeId))
                .ToListAsync(cancellationToken);

            requests.AddRange(payrollAdjustments.Select(record => (
                "payroll_adjustment",
                record.EmployeeId,
                record.Employee?.EmployeeCode ?? string.Empty,
                record.Employee == null ? string.Empty : BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
                record.Employee?.Department?.Name ?? string.Empty,
                record.Employee?.Branch?.Name ?? string.Empty,
                $"Payroll adjustment {record.Amount:0.00}",
                record.Status,
                record.CreatedAtUtc,
                record.UpdatedAtUtc ?? record.CreatedAtUtc,
                BuildUserDisplayName(record.ApprovedByUser))));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            requests = requests.Where(record => string.Equals(record.Status, query.Status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            requests = requests.Where(record => string.Equals(record.Type, query.Source.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            requests = requests.Where(record =>
                record.EmployeeCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                record.EmployeeName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                record.Title.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (query.DateFrom is not null)
        {
            var fromUtc = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            requests = requests.Where(record => record.SubmittedAtUtc >= fromUtc).ToList();
        }

        if (query.DateTo is not null)
        {
            var toExclusiveUtc = query.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            requests = requests.Where(record => record.SubmittedAtUtc < toExclusiveUtc).ToList();
        }

        requests = query.Descending
            ? requests.OrderByDescending(record => (DateTime.UtcNow.Date - record.SubmittedAtUtc.Date).TotalDays).ThenBy(record => record.EmployeeName).ToList()
            : requests.OrderBy(record => record.SubmittedAtUtc).ThenBy(record => record.EmployeeName).ToList();

        var totalCount = requests.Count;
        var pageRows = requests
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(record => new ReportRowDto
            {
                Id = $"{record.Type}:{record.EmployeeId}:{record.SubmittedAtUtc:O}",
                LinkPath = "/approvals",
                Values = new Dictionary<string, string>
                {
                    ["requestType"] = ToTitle(record.Type.Replace('_', ' ')),
                    ["employeeCode"] = record.EmployeeCode,
                    ["employeeName"] = record.EmployeeName,
                    ["departmentName"] = record.DepartmentName,
                    ["branchName"] = record.BranchName,
                    ["title"] = record.Title,
                    ["status"] = record.Status,
                    ["currentApprover"] = record.CurrentApprover,
                    ["submittedAt"] = record.SubmittedAtUtc.ToString("yyyy-MM-dd HH:mm"),
                    ["agingDays"] = Math.Max(0, (DateTime.UtcNow.Date - record.SubmittedAtUtc.Date).Days).ToString()
                }
            })
            .ToList();

        return BuildReport(
            ReportKeys.ApprovalAging,
            "Approval Aging",
            "Unified view of request volume, status, and aging across approval workflows.",
            query,
            totalCount,
            [
                new ReportColumnDto { Key = "requestType", Label = "Request Type", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "employeeCode", Label = "Employee Code", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "employeeName", Label = "Employee", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "departmentName", Label = "Department", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "branchName", Label = "Branch", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "title", Label = "Request", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "status", Label = "Status", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "currentApprover", Label = "Current Approver", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "submittedAt", Label = "Submitted", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "agingDays", Label = "Aging (days)", Alignment = ReportColumnAlignment.Right }
            ],
            pageRows,
            [
                Metric("pending", "Pending", requests.Count(record => string.Equals(record.Status, RequestStatuses.Pending, StringComparison.OrdinalIgnoreCase) || string.Equals(record.Status, LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase)), requests.Any(record => string.Equals(record.Status, RequestStatuses.Pending, StringComparison.OrdinalIgnoreCase) || string.Equals(record.Status, LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase)) ? "warning" : "default"),
                Metric("approved", "Approved", requests.Count(record => string.Equals(record.Status, LeaveRequestStatuses.Approved, StringComparison.OrdinalIgnoreCase) || string.Equals(record.Status, PayrollAdjustmentStatuses.Approved, StringComparison.OrdinalIgnoreCase)), "success"),
                Metric("rejected", "Rejected", requests.Count(record => string.Equals(record.Status, LeaveRequestStatuses.Rejected, StringComparison.OrdinalIgnoreCase) || string.Equals(record.Status, RequestStatuses.Rejected, StringComparison.OrdinalIgnoreCase)), requests.Any(record => string.Equals(record.Status, LeaveRequestStatuses.Rejected, StringComparison.OrdinalIgnoreCase) || string.Equals(record.Status, RequestStatuses.Rejected, StringComparison.OrdinalIgnoreCase)) ? "danger" : "default")
            ]);
    }

    private async Task<ReportResultDto> BuildAuditActivityReportAsync(ReportQueryDto query, string? actorUserId, CancellationToken cancellationToken)
    {
        var logs = await _auditLogService.GetAuditLogsAsync(new AuditLogQueryDto
        {
            Search = query.Search,
            EntityType = query.EntityType,
            Action = query.Action,
            EmployeeId = query.EmployeeId,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            SortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "created" : query.SortBy,
            Descending = query.Descending
        }, actorUserId, cancellationToken);

        return BuildReport(
            ReportKeys.AuditActivity,
            "Audit Activity",
            "Central audit trail for sensitive employee, attendance, leave, document, payroll, and access changes.",
            query,
            logs.TotalCount,
            [
                new ReportColumnDto { Key = "createdAt", Label = "Created", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "actorName", Label = "Actor", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "action", Label = "Action", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "entityType", Label = "Entity", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "employeeCode", Label = "Employee Code", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "employeeFullName", Label = "Employee", Alignment = ReportColumnAlignment.Left },
                new ReportColumnDto { Key = "remarks", Label = "Remarks", Alignment = ReportColumnAlignment.Left }
            ],
            logs.Items.Select(item => new ReportRowDto
            {
                Id = item.Id.ToString(),
                LinkPath = $"/audit-logs?auditLogId={item.Id}",
                Values = new Dictionary<string, string>
                {
                    ["createdAt"] = item.CreatedAtUtc.ToString("yyyy-MM-dd HH:mm"),
                    ["actorName"] = item.ActorName,
                    ["action"] = item.Action,
                    ["entityType"] = item.EntityType,
                    ["employeeCode"] = item.EmployeeCode,
                    ["employeeFullName"] = item.EmployeeFullName,
                    ["remarks"] = item.Remarks
                }
            }).ToList(),
            [
                Metric("records", "Records", logs.TotalCount),
                Metric("withEmployee", "Employee-linked", logs.Items.Count(item => item.EmployeeId.HasValue)),
                Metric("dateRange", "Date filter", query.DateFrom.HasValue || query.DateTo.HasValue ? "Applied" : "All")
            ],
            pageNumberOverride: logs.PageNumber,
            pageSizeOverride: logs.PageSize,
            totalPagesOverride: logs.TotalPages);
    }

    private IQueryable<Employee> BuildScopedEmployeeQuery(PortalActorContext actor, bool includeInactive)
    {
        var source = _dbContext.Employees.AsNoTracking().AsQueryable();
        if (!includeInactive)
        {
            source = source.Where(record => record.IsActive);
        }

        if (actor.IsAdministrator || actor.IsHumanResources || actor.IsPayrollOfficer)
        {
            return source;
        }

        return source.Where(record => actor.ManagedEmployeeIds.Contains(record.Id));
    }

    private IQueryable<Employee> ApplyCommonEmployeeFilters(IQueryable<Employee> source, ReportQueryDto query, bool includeInactiveByDefault)
    {
        if (query.IncludeInactive != true && !includeInactiveByDefault)
        {
            source = source.Where(record => record.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.EmployeeCode.Contains(search) ||
                record.FirstName.Contains(search) ||
                record.MiddleName.Contains(search) ||
                record.LastName.Contains(search) ||
                record.Email.Contains(search));
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.Id == query.EmployeeId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.BranchId == query.BranchId.Value);
        }

        if (query.EmploymentTypeId is not null)
        {
            source = source.Where(record => record.EmploymentTypeId == query.EmploymentTypeId.Value);
        }

        if (query.EmploymentStatusId is not null)
        {
            source = source.Where(record => record.EmploymentStatusId == query.EmploymentStatusId.Value);
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(record => record.DateHired != null && record.DateHired >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(record => record.DateHired != null && record.DateHired <= query.DateTo.Value);
        }

        return source;
    }

    private IQueryable<Employee> ApplyEmployeeSorting(IQueryable<Employee> source, string sortBy, bool descending)
    {
        return (sortBy.Trim().ToLowerInvariant(), descending) switch
        {
            ("code", true) => source.OrderByDescending(record => record.EmployeeCode),
            ("code", false) => source.OrderBy(record => record.EmployeeCode),
            ("department", true) => source.OrderByDescending(record => record.Department != null ? record.Department.Name : string.Empty).ThenBy(record => record.LastName),
            ("department", false) => source.OrderBy(record => record.Department != null ? record.Department.Name : string.Empty).ThenBy(record => record.LastName),
            ("hired", true) => source.OrderByDescending(record => record.DateHired).ThenBy(record => record.LastName),
            ("hired", false) => source.OrderBy(record => record.DateHired).ThenBy(record => record.LastName),
            (_, true) => source.OrderByDescending(record => record.LastName).ThenByDescending(record => record.FirstName),
            _ => source.OrderBy(record => record.LastName).ThenBy(record => record.FirstName)
        };
    }

    private IQueryable<LeaveRequest> ApplyLeaveScopeAndFilters(IQueryable<LeaveRequest> source, PortalActorContext actor, ReportQueryDto query)
    {
        if (!(actor.IsAdministrator || actor.IsHumanResources || actor.IsPayrollOfficer))
        {
            source = source.Where(record => actor.ManagedEmployeeIds.Contains(record.EmployeeId));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Employee!.EmployeeCode.Contains(search) ||
                record.Employee.FirstName.Contains(search) ||
                record.Employee.MiddleName.Contains(search) ||
                record.Employee.LastName.Contains(search) ||
                record.LeaveType!.Name.Contains(search));
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee!.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.Employee!.BranchId == query.BranchId.Value);
        }

        if (query.LeaveTypeId is not null)
        {
            source = source.Where(record => record.LeaveTypeId == query.LeaveTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim());
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(record => record.StartDate >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(record => record.EndDate <= query.DateTo.Value);
        }

        return source;
    }

    private IQueryable<EmployeeLeaveBalance> ApplyLeaveBalanceScopeAndFilters(IQueryable<EmployeeLeaveBalance> source, PortalActorContext actor, ReportQueryDto query)
    {
        if (!(actor.IsAdministrator || actor.IsHumanResources || actor.IsPayrollOfficer))
        {
            source = source.Where(record => actor.ManagedEmployeeIds.Contains(record.EmployeeId));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Employee!.EmployeeCode.Contains(search) ||
                record.Employee.FirstName.Contains(search) ||
                record.Employee.MiddleName.Contains(search) ||
                record.Employee.LastName.Contains(search) ||
                record.LeaveType!.Name.Contains(search));
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee!.DepartmentId == query.DepartmentId.Value);
        }

        if (query.LeaveTypeId is not null)
        {
            source = source.Where(record => record.LeaveTypeId == query.LeaveTypeId.Value);
        }

        return source;
    }

    private async Task ClearDefaultSavedReportsAsync(string userId, string reportKey, CancellationToken cancellationToken)
    {
        var defaults = await _dbContext.SavedReports
            .Where(record => record.UserId == userId && record.ReportKey == reportKey && record.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var item in defaults)
        {
            item.IsDefault = false;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private static (DateOnly DateFrom, DateOnly DateTo) NormalizeDateRange(DateOnly? from, DateOnly? to, DateOnly fallbackDate)
    {
        var dateFrom = from ?? fallbackDate;
        var dateTo = to ?? dateFrom;
        return dateTo < dateFrom ? (dateTo, dateFrom) : (dateFrom, dateTo);
    }

    private static IReadOnlyList<ReportDefinitionDto> GetDefinitions()
    {
        return
        [
            new ReportDefinitionDto
            {
                Key = ReportKeys.EmployeeMasterList,
                Name = "Employee Master List",
                Category = ReportCategories.Employee,
                Description = "Employee roster with organization and contact details.",
                RoutePath = $"/reports/{ReportKeys.EmployeeMasterList}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.HumanResources, SystemRoles.Manager],
                Filters =
                [
                    ReportFilterKeys.Search,
                    ReportFilterKeys.Department,
                    ReportFilterKeys.Branch,
                    ReportFilterKeys.EmploymentType,
                    ReportFilterKeys.EmploymentStatus,
                    ReportFilterKeys.DateFrom,
                    ReportFilterKeys.DateTo,
                    ReportFilterKeys.IncludeInactive
                ],
                SupportsExport = true,
                SupportsSavedViews = true
            },
            new ReportDefinitionDto
            {
                Key = ReportKeys.EmployeeProfileCompleteness,
                Name = "Employee Profile Completeness",
                Category = ReportCategories.Employee,
                Description = "Employees with missing master-profile details.",
                RoutePath = $"/reports/{ReportKeys.EmployeeProfileCompleteness}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.HumanResources, SystemRoles.Manager],
                Filters =
                [
                    ReportFilterKeys.Search,
                    ReportFilterKeys.Department,
                    ReportFilterKeys.Branch,
                    ReportFilterKeys.IncludeInactive
                ],
                SupportsExport = true,
                SupportsSavedViews = true
            },
            new ReportDefinitionDto
            {
                Key = ReportKeys.DepartmentHeadcount,
                Name = "Department Headcount",
                Category = ReportCategories.Organization,
                Description = "Headcount by department.",
                RoutePath = $"/reports/{ReportKeys.DepartmentHeadcount}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.HumanResources, SystemRoles.Manager],
                Filters = [ReportFilterKeys.Branch, ReportFilterKeys.EmploymentType, ReportFilterKeys.IncludeInactive],
                SupportsExport = true,
                SupportsSavedViews = true
            },
            new ReportDefinitionDto
            {
                Key = ReportKeys.BranchHeadcount,
                Name = "Branch Headcount",
                Category = ReportCategories.Organization,
                Description = "Headcount by branch or location.",
                RoutePath = $"/reports/{ReportKeys.BranchHeadcount}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.HumanResources, SystemRoles.Manager],
                Filters = [ReportFilterKeys.Department, ReportFilterKeys.EmploymentType, ReportFilterKeys.IncludeInactive],
                SupportsExport = true,
                SupportsSavedViews = true
            },
            new ReportDefinitionDto
            {
                Key = ReportKeys.DocumentComplianceIssues,
                Name = "Document Compliance Issues",
                Category = ReportCategories.DocumentCompliance,
                Description = "Missing, expired, and expiring employee documents.",
                RoutePath = $"/reports/{ReportKeys.DocumentComplianceIssues}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.HumanResources, SystemRoles.Manager],
                Filters =
                [
                    ReportFilterKeys.Search,
                    ReportFilterKeys.Employee,
                    ReportFilterKeys.Department,
                    ReportFilterKeys.Branch,
                    ReportFilterKeys.IssueType,
                    ReportFilterKeys.Severity
                ],
                SupportsExport = true,
                SupportsSavedViews = true
            },
            new ReportDefinitionDto
            {
                Key = ReportKeys.AttendanceDaily,
                Name = "Daily Attendance",
                Category = ReportCategories.Attendance,
                Description = "Attendance detail with scheduled and actual times.",
                RoutePath = $"/reports/{ReportKeys.AttendanceDaily}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.HumanResources, SystemRoles.Manager],
                Filters =
                [
                    ReportFilterKeys.Search,
                    ReportFilterKeys.Employee,
                    ReportFilterKeys.Department,
                    ReportFilterKeys.Branch,
                    ReportFilterKeys.Status,
                    ReportFilterKeys.Source,
                    ReportFilterKeys.DateFrom,
                    ReportFilterKeys.DateTo
                ],
                SupportsExport = true,
                SupportsSavedViews = true
            },
            new ReportDefinitionDto
            {
                Key = ReportKeys.AttendanceSummary,
                Name = "Attendance Summary",
                Category = ReportCategories.Attendance,
                Description = "Department-level attendance trend snapshot.",
                RoutePath = $"/reports/{ReportKeys.AttendanceSummary}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.HumanResources, SystemRoles.Manager],
                Filters =
                [
                    ReportFilterKeys.Department,
                    ReportFilterKeys.Branch,
                    ReportFilterKeys.Status,
                    ReportFilterKeys.Source,
                    ReportFilterKeys.DateFrom,
                    ReportFilterKeys.DateTo
                ],
                SupportsExport = true,
                SupportsSavedViews = true
            },
            new ReportDefinitionDto
            {
                Key = ReportKeys.LeaveUsage,
                Name = "Leave Usage",
                Category = ReportCategories.Leave,
                Description = "Leave requests by employee, type, and status.",
                RoutePath = $"/reports/{ReportKeys.LeaveUsage}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.HumanResources, SystemRoles.Manager],
                Filters =
                [
                    ReportFilterKeys.Search,
                    ReportFilterKeys.Employee,
                    ReportFilterKeys.Department,
                    ReportFilterKeys.Branch,
                    ReportFilterKeys.LeaveType,
                    ReportFilterKeys.Status,
                    ReportFilterKeys.DateFrom,
                    ReportFilterKeys.DateTo
                ],
                SupportsExport = true,
                SupportsSavedViews = true
            },
            new ReportDefinitionDto
            {
                Key = ReportKeys.LeaveBalances,
                Name = "Leave Balances",
                Category = ReportCategories.Leave,
                Description = "Leave balance snapshot per employee and leave type.",
                RoutePath = $"/reports/{ReportKeys.LeaveBalances}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.HumanResources, SystemRoles.Manager],
                Filters =
                [
                    ReportFilterKeys.Search,
                    ReportFilterKeys.Employee,
                    ReportFilterKeys.Department,
                    ReportFilterKeys.LeaveType,
                    ReportFilterKeys.Year
                ],
                SupportsExport = true,
                SupportsSavedViews = true
            },
            new ReportDefinitionDto
            {
                Key = ReportKeys.PayrollRegister,
                Name = "Payroll Register",
                Category = ReportCategories.Payroll,
                Description = "Payroll snapshot register from payroll run items.",
                RoutePath = $"/reports/{ReportKeys.PayrollRegister}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.PayrollOfficer],
                Filters =
                [
                    ReportFilterKeys.Search,
                    ReportFilterKeys.Employee,
                    ReportFilterKeys.Department,
                    ReportFilterKeys.Branch,
                    ReportFilterKeys.EmploymentType,
                    ReportFilterKeys.Status,
                    ReportFilterKeys.PayPeriod,
                    ReportFilterKeys.PayrollRun,
                    ReportFilterKeys.DateFrom,
                    ReportFilterKeys.DateTo
                ],
                SupportsExport = true,
                SupportsSavedViews = true
            },
            new ReportDefinitionDto
            {
                Key = ReportKeys.ApprovalAging,
                Name = "Approval Aging",
                Category = ReportCategories.Approval,
                Description = "Pending and completed request approvals with aging.",
                RoutePath = $"/reports/{ReportKeys.ApprovalAging}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.HumanResources, SystemRoles.Manager, SystemRoles.PayrollOfficer],
                Filters =
                [
                    ReportFilterKeys.Search,
                    ReportFilterKeys.Status,
                    ReportFilterKeys.Source,
                    ReportFilterKeys.DateFrom,
                    ReportFilterKeys.DateTo
                ],
                SupportsExport = true,
                SupportsSavedViews = true
            },
            new ReportDefinitionDto
            {
                Key = ReportKeys.AuditActivity,
                Name = "Audit Activity",
                Category = ReportCategories.Audit,
                Description = "Central audit trail for sensitive actions.",
                RoutePath = $"/reports/{ReportKeys.AuditActivity}",
                AllowedRoles = [SystemRoles.Administrator, SystemRoles.HumanResources, SystemRoles.PayrollOfficer],
                Filters =
                [
                    ReportFilterKeys.Search,
                    ReportFilterKeys.Employee,
                    ReportFilterKeys.EntityType,
                    ReportFilterKeys.Action,
                    ReportFilterKeys.DateFrom,
                    ReportFilterKeys.DateTo
                ],
                SupportsExport = true,
                SupportsSavedViews = true
            }
        ];
    }

    private bool CanRunReport(PortalActorContext actor, string reportKey)
    {
        return reportKey switch
        {
            ReportKeys.PayrollRegister => actor.IsAdministrator || actor.IsPayrollOfficer,
            ReportKeys.AuditActivity => actor.IsAdministrator || actor.IsHumanResources || actor.IsPayrollOfficer,
            _ => actor.IsAdministrator || actor.IsHumanResources || actor.IsManager || actor.IsPayrollOfficer
        };
    }

    private void EnsureCanRunReport(PortalActorContext actor, string reportKey)
    {
        if (!CanRunReport(actor, reportKey))
        {
            throw new ForbiddenApiException("You do not have permission to run this report.");
        }
    }

    private static string BuildFullName(string firstName, string middleName, string lastName, string suffix)
    {
        return string.Join(" ", new[] { firstName.Trim(), middleName.Trim(), lastName.Trim(), suffix.Trim() }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string BuildScheduleLabel(DateTime? start, DateTime? end)
    {
        if (start is null && end is null)
        {
            return string.Empty;
        }

        return $"{start:HH:mm} - {end:HH:mm}".Trim();
    }

    private static ReportMetricDto Metric(string key, string label, object value, string tone = "default")
    {
        return new ReportMetricDto
        {
            Key = key,
            Label = label,
            Value = value.ToString() ?? string.Empty,
            Tone = tone
        };
    }

    private static ReportResultDto BuildReport(
        string reportKey,
        string title,
        string description,
        ReportQueryDto query,
        int totalCount,
        IReadOnlyList<ReportColumnDto> columns,
        IReadOnlyList<ReportRowDto> rows,
        IReadOnlyList<ReportMetricDto> metrics,
        int? pageNumberOverride = null,
        int? pageSizeOverride = null,
        int? totalPagesOverride = null)
    {
        var pageNumber = pageNumberOverride ?? query.PageNumber;
        var pageSize = pageSizeOverride ?? query.PageSize;
        var totalPages = totalPagesOverride ?? (totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));

        return new ReportResultDto
        {
            ReportKey = reportKey,
            Title = title,
            Description = description,
            GeneratedAtUtc = DateTime.UtcNow,
            Columns = columns,
            Rows = rows,
            Metrics = metrics,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private static IReadOnlyList<ReportColumnDto> GetEmployeeMasterColumns()
    {
        return
        [
            new ReportColumnDto { Key = "employeeCode", Label = "Employee Code", Alignment = ReportColumnAlignment.Left },
            new ReportColumnDto { Key = "fullName", Label = "Employee", Alignment = ReportColumnAlignment.Left },
            new ReportColumnDto { Key = "departmentName", Label = "Department", Alignment = ReportColumnAlignment.Left },
            new ReportColumnDto { Key = "positionName", Label = "Position", Alignment = ReportColumnAlignment.Left },
            new ReportColumnDto { Key = "branchName", Label = "Branch", Alignment = ReportColumnAlignment.Left },
            new ReportColumnDto { Key = "employmentTypeName", Label = "Employment Type", Alignment = ReportColumnAlignment.Left },
            new ReportColumnDto { Key = "employmentStatusName", Label = "Employment Status", Alignment = ReportColumnAlignment.Left },
            new ReportColumnDto { Key = "dateHired", Label = "Date Hired", Alignment = ReportColumnAlignment.Left },
            new ReportColumnDto { Key = "managerName", Label = "Manager", Alignment = ReportColumnAlignment.Left },
            new ReportColumnDto { Key = "email", Label = "Email", Alignment = ReportColumnAlignment.Left },
            new ReportColumnDto { Key = "mobileNumber", Label = "Mobile", Alignment = ReportColumnAlignment.Left },
            new ReportColumnDto { Key = "active", Label = "Lifecycle", Alignment = ReportColumnAlignment.Left }
        ];
    }

    private static int CalculateProfileCompletion(Employee record)
    {
        var values = new[]
        {
            record.MobileNumber,
            record.Email,
            record.Address,
            record.CityProvince,
            record.PostalCode,
            record.EmergencyContactName,
            record.EmergencyContactPhone,
            record.CivilStatus,
            record.Nationality,
            record.SssNumber,
            record.PhilHealthNumber,
            record.PagIbigNumber,
            record.TinNumber
        };

        var completed = values.Count(value => !string.IsNullOrWhiteSpace(value));
        return (int)Math.Round((completed / (double)values.Length) * 100);
    }

    private static IReadOnlyList<string> GetMissingProfileFields(Employee record)
    {
        var missing = new List<string>();
        AddIfMissing(missing, "Mobile number", record.MobileNumber);
        AddIfMissing(missing, "Email", record.Email);
        AddIfMissing(missing, "Address", record.Address);
        AddIfMissing(missing, "City / Province", record.CityProvince);
        AddIfMissing(missing, "ZIP / Postal code", record.PostalCode);
        AddIfMissing(missing, "Emergency contact name", record.EmergencyContactName);
        AddIfMissing(missing, "Emergency contact phone", record.EmergencyContactPhone);
        AddIfMissing(missing, "Civil status", record.CivilStatus);
        AddIfMissing(missing, "Nationality", record.Nationality);
        AddIfMissing(missing, "SSS", record.SssNumber);
        AddIfMissing(missing, "PhilHealth", record.PhilHealthNumber);
        AddIfMissing(missing, "Pag-IBIG", record.PagIbigNumber);
        AddIfMissing(missing, "TIN", record.TinNumber);
        return missing;
    }

    private static void AddIfMissing(ICollection<string> items, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            items.Add(label);
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string ToTitle(string value)
    {
        return string.Join(" ", value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Length == 0 ? string.Empty : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
    }

    private static string BuildUserDisplayName(ApplicationUser? user)
    {
        return user is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.Email ?? string.Empty
                : user.DisplayName;
    }
}
