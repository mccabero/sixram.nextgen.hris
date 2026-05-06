using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Portal;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IApprovalCenterService
{
    Task<ApprovalCenterSummaryDto> GetSummaryAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<ApprovalCenterOptionsDto> GetOptionsAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ApprovalCenterInboxItemDto>> GetInboxAsync(ApprovalCenterQueryDto query, string? actorUserId, CancellationToken cancellationToken = default);
}

public class ApprovalCenterService : IApprovalCenterService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserAccessService _userAccessService;

    public ApprovalCenterService(ApplicationDbContext dbContext, IUserAccessService userAccessService)
    {
        _dbContext = dbContext;
        _userAccessService = userAccessService;
    }

    public async Task<ApprovalCenterSummaryDto> GetSummaryAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        if (!(actor.IsAdministrator || actor.IsHumanResources || actor.IsManager || actor.IsPayrollOfficer))
        {
            throw new ForbiddenApiException("You do not have permission to access the approval center.");
        }

        var pendingLeaveRequestCount = await BuildLeaveQuery(actor)
            .CountAsync(record => record.Status == LeaveRequestStatuses.Pending, cancellationToken);

        var pendingAttendanceAdjustmentRequestCount = await BuildAttendanceAdjustmentQuery(actor)
            .CountAsync(record => record.Status == RequestStatuses.Pending, cancellationToken);

        var pendingProfileChangeRequestCount = actor.IsAdministrator || actor.IsHumanResources
            ? await _dbContext.EmployeeProfileChangeRequests.CountAsync(record => record.Status == RequestStatuses.Pending, cancellationToken)
            : 0;

        var pendingPayrollAdjustmentCount = actor.IsAdministrator || actor.IsPayrollOfficer
            ? await _dbContext.PayrollAdjustments.CountAsync(record => record.Status == PayrollAdjustmentStatuses.Pending, cancellationToken)
            : 0;

        return new ApprovalCenterSummaryDto
        {
            PendingLeaveRequestCount = pendingLeaveRequestCount,
            PendingAttendanceAdjustmentRequestCount = pendingAttendanceAdjustmentRequestCount,
            PendingProfileChangeRequestCount = pendingProfileChangeRequestCount,
            PendingPayrollAdjustmentCount = pendingPayrollAdjustmentCount,
            TotalPendingCount = pendingLeaveRequestCount + pendingAttendanceAdjustmentRequestCount + pendingProfileChangeRequestCount + pendingPayrollAdjustmentCount
        };
    }

    public async Task<ApprovalCenterOptionsDto> GetOptionsAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        if (!(actor.IsAdministrator || actor.IsHumanResources || actor.IsManager || actor.IsPayrollOfficer))
        {
            throw new ForbiddenApiException("You do not have permission to access the approval center.");
        }

        IQueryable<Employee> employeeSource = _dbContext.Employees
            .AsNoTracking()
            .Include(record => record.Department)
            .Include(record => record.Branch);

        if (actor.IsManager && !(actor.IsAdministrator || actor.IsHumanResources))
        {
            employeeSource = employeeSource.Where(record => actor.ManagedEmployeeIds.Contains(record.Id));
        }

        var employees = await employeeSource.ToListAsync(cancellationToken);

        return new ApprovalCenterOptionsDto
        {
            Departments = employees
                .Where(record => record.DepartmentId != null && record.Department != null)
                .GroupBy(record => new { record.DepartmentId, record.Department!.Code, record.Department.Name, record.Department.IsActive })
                .Select(group => new LookupOptionDto
                {
                    Id = group.Key.DepartmentId!.Value,
                    Code = group.Key.Code,
                    Name = group.Key.Name,
                    IsActive = group.Key.IsActive
                })
                .OrderBy(record => record.Name)
                .ToList(),
            Branches = employees
                .Where(record => record.BranchId != null && record.Branch != null)
                .GroupBy(record => new { record.BranchId, record.Branch!.Code, record.Branch.Name, record.Branch.IsActive })
                .Select(group => new LookupOptionDto
                {
                    Id = group.Key.BranchId!.Value,
                    Code = group.Key.Code,
                    Name = group.Key.Name,
                    IsActive = group.Key.IsActive
                })
                .OrderBy(record => record.Name)
                .ToList()
        };
    }

    public async Task<PagedResultDto<ApprovalCenterInboxItemDto>> GetInboxAsync(ApprovalCenterQueryDto query, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        if (!(actor.IsAdministrator || actor.IsHumanResources || actor.IsManager || actor.IsPayrollOfficer))
        {
            throw new ForbiddenApiException("You do not have permission to access the approval center.");
        }

        var requestedType = query.Type.Trim().ToLowerInvariant();
        var items = new List<ApprovalCenterInboxItemDto>();

        if (string.IsNullOrWhiteSpace(requestedType) || requestedType == ApprovableTypes.LeaveRequest)
        {
            items.AddRange(await BuildLeaveItemsAsync(BuildLeaveQuery(actor), query, cancellationToken));
        }

        if (string.IsNullOrWhiteSpace(requestedType) || requestedType == ApprovableTypes.AttendanceAdjustmentRequest)
        {
            items.AddRange(await BuildAttendanceAdjustmentItemsAsync(BuildAttendanceAdjustmentQuery(actor), query, cancellationToken));
        }

        if ((string.IsNullOrWhiteSpace(requestedType) || requestedType == ApprovableTypes.ProfileChangeRequest) &&
            (actor.IsAdministrator || actor.IsHumanResources))
        {
            items.AddRange(await BuildProfileChangeItemsAsync(query, cancellationToken));
        }

        if ((string.IsNullOrWhiteSpace(requestedType) || requestedType == ApprovableTypes.PayrollAdjustment) &&
            (actor.IsAdministrator || actor.IsPayrollOfficer))
        {
            items.AddRange(await BuildPayrollAdjustmentItemsAsync(query, cancellationToken));
        }

        items = ApplySearch(items, query.Search);
        items = ApplySorting(items, query.SortBy, query.Descending);
        var totalCount = items.Count;
        var pagedItems = items
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedResultDto<ApprovalCenterInboxItemDto>
        {
            Items = pagedItems,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    private IQueryable<LeaveRequest> BuildLeaveQuery(PortalActorContext actor)
    {
        var source = _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.CurrentApproverUser)
            .AsQueryable();

        if (actor.IsAdministrator || actor.IsHumanResources)
        {
            return source;
        }

        if (actor.IsManager)
        {
            return source.Where(record => actor.ManagedEmployeeIds.Contains(record.EmployeeId) && record.CurrentApproverUserId == actor.UserId);
        }

        return source.Where(record => false);
    }

    private IQueryable<AttendanceAdjustmentRequest> BuildAttendanceAdjustmentQuery(PortalActorContext actor)
    {
        var source = _dbContext.AttendanceAdjustmentRequests
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.CurrentApproverUser)
            .AsQueryable();

        if (actor.IsAdministrator || actor.IsHumanResources)
        {
            return source;
        }

        if (actor.IsManager)
        {
            return source.Where(record => actor.ManagedEmployeeIds.Contains(record.EmployeeId) && record.CurrentApproverUserId == actor.UserId);
        }

        return source.Where(record => false);
    }

    private async Task<IReadOnlyList<ApprovalCenterInboxItemDto>> BuildLeaveItemsAsync(
        IQueryable<LeaveRequest> source,
        ApprovalCenterQueryDto query,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim());
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee!.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.Employee!.BranchId == query.BranchId.Value);
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(record => record.EndDate >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(record => record.StartDate <= query.DateTo.Value);
        }

        return await source
            .Select(record => new ApprovalCenterInboxItemDto
            {
                ApprovalType = ApprovableTypes.LeaveRequest,
                ApprovalTypeLabel = "Leave request",
                RequestId = record.Id.ToString(),
                EmployeeId = record.EmployeeId,
                EmployeeCode = record.Employee != null ? record.Employee.EmployeeCode : string.Empty,
                EmployeeFullName = record.Employee != null ? BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix) : string.Empty,
                DepartmentName = record.Employee != null && record.Employee.Department != null ? record.Employee.Department.Name : string.Empty,
                BranchName = record.Employee != null && record.Employee.Branch != null ? record.Employee.Branch.Name : string.Empty,
                Title = $"{record.StartDate:yyyy-MM-dd} to {record.EndDate:yyyy-MM-dd}",
                Subtitle = record.Reason,
                Status = record.Status,
                CurrentApproverDisplayName = BuildUserDisplayName(record.CurrentApproverUser),
                SubmittedAtUtc = record.SubmittedAtUtc ?? record.CreatedAtUtc,
                LastUpdatedAtUtc = record.UpdatedAtUtc ?? record.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ApprovalCenterInboxItemDto>> BuildAttendanceAdjustmentItemsAsync(
        IQueryable<AttendanceAdjustmentRequest> source,
        ApprovalCenterQueryDto query,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim().ToLowerInvariant());
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee!.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.Employee!.BranchId == query.BranchId.Value);
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(record => record.AttendanceDate >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(record => record.AttendanceDate <= query.DateTo.Value);
        }

        return await source
            .Select(record => new ApprovalCenterInboxItemDto
            {
                ApprovalType = ApprovableTypes.AttendanceAdjustmentRequest,
                ApprovalTypeLabel = "Attendance correction",
                RequestId = record.Id.ToString(),
                EmployeeId = record.EmployeeId,
                EmployeeCode = record.Employee != null ? record.Employee.EmployeeCode : string.Empty,
                EmployeeFullName = record.Employee != null ? BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix) : string.Empty,
                DepartmentName = record.Employee != null && record.Employee.Department != null ? record.Employee.Department.Name : string.Empty,
                BranchName = record.Employee != null && record.Employee.Branch != null ? record.Employee.Branch.Name : string.Empty,
                Title = record.AttendanceDate.ToString("yyyy-MM-dd"),
                Subtitle = record.Reason,
                Status = record.Status,
                CurrentApproverDisplayName = BuildUserDisplayName(record.CurrentApproverUser),
                SubmittedAtUtc = record.CreatedAtUtc,
                LastUpdatedAtUtc = record.UpdatedAtUtc ?? record.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ApprovalCenterInboxItemDto>> BuildProfileChangeItemsAsync(ApprovalCenterQueryDto query, CancellationToken cancellationToken)
    {
        var source = _dbContext.EmployeeProfileChangeRequests
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.ReviewedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim().ToLowerInvariant());
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee!.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.Employee!.BranchId == query.BranchId.Value);
        }

        if (query.DateFrom is not null)
        {
            var fromUtc = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            source = source.Where(record => record.CreatedAtUtc >= fromUtc);
        }

        if (query.DateTo is not null)
        {
            var toExclusiveUtc = query.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            source = source.Where(record => record.CreatedAtUtc < toExclusiveUtc);
        }

        return await source
            .Select(record => new ApprovalCenterInboxItemDto
            {
                ApprovalType = ApprovableTypes.ProfileChangeRequest,
                ApprovalTypeLabel = "Profile change",
                RequestId = record.Id.ToString(),
                EmployeeId = record.EmployeeId,
                EmployeeCode = record.Employee != null ? record.Employee.EmployeeCode : string.Empty,
                EmployeeFullName = record.Employee != null ? BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix) : string.Empty,
                DepartmentName = record.Employee != null && record.Employee.Department != null ? record.Employee.Department.Name : string.Empty,
                BranchName = record.Employee != null && record.Employee.Branch != null ? record.Employee.Branch.Name : string.Empty,
                Title = "Personal profile update",
                Subtitle = record.Reason,
                Status = record.Status,
                CurrentApproverDisplayName = record.Status == RequestStatuses.Pending ? "HR Review" : BuildUserDisplayName(record.ReviewedByUser),
                SubmittedAtUtc = record.CreatedAtUtc,
                LastUpdatedAtUtc = record.UpdatedAtUtc ?? record.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ApprovalCenterInboxItemDto>> BuildPayrollAdjustmentItemsAsync(ApprovalCenterQueryDto query, CancellationToken cancellationToken)
    {
        var source = _dbContext.PayrollAdjustments
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim().ToLowerInvariant());
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee!.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.Employee!.BranchId == query.BranchId.Value);
        }

        if (query.DateFrom is not null)
        {
            var fromUtc = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            source = source.Where(record => record.CreatedAtUtc >= fromUtc);
        }

        if (query.DateTo is not null)
        {
            var toExclusiveUtc = query.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            source = source.Where(record => record.CreatedAtUtc < toExclusiveUtc);
        }

        return await source
            .Select(record => new ApprovalCenterInboxItemDto
            {
                ApprovalType = ApprovableTypes.PayrollAdjustment,
                ApprovalTypeLabel = "Payroll adjustment",
                RequestId = record.Id.ToString(),
                EmployeeId = record.EmployeeId,
                EmployeeCode = record.Employee != null ? record.Employee.EmployeeCode : string.Empty,
                EmployeeFullName = record.Employee != null ? BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix) : string.Empty,
                DepartmentName = record.Employee != null && record.Employee.Department != null ? record.Employee.Department.Name : string.Empty,
                BranchName = record.Employee != null && record.Employee.Branch != null ? record.Employee.Branch.Name : string.Empty,
                Title = $"{record.AdjustmentType}: {record.Amount:0.00}",
                Subtitle = record.Reason,
                Status = record.Status,
                CurrentApproverDisplayName = string.Empty,
                SubmittedAtUtc = record.CreatedAtUtc,
                LastUpdatedAtUtc = record.UpdatedAtUtc ?? record.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    private static List<ApprovalCenterInboxItemDto> ApplySearch(IEnumerable<ApprovalCenterInboxItemDto> items, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return items.ToList();
        }

        var normalizedSearch = search.Trim();
        return items
            .Where(item =>
                item.EmployeeCode.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                item.EmployeeFullName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                item.Title.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                item.Subtitle.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static List<ApprovalCenterInboxItemDto> ApplySorting(IEnumerable<ApprovalCenterInboxItemDto> items, string sortBy, bool descending)
    {
        return (sortBy.Trim().ToLowerInvariant(), descending) switch
        {
            ("employee", false) => items.OrderBy(item => item.EmployeeFullName).ThenByDescending(item => item.SubmittedAtUtc).ToList(),
            ("employee", true) => items.OrderByDescending(item => item.EmployeeFullName).ThenByDescending(item => item.SubmittedAtUtc).ToList(),
            ("status", false) => items.OrderBy(item => item.Status).ThenByDescending(item => item.SubmittedAtUtc).ToList(),
            ("status", true) => items.OrderByDescending(item => item.Status).ThenByDescending(item => item.SubmittedAtUtc).ToList(),
            ("type", false) => items.OrderBy(item => item.ApprovalTypeLabel).ThenByDescending(item => item.SubmittedAtUtc).ToList(),
            ("type", true) => items.OrderByDescending(item => item.ApprovalTypeLabel).ThenByDescending(item => item.SubmittedAtUtc).ToList(),
            (_, false) => items.OrderBy(item => item.SubmittedAtUtc).ToList(),
            _ => items.OrderByDescending(item => item.SubmittedAtUtc).ToList()
        };
    }

    private static string BuildUserDisplayName(ApplicationUser? user)
    {
        return user is null
            ? string.Empty
            : !string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.DisplayName
                : user.Email ?? string.Empty;
    }

    private static string BuildFullName(string firstName, string middleName, string lastName, string suffix)
    {
        var parts = new[] { firstName, middleName, lastName, suffix }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim());

        return string.Join(" ", parts);
    }
}
