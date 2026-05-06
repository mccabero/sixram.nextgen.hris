using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sixram.Api.Configuration;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface ILeaveService
{
    Task<LeaveDashboardSummaryDto> GetDashboardSummaryAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<LeaveManagementOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);

    Task<PagedResultDto<EmployeeLeaveBalanceDto>> GetBalancesAsync(LeaveBalanceListQueryDto query, CancellationToken cancellationToken = default);

    Task<LeaveBalanceTransactionDto> AdjustBalanceAsync(LeaveBalanceAdjustmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<LeaveRequestListItemDto>> GetLeaveRequestsAsync(LeaveRequestListQueryDto query, CancellationToken cancellationToken = default);

    Task<LeaveRequestListItemDto> GetLeaveRequestByIdAsync(Guid leaveRequestId, CancellationToken cancellationToken = default);

    Task<LeaveRequestListItemDto> CreateLeaveRequestAsync(SaveLeaveRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<LeaveRequestListItemDto> UpdateLeaveRequestAsync(Guid leaveRequestId, SaveLeaveRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<LeaveRequestListItemDto> ApproveLeaveRequestAsync(Guid leaveRequestId, LeaveActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<LeaveRequestListItemDto> RejectLeaveRequestAsync(Guid leaveRequestId, LeaveActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<LeaveRequestListItemDto> CancelLeaveRequestAsync(Guid leaveRequestId, LeaveActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task DeleteLeaveRequestAsync(Guid leaveRequestId, CancellationToken cancellationToken = default);

    Task<LeaveCalendarResponseDto> GetCalendarAsync(LeaveCalendarQueryDto query, CancellationToken cancellationToken = default);

    Task<EmployeeLeaveProfileDto> GetEmployeeLeaveProfileAsync(Guid employeeId, int? periodYear, CancellationToken cancellationToken = default);

    Task<StreamedLeaveAttachmentFile> GetAttachmentAsync(Guid leaveRequestId, CancellationToken cancellationToken = default);
}

public class LeaveService : ILeaveService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAttendanceCalculationService _attendanceCalculationService;
    private readonly ILeaveAttachmentStorageService _attachmentStorageService;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;
    private readonly LeaveOptions _options;
    private readonly ILogger<LeaveService> _logger;

    public LeaveService(
        ApplicationDbContext dbContext,
        IAttendanceCalculationService attendanceCalculationService,
        ILeaveAttachmentStorageService attachmentStorageService,
        IAuditLogService auditLogService,
        INotificationService notificationService,
        IOptions<LeaveOptions> options,
        ILogger<LeaveService> logger)
    {
        _dbContext = dbContext;
        _attendanceCalculationService = attendanceCalculationService;
        _attachmentStorageService = attachmentStorageService;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LeaveDashboardSummaryDto> GetDashboardSummaryAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var businessDate = _attendanceCalculationService.GetBusinessToday();
        await EnsureBalancesForYearAsync(businessDate.Year, cancellationToken);

        var lowThreshold = _options.LowBalanceThreshold;
        var pendingLeaveRequestCount = await _dbContext.LeaveRequests.CountAsync(record => record.Status == LeaveRequestStatuses.Pending, cancellationToken);
        var approvedLeavesTodayCount = await _dbContext.LeaveRequests.CountAsync(
            record => record.StartDate <= businessDate &&
                      record.EndDate >= businessDate &&
                      record.Status == LeaveRequestStatuses.Approved,
            cancellationToken);

        var employeesOnLeaveTodayCount = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(record =>
                record.AttendanceDate == businessDate &&
                record.Status == AttendanceStatuses.OnLeave)
            .Select(record => record.EmployeeId)
            .Distinct()
            .CountAsync(cancellationToken);

        var lowBalanceCount = await _dbContext.EmployeeLeaveBalances
            .AsNoTracking()
            .CountAsync(record => record.PeriodYear == businessDate.Year && record.AvailableBalance >= 0m && record.AvailableBalance <= lowThreshold, cancellationToken);

        var negativeBalanceCount = await _dbContext.EmployeeLeaveBalances
            .AsNoTracking()
            .CountAsync(record => record.PeriodYear == businessDate.Year && record.AvailableBalance < 0m, cancellationToken);

        var upcomingCutoff = businessDate.AddDays(_options.UpcomingWindowDays);
        var upcomingApprovedLeaveCount = await _dbContext.LeaveRequests.CountAsync(
            record => record.Status == LeaveRequestStatuses.Approved &&
                      record.StartDate > businessDate &&
                      record.StartDate <= upcomingCutoff,
            cancellationToken);

        var attendanceConflictCount = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .CountAsync(
                attendance =>
                    _dbContext.LeaveRequests.Any(
                        request =>
                            request.Status == LeaveRequestStatuses.Approved &&
                            request.EmployeeId == attendance.EmployeeId &&
                            request.StartDate <= attendance.AttendanceDate &&
                            request.EndDate >= attendance.AttendanceDate &&
                            !(attendance.Source == AttendanceSources.Leave && attendance.LeaveRequestId == request.Id)),
                cancellationToken);

        _ = actorUserId;

        return new LeaveDashboardSummaryDto
        {
            BusinessDate = businessDate,
            PendingLeaveRequestCount = pendingLeaveRequestCount,
            ApprovedLeavesTodayCount = approvedLeavesTodayCount,
            EmployeesOnLeaveTodayCount = employeesOnLeaveTodayCount,
            LowBalanceCount = lowBalanceCount,
            NegativeBalanceCount = negativeBalanceCount,
            UpcomingApprovedLeaveCount = upcomingApprovedLeaveCount,
            AttendanceConflictCount = attendanceConflictCount
        };
    }

    public async Task<LeaveManagementOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        var businessYear = _attendanceCalculationService.GetBusinessToday().Year;
        var discoveredYears = await _dbContext.EmployeeLeaveBalances
            .AsNoTracking()
            .Select(record => record.PeriodYear)
            .Union(_dbContext.LeaveRequests.AsNoTracking().Select(record => record.StartDate.Year))
            .Distinct()
            .OrderBy(record => record)
            .ToListAsync(cancellationToken);

        if (!discoveredYears.Contains(businessYear))
        {
            discoveredYears.Add(businessYear);
        }

        return new LeaveManagementOptionsDto
        {
            Employees = await _dbContext.Employees
                .AsNoTracking()
                .Where(record => record.IsActive)
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
                .ToListAsync(cancellationToken),
            Departments = await _dbContext.Departments
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new LookupOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            Branches = await _dbContext.Branches
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new LookupOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            EmploymentTypes = await _dbContext.EmploymentTypes
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new LookupOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            LeaveTypes = await _dbContext.LeaveTypes
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new LeaveTypeOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    AllowHalfDay = record.AllowHalfDay,
                    RequiresAttachment = record.RequiresAttachment,
                    RequiresReason = record.RequiresReason,
                    AllowNegativeBalance = record.AllowNegativeBalance,
                    DefaultAnnualCredits = record.DefaultAnnualCredits,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            Statuses = LeaveRequestStatuses.All,
            PeriodYears = discoveredYears
                .Concat([businessYear - 1, businessYear, businessYear + 1])
                .Distinct()
                .OrderBy(record => record)
                .ToArray()
        };
    }

    public async Task<PagedResultDto<EmployeeLeaveBalanceDto>> GetBalancesAsync(LeaveBalanceListQueryDto query, CancellationToken cancellationToken = default)
    {
        var periodYear = query.PeriodYear ?? _attendanceCalculationService.GetBusinessToday().Year;
        await EnsureBalancesForYearAsync(periodYear, cancellationToken);

        var source = _dbContext.EmployeeLeaveBalances
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.LeaveType)
            .Where(record => record.PeriodYear == periodYear);

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

        if (query.LowBalanceOnly == true)
        {
            source = source.Where(record => record.AvailableBalance >= 0m && record.AvailableBalance <= _options.LowBalanceThreshold);
        }

        if (query.NegativeBalanceOnly == true)
        {
            source = source.Where(record => record.AvailableBalance < 0m);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("available", true) => source.OrderByDescending(record => record.AvailableBalance).ThenBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.FirstName : string.Empty),
            ("available", false) => source.OrderBy(record => record.AvailableBalance).ThenBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.FirstName : string.Empty),
            ("leave-type", true) => source.OrderByDescending(record => record.LeaveType != null ? record.LeaveType.Name : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.FirstName : string.Empty),
            ("leave-type", false) => source.OrderBy(record => record.LeaveType != null ? record.LeaveType.Name : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.FirstName : string.Empty),
            ("used", true) => source.OrderByDescending(record => record.Used).ThenBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.FirstName : string.Empty),
            ("used", false) => source.OrderBy(record => record.Used).ThenBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.FirstName : string.Empty),
            (_, true) => source.OrderByDescending(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenByDescending(record => record.Employee != null ? record.Employee.FirstName : string.Empty).ThenBy(record => record.LeaveType != null ? record.LeaveType.Name : string.Empty),
            _ => source.OrderBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.FirstName : string.Empty).ThenBy(record => record.LeaveType != null ? record.LeaveType.Name : string.Empty)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = records
            .Select(MapBalance)
            .ToList();

        return new PagedResultDto<EmployeeLeaveBalanceDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<LeaveBalanceTransactionDto> AdjustBalanceAsync(LeaveBalanceAdjustmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeForLeaveAsync(request.EmployeeId!.Value, cancellationToken);
        var leaveType = await GetLeaveTypeForRequestAsync(request.LeaveTypeId!.Value, null, cancellationToken);
        var periodYear = request.PeriodYear!.Value;
        var balance = await EnsureBalanceRowAsync(employee.Id, leaveType.Id, periodYear, cancellationToken);
        var before = balance.AvailableBalance;

        balance.Adjusted += request.Amount;
        balance.UpdatedAtUtc = DateTime.UtcNow;

        var transaction = new LeaveBalanceTransaction
        {
            EmployeeId = employee.Id,
            LeaveTypeId = leaveType.Id,
            PeriodYear = periodYear,
            TransactionType = LeaveBalanceTransactionTypes.Adjustment,
            Amount = request.Amount,
            BalanceBefore = before,
            BalanceAfter = before + request.Amount,
            Remarks = string.IsNullOrWhiteSpace(request.Remarks)
                ? $"Manual leave balance adjustment effective {request.EffectiveDate?.ToString("yyyy-MM-dd") ?? "immediately"}."
                : request.Remarks.Trim(),
            CreatedByUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.LeaveBalanceTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await RecalculateBalanceAsync(employee.Id, leaveType.Id, periodYear, cancellationToken);

        _logger.LogInformation(
            "Leave balance adjusted for employee {EmployeeId}, leave type {LeaveTypeId}, year {PeriodYear}.",
            employee.Id,
            leaveType.Id,
            periodYear);

        return await GetLeaveBalanceTransactionByIdAsync(transaction.Id, cancellationToken);
    }

    public async Task<PagedResultDto<LeaveRequestListItemDto>> GetLeaveRequestsAsync(LeaveRequestListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.LeaveType)
            .Include(record => record.CurrentApproverUser)
            .Include(record => record.CreatedByUser)
            .Include(record => record.UpdatedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Employee!.EmployeeCode.Contains(search) ||
                record.Employee.FirstName.Contains(search) ||
                record.Employee.MiddleName.Contains(search) ||
                record.Employee.LastName.Contains(search) ||
                record.LeaveType!.Name.Contains(search) ||
                record.Reason.Contains(search));
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (query.EmployeeIds.Count > 0)
        {
            source = source.Where(record => query.EmployeeIds.Contains(record.EmployeeId));
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

        if (!string.IsNullOrWhiteSpace(query.ApproverId))
        {
            source = source.Where(record => record.CurrentApproverUserId == query.ApproverId.Trim());
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(record => record.EndDate >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(record => record.StartDate <= query.DateTo.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("employee", true) => source.OrderByDescending(record => record.Employee!.LastName).ThenByDescending(record => record.StartDate),
            ("employee", false) => source.OrderBy(record => record.Employee!.LastName).ThenBy(record => record.StartDate),
            ("start", true) => source.OrderByDescending(record => record.StartDate).ThenByDescending(record => record.SubmittedAtUtc),
            ("start", false) => source.OrderBy(record => record.StartDate).ThenByDescending(record => record.SubmittedAtUtc),
            ("status", true) => source.OrderByDescending(record => record.Status).ThenByDescending(record => record.SubmittedAtUtc),
            ("status", false) => source.OrderBy(record => record.Status).ThenByDescending(record => record.SubmittedAtUtc),
            (_, true) => source.OrderByDescending(record => record.SubmittedAtUtc).ThenByDescending(record => record.StartDate),
            _ => source.OrderBy(record => record.SubmittedAtUtc).ThenBy(record => record.StartDate)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = new List<LeaveRequestListItemDto>(records.Count);
        foreach (var record in records)
        {
            items.Add(await MapLeaveRequestAsync(record, cancellationToken));
        }

        return new PagedResultDto<LeaveRequestListItemDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<LeaveRequestListItemDto> GetLeaveRequestByIdAsync(Guid leaveRequestId, CancellationToken cancellationToken = default)
    {
        var record = await LoadLeaveRequestAsync(leaveRequestId, cancellationToken);
        return await MapLeaveRequestAsync(record, cancellationToken);
    }

    public async Task<LeaveRequestListItemDto> CreateLeaveRequestAsync(SaveLeaveRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var employee = await GetEmployeeForLeaveAsync(request.EmployeeId!.Value, cancellationToken);
        var leaveType = await GetLeaveTypeForRequestAsync(request.LeaveTypeId!.Value, null, cancellationToken);
        ValidateDayTypeRequest(leaveType, request.StartDate!.Value, request.EndDate!.Value, request.StartDayType, request.EndDayType);
        ValidateLeaveTypePolicy(employee, leaveType, request.StartDate.Value, request.Reason, request.Attachment is not null);

        var dayCalculation = await CalculateLeaveDaysAsync(
            employee.Id,
            leaveType,
            request.StartDate.Value,
            request.EndDate.Value,
            request.StartDayType,
            request.EndDayType,
            cancellationToken);

        if (dayCalculation.TotalDays <= 0m)
        {
            throw new BadRequestException("The selected leave dates do not contain any counted leave days.");
        }

        ValidateMaxDaysPerRequest(leaveType, dayCalculation.TotalDays);
        await EnsureNoOverlapAsync(employee.Id, request.StartDate.Value, request.EndDate.Value, null, cancellationToken);
        await EnsureSufficientBalanceAsync(employee.Id, leaveType, dayCalculation.DaysByYear, null, cancellationToken);

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = employee.Id,
            LeaveTypeId = leaveType.Id,
            StartDate = request.StartDate.Value,
            EndDate = request.EndDate.Value,
            StartDayType = request.StartDayType.Trim().ToLowerInvariant(),
            EndDayType = request.EndDayType.Trim().ToLowerInvariant(),
            TotalLeaveDays = dayCalculation.TotalDays,
            Reason = request.Reason.Trim(),
            Status = LeaveRequestStatuses.Pending,
            SubmittedAtUtc = DateTime.UtcNow,
            CurrentApproverUserId = await ResolveDefaultApproverUserIdAsync(employee.Id, cancellationToken),
            CreatedByUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow
        };

        StoredLeaveAttachmentFile? storedFile = null;
        if (request.Attachment is not null)
        {
            storedFile = await _attachmentStorageService.SaveAsync(employee.Id, leaveRequest.Id, request.Attachment, cancellationToken);
            ApplyStoredAttachment(leaveRequest, storedFile);
        }

        _dbContext.LeaveRequests.Add(leaveRequest);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (storedFile is not null)
            {
                await _attachmentStorageService.DeleteAsync(storedFile.StoragePath, cancellationToken);
            }

            throw;
        }

        foreach (var year in dayCalculation.DaysByYear.Keys)
        {
            await RecalculateBalanceAsync(employee.Id, leaveType.Id, year, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(leaveRequest.CurrentApproverUserId))
        {
            await _notificationService.CreateAsync(
                new NotificationDraft(
                    leaveRequest.CurrentApproverUserId,
                    "Leave request submitted",
                    $"{BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix)} submitted a leave request for {leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd}.",
                    NotificationTypes.LeaveSubmitted,
                    ApprovableTypes.LeaveRequest,
                    leaveRequest.Id.ToString(),
                    "/approvals"),
                cancellationToken);
        }

        _logger.LogInformation("Leave request {LeaveRequestId} created for employee {EmployeeId}.", leaveRequest.Id, employee.Id);
        return await GetLeaveRequestByIdAsync(leaveRequest.Id, cancellationToken);
    }

    public async Task<LeaveRequestListItemDto> UpdateLeaveRequestAsync(Guid leaveRequestId, SaveLeaveRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _dbContext.LeaveRequests
            .SingleOrDefaultAsync(record => record.Id == leaveRequestId, cancellationToken)
            ?? throw new NotFoundException($"Leave request '{leaveRequestId}' was not found.");

        EnsureMutableStatus(leaveRequest.Status);

        var employee = await GetEmployeeForLeaveAsync(request.EmployeeId!.Value, cancellationToken);
        var leaveType = await GetLeaveTypeForRequestAsync(request.LeaveTypeId!.Value, leaveRequest.LeaveTypeId, cancellationToken);
        ValidateDayTypeRequest(leaveType, request.StartDate!.Value, request.EndDate!.Value, request.StartDayType, request.EndDayType);
        ValidateLeaveTypePolicy(
            employee,
            leaveType,
            request.StartDate.Value,
            request.Reason,
            request.Attachment is not null || !string.IsNullOrWhiteSpace(leaveRequest.AttachmentPath));

        var previousAllocationKeys = await GetRequestYearsAsync(leaveRequest, cancellationToken);
        var dayCalculation = await CalculateLeaveDaysAsync(
            employee.Id,
            leaveType,
            request.StartDate.Value,
            request.EndDate.Value,
            request.StartDayType,
            request.EndDayType,
            cancellationToken);

        if (dayCalculation.TotalDays <= 0m)
        {
            throw new BadRequestException("The selected leave dates do not contain any counted leave days.");
        }

        ValidateMaxDaysPerRequest(leaveType, dayCalculation.TotalDays);
        await EnsureNoOverlapAsync(employee.Id, request.StartDate.Value, request.EndDate.Value, leaveRequestId, cancellationToken);
        await EnsureSufficientBalanceAsync(employee.Id, leaveType, dayCalculation.DaysByYear, leaveRequestId, cancellationToken);

        var previousStoragePath = leaveRequest.AttachmentPath;
        StoredLeaveAttachmentFile? storedFile = null;
        if (request.Attachment is not null)
        {
            storedFile = await _attachmentStorageService.SaveAsync(employee.Id, leaveRequest.Id, request.Attachment, cancellationToken);
            ApplyStoredAttachment(leaveRequest, storedFile);
        }

        leaveRequest.EmployeeId = employee.Id;
        leaveRequest.LeaveTypeId = leaveType.Id;
        leaveRequest.StartDate = request.StartDate.Value;
        leaveRequest.EndDate = request.EndDate.Value;
        leaveRequest.StartDayType = request.StartDayType.Trim().ToLowerInvariant();
        leaveRequest.EndDayType = request.EndDayType.Trim().ToLowerInvariant();
        leaveRequest.TotalLeaveDays = dayCalculation.TotalDays;
        leaveRequest.Reason = request.Reason.Trim();
        leaveRequest.CurrentApproverUserId = await ResolveDefaultApproverUserIdAsync(employee.Id, cancellationToken);
        leaveRequest.UpdatedByUserId = NormalizeUserId(actorUserId);
        leaveRequest.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (storedFile is not null)
            {
                await _attachmentStorageService.DeleteAsync(storedFile.StoragePath, cancellationToken);
            }

            throw;
        }

        if (storedFile is not null && !string.IsNullOrWhiteSpace(previousStoragePath) && !string.Equals(previousStoragePath, storedFile.StoragePath, StringComparison.OrdinalIgnoreCase))
        {
            await _attachmentStorageService.DeleteAsync(previousStoragePath, cancellationToken);
        }

        foreach (var allocationKey in previousAllocationKeys.Union(dayCalculation.DaysByYear.Keys.Select(year => new RequestAllocationKey(employee.Id, leaveType.Id, year))))
        {
            await RecalculateBalanceAsync(allocationKey.EmployeeId, allocationKey.LeaveTypeId, allocationKey.PeriodYear, cancellationToken);
        }

        _logger.LogInformation("Leave request {LeaveRequestId} updated.", leaveRequestId);
        return await GetLeaveRequestByIdAsync(leaveRequestId, cancellationToken);
    }

    public async Task<LeaveRequestListItemDto> ApproveLeaveRequestAsync(Guid leaveRequestId, LeaveActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _dbContext.LeaveRequests
            .Include(record => record.LeaveType)
            .SingleOrDefaultAsync(record => record.Id == leaveRequestId, cancellationToken)
            ?? throw new NotFoundException($"Leave request '{leaveRequestId}' was not found.");

        if (leaveRequest.Status != LeaveRequestStatuses.Pending)
        {
            throw new BadRequestException("Only pending leave requests can be approved.");
        }

        var employee = await GetEmployeeForLeaveAsync(leaveRequest.EmployeeId, cancellationToken);
        var leaveType = leaveRequest.LeaveType ?? await GetLeaveTypeForRequestAsync(leaveRequest.LeaveTypeId, leaveRequest.LeaveTypeId, cancellationToken);
        var dayCalculation = await CalculateLeaveDaysAsync(
            employee.Id,
            leaveType,
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.StartDayType,
            leaveRequest.EndDayType,
            cancellationToken);

        await EnsureBalanceStillApprovableAsync(employee.Id, leaveType, dayCalculation.DaysByYear, leaveRequestId, cancellationToken);

        var yearlyBalances = new Dictionary<int, EmployeeLeaveBalance>();
        foreach (var year in dayCalculation.DaysByYear.Keys)
        {
            yearlyBalances[year] = await EnsureBalanceRowAsync(employee.Id, leaveType.Id, year, cancellationToken);
        }

        leaveRequest.Status = LeaveRequestStatuses.Approved;
        leaveRequest.ApprovedAtUtc = DateTime.UtcNow;
        leaveRequest.DecisionRemarks = request.Remarks.Trim();
        leaveRequest.CurrentApproverUserId = NormalizeUserId(actorUserId);
        leaveRequest.UpdatedByUserId = NormalizeUserId(actorUserId);
        leaveRequest.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var (year, days) in dayCalculation.DaysByYear)
        {
            var balance = yearlyBalances[year];
            var before = balance.AvailableBalance + days;

            _dbContext.LeaveBalanceTransactions.Add(new LeaveBalanceTransaction
            {
                EmployeeId = employee.Id,
                LeaveTypeId = leaveType.Id,
                PeriodYear = year,
                LeaveRequestId = leaveRequest.Id,
                TransactionType = LeaveBalanceTransactionTypes.Usage,
                Amount = -days,
                BalanceBefore = before,
                BalanceAfter = balance.AvailableBalance,
                Remarks = $"Approved leave request from {leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd}.",
                CreatedByUserId = NormalizeUserId(actorUserId),
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var year in dayCalculation.DaysByYear.Keys)
        {
            await RecalculateBalanceAsync(employee.Id, leaveType.Id, year, cancellationToken);
        }

        await SyncAttendanceForApprovedLeaveAsync(leaveRequest, leaveType, dayCalculation, NormalizeUserId(actorUserId), cancellationToken);
        var employeeUserId = await _notificationService.GetUserIdForEmployeeAsync(employee.Id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(employeeUserId))
        {
            await _notificationService.CreateAsync(
                new NotificationDraft(
                    employeeUserId,
                    "Leave request approved",
                    $"Your leave request for {leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd} has been approved.",
                    NotificationTypes.LeaveApproved,
                    ApprovableTypes.LeaveRequest,
                    leaveRequest.Id.ToString(),
                    "/me/leave"),
                cancellationToken);
        }

        _logger.LogInformation("Leave request {LeaveRequestId} approved.", leaveRequestId);
        return await GetLeaveRequestByIdAsync(leaveRequestId, cancellationToken);
    }

    public async Task<LeaveRequestListItemDto> RejectLeaveRequestAsync(Guid leaveRequestId, LeaveActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _dbContext.LeaveRequests
            .SingleOrDefaultAsync(record => record.Id == leaveRequestId, cancellationToken)
            ?? throw new NotFoundException($"Leave request '{leaveRequestId}' was not found.");

        if (leaveRequest.Status != LeaveRequestStatuses.Pending)
        {
            throw new BadRequestException("Only pending leave requests can be rejected.");
        }

        var allocationKeys = await GetRequestYearsAsync(leaveRequest, cancellationToken);

        leaveRequest.Status = LeaveRequestStatuses.Rejected;
        leaveRequest.RejectedAtUtc = DateTime.UtcNow;
        leaveRequest.DecisionRemarks = request.Remarks.Trim();
        leaveRequest.CurrentApproverUserId = NormalizeUserId(actorUserId);
        leaveRequest.UpdatedByUserId = NormalizeUserId(actorUserId);
        leaveRequest.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var allocationKey in allocationKeys)
        {
            await RecalculateBalanceAsync(allocationKey.EmployeeId, allocationKey.LeaveTypeId, allocationKey.PeriodYear, cancellationToken);
        }

        var rejectedEmployeeUserId = await _notificationService.GetUserIdForEmployeeAsync(leaveRequest.EmployeeId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(rejectedEmployeeUserId))
        {
            await _notificationService.CreateAsync(
                new NotificationDraft(
                    rejectedEmployeeUserId,
                    "Leave request rejected",
                    string.IsNullOrWhiteSpace(request.Remarks)
                        ? "Your leave request was reviewed but not approved."
                        : $"Your leave request was rejected: {request.Remarks.Trim()}",
                    NotificationTypes.LeaveRejected,
                    ApprovableTypes.LeaveRequest,
                    leaveRequest.Id.ToString(),
                    "/me/leave"),
                cancellationToken);
        }

        _logger.LogInformation("Leave request {LeaveRequestId} rejected.", leaveRequestId);
        return await GetLeaveRequestByIdAsync(leaveRequestId, cancellationToken);
    }

    public async Task<LeaveRequestListItemDto> CancelLeaveRequestAsync(Guid leaveRequestId, LeaveActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _dbContext.LeaveRequests
            .Include(record => record.LeaveType)
            .SingleOrDefaultAsync(record => record.Id == leaveRequestId, cancellationToken)
            ?? throw new NotFoundException($"Leave request '{leaveRequestId}' was not found.");

        if (leaveRequest.Status != LeaveRequestStatuses.Pending && leaveRequest.Status != LeaveRequestStatuses.Approved)
        {
            throw new BadRequestException("Only pending or approved leave requests can be cancelled.");
        }

        var employee = await GetEmployeeForLeaveAsync(leaveRequest.EmployeeId, cancellationToken);
        var leaveType = leaveRequest.LeaveType ?? await GetLeaveTypeForRequestAsync(leaveRequest.LeaveTypeId, leaveRequest.LeaveTypeId, cancellationToken);
        var dayCalculation = await CalculateLeaveDaysAsync(
            employee.Id,
            leaveType,
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.StartDayType,
            leaveRequest.EndDayType,
            cancellationToken);

        var wasApproved = leaveRequest.Status == LeaveRequestStatuses.Approved;

        leaveRequest.Status = LeaveRequestStatuses.Cancelled;
        leaveRequest.CancelledAtUtc = DateTime.UtcNow;
        leaveRequest.DecisionRemarks = request.Remarks.Trim();
        leaveRequest.CurrentApproverUserId = NormalizeUserId(actorUserId);
        leaveRequest.UpdatedByUserId = NormalizeUserId(actorUserId);
        leaveRequest.UpdatedAtUtc = DateTime.UtcNow;

        if (wasApproved)
        {
            foreach (var (year, days) in dayCalculation.DaysByYear)
            {
                var balance = await EnsureBalanceRowAsync(employee.Id, leaveType.Id, year, cancellationToken);
                _dbContext.LeaveBalanceTransactions.Add(new LeaveBalanceTransaction
                {
                    EmployeeId = employee.Id,
                    LeaveTypeId = leaveType.Id,
                    PeriodYear = year,
                    LeaveRequestId = leaveRequest.Id,
                    TransactionType = LeaveBalanceTransactionTypes.Cancellation,
                    Amount = days,
                    BalanceBefore = balance.AvailableBalance,
                    BalanceAfter = balance.AvailableBalance + days,
                    Remarks = $"Cancelled approved leave request from {leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd}.",
                    CreatedByUserId = NormalizeUserId(actorUserId),
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var year in dayCalculation.DaysByYear.Keys)
        {
            await RecalculateBalanceAsync(employee.Id, leaveType.Id, year, cancellationToken);
        }

        if (wasApproved)
        {
            await RemoveLeaveAttendanceAsync(leaveRequest.Id, cancellationToken);
        }

        var cancelledEmployeeUserId = await _notificationService.GetUserIdForEmployeeAsync(employee.Id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cancelledEmployeeUserId))
        {
            await _notificationService.CreateAsync(
                new NotificationDraft(
                    cancelledEmployeeUserId,
                    "Leave request cancelled",
                    $"Your leave request for {leaveRequest.StartDate:yyyy-MM-dd} to {leaveRequest.EndDate:yyyy-MM-dd} was cancelled.",
                    NotificationTypes.LeaveCancelled,
                    ApprovableTypes.LeaveRequest,
                    leaveRequest.Id.ToString(),
                    "/me/leave"),
                cancellationToken);
        }

        _logger.LogInformation("Leave request {LeaveRequestId} cancelled.", leaveRequestId);
        return await GetLeaveRequestByIdAsync(leaveRequestId, cancellationToken);
    }

    public async Task DeleteLeaveRequestAsync(Guid leaveRequestId, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await _dbContext.LeaveRequests
            .Include(record => record.LeaveBalanceTransactions)
            .SingleOrDefaultAsync(record => record.Id == leaveRequestId, cancellationToken)
            ?? throw new NotFoundException($"Leave request '{leaveRequestId}' was not found.");

        if (leaveRequest.Status == LeaveRequestStatuses.Approved || leaveRequest.LeaveBalanceTransactions.Count > 0)
        {
            throw new BadRequestException("Approved or historically posted leave requests cannot be deleted. Cancel them instead.");
        }

        var allocationKeys = await GetRequestYearsAsync(leaveRequest, cancellationToken);
        var storagePath = leaveRequest.AttachmentPath;

        _dbContext.LeaveRequests.Remove(leaveRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _attachmentStorageService.DeleteAsync(storagePath, cancellationToken);

        foreach (var allocationKey in allocationKeys)
        {
            await RecalculateBalanceAsync(allocationKey.EmployeeId, allocationKey.LeaveTypeId, allocationKey.PeriodYear, cancellationToken);
        }

        _logger.LogInformation("Leave request {LeaveRequestId} deleted.", leaveRequestId);
    }

    public async Task<LeaveCalendarResponseDto> GetCalendarAsync(LeaveCalendarQueryDto query, CancellationToken cancellationToken = default)
    {
        var calendarStart = new DateOnly(query.Year, query.Month, 1);
        var calendarEnd = calendarStart.AddMonths(1).AddDays(-1);
        var source = _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.LeaveType)
            .Where(record => record.EndDate >= calendarStart && record.StartDate <= calendarEnd);

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
        else
        {
            source = source.Where(record => record.Status == LeaveRequestStatuses.Approved || record.Status == LeaveRequestStatuses.Pending);
        }

        var entries = await source
            .OrderBy(record => record.StartDate)
            .ThenBy(record => record.Employee!.LastName)
            .Select(record => new LeaveCalendarEntryDto
            {
                Id = record.Id,
                EmployeeId = record.EmployeeId,
                EmployeeCode = record.Employee!.EmployeeCode,
                EmployeeFullName = BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
                DepartmentName = record.Employee.Department != null ? record.Employee.Department.Name : string.Empty,
                BranchName = record.Employee.Branch != null ? record.Employee.Branch.Name : string.Empty,
                LeaveTypeName = record.LeaveType!.Name,
                LeaveTypeIsPaid = record.LeaveType.IsPaid,
                StartDate = record.StartDate,
                EndDate = record.EndDate,
                TotalLeaveDays = record.TotalLeaveDays,
                Status = record.Status
            })
            .ToListAsync(cancellationToken);

        return new LeaveCalendarResponseDto
        {
            Year = query.Year,
            Month = query.Month,
            Entries = entries
        };
    }

    public async Task<EmployeeLeaveProfileDto> GetEmployeeLeaveProfileAsync(Guid employeeId, int? periodYear, CancellationToken cancellationToken = default)
    {
        var employee = await _dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(record => record.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Employee '{employeeId}' was not found.");

        var targetYear = periodYear ?? _attendanceCalculationService.GetBusinessToday().Year;
        await EnsureBalancesForEmployeeAsync(employeeId, targetYear, cancellationToken);

        var balances = await _dbContext.EmployeeLeaveBalances
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId && record.PeriodYear == targetYear)
            .OrderBy(record => record.LeaveType!.Name)
            .Select(record => new EmployeeLeaveBalanceDto
            {
                Id = record.Id,
                EmployeeId = record.EmployeeId,
                EmployeeCode = record.Employee!.EmployeeCode,
                EmployeeFullName = BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
                DepartmentName = record.Employee.Department != null ? record.Employee.Department.Name : string.Empty,
                BranchName = record.Employee.Branch != null ? record.Employee.Branch.Name : string.Empty,
                LeaveTypeId = record.LeaveTypeId,
                LeaveTypeCode = record.LeaveType!.Code,
                LeaveTypeName = record.LeaveType.Name,
                LeaveTypeIsPaid = record.LeaveType.IsPaid,
                PeriodYear = record.PeriodYear,
                OpeningBalance = record.OpeningBalance,
                Accrued = record.Accrued,
                Used = record.Used,
                Pending = record.Pending,
                Adjusted = record.Adjusted,
                CarriedForward = record.CarriedForward,
                AvailableBalance = record.AvailableBalance,
                IsLowBalance = record.AvailableBalance >= 0m && record.AvailableBalance <= _options.LowBalanceThreshold,
                IsNegativeBalance = record.AvailableBalance < 0m,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var pendingRequests = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId && record.Status == LeaveRequestStatuses.Pending)
            .OrderBy(record => record.StartDate)
            .ToListAsync(cancellationToken);

        var historyRequests = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(record =>
                record.EmployeeId == employeeId &&
                record.Status != LeaveRequestStatuses.Pending)
            .OrderByDescending(record => record.StartDate)
            .Take(10)
            .ToListAsync(cancellationToken);

        var ledger = await _dbContext.LeaveBalanceTransactions
            .AsNoTracking()
            .Include(record => record.CreatedByUser)
            .Where(record => record.EmployeeId == employeeId && record.PeriodYear == targetYear)
            .OrderByDescending(record => record.CreatedAtUtc)
            .Take(10)
            .Select(record => new LeaveBalanceTransactionDto
            {
                Id = record.Id,
                EmployeeId = record.EmployeeId,
                LeaveTypeId = record.LeaveTypeId,
                PeriodYear = record.PeriodYear,
                LeaveRequestId = record.LeaveRequestId,
                TransactionType = record.TransactionType,
                Amount = record.Amount,
                BalanceBefore = record.BalanceBefore,
                BalanceAfter = record.BalanceAfter,
                Remarks = record.Remarks,
                CreatedByDisplayName = BuildUserDisplayName(record.CreatedByUser),
                CreatedAtUtc = record.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new EmployeeLeaveProfileDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            Summary = new EmployeeLeaveProfileSummaryDto
            {
                PendingRequestCount = pendingRequests.Count,
                ApprovedRequestCount = historyRequests.Count(record => record.Status == LeaveRequestStatuses.Approved),
                RejectedOrCancelledRequestCount = historyRequests.Count(record => record.Status == LeaveRequestStatuses.Rejected || record.Status == LeaveRequestStatuses.Cancelled),
                LowBalanceCount = balances.Count(record => record.IsLowBalance),
                NegativeBalanceCount = balances.Count(record => record.IsNegativeBalance)
            },
            Balances = balances,
            PendingRequests = await MapLeaveRequestsAsync(pendingRequests, cancellationToken),
            History = await MapLeaveRequestsAsync(historyRequests, cancellationToken),
            Ledger = ledger
        };
    }

    public async Task<StreamedLeaveAttachmentFile> GetAttachmentAsync(Guid leaveRequestId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(item => item.Id == leaveRequestId)
            .Select(item => new
            {
                item.Id,
                item.EmployeeId,
                item.Reason,
                item.AttachmentOriginalFileName,
                item.AttachmentPath,
                item.AttachmentMimeType
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Leave request '{leaveRequestId}' was not found.");

        if (string.IsNullOrWhiteSpace(record.AttachmentPath))
        {
            throw new NotFoundException("This leave request does not have an attachment.");
        }

        await _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Action = "download",
                EntityType = AuditEntityTypes.LeaveRequest,
                EntityId = record.Id.ToString(),
                EmployeeId = record.EmployeeId,
                Remarks = string.IsNullOrWhiteSpace(record.Reason)
                    ? "Downloaded leave request attachment."
                    : $"Downloaded leave request attachment for reason '{record.Reason}'."
            },
            cancellationToken);

        return await _attachmentStorageService.OpenReadAsync(
            record.AttachmentPath,
            string.IsNullOrWhiteSpace(record.AttachmentOriginalFileName) ? "leave-attachment" : record.AttachmentOriginalFileName,
            record.AttachmentMimeType,
            cancellationToken);
    }

    private async Task<Employee> GetEmployeeForLeaveAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .Include(record => record.Department)
            .Include(record => record.Branch)
            .Include(record => record.EmploymentType)
            .Include(record => record.EmploymentStatus)
            .SingleOrDefaultAsync(record => record.Id == employeeId, cancellationToken)
            ?? throw new BadRequestException("The selected employee does not exist.");

        if (!employee.IsActive)
        {
            throw new BadRequestException("Leave requests cannot be filed for inactive employees.");
        }

        return employee;
    }

    private async Task<LeaveType> GetLeaveTypeForRequestAsync(Guid leaveTypeId, Guid? currentLeaveTypeId, CancellationToken cancellationToken)
    {
        var leaveType = await _dbContext.LeaveTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(record => record.Id == leaveTypeId, cancellationToken)
            ?? throw BuildValidationException("The selected leave type does not exist.", nameof(SaveLeaveRequestDto.LeaveTypeId));

        if (!leaveType.IsActive && leaveType.Id != currentLeaveTypeId)
        {
            throw BuildValidationException("The selected leave type is inactive.", nameof(SaveLeaveRequestDto.LeaveTypeId));
        }

        return leaveType;
    }

    private void ValidateLeaveTypePolicy(Employee employee, LeaveType leaveType, DateOnly startDate, string reason, bool hasAttachment)
    {
        if (leaveType.RequiresReason && string.IsNullOrWhiteSpace(reason))
        {
            throw BuildValidationException("This leave type requires a reason.", nameof(SaveLeaveRequestDto.Reason));
        }

        if (leaveType.RequiresAttachment && !hasAttachment)
        {
            throw BuildValidationException("This leave type requires an attachment.", nameof(SaveLeaveRequestDto.Attachment));
        }

        if (!string.IsNullOrWhiteSpace(leaveType.GenderRestriction) &&
            !string.Equals(leaveType.GenderRestriction, employee.Gender, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException($"The leave type '{leaveType.Name}' is restricted to {leaveType.GenderRestriction} employees.");
        }

        var employmentTypeRestrictionIds = LeaveTypeService.ParseEmploymentTypeRestrictions(leaveType.EmploymentTypeRestrictions);
        if (employmentTypeRestrictionIds.Count > 0 &&
            (employee.EmploymentTypeId is null || !employmentTypeRestrictionIds.Contains(employee.EmploymentTypeId.Value)))
        {
            throw new BadRequestException($"The leave type '{leaveType.Name}' is not available for the employee's employment type.");
        }

        if (!leaveType.AllowDuringProbationaryPeriod && IsProbationary(employee))
        {
            throw new BadRequestException($"The leave type '{leaveType.Name}' is not available during probationary status.");
        }

        if (leaveType.MinDaysBeforeFiling is not null)
        {
            var businessToday = _attendanceCalculationService.GetBusinessToday();
            if (startDate >= businessToday)
            {
                var daysBeforeFiling = startDate.DayNumber - businessToday.DayNumber;
                if (daysBeforeFiling < leaveType.MinDaysBeforeFiling.Value)
                {
                    throw new BadRequestException($"This leave type requires filing at least {leaveType.MinDaysBeforeFiling.Value} day(s) in advance.");
                }
            }
        }
    }

    private static bool IsProbationary(Employee employee)
    {
        return string.Equals(employee.EmploymentStatus?.Code, "PROB", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(employee.EmploymentType?.Code, "PROB", StringComparison.OrdinalIgnoreCase) ||
               employee.EmploymentStatus?.Name.Contains("probation", StringComparison.OrdinalIgnoreCase) == true ||
               employee.EmploymentType?.Name.Contains("probation", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static void ValidateDayTypeRequest(LeaveType leaveType, DateOnly startDate, DateOnly endDate, string startDayType, string endDayType)
    {
        var normalizedStartDayType = startDayType.Trim().ToLowerInvariant();
        var normalizedEndDayType = endDayType.Trim().ToLowerInvariant();

        if (!LeaveDayTypes.All.Contains(normalizedStartDayType, StringComparer.OrdinalIgnoreCase))
        {
            throw BuildValidationException("The leave start day type is invalid.", nameof(SaveLeaveRequestDto.StartDayType));
        }

        if (!LeaveDayTypes.All.Contains(normalizedEndDayType, StringComparer.OrdinalIgnoreCase))
        {
            throw BuildValidationException("The leave end day type is invalid.", nameof(SaveLeaveRequestDto.EndDayType));
        }

        var usesHalfDay = normalizedStartDayType != LeaveDayTypes.FullDay || normalizedEndDayType != LeaveDayTypes.FullDay;
        if (usesHalfDay && !leaveType.AllowHalfDay)
        {
            throw new BadRequestException($"The leave type '{leaveType.Name}' does not allow half-day requests.");
        }

        if (startDate == endDate)
        {
            var validSameDayCombination =
                (normalizedStartDayType == LeaveDayTypes.FullDay && normalizedEndDayType == LeaveDayTypes.FullDay) ||
                (normalizedStartDayType == LeaveDayTypes.FirstHalf && normalizedEndDayType == LeaveDayTypes.FirstHalf) ||
                (normalizedStartDayType == LeaveDayTypes.SecondHalf && normalizedEndDayType == LeaveDayTypes.SecondHalf) ||
                (normalizedStartDayType == LeaveDayTypes.FirstHalf && normalizedEndDayType == LeaveDayTypes.SecondHalf);

            if (!validSameDayCombination)
            {
                throw new BadRequestException("The selected same-day half-day combination is invalid.");
            }
        }
    }

    private static void ValidateMaxDaysPerRequest(LeaveType leaveType, decimal totalLeaveDays)
    {
        if (leaveType.MaxDaysPerRequest is not null && totalLeaveDays > leaveType.MaxDaysPerRequest.Value)
        {
            throw new BadRequestException($"This leave type cannot exceed {leaveType.MaxDaysPerRequest.Value:0.##} day(s) per request.");
        }
    }

    private async Task EnsureNoOverlapAsync(Guid employeeId, DateOnly startDate, DateOnly endDate, Guid? existingLeaveRequestId, CancellationToken cancellationToken)
    {
        var overlapExists = await _dbContext.LeaveRequests.AnyAsync(
            record =>
                record.EmployeeId == employeeId &&
                record.Id != existingLeaveRequestId &&
                (record.Status == LeaveRequestStatuses.Pending || record.Status == LeaveRequestStatuses.Approved) &&
                record.StartDate <= endDate &&
                record.EndDate >= startDate,
            cancellationToken);

        if (overlapExists)
        {
            throw new ConflictException("This leave request overlaps an existing pending or approved leave.");
        }
    }

    private async Task EnsureSufficientBalanceAsync(
        Guid employeeId,
        LeaveType leaveType,
        IReadOnlyDictionary<int, decimal> daysByYear,
        Guid? existingLeaveRequestId,
        CancellationToken cancellationToken)
    {
        if (leaveType.AllowNegativeBalance)
        {
            return;
        }

        foreach (var (year, requestedDays) in daysByYear)
        {
            var balance = await EnsureBalanceRowAsync(employeeId, leaveType.Id, year, cancellationToken);
            var currentPendingContribution = existingLeaveRequestId is null
                ? 0m
                : await GetPendingContributionForRequestYearAsync(existingLeaveRequestId.Value, year, cancellationToken);

            var availableAfterRequest = balance.AvailableBalance + currentPendingContribution - requestedDays;
            if (availableAfterRequest < 0m)
            {
                throw new BadRequestException($"Insufficient leave balance for {leaveType.Name} in {year}.");
            }
        }
    }

    private async Task EnsureBalanceStillApprovableAsync(
        Guid employeeId,
        LeaveType leaveType,
        IReadOnlyDictionary<int, decimal> daysByYear,
        Guid leaveRequestId,
        CancellationToken cancellationToken)
    {
        if (leaveType.AllowNegativeBalance)
        {
            return;
        }

        foreach (var (year, requestedDays) in daysByYear)
        {
            var balance = await EnsureBalanceRowAsync(employeeId, leaveType.Id, year, cancellationToken);
            var currentPendingContribution = await GetPendingContributionForRequestYearAsync(leaveRequestId, year, cancellationToken);
            var availableAfterApproval = balance.AvailableBalance + currentPendingContribution;

            if (availableAfterApproval < 0m)
            {
                throw new BadRequestException($"Approving this request would exceed the available leave balance for {leaveType.Name} in {year}.");
            }

            _ = requestedDays;
        }
    }

    private async Task<decimal> GetPendingContributionForRequestYearAsync(Guid leaveRequestId, int periodYear, CancellationToken cancellationToken)
    {
        var request = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(record => record.LeaveType)
            .SingleOrDefaultAsync(record => record.Id == leaveRequestId, cancellationToken);

        if (request is null || request.Status != LeaveRequestStatuses.Pending)
        {
            return 0m;
        }

        var leaveType = request.LeaveType ?? await GetLeaveTypeForRequestAsync(request.LeaveTypeId, request.LeaveTypeId, cancellationToken);
        var dayCalculation = await CalculateLeaveDaysAsync(
            request.EmployeeId,
            leaveType,
            request.StartDate,
            request.EndDate,
            request.StartDayType,
            request.EndDayType,
            cancellationToken);

        return dayCalculation.DaysByYear.GetValueOrDefault(periodYear);
    }

    private async Task EnsureBalancesForYearAsync(int periodYear, CancellationToken cancellationToken)
    {
        var employeeIds = await _dbContext.Employees
            .AsNoTracking()
            .Where(record => record.IsActive)
            .Select(record => record.Id)
            .ToListAsync(cancellationToken);

        var leaveTypes = await _dbContext.LeaveTypes
            .AsNoTracking()
            .Where(record => record.IsActive)
            .Select(record => new { record.Id, record.DefaultAnnualCredits })
            .ToListAsync(cancellationToken);

        if (employeeIds.Count == 0 || leaveTypes.Count == 0)
        {
            return;
        }

        var existingKeys = await _dbContext.EmployeeLeaveBalances
            .AsNoTracking()
            .Where(record => record.PeriodYear == periodYear)
            .Select(record => new { record.EmployeeId, record.LeaveTypeId })
            .ToListAsync(cancellationToken);

        var keySet = existingKeys
            .Select(record => (record.EmployeeId, record.LeaveTypeId))
            .ToHashSet();

        var changes = false;
        foreach (var employeeId in employeeIds)
        {
            foreach (var leaveType in leaveTypes)
            {
                if (keySet.Contains((employeeId, leaveType.Id)))
                {
                    continue;
                }

                changes = true;
                var openingBalance = leaveType.DefaultAnnualCredits ?? 0m;
                var balance = new EmployeeLeaveBalance
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = leaveType.Id,
                    PeriodYear = periodYear,
                    OpeningBalance = openingBalance,
                    AvailableBalance = openingBalance,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _dbContext.EmployeeLeaveBalances.Add(balance);

                if (openingBalance > 0m)
                {
                    _dbContext.LeaveBalanceTransactions.Add(new LeaveBalanceTransaction
                    {
                        EmployeeId = employeeId,
                        LeaveTypeId = leaveType.Id,
                        PeriodYear = periodYear,
                        TransactionType = LeaveBalanceTransactionTypes.Grant,
                        Amount = openingBalance,
                        BalanceBefore = 0m,
                        BalanceAfter = openingBalance,
                        Remarks = $"Initial annual leave grant for {periodYear}.",
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }
        }

        if (changes)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureBalancesForEmployeeAsync(Guid employeeId, int periodYear, CancellationToken cancellationToken)
    {
        var leaveTypes = await _dbContext.LeaveTypes
            .AsNoTracking()
            .Where(record => record.IsActive)
            .Select(record => new { record.Id, record.DefaultAnnualCredits })
            .ToListAsync(cancellationToken);

        var existingLeaveTypeIds = await _dbContext.EmployeeLeaveBalances
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId && record.PeriodYear == periodYear)
            .Select(record => record.LeaveTypeId)
            .ToListAsync(cancellationToken);

        var existingSet = existingLeaveTypeIds.ToHashSet();
        var changes = false;

        foreach (var leaveType in leaveTypes)
        {
            if (existingSet.Contains(leaveType.Id))
            {
                continue;
            }

            changes = true;
            var openingBalance = leaveType.DefaultAnnualCredits ?? 0m;
            var balance = new EmployeeLeaveBalance
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveType.Id,
                PeriodYear = periodYear,
                OpeningBalance = openingBalance,
                AvailableBalance = openingBalance,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.EmployeeLeaveBalances.Add(balance);

            if (openingBalance > 0m)
            {
                _dbContext.LeaveBalanceTransactions.Add(new LeaveBalanceTransaction
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = leaveType.Id,
                    PeriodYear = periodYear,
                    TransactionType = LeaveBalanceTransactionTypes.Grant,
                    Amount = openingBalance,
                    BalanceBefore = 0m,
                    BalanceAfter = openingBalance,
                    Remarks = $"Initial annual leave grant for {periodYear}.",
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        if (changes)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<EmployeeLeaveBalance> EnsureBalanceRowAsync(Guid employeeId, Guid leaveTypeId, int periodYear, CancellationToken cancellationToken)
    {
        var record = await _dbContext.EmployeeLeaveBalances
            .SingleOrDefaultAsync(
                item => item.EmployeeId == employeeId &&
                        item.LeaveTypeId == leaveTypeId &&
                        item.PeriodYear == periodYear,
                cancellationToken);

        if (record is not null)
        {
            return record;
        }

        var leaveType = await _dbContext.LeaveTypes
            .AsNoTracking()
            .SingleAsync(record => record.Id == leaveTypeId, cancellationToken);

        record = new EmployeeLeaveBalance
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveTypeId,
            PeriodYear = periodYear,
            OpeningBalance = leaveType.DefaultAnnualCredits ?? 0m,
            AvailableBalance = leaveType.DefaultAnnualCredits ?? 0m,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.EmployeeLeaveBalances.Add(record);

        if (record.OpeningBalance > 0m)
        {
            _dbContext.LeaveBalanceTransactions.Add(new LeaveBalanceTransaction
            {
                EmployeeId = employeeId,
                LeaveTypeId = leaveTypeId,
                PeriodYear = periodYear,
                TransactionType = LeaveBalanceTransactionTypes.Grant,
                Amount = record.OpeningBalance,
                BalanceBefore = 0m,
                BalanceAfter = record.OpeningBalance,
                Remarks = $"Initial annual leave grant for {periodYear}.",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    private async Task RecalculateBalanceAsync(Guid employeeId, Guid leaveTypeId, int periodYear, CancellationToken cancellationToken)
    {
        var balance = await EnsureBalanceRowAsync(employeeId, leaveTypeId, periodYear, cancellationToken);

        var approvedRequests = await GetRequestsForBalanceYearAsync(employeeId, leaveTypeId, periodYear, LeaveRequestStatuses.Approved, cancellationToken);
        var pendingRequests = await GetRequestsForBalanceYearAsync(employeeId, leaveTypeId, periodYear, LeaveRequestStatuses.Pending, cancellationToken);

        var used = 0m;
        foreach (var request in approvedRequests)
        {
            var leaveType = request.LeaveType ?? await GetLeaveTypeForRequestAsync(request.LeaveTypeId, request.LeaveTypeId, cancellationToken);
            var allocation = await CalculateLeaveDaysAsync(
                request.EmployeeId,
                leaveType,
                request.StartDate,
                request.EndDate,
                request.StartDayType,
                request.EndDayType,
                cancellationToken);

            used += allocation.DaysByYear.GetValueOrDefault(periodYear);
        }

        var pending = 0m;
        foreach (var request in pendingRequests)
        {
            var leaveType = request.LeaveType ?? await GetLeaveTypeForRequestAsync(request.LeaveTypeId, request.LeaveTypeId, cancellationToken);
            var allocation = await CalculateLeaveDaysAsync(
                request.EmployeeId,
                leaveType,
                request.StartDate,
                request.EndDate,
                request.StartDayType,
                request.EndDayType,
                cancellationToken);

            pending += allocation.DaysByYear.GetValueOrDefault(periodYear);
        }

        balance.Used = used;
        balance.Pending = pending;
        balance.AvailableBalance = balance.OpeningBalance + balance.Accrued + balance.Adjusted + balance.CarriedForward - used - pending;
        balance.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<LeaveRequest>> GetRequestsForBalanceYearAsync(Guid employeeId, Guid leaveTypeId, int periodYear, string status, CancellationToken cancellationToken)
    {
        var dateFrom = new DateOnly(periodYear, 1, 1);
        var dateTo = new DateOnly(periodYear, 12, 31);

        return await _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(record => record.LeaveType)
            .Where(record =>
                record.EmployeeId == employeeId &&
                record.LeaveTypeId == leaveTypeId &&
                record.Status == status &&
                record.StartDate <= dateTo &&
                record.EndDate >= dateFrom)
            .ToListAsync(cancellationToken);
    }

    private async Task<LeaveDayCalculationResult> CalculateLeaveDaysAsync(
        Guid employeeId,
        LeaveType leaveType,
        DateOnly startDate,
        DateOnly endDate,
        string startDayType,
        string endDayType,
        CancellationToken cancellationToken)
    {
        var assignments = await _dbContext.EmployeeScheduleAssignments
            .AsNoTracking()
            .Include(record => record.WorkSchedule)
            .Include(record => record.Shift)
            .Where(record =>
                record.EmployeeId == employeeId &&
                record.EffectiveStartDate <= endDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= startDate))
            .ToListAsync(cancellationToken);

        var totalDays = 0m;
        var daysByYear = new Dictionary<int, decimal>();
        var countedDates = new List<CountedLeaveDate>();

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var resolvedSchedule = _attendanceCalculationService.ResolveSchedule(assignments, date);
            if (!leaveType.CountsRestDays && resolvedSchedule.HasScheduleAssignment && resolvedSchedule.IsRestDay)
            {
                continue;
            }

            var fraction = GetLeaveDayFraction(date, startDate, endDate, startDayType, endDayType);
            if (fraction <= 0m)
            {
                continue;
            }

            totalDays += fraction;
            countedDates.Add(new CountedLeaveDate(date, fraction, resolvedSchedule));
            daysByYear[date.Year] = daysByYear.GetValueOrDefault(date.Year) + fraction;
        }

        return new LeaveDayCalculationResult(totalDays, daysByYear, countedDates);
    }

    private static decimal GetLeaveDayFraction(DateOnly date, DateOnly startDate, DateOnly endDate, string startDayType, string endDayType)
    {
        var normalizedStart = startDayType.Trim().ToLowerInvariant();
        var normalizedEnd = endDayType.Trim().ToLowerInvariant();

        if (startDate == endDate)
        {
            return (normalizedStart, normalizedEnd) switch
            {
                (LeaveDayTypes.FullDay, LeaveDayTypes.FullDay) => 1m,
                (LeaveDayTypes.FirstHalf, LeaveDayTypes.FirstHalf) => 0.5m,
                (LeaveDayTypes.SecondHalf, LeaveDayTypes.SecondHalf) => 0.5m,
                (LeaveDayTypes.FirstHalf, LeaveDayTypes.SecondHalf) => 1m,
                _ => 0m
            };
        }

        if (date == startDate)
        {
            return normalizedStart == LeaveDayTypes.FullDay ? 1m : 0.5m;
        }

        if (date == endDate)
        {
            return normalizedEnd == LeaveDayTypes.FullDay ? 1m : 0.5m;
        }

        return 1m;
    }

    private async Task<LeaveRequest> LoadLeaveRequestAsync(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        return await _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.LeaveType)
            .Include(record => record.CurrentApproverUser)
            .Include(record => record.CreatedByUser)
            .Include(record => record.UpdatedByUser)
            .SingleOrDefaultAsync(record => record.Id == leaveRequestId, cancellationToken)
            ?? throw new NotFoundException($"Leave request '{leaveRequestId}' was not found.");
    }

    private async Task<List<LeaveRequestListItemDto>> MapLeaveRequestsAsync(IEnumerable<LeaveRequest> records, CancellationToken cancellationToken)
    {
        var items = new List<LeaveRequestListItemDto>();
        foreach (var record in records)
        {
            items.Add(await GetLeaveRequestByIdAsync(record.Id, cancellationToken));
        }

        return items;
    }

    private async Task<LeaveRequestListItemDto> MapLeaveRequestAsync(LeaveRequest record, CancellationToken cancellationToken)
    {
        var employee = record.Employee ?? await _dbContext.Employees
            .AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.Branch)
            .SingleAsync(item => item.Id == record.EmployeeId, cancellationToken);

        var leaveType = record.LeaveType ?? await _dbContext.LeaveTypes
            .AsNoTracking()
            .SingleAsync(item => item.Id == record.LeaveTypeId, cancellationToken);

        var dayCalculation = await CalculateLeaveDaysAsync(
            record.EmployeeId,
            leaveType,
            record.StartDate,
            record.EndDate,
            record.StartDayType,
            record.EndDayType,
            cancellationToken);

        decimal availableAfterApproval = 0m;
        foreach (var year in dayCalculation.DaysByYear.Keys)
        {
            var balance = await EnsureBalanceRowAsync(record.EmployeeId, record.LeaveTypeId, year, cancellationToken);
            var pendingContribution = record.Status == LeaveRequestStatuses.Pending
                ? dayCalculation.DaysByYear[year]
                : 0m;

            availableAfterApproval += balance.AvailableBalance + pendingContribution;
        }

        var conflictCount = await CountAttendanceConflictsAsync(record.Id, record.EmployeeId, record.StartDate, record.EndDate, cancellationToken);

        return new LeaveRequestListItemDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            DepartmentName = employee.Department?.Name ?? string.Empty,
            BranchName = employee.Branch?.Name ?? string.Empty,
            LeaveTypeId = record.LeaveTypeId,
            LeaveTypeCode = leaveType.Code,
            LeaveTypeName = leaveType.Name,
            LeaveTypeIsPaid = leaveType.IsPaid,
            StartDate = record.StartDate,
            EndDate = record.EndDate,
            StartDayType = record.StartDayType,
            EndDayType = record.EndDayType,
            TotalLeaveDays = record.TotalLeaveDays,
            Reason = record.Reason,
            Status = record.Status,
            SubmittedAtUtc = record.SubmittedAtUtc,
            ApprovedAtUtc = record.ApprovedAtUtc,
            RejectedAtUtc = record.RejectedAtUtc,
            CancelledAtUtc = record.CancelledAtUtc,
            CurrentApproverDisplayName = BuildUserDisplayName(record.CurrentApproverUser),
            DecisionRemarks = record.DecisionRemarks,
            HasAttachment = !string.IsNullOrWhiteSpace(record.AttachmentPath),
            AttachmentOriginalFileName = record.AttachmentOriginalFileName,
            AttachmentFileSize = record.AttachmentFileSize,
            HasAttendanceConflict = conflictCount > 0,
            AttendanceConflictCount = conflictCount,
            AvailableBalanceAfterApproval = availableAfterApproval,
            CreatedByDisplayName = BuildUserDisplayName(record.CreatedByUser),
            UpdatedByDisplayName = BuildUserDisplayName(record.UpdatedByUser),
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private async Task<int> CountAttendanceConflictsAsync(Guid leaveRequestId, Guid employeeId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken)
    {
        return await _dbContext.AttendanceRecords
            .AsNoTracking()
            .CountAsync(
                record =>
                    record.EmployeeId == employeeId &&
                    record.AttendanceDate >= startDate &&
                    record.AttendanceDate <= endDate &&
                    !(record.Source == AttendanceSources.Leave && record.LeaveRequestId == leaveRequestId),
                cancellationToken);
    }

    private async Task SyncAttendanceForApprovedLeaveAsync(
        LeaveRequest leaveRequest,
        LeaveType leaveType,
        LeaveDayCalculationResult dayCalculation,
        string? actorUserId,
        CancellationToken cancellationToken)
    {
        foreach (var countedDate in dayCalculation.CountedDates)
        {
            if (countedDate.ResolvedSchedule.HasScheduleAssignment && countedDate.ResolvedSchedule.IsRestDay)
            {
                continue;
            }

            var existingRecord = await _dbContext.AttendanceRecords
                .SingleOrDefaultAsync(
                    record => record.EmployeeId == leaveRequest.EmployeeId && record.AttendanceDate == countedDate.Date,
                    cancellationToken);

            if (existingRecord is not null)
            {
                if (existingRecord.Source == AttendanceSources.Leave && existingRecord.LeaveRequestId == leaveRequest.Id)
                {
                    existingRecord.Status = AttendanceStatuses.OnLeave;
                    existingRecord.Source = AttendanceSources.Leave;
                    existingRecord.Remarks = BuildLeaveAttendanceRemark(leaveType.Name, leaveRequest.TotalLeaveDays);
                    existingRecord.UpdatedByUserId = actorUserId;
                    existingRecord.UpdatedAtUtc = DateTime.UtcNow;
                }

                continue;
            }

            _dbContext.AttendanceRecords.Add(new AttendanceRecord
            {
                EmployeeId = leaveRequest.EmployeeId,
                AttendanceDate = countedDate.Date,
                ScheduledStartTime = countedDate.ResolvedSchedule.ScheduledStartTime,
                ScheduledEndTime = countedDate.ResolvedSchedule.ScheduledEndTime,
                ActualTimeIn = null,
                ActualTimeOut = null,
                BreakStartTime = null,
                BreakEndTime = null,
                TotalWorkedMinutes = 0,
                LateMinutes = 0,
                UndertimeMinutes = 0,
                OvertimeMinutes = 0,
                Status = AttendanceStatuses.OnLeave,
                Source = AttendanceSources.Leave,
                Remarks = BuildLeaveAttendanceRemark(leaveType.Name, leaveRequest.TotalLeaveDays),
                LeaveRequestId = leaveRequest.Id,
                CreatedByUserId = actorUserId,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RemoveLeaveAttendanceAsync(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        var leaveRecords = await _dbContext.AttendanceRecords
            .Where(record =>
                record.LeaveRequestId == leaveRequestId &&
                record.Source == AttendanceSources.Leave &&
                record.ActualTimeIn == null &&
                record.ActualTimeOut == null)
            .ToListAsync(cancellationToken);

        if (leaveRecords.Count == 0)
        {
            return;
        }

        _dbContext.AttendanceRecords.RemoveRange(leaveRecords);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> ResolveDefaultApproverUserIdAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .Where(record => record.Id == employeeId)
            .Select(record => record.Manager != null ? record.Manager.UserId : null)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<HashSet<RequestAllocationKey>> GetRequestYearsAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken)
    {
        var leaveType = leaveRequest.LeaveType ?? await GetLeaveTypeForRequestAsync(leaveRequest.LeaveTypeId, leaveRequest.LeaveTypeId, cancellationToken);
        var allocation = await CalculateLeaveDaysAsync(
            leaveRequest.EmployeeId,
            leaveType,
            leaveRequest.StartDate,
            leaveRequest.EndDate,
            leaveRequest.StartDayType,
            leaveRequest.EndDayType,
            cancellationToken);

        return allocation.DaysByYear.Keys
            .Select(year => new RequestAllocationKey(leaveRequest.EmployeeId, leaveRequest.LeaveTypeId, year))
            .ToHashSet();
    }

    private async Task<LeaveBalanceTransactionDto> GetLeaveBalanceTransactionByIdAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.LeaveBalanceTransactions
            .AsNoTracking()
            .Include(item => item.CreatedByUser)
            .Where(item => item.Id == transactionId)
            .Select(item => new LeaveBalanceTransactionDto
            {
                Id = item.Id,
                EmployeeId = item.EmployeeId,
                LeaveTypeId = item.LeaveTypeId,
                PeriodYear = item.PeriodYear,
                LeaveRequestId = item.LeaveRequestId,
                TransactionType = item.TransactionType,
                Amount = item.Amount,
                BalanceBefore = item.BalanceBefore,
                BalanceAfter = item.BalanceAfter,
                Remarks = item.Remarks,
                CreatedByDisplayName = item.CreatedByUser != null
                    ? (!string.IsNullOrWhiteSpace(item.CreatedByUser.DisplayName) ? item.CreatedByUser.DisplayName : item.CreatedByUser.Email ?? string.Empty)
                    : string.Empty,
                CreatedAtUtc = item.CreatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Leave balance transaction '{transactionId}' was not found.");

        return record;
    }

    private EmployeeLeaveBalanceDto MapBalance(EmployeeLeaveBalance record)
    {
        var employee = record.Employee ?? throw new NotFoundException("The employee linked to this balance could not be found.");
        var leaveType = record.LeaveType ?? throw new NotFoundException("The leave type linked to this balance could not be found.");

        return new EmployeeLeaveBalanceDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            DepartmentName = employee.Department?.Name ?? string.Empty,
            BranchName = employee.Branch?.Name ?? string.Empty,
            LeaveTypeId = record.LeaveTypeId,
            LeaveTypeCode = leaveType.Code,
            LeaveTypeName = leaveType.Name,
            LeaveTypeIsPaid = leaveType.IsPaid,
            PeriodYear = record.PeriodYear,
            OpeningBalance = record.OpeningBalance,
            Accrued = record.Accrued,
            Used = record.Used,
            Pending = record.Pending,
            Adjusted = record.Adjusted,
            CarriedForward = record.CarriedForward,
            AvailableBalance = record.AvailableBalance,
            IsLowBalance = record.AvailableBalance >= 0m && record.AvailableBalance <= _options.LowBalanceThreshold,
            IsNegativeBalance = record.AvailableBalance < 0m,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static void EnsureMutableStatus(string status)
    {
        if (status != LeaveRequestStatuses.Draft && status != LeaveRequestStatuses.Pending)
        {
            throw new BadRequestException("Only draft or pending leave requests can be edited.");
        }
    }

    private static void ApplyStoredAttachment(LeaveRequest leaveRequest, StoredLeaveAttachmentFile storedFile)
    {
        leaveRequest.AttachmentOriginalFileName = storedFile.OriginalFileName;
        leaveRequest.AttachmentPath = storedFile.StoragePath;
        leaveRequest.AttachmentFileSize = storedFile.FileSize;
        leaveRequest.AttachmentMimeType = storedFile.MimeType;
    }

    private static string BuildLeaveAttendanceRemark(string leaveTypeName, decimal totalLeaveDays)
    {
        return $"Approved leave: {leaveTypeName} ({totalLeaveDays:0.##} day(s)).";
    }

    private static string BuildFullName(string firstName, string middleName, string lastName, string suffix)
    {
        return string.Join(
            " ",
            new[]
            {
                firstName.Trim(),
                middleName.Trim(),
                lastName.Trim(),
                suffix.Trim()
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string BuildUserDisplayName(ApplicationUser? user)
    {
        return user is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.Email ?? string.Empty
                : user.DisplayName;
    }

    private static string? NormalizeUserId(string? userId)
    {
        return string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
    }

    private static BadRequestException BuildValidationException(string message, string fieldName)
    {
        return new BadRequestException(message, new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }

    private sealed record LeaveDayCalculationResult(
        decimal TotalDays,
        IReadOnlyDictionary<int, decimal> DaysByYear,
        IReadOnlyList<CountedLeaveDate> CountedDates);

    private sealed record CountedLeaveDate(
        DateOnly Date,
        decimal Fraction,
        ResolvedAttendanceSchedule ResolvedSchedule);

    private sealed record RequestAllocationKey(
        Guid EmployeeId,
        Guid LeaveTypeId,
        int PeriodYear);
}
