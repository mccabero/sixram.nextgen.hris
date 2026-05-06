using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.DTOs.Payroll;
using Sixram.Api.DTOs.Portal;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IPortalService
{
    Task<EmployeePortalDashboardDto> GetEmployeeDashboardAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<EmployeeSelfProfileDto> GetMyProfileAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<PayslipSummaryDto>> GetMyPayslipsAsync(MyPayslipListQueryDto query, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PayslipDto> GetMyPayslipAsync(Guid payrollRunItemId, string? actorUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeeRequestHistoryItemDto>> GetMyRequestHistoryAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<ManagerDashboardDto> GetManagerDashboardAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<ManagerPortalOptionsDto> GetManagerOptionsAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ManagerTeamMemberDto>> GetMyTeamAsync(ManagerTeamMemberListQueryDto query, string? actorUserId, CancellationToken cancellationToken = default);
}

public class PortalService : IPortalService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserAccessService _userAccessService;
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceCalculationService _attendanceCalculationService;
    private readonly ILeaveService _leaveService;
    private readonly IEmployeeDocumentService _employeeDocumentService;
    private readonly INotificationService _notificationService;
    private readonly IPayrollService _payrollService;

    public PortalService(
        ApplicationDbContext dbContext,
        IUserAccessService userAccessService,
        IAttendanceService attendanceService,
        IAttendanceCalculationService attendanceCalculationService,
        ILeaveService leaveService,
        IEmployeeDocumentService employeeDocumentService,
        INotificationService notificationService,
        IPayrollService payrollService)
    {
        _dbContext = dbContext;
        _userAccessService = userAccessService;
        _attendanceService = attendanceService;
        _attendanceCalculationService = attendanceCalculationService;
        _leaveService = leaveService;
        _employeeDocumentService = employeeDocumentService;
        _notificationService = notificationService;
        _payrollService = payrollService;
    }

    public async Task<EmployeePortalDashboardDto> GetEmployeeDashboardAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        var employeeId = actor.LinkedEmployeeId!.Value;
        var employee = await LoadEmployeeAsync(employeeId, cancellationToken);
        var today = _attendanceCalculationService.GetBusinessToday();
        var currentYear = today.Year;

        var todayAttendancePage = await _attendanceService.GetAttendanceRecordsAsync(
            new AttendanceRecordListQueryDto
            {
                EmployeeId = employeeId,
                DateFrom = today,
                DateTo = today,
                PageNumber = 1,
                PageSize = 1,
                SortBy = "date",
                Descending = true
            },
            cancellationToken);

        var leaveProfile = await _leaveService.GetEmployeeLeaveProfileAsync(employeeId, currentYear, cancellationToken);
        var documentProfile = await _employeeDocumentService.GetEmployeeDocumentProfileAsync(employeeId, cancellationToken);
        var notificationSummary = await _notificationService.GetSummaryAsync(actor.UserId, cancellationToken);
        var latestPayslip = await GetLatestVisiblePayslipAsync(employeeId, cancellationToken);

        var todayAttendance = todayAttendancePage.Items.FirstOrDefault();
        var lastAttendance = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId && (record.ActualTimeIn != null || record.ActualTimeOut != null))
            .OrderByDescending(record => record.AttendanceDate)
            .Select(record => new AttendanceRecordListItemDto
            {
                AttendanceRecordId = record.Id,
                EmployeeId = record.EmployeeId,
                EmployeeCode = employee.EmployeeCode,
                EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
                DepartmentName = employee.Department != null ? employee.Department.Name : string.Empty,
                BranchName = employee.Branch != null ? employee.Branch.Name : string.Empty,
                AttendanceDate = record.AttendanceDate,
                ScheduledStartTime = record.ScheduledStartTime,
                ScheduledEndTime = record.ScheduledEndTime,
                ActualTimeIn = record.ActualTimeIn,
                ActualTimeOut = record.ActualTimeOut,
                BreakStartTime = record.BreakStartTime,
                BreakEndTime = record.BreakEndTime,
                TotalWorkedMinutes = record.TotalWorkedMinutes,
                LateMinutes = record.LateMinutes,
                UndertimeMinutes = record.UndertimeMinutes,
                OvertimeMinutes = record.OvertimeMinutes,
                Status = record.Status,
                Source = record.Source,
                Remarks = record.Remarks,
                HasScheduleAssignment = true,
                HasBackingRecord = true,
                CreatedByDisplayName = string.Empty,
                UpdatedByDisplayName = string.Empty,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new EmployeePortalDashboardDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            ProfileCompletionPercent = CalculateProfileCompletion(employee),
            TodayAttendance = todayAttendance,
            LastAttendance = lastAttendance,
            LeaveBalances = leaveProfile.Balances,
            PendingLeaveRequestCount = leaveProfile.PendingRequests.Count,
            PendingAttendanceAdjustmentRequestCount = await _dbContext.AttendanceAdjustmentRequests.CountAsync(
                record => record.EmployeeId == employeeId && record.Status == RequestStatuses.Pending,
                cancellationToken),
            PendingProfileChangeRequestCount = await _dbContext.EmployeeProfileChangeRequests.CountAsync(
                record => record.EmployeeId == employeeId && record.Status == RequestStatuses.Pending,
                cancellationToken),
            UpcomingApprovedLeaves = leaveProfile.History
                .Where(record => record.Status == LeaveRequestStatuses.Approved && record.StartDate >= today)
                .OrderBy(record => record.StartDate)
                .Take(5)
                .ToList(),
            LatestPayslip = latestPayslip,
            DocumentSummary = documentProfile.Summary,
            Notifications = notificationSummary.Recent
        };
    }

    public async Task<EmployeeSelfProfileDto> GetMyProfileAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        var employee = await LoadEmployeeAsync(actor.LinkedEmployeeId!.Value, cancellationToken);
        return MapSelfProfile(employee);
    }

    public async Task<PagedResultDto<PayslipSummaryDto>> GetMyPayslipsAsync(MyPayslipListQueryDto query, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        var employeeId = actor.LinkedEmployeeId!.Value;
        var visibilityStatuses = await ResolveVisiblePayrollRunStatusesAsync(cancellationToken);

        var source = _dbContext.PayrollRunItems
            .AsNoTracking()
            .Include(item => item.PayrollRun)
                .ThenInclude(run => run!.PayPeriod)
            .Where(item =>
                item.EmployeeId == employeeId &&
                item.PayrollRun != null &&
                item.PayrollRun.PayPeriod != null &&
                visibilityStatuses.Contains(item.PayrollRun.Status) &&
                item.Status != PayrollItemStatuses.Held);

        if (query.Year is not null)
        {
            source = source.Where(item => item.PayrollRun!.PayPeriod!.PayrollDate.Year == query.Year.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("period", false) => source.OrderBy(item => item.PayrollRun!.PayPeriod!.PeriodStartDate),
            ("period", true) => source.OrderByDescending(item => item.PayrollRun!.PayPeriod!.PeriodStartDate),
            (_, false) => source.OrderBy(item => item.PayrollRun!.PayPeriod!.PayrollDate),
            _ => source.OrderByDescending(item => item.PayrollRun!.PayPeriod!.PayrollDate)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(item => new PayslipSummaryDto
            {
                PayrollRunItemId = item.Id,
                PayrollRunReferenceNumber = item.PayrollRun != null ? item.PayrollRun.ReferenceNumber : string.Empty,
                PayPeriodName = item.PayrollRun != null && item.PayrollRun.PayPeriod != null ? item.PayrollRun.PayPeriod.Name : string.Empty,
                PeriodStartDate = item.PayrollRun != null && item.PayrollRun.PayPeriod != null ? item.PayrollRun.PayPeriod.PeriodStartDate : default,
                PeriodEndDate = item.PayrollRun != null && item.PayrollRun.PayPeriod != null ? item.PayrollRun.PayPeriod.PeriodEndDate : default,
                PayrollDate = item.PayrollRun != null && item.PayrollRun.PayPeriod != null ? item.PayrollRun.PayPeriod.PayrollDate : default,
                Currency = item.CurrencySnapshot,
                GrossPay = item.GrossPay,
                NetPay = item.NetPay,
                Status = item.PayrollRun != null ? item.PayrollRun.Status : string.Empty
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<PayslipSummaryDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<PayslipDto> GetMyPayslipAsync(Guid payrollRunItemId, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        var visibilityStatuses = await ResolveVisiblePayrollRunStatusesAsync(cancellationToken);
        var payrollRunItem = await _dbContext.PayrollRunItems
            .AsNoTracking()
            .Include(item => item.PayrollRun)
            .SingleOrDefaultAsync(item => item.Id == payrollRunItemId, cancellationToken)
            ?? throw new NotFoundException("The requested payslip was not found.");

        if (payrollRunItem.EmployeeId != actor.LinkedEmployeeId)
        {
            throw new ForbiddenApiException("You do not have permission to view this payslip.");
        }

        if (payrollRunItem.PayrollRun is null || !visibilityStatuses.Contains(payrollRunItem.PayrollRun.Status) || payrollRunItem.Status == PayrollItemStatuses.Held)
        {
            throw new ForbiddenApiException("This payslip is not visible yet.");
        }

        return await _payrollService.GetPayslipAsync(payrollRunItemId, cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeRequestHistoryItemDto>> GetMyRequestHistoryAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        var employeeId = actor.LinkedEmployeeId!.Value;

        var leaveRequests = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId)
            .OrderByDescending(record => record.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        var attendanceRequests = await _dbContext.AttendanceAdjustmentRequests
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId)
            .OrderByDescending(record => record.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        var profileRequests = await _dbContext.EmployeeProfileChangeRequests
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId)
            .OrderByDescending(record => record.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

        var items = new List<EmployeeRequestHistoryItemDto>();
        items.AddRange(leaveRequests.Select(record => new EmployeeRequestHistoryItemDto
        {
            RequestType = ApprovableTypes.LeaveRequest,
            RequestLabel = "Leave request",
            RequestId = record.Id.ToString(),
            Title = $"{record.StartDate:yyyy-MM-dd} to {record.EndDate:yyyy-MM-dd}",
            Subtitle = record.Reason,
            Status = record.Status,
            CurrentApproverDisplayName = string.Empty,
            SubmittedAtUtc = record.SubmittedAtUtc ?? record.CreatedAtUtc,
            LastUpdatedAtUtc = record.UpdatedAtUtc ?? record.CreatedAtUtc,
            CanCancel = string.Equals(record.Status, LeaveRequestStatuses.Pending, StringComparison.OrdinalIgnoreCase)
        }));

        items.AddRange(attendanceRequests.Select(record => new EmployeeRequestHistoryItemDto
        {
            RequestType = ApprovableTypes.AttendanceAdjustmentRequest,
            RequestLabel = "Attendance correction",
            RequestId = record.Id.ToString(),
            Title = record.AttendanceDate.ToString("yyyy-MM-dd"),
            Subtitle = record.Reason,
            Status = record.Status,
            CurrentApproverDisplayName = string.Empty,
            SubmittedAtUtc = record.CreatedAtUtc,
            LastUpdatedAtUtc = record.UpdatedAtUtc ?? record.CreatedAtUtc,
            CanCancel = string.Equals(record.Status, RequestStatuses.Pending, StringComparison.OrdinalIgnoreCase)
        }));

        items.AddRange(profileRequests.Select(record => new EmployeeRequestHistoryItemDto
        {
            RequestType = ApprovableTypes.ProfileChangeRequest,
            RequestLabel = "Profile change",
            RequestId = record.Id.ToString(),
            Title = "Personal profile update",
            Subtitle = record.Reason,
            Status = record.Status,
            CurrentApproverDisplayName = "HR Review",
            SubmittedAtUtc = record.CreatedAtUtc,
            LastUpdatedAtUtc = record.UpdatedAtUtc ?? record.CreatedAtUtc,
            CanCancel = string.Equals(record.Status, RequestStatuses.Pending, StringComparison.OrdinalIgnoreCase)
        }));

        return items
            .OrderByDescending(item => item.SubmittedAtUtc)
            .Take(100)
            .ToList();
    }

    public async Task<ManagerDashboardDto> GetManagerDashboardAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        if (!actor.IsManager)
        {
            throw new ForbiddenApiException("This employee does not have direct reports assigned.");
        }

        var directReportIds = actor.ManagedEmployeeIds;
        var today = _attendanceCalculationService.GetBusinessToday();
        var attendancePage = await _attendanceService.GetAttendanceRecordsAsync(
            new AttendanceRecordListQueryDto
            {
                EmployeeIds = directReportIds,
                DateFrom = today,
                DateTo = today,
                PageNumber = 1,
                PageSize = Math.Max(25, directReportIds.Count),
                SortBy = "date",
                Descending = true
            },
            cancellationToken);

        var notifications = await _notificationService.GetSummaryAsync(actor.UserId, cancellationToken);

        return new ManagerDashboardDto
        {
            ManagerEmployeeId = actor.LinkedEmployeeId!.Value,
            DirectReportCount = directReportIds.Count,
            PresentTodayCount = attendancePage.Items.Count(item => item.Status == AttendanceStatuses.Present),
            LateTodayCount = attendancePage.Items.Count(item => item.Status == AttendanceStatuses.Late),
            AbsentTodayCount = attendancePage.Items.Count(item => item.Status == AttendanceStatuses.Absent),
            OnLeaveTodayCount = attendancePage.Items.Count(item => item.Status == AttendanceStatuses.OnLeave),
            IncompleteLogCount = attendancePage.Items.Count(item => item.Status == AttendanceStatuses.Incomplete),
            EmployeesWithoutScheduleCount = attendancePage.Items.Count(item => item.Status == AttendanceStatuses.NoSchedule),
            PendingApprovalCount =
                await _dbContext.LeaveRequests.CountAsync(
                    record =>
                        directReportIds.Contains(record.EmployeeId) &&
                        record.Status == LeaveRequestStatuses.Pending &&
                        record.CurrentApproverUserId == actor.UserId,
                    cancellationToken) +
                await _dbContext.AttendanceAdjustmentRequests.CountAsync(
                    record =>
                        directReportIds.Contains(record.EmployeeId) &&
                        record.Status == RequestStatuses.Pending &&
                        (record.CurrentApproverUserId == actor.UserId || record.CurrentApproverUserId == null),
                    cancellationToken),
            UpcomingTeamLeaveCount = await _dbContext.LeaveRequests.CountAsync(
                record =>
                    directReportIds.Contains(record.EmployeeId) &&
                    record.Status == LeaveRequestStatuses.Approved &&
                    record.StartDate > today &&
                    record.StartDate <= today.AddDays(30),
                cancellationToken),
            Notifications = notifications.Recent
        };
    }

    public async Task<ManagerPortalOptionsDto> GetManagerOptionsAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        if (!actor.IsManager)
        {
            throw new ForbiddenApiException("This employee does not have direct reports assigned.");
        }

        var employees = await _dbContext.Employees
            .AsNoTracking()
            .Include(record => record.Department)
            .Include(record => record.Branch)
            .Where(record => actor.ManagedEmployeeIds.Contains(record.Id))
            .OrderBy(record => record.LastName)
            .ThenBy(record => record.FirstName)
            .ToListAsync(cancellationToken);

        return new ManagerPortalOptionsDto
        {
            Employees = employees
                .Select(record => new EmployeeAttendanceOptionDto
                {
                    Id = record.Id,
                    EmployeeCode = record.EmployeeCode,
                    FullName = BuildFullName(record.FirstName, record.MiddleName, record.LastName, record.Suffix),
                    DepartmentName = record.Department?.Name ?? string.Empty,
                    BranchName = record.Branch?.Name ?? string.Empty,
                    IsActive = record.IsActive
                })
                .ToList(),
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

    public async Task<PagedResultDto<ManagerTeamMemberDto>> GetMyTeamAsync(ManagerTeamMemberListQueryDto query, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        if (!actor.IsManager)
        {
            throw new ForbiddenApiException("This employee does not have direct reports assigned.");
        }

        var source = _dbContext.Employees
            .AsNoTracking()
            .Include(record => record.Department)
            .Include(record => record.Position)
            .Include(record => record.Branch)
            .Include(record => record.EmploymentStatus)
            .Where(record => actor.ManagedEmployeeIds.Contains(record.Id));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.EmployeeCode.Contains(search) ||
                record.FirstName.Contains(search) ||
                record.MiddleName.Contains(search) ||
                record.LastName.Contains(search));
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.BranchId == query.BranchId.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("code", true) => source.OrderByDescending(record => record.EmployeeCode),
            ("code", false) => source.OrderBy(record => record.EmployeeCode),
            (_, true) => source.OrderByDescending(record => record.LastName).ThenByDescending(record => record.FirstName),
            _ => source.OrderBy(record => record.LastName).ThenBy(record => record.FirstName)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var employees = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var employeeIds = employees.Select(record => record.Id).ToArray();
        var today = _attendanceCalculationService.GetBusinessToday();
        var attendance = await _attendanceService.GetAttendanceRecordsAsync(
            new AttendanceRecordListQueryDto
            {
                EmployeeIds = employeeIds,
                DateFrom = today,
                DateTo = today,
                PageNumber = 1,
                PageSize = Math.Max(25, employeeIds.Length),
                SortBy = "date",
                Descending = true
            },
            cancellationToken);

        var attendanceLookup = attendance.Items.ToDictionary(record => record.EmployeeId, record => record);
        var leaveLookup = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(record =>
                employeeIds.Contains(record.EmployeeId) &&
                record.StartDate <= today &&
                record.EndDate >= today &&
                (record.Status == LeaveRequestStatuses.Pending || record.Status == LeaveRequestStatuses.Approved))
            .GroupBy(record => record.EmployeeId)
            .ToDictionaryAsync(group => group.Key, group => group.OrderByDescending(record => record.Status == LeaveRequestStatuses.Approved).ThenBy(record => record.StartDate).First().Status, cancellationToken);

        var items = employees.Select(record =>
        {
            var attendanceRecord = attendanceLookup.GetValueOrDefault(record.Id);
            return new ManagerTeamMemberDto
            {
                EmployeeId = record.Id,
                EmployeeCode = record.EmployeeCode,
                FullName = BuildFullName(record.FirstName, record.MiddleName, record.LastName, record.Suffix),
                DepartmentName = record.Department?.Name ?? string.Empty,
                PositionName = record.Position?.Name ?? string.Empty,
                BranchName = record.Branch?.Name ?? string.Empty,
                EmploymentStatusName = record.EmploymentStatus?.Name ?? string.Empty,
                MobileNumber = record.MobileNumber,
                Email = record.Email,
                TodayAttendanceStatus = attendanceRecord?.Status ?? string.Empty,
                TodayAttendanceTimeInLabel = attendanceRecord?.ActualTimeIn?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty,
                LeaveStatus = leaveLookup.GetValueOrDefault(record.Id, string.Empty),
                IsActive = record.IsActive
            };
        }).ToList();

        return new PagedResultDto<ManagerTeamMemberDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    private async Task<Employee> LoadEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .Include(record => record.Department)
            .Include(record => record.Position)
            .Include(record => record.Branch)
            .Include(record => record.EmploymentType)
            .Include(record => record.EmploymentStatus)
            .Include(record => record.Manager)
            .SingleOrDefaultAsync(record => record.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException("The linked employee record could not be found.");
    }

    private async Task<PayslipSummaryDto?> GetLatestVisiblePayslipAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var visibilityStatuses = await ResolveVisiblePayrollRunStatusesAsync(cancellationToken);
        return await _dbContext.PayrollRunItems
            .AsNoTracking()
            .Include(item => item.PayrollRun)
                .ThenInclude(run => run!.PayPeriod)
            .Where(item =>
                item.EmployeeId == employeeId &&
                item.PayrollRun != null &&
                item.PayrollRun.PayPeriod != null &&
                visibilityStatuses.Contains(item.PayrollRun.Status) &&
                item.Status != PayrollItemStatuses.Held)
            .OrderByDescending(item => item.PayrollRun!.PayPeriod!.PayrollDate)
            .Select(item => new PayslipSummaryDto
            {
                PayrollRunItemId = item.Id,
                PayrollRunReferenceNumber = item.PayrollRun != null ? item.PayrollRun.ReferenceNumber : string.Empty,
                PayPeriodName = item.PayrollRun != null && item.PayrollRun.PayPeriod != null ? item.PayrollRun.PayPeriod.Name : string.Empty,
                PeriodStartDate = item.PayrollRun != null && item.PayrollRun.PayPeriod != null ? item.PayrollRun.PayPeriod.PeriodStartDate : default,
                PeriodEndDate = item.PayrollRun != null && item.PayrollRun.PayPeriod != null ? item.PayrollRun.PayPeriod.PeriodEndDate : default,
                PayrollDate = item.PayrollRun != null && item.PayrollRun.PayPeriod != null ? item.PayrollRun.PayPeriod.PayrollDate : default,
                Currency = item.CurrencySnapshot,
                GrossPay = item.GrossPay,
                NetPay = item.NetPay,
                Status = item.PayrollRun != null ? item.PayrollRun.Status : string.Empty
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<string>> ResolveVisiblePayrollRunStatusesAsync(CancellationToken cancellationToken)
    {
        var rule = await _dbContext.PayrollSettings
            .AsNoTracking()
            .Where(record => record.Key == PayrollSettingKeys.PayslipVisibilityRule)
            .Select(record => record.Value)
            .SingleOrDefaultAsync(cancellationToken)
            ?? "approved_or_paid";

        return rule.Trim().ToLowerInvariant() switch
        {
            "paid_only" => [PayrollRunStatuses.Paid],
            "approved_only" => [PayrollRunStatuses.Approved],
            _ => [PayrollRunStatuses.Approved, PayrollRunStatuses.Paid]
        };
    }

    private static EmployeeSelfProfileDto MapSelfProfile(Employee employee)
    {
        return new EmployeeSelfProfileDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            FirstName = employee.FirstName,
            MiddleName = employee.MiddleName,
            LastName = employee.LastName,
            Suffix = employee.Suffix,
            Gender = employee.Gender,
            BirthDate = employee.BirthDate,
            CivilStatus = employee.CivilStatus,
            Nationality = employee.Nationality,
            MobileNumber = employee.MobileNumber,
            Email = employee.Email,
            Address = employee.Address,
            CityProvince = employee.CityProvince,
            PostalCode = employee.PostalCode,
            EmergencyContactName = employee.EmergencyContactName,
            EmergencyContactRelationship = employee.EmergencyContactRelationship,
            EmergencyContactPhone = employee.EmergencyContactPhone,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            PositionName = employee.Position?.Name ?? string.Empty,
            BranchName = employee.Branch?.Name ?? string.Empty,
            EmploymentTypeName = employee.EmploymentType?.Name ?? string.Empty,
            EmploymentStatusName = employee.EmploymentStatus?.Name ?? string.Empty,
            ManagerName = employee.Manager is null ? string.Empty : BuildFullName(employee.Manager.FirstName, employee.Manager.MiddleName, employee.Manager.LastName, employee.Manager.Suffix),
            WorkSchedule = employee.WorkSchedule,
            DateHired = employee.DateHired,
            DateRegularized = employee.DateRegularized,
            DateSeparated = employee.DateSeparated,
            SssNumberMasked = MaskIdentifier(employee.SssNumber),
            PhilHealthNumberMasked = MaskIdentifier(employee.PhilHealthNumber),
            PagIbigNumberMasked = MaskIdentifier(employee.PagIbigNumber),
            TinNumberMasked = MaskIdentifier(employee.TinNumber),
            OtherGovernmentIdMasked = MaskIdentifier(employee.OtherGovernmentId),
            IsActive = employee.IsActive,
            CreatedAtUtc = employee.CreatedAtUtc,
            UpdatedAtUtc = employee.UpdatedAtUtc
        };
    }

    private static int CalculateProfileCompletion(Employee employee)
    {
        var values = new[]
        {
            employee.EmployeeCode,
            employee.FirstName,
            employee.LastName,
            employee.Gender,
            employee.CivilStatus,
            employee.Nationality,
            employee.MobileNumber,
            employee.Email,
            employee.Address,
            employee.CityProvince,
            employee.PostalCode,
            employee.EmergencyContactName,
            employee.EmergencyContactRelationship,
            employee.EmergencyContactPhone,
            employee.DepartmentId?.ToString() ?? string.Empty,
            employee.PositionId?.ToString() ?? string.Empty,
            employee.BranchId?.ToString() ?? string.Empty,
            employee.EmploymentTypeId?.ToString() ?? string.Empty,
            employee.EmploymentStatusId?.ToString() ?? string.Empty,
            employee.DateHired?.ToString() ?? string.Empty
        };

        var completed = values.Count(value => !string.IsNullOrWhiteSpace(value));
        return (int)Math.Round(completed / (double)values.Length * 100, MidpointRounding.AwayFromZero);
    }

    private static string BuildFullName(string firstName, string middleName, string lastName, string suffix)
    {
        var parts = new[] { firstName, middleName, lastName, suffix }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim());

        return string.Join(" ", parts);
    }

    private static string MaskIdentifier(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length <= 4)
        {
            return normalized;
        }

        return $"{new string('*', normalized.Length - 4)}{normalized[^4..]}";
    }
}
