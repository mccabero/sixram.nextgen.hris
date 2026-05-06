using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Portal;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IAttendanceAdjustmentService
{
    Task<PagedResultDto<AttendanceAdjustmentRequestDto>> GetRequestsAsync(
        AttendanceAdjustmentRequestListQueryDto query,
        string? actorUserId,
        bool ownOnly,
        CancellationToken cancellationToken = default);

    Task<AttendanceAdjustmentRequestDto> GetByIdAsync(Guid requestId, string? actorUserId, bool ownOnly, CancellationToken cancellationToken = default);

    Task<AttendanceAdjustmentRequestDto> CreateOwnRequestAsync(SaveAttendanceAdjustmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<AttendanceAdjustmentRequestDto> ApproveAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<AttendanceAdjustmentRequestDto> RejectAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<AttendanceAdjustmentRequestDto> CancelAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);
}

public class AttendanceAdjustmentService : IAttendanceAdjustmentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserAccessService _userAccessService;
    private readonly IAttendanceCalculationService _attendanceCalculationService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AttendanceAdjustmentService> _logger;

    public AttendanceAdjustmentService(
        ApplicationDbContext dbContext,
        IUserAccessService userAccessService,
        IAttendanceCalculationService attendanceCalculationService,
        INotificationService notificationService,
        ILogger<AttendanceAdjustmentService> logger)
    {
        _dbContext = dbContext;
        _userAccessService = userAccessService;
        _attendanceCalculationService = attendanceCalculationService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PagedResultDto<AttendanceAdjustmentRequestDto>> GetRequestsAsync(
        AttendanceAdjustmentRequestListQueryDto query,
        string? actorUserId,
        bool ownOnly,
        CancellationToken cancellationToken = default)
    {
        var actor = ownOnly
            ? await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken)
            : await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);

        var source = _dbContext.AttendanceAdjustmentRequests
            .AsNoTracking()
            .Include(record => record.Employee)
            .Include(record => record.AttendanceRecord)
            .Include(record => record.CurrentApproverUser)
            .Include(record => record.RequestedByUser)
            .Include(record => record.ReviewedByUser)
            .AsQueryable();

        if (ownOnly)
        {
            source = source.Where(record => record.EmployeeId == actor.LinkedEmployeeId);
        }
        else if (actor.IsAdministrator || actor.IsHumanResources)
        {
            if (query.EmployeeId is not null)
            {
                source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
            }
        }
        else if (actor.IsManager)
        {
            source = source.Where(record => actor.ManagedEmployeeIds.Contains(record.EmployeeId));

            if (query.EmployeeId is not null)
            {
                source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
            }
        }
        else
        {
            throw new ForbiddenApiException("You do not have permission to view attendance adjustment requests.");
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Employee!.EmployeeCode.Contains(search) ||
                record.Employee.FirstName.Contains(search) ||
                record.Employee.MiddleName.Contains(search) ||
                record.Employee.LastName.Contains(search) ||
                record.Reason.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim().ToLowerInvariant());
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(record => record.AttendanceDate >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(record => record.AttendanceDate <= query.DateTo.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("date", false) => source.OrderBy(record => record.AttendanceDate).ThenByDescending(record => record.CreatedAtUtc),
            ("date", true) => source.OrderByDescending(record => record.AttendanceDate).ThenByDescending(record => record.CreatedAtUtc),
            ("status", false) => source.OrderBy(record => record.Status).ThenByDescending(record => record.CreatedAtUtc),
            ("status", true) => source.OrderByDescending(record => record.Status).ThenByDescending(record => record.CreatedAtUtc),
            (_, false) => source.OrderBy(record => record.CreatedAtUtc),
            _ => source.OrderByDescending(record => record.CreatedAtUtc)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<AttendanceAdjustmentRequestDto>
        {
            Items = items.Select(Map).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<AttendanceAdjustmentRequestDto> GetByIdAsync(Guid requestId, string? actorUserId, bool ownOnly, CancellationToken cancellationToken = default)
    {
        var actor = ownOnly
            ? await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken)
            : await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);

        var request = await LoadAsync(requestId, cancellationToken);

        if (ownOnly)
        {
            _userAccessService.EnsureCanAccessEmployee(actor, request.EmployeeId, allowSelf: true, allowManagedEmployees: false);
        }
        else
        {
            _userAccessService.EnsureCanManageEmployee(actor, request.EmployeeId);
        }

        return Map(request);
    }

    public async Task<AttendanceAdjustmentRequestDto> CreateOwnRequestAsync(SaveAttendanceAdjustmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        var employeeId = actor.LinkedEmployeeId!.Value;

        AttendanceRecord? attendanceRecord = null;
        if (request.AttendanceRecordId is not null)
        {
            attendanceRecord = await _dbContext.AttendanceRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(record => record.Id == request.AttendanceRecordId.Value && record.EmployeeId == employeeId, cancellationToken)
                ?? throw new NotFoundException("The selected attendance record could not be found.");
        }

        var attendanceDate = request.AttendanceDate ?? attendanceRecord?.AttendanceDate
            ?? throw BuildValidationException("An attendance date is required.", nameof(SaveAttendanceAdjustmentRequestDto.AttendanceDate));

        EnsureAttendanceDateNotInFuture(attendanceDate);
        await EnsureNotInLockedPayrollPeriodAsync(employeeId, attendanceDate, cancellationToken);

        var duplicatePendingExists = await _dbContext.AttendanceAdjustmentRequests.AnyAsync(
            record =>
                record.EmployeeId == employeeId &&
                record.AttendanceDate == attendanceDate &&
                record.Status == RequestStatuses.Pending,
            cancellationToken);

        if (duplicatePendingExists)
        {
            throw new ConflictException("There is already a pending attendance adjustment request for this date.");
        }

        var managerApproverUserId = await ResolveDefaultApproverUserIdAsync(employeeId, cancellationToken);
        var entity = new AttendanceAdjustmentRequest
        {
            EmployeeId = employeeId,
            AttendanceRecordId = attendanceRecord?.Id,
            RequestedByUserId = actor.UserId,
            RequestType = request.RequestType.Trim().ToLowerInvariant(),
            AttendanceDate = attendanceDate,
            RequestedTimeIn = request.RequestedTimeIn,
            RequestedTimeOut = request.RequestedTimeOut,
            RequestedRemarks = request.RequestedRemarks.Trim(),
            Reason = request.Reason.Trim(),
            Status = RequestStatuses.Pending,
            CurrentApproverUserId = managerApproverUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.AttendanceAdjustmentRequests.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .SingleAsync(record => record.Id == employeeId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(managerApproverUserId))
        {
            await _notificationService.CreateAsync(
                new NotificationDraft(
                    managerApproverUserId,
                    "Attendance correction submitted",
                    $"{BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix)} submitted an attendance correction request.",
                    NotificationTypes.AttendanceAdjustmentSubmitted,
                    ApprovableTypes.AttendanceAdjustmentRequest,
                    entity.Id.ToString(),
                    "/approvals"),
                cancellationToken);
        }
        else
        {
            await NotifyReviewerGroupAsync(
                "Attendance correction submitted",
                $"{BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix)} submitted an attendance correction request.",
                NotificationTypes.AttendanceAdjustmentSubmitted,
                entity.Id,
                cancellationToken);
        }

        return await GetByIdAsync(entity.Id, actorUserId, ownOnly: true, cancellationToken);
    }

    public async Task<AttendanceAdjustmentRequestDto> ApproveAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        var entity = await LoadForUpdateAsync(requestId, cancellationToken);

        if (!actor.IsAdministrator && !actor.IsHumanResources)
        {
            _userAccessService.EnsureCanManageEmployee(actor, entity.EmployeeId);
            if (actor.LinkedEmployeeId == entity.EmployeeId)
            {
                throw new ForbiddenApiException("Managers cannot approve their own attendance correction requests.");
            }
        }

        EnsurePending(entity.Status);
        await EnsureNotInLockedPayrollPeriodAsync(entity.EmployeeId, entity.AttendanceDate, cancellationToken);

        var attendanceRecord = entity.AttendanceRecord;
        if (attendanceRecord is null)
        {
            attendanceRecord = await _dbContext.AttendanceRecords
                .SingleOrDefaultAsync(
                    record => record.EmployeeId == entity.EmployeeId && record.AttendanceDate == entity.AttendanceDate,
                    cancellationToken);
        }

        var assignments = await _dbContext.EmployeeScheduleAssignments
            .AsNoTracking()
            .Include(record => record.WorkSchedule)
            .Include(record => record.Shift)
            .Where(record =>
                record.EmployeeId == entity.EmployeeId &&
                record.EffectiveStartDate <= entity.AttendanceDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= entity.AttendanceDate))
            .ToListAsync(cancellationToken);

        var resolvedSchedule = _attendanceCalculationService.ResolveSchedule(assignments, entity.AttendanceDate);
        var actualTimeIn = entity.RequestedTimeIn ?? attendanceRecord?.ActualTimeIn;
        var actualTimeOut = entity.RequestedTimeOut ?? attendanceRecord?.ActualTimeOut;
        var remarks = string.IsNullOrWhiteSpace(entity.RequestedRemarks)
            ? attendanceRecord?.Remarks ?? string.Empty
            : entity.RequestedRemarks.Trim();

        var calculation = _attendanceCalculationService.CalculateAttendance(
            entity.AttendanceDate,
            resolvedSchedule,
            actualTimeIn,
            actualTimeOut,
            attendanceRecord?.BreakStartTime,
            attendanceRecord?.BreakEndTime);

        if (attendanceRecord is null)
        {
            attendanceRecord = new AttendanceRecord
            {
                EmployeeId = entity.EmployeeId,
                AttendanceDate = entity.AttendanceDate,
                CreatedByUserId = actor.UserId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.AttendanceRecords.Add(attendanceRecord);
        }
        else
        {
            attendanceRecord.UpdatedByUserId = actor.UserId;
            attendanceRecord.UpdatedAtUtc = DateTime.UtcNow;
        }

        attendanceRecord.ActualTimeIn = actualTimeIn;
        attendanceRecord.ActualTimeOut = actualTimeOut;
        attendanceRecord.Remarks = remarks;
        attendanceRecord.Source = AttendanceSources.Manual;
        attendanceRecord.ScheduledStartTime = calculation.ScheduledStartTime;
        attendanceRecord.ScheduledEndTime = calculation.ScheduledEndTime;
        attendanceRecord.TotalWorkedMinutes = calculation.TotalWorkedMinutes;
        attendanceRecord.LateMinutes = calculation.LateMinutes;
        attendanceRecord.UndertimeMinutes = calculation.UndertimeMinutes;
        attendanceRecord.OvertimeMinutes = calculation.OvertimeMinutes;
        attendanceRecord.Status = calculation.Status;

        entity.AttendanceRecord = attendanceRecord;
        entity.Status = RequestStatuses.Approved;
        entity.ReviewedByUserId = actor.UserId;
        entity.ReviewedAtUtc = DateTime.UtcNow;
        entity.ReviewerRemarks = request.Remarks.Trim();
        entity.AppliedAtUtc = DateTime.UtcNow;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await NotifyRequesterAsync(
            entity.RequestedByUserId,
            "Attendance correction approved",
            "Your attendance correction request was approved and applied to your attendance record.",
            NotificationTypes.AttendanceAdjustmentApproved,
            entity.Id,
            cancellationToken);

        return await GetByIdAsync(requestId, actorUserId, ownOnly: false, cancellationToken);
    }

    public async Task<AttendanceAdjustmentRequestDto> RejectAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        var entity = await LoadForUpdateAsync(requestId, cancellationToken);

        if (!actor.IsAdministrator && !actor.IsHumanResources)
        {
            _userAccessService.EnsureCanManageEmployee(actor, entity.EmployeeId);
            if (actor.LinkedEmployeeId == entity.EmployeeId)
            {
                throw new ForbiddenApiException("Managers cannot reject their own attendance correction requests.");
            }
        }

        EnsurePending(entity.Status);
        entity.Status = RequestStatuses.Rejected;
        entity.ReviewedByUserId = actor.UserId;
        entity.ReviewedAtUtc = DateTime.UtcNow;
        entity.ReviewerRemarks = request.Remarks.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await NotifyRequesterAsync(
            entity.RequestedByUserId,
            "Attendance correction rejected",
            string.IsNullOrWhiteSpace(request.Remarks)
                ? "Your attendance correction request was reviewed but not approved."
                : $"Your attendance correction request was rejected: {request.Remarks.Trim()}",
            NotificationTypes.AttendanceAdjustmentRejected,
            entity.Id,
            cancellationToken);

        return await GetByIdAsync(requestId, actorUserId, ownOnly: false, cancellationToken);
    }

    public async Task<AttendanceAdjustmentRequestDto> CancelAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        var entity = await LoadForUpdateAsync(requestId, cancellationToken);
        _userAccessService.EnsureCanAccessEmployee(actor, entity.EmployeeId, allowSelf: true, allowManagedEmployees: false);
        EnsurePending(entity.Status);

        entity.Status = RequestStatuses.Cancelled;
        entity.ReviewerRemarks = request.Remarks.Trim();
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(entity.CurrentApproverUserId))
        {
            await _notificationService.CreateAsync(
                new NotificationDraft(
                    entity.CurrentApproverUserId,
                    "Attendance correction cancelled",
                    "A pending attendance correction request was cancelled by the employee.",
                    NotificationTypes.AttendanceAdjustmentCancelled,
                    ApprovableTypes.AttendanceAdjustmentRequest,
                    entity.Id.ToString(),
                    "/approvals"),
                cancellationToken);
        }

        return await GetByIdAsync(requestId, actorUserId, ownOnly: true, cancellationToken);
    }

    private async Task<AttendanceAdjustmentRequest> LoadAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return await _dbContext.AttendanceAdjustmentRequests
            .AsNoTracking()
            .Include(record => record.Employee)
            .Include(record => record.AttendanceRecord)
            .Include(record => record.CurrentApproverUser)
            .Include(record => record.RequestedByUser)
            .Include(record => record.ReviewedByUser)
            .SingleOrDefaultAsync(record => record.Id == requestId, cancellationToken)
            ?? throw new NotFoundException($"Attendance adjustment request '{requestId}' was not found.");
    }

    private async Task<AttendanceAdjustmentRequest> LoadForUpdateAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return await _dbContext.AttendanceAdjustmentRequests
            .Include(record => record.Employee)
            .Include(record => record.AttendanceRecord)
            .Include(record => record.CurrentApproverUser)
            .Include(record => record.RequestedByUser)
            .Include(record => record.ReviewedByUser)
            .SingleOrDefaultAsync(record => record.Id == requestId, cancellationToken)
            ?? throw new NotFoundException($"Attendance adjustment request '{requestId}' was not found.");
    }

    private async Task EnsureNotInLockedPayrollPeriodAsync(Guid employeeId, DateOnly attendanceDate, CancellationToken cancellationToken)
    {
        var isLocked = await _dbContext.PayrollRunItems
            .AsNoTracking()
            .AnyAsync(
                item =>
                    item.EmployeeId == employeeId &&
                    item.PayrollRun != null &&
                    (item.PayrollRun.Status == PayrollRunStatuses.Approved || item.PayrollRun.Status == PayrollRunStatuses.Paid) &&
                    item.PayrollRun.PayPeriod != null &&
                    item.PayrollRun.PayPeriod.PeriodStartDate <= attendanceDate &&
                    item.PayrollRun.PayPeriod.PeriodEndDate >= attendanceDate,
                cancellationToken);

        if (isLocked)
        {
            throw new ConflictException("Attendance corrections are blocked because this attendance date already belongs to an approved or paid payroll run.");
        }
    }

    private async Task<string?> ResolveDefaultApproverUserIdAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Employees
            .AsNoTracking()
            .Where(record => record.Id == employeeId)
            .Select(record => record.Manager != null ? record.Manager.UserId : null)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task NotifyRequesterAsync(string requesterUserId, string title, string message, string type, Guid requestId, CancellationToken cancellationToken)
    {
        await _notificationService.CreateAsync(
            new NotificationDraft(
                requesterUserId,
                title,
                message,
                type,
                ApprovableTypes.AttendanceAdjustmentRequest,
                requestId.ToString(),
                "/me/requests"),
            cancellationToken);
    }

    private async Task NotifyReviewerGroupAsync(string title, string message, string type, Guid requestId, CancellationToken cancellationToken)
    {
        var reviewers = await _dbContext.UserRoles
            .AsNoTracking()
            .Where(record =>
                record.Role != null &&
                (record.Role.Name == SystemRoles.Administrator || record.Role.Name == SystemRoles.HumanResources))
            .Select(record => record.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        await _notificationService.CreateManyAsync(
            reviewers.Select(userId => new NotificationDraft(
                userId,
                title,
                message,
                type,
                ApprovableTypes.AttendanceAdjustmentRequest,
                requestId.ToString(),
                "/approvals")),
            cancellationToken);
    }

    private static AttendanceAdjustmentRequestDto Map(AttendanceAdjustmentRequest record)
    {
        var employee = record.Employee;
        return new AttendanceAdjustmentRequestDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = employee is null ? string.Empty : BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            AttendanceRecordId = record.AttendanceRecordId,
            AttendanceDate = record.AttendanceDate,
            RequestType = record.RequestType,
            CurrentTimeIn = record.AttendanceRecord?.ActualTimeIn,
            CurrentTimeOut = record.AttendanceRecord?.ActualTimeOut,
            CurrentRemarks = record.AttendanceRecord?.Remarks ?? string.Empty,
            RequestedTimeIn = record.RequestedTimeIn,
            RequestedTimeOut = record.RequestedTimeOut,
            RequestedRemarks = record.RequestedRemarks,
            Reason = record.Reason,
            Status = record.Status,
            CurrentApproverDisplayName = BuildUserDisplayName(record.CurrentApproverUser),
            RequestedByDisplayName = BuildUserDisplayName(record.RequestedByUser),
            ReviewedByDisplayName = BuildUserDisplayName(record.ReviewedByUser),
            ReviewerRemarks = record.ReviewerRemarks,
            ReviewedAtUtc = record.ReviewedAtUtc,
            AppliedAtUtc = record.AppliedAtUtc,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
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

    private static void EnsurePending(string status)
    {
        if (!string.Equals(status, RequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Only pending attendance adjustment requests can be reviewed or cancelled.");
        }
    }

    private void EnsureAttendanceDateNotInFuture(DateOnly attendanceDate)
    {
        if (attendanceDate > _attendanceCalculationService.GetBusinessToday())
        {
            throw BuildValidationException("Attendance correction requests cannot be filed for a future date.", nameof(SaveAttendanceAdjustmentRequestDto.AttendanceDate));
        }
    }

    private static BadRequestException BuildValidationException(string message, string fieldName)
    {
        return new BadRequestException(message, new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }
}
