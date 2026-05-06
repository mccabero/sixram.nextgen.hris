using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sixram.Api.Configuration;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IAttendanceService
{
    Task<AttendanceDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    Task<PagedResultDto<AttendanceRecordListItemDto>> GetAttendanceRecordsAsync(
        AttendanceRecordListQueryDto query,
        CancellationToken cancellationToken = default);

    Task<AttendanceRecordListItemDto> GetAttendanceRecordByIdAsync(Guid attendanceRecordId, CancellationToken cancellationToken = default);

    Task<AttendanceRecordListItemDto> CreateAttendanceRecordAsync(
        SaveAttendanceRecordRequestDto request,
        string? actorUserId,
        CancellationToken cancellationToken = default);

    Task<AttendanceRecordListItemDto> UpdateAttendanceRecordAsync(
        Guid attendanceRecordId,
        SaveAttendanceRecordRequestDto request,
        string? actorUserId,
        CancellationToken cancellationToken = default);

    Task DeleteAttendanceRecordAsync(Guid attendanceRecordId, CancellationToken cancellationToken = default);
}

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAttendanceCalculationService _attendanceCalculationService;
    private readonly AttendanceOptions _options;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        ApplicationDbContext dbContext,
        IAttendanceCalculationService attendanceCalculationService,
        IOptions<AttendanceOptions> options,
        ILogger<AttendanceService> logger)
    {
        _dbContext = dbContext;
        _attendanceCalculationService = attendanceCalculationService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AttendanceDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var attendanceDate = _attendanceCalculationService.GetBusinessToday();
        var trendDays = Math.Max(1, _options.DashboardTrendDays);
        var trendStartDate = attendanceDate.AddDays(-(trendDays - 1));
        var employees = await GetEmployeeRosterAsync(
            search: string.Empty,
            departmentId: null,
            branchId: null,
            activeOnly: true,
            cancellationToken);

        var items = await BuildAttendanceItemsAsync(employees, trendStartDate, attendanceDate, cancellationToken);
        var todayItems = items
            .Where(item => item.AttendanceDate == attendanceDate)
            .ToList();

        return new AttendanceDashboardSummaryDto
        {
            AttendanceDate = attendanceDate,
            PresentCount = todayItems.Count(item => IsPresentLike(item.Status)),
            LateCount = todayItems.Count(item => string.Equals(item.Status, AttendanceStatuses.Late, StringComparison.OrdinalIgnoreCase)),
            AbsentCount = todayItems.Count(item => string.Equals(item.Status, AttendanceStatuses.Absent, StringComparison.OrdinalIgnoreCase)),
            IncompleteCount = todayItems.Count(item => string.Equals(item.Status, AttendanceStatuses.Incomplete, StringComparison.OrdinalIgnoreCase)),
            RestDayCount = todayItems.Count(item => string.Equals(item.Status, AttendanceStatuses.RestDay, StringComparison.OrdinalIgnoreCase)),
            NoScheduleCount = todayItems.Count(item => string.Equals(item.Status, AttendanceStatuses.NoSchedule, StringComparison.OrdinalIgnoreCase)),
            UndertimeCount = todayItems.Count(item => string.Equals(item.Status, AttendanceStatuses.Undertime, StringComparison.OrdinalIgnoreCase)),
            PendingAdjustmentRequestCount = 0,
            EmployeesWithoutScheduleAssignmentCount = todayItems.Count(item => !item.HasScheduleAssignment),
            Trend = items
                .GroupBy(item => item.AttendanceDate)
                .OrderBy(group => group.Key)
                .Select(group => new AttendanceTrendPointDto
                {
                    Date = group.Key,
                    PresentCount = group.Count(item => IsPresentLike(item.Status)),
                    LateCount = group.Count(item => string.Equals(item.Status, AttendanceStatuses.Late, StringComparison.OrdinalIgnoreCase)),
                    AbsentCount = group.Count(item => string.Equals(item.Status, AttendanceStatuses.Absent, StringComparison.OrdinalIgnoreCase)),
                    IncompleteCount = group.Count(item => string.Equals(item.Status, AttendanceStatuses.Incomplete, StringComparison.OrdinalIgnoreCase))
                })
                .ToArray()
        };
    }

    public async Task<PagedResultDto<AttendanceRecordListItemDto>> GetAttendanceRecordsAsync(
        AttendanceRecordListQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var (dateFrom, dateTo) = NormalizeDateRange(query.DateFrom, query.DateTo);
        ValidateDateRange(dateFrom, dateTo);

        var employees = await GetEmployeeRosterAsync(
            query.Search,
            query.DepartmentId,
            query.BranchId,
            activeOnly: false,
            cancellationToken);

        if (employees.Count == 0)
        {
            return EmptyPage(query.PageNumber, query.PageSize);
        }

        if (query.EmployeeId is not null)
        {
            employees = employees
                .Where(record => record.Id == query.EmployeeId.Value)
                .ToArray();

            if (employees.Count == 0)
            {
                return EmptyPage(query.PageNumber, query.PageSize);
            }
        }

        if (query.EmployeeIds.Count > 0)
        {
            var scopedIds = query.EmployeeIds.ToHashSet();
            employees = employees
                .Where(record => scopedIds.Contains(record.Id))
                .ToArray();

            if (employees.Count == 0)
            {
                return EmptyPage(query.PageNumber, query.PageSize);
            }
        }

        var items = await BuildAttendanceItemsAsync(employees, dateFrom, dateTo, cancellationToken);

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            items = items
                .Where(item => string.Equals(item.Status, query.Status.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(query.Source))
        {
            items = items
                .Where(item => string.Equals(item.Source, query.Source.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var orderedItems = ApplySorting(items, query.SortBy, query.Descending);
        return ToPage(orderedItems, query.PageNumber, query.PageSize);
    }

    public async Task<AttendanceRecordListItemDto> GetAttendanceRecordByIdAsync(Guid attendanceRecordId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Include(item => item.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(item => item.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(item => item.CreatedByUser)
            .Include(item => item.UpdatedByUser)
            .SingleOrDefaultAsync(item => item.Id == attendanceRecordId, cancellationToken)
            ?? throw new NotFoundException($"Attendance record '{attendanceRecordId}' was not found.");

        var employee = record.Employee ?? throw new NotFoundException("The employee linked to this attendance record could not be found.");
        var resolvedSchedule = await ResolveScheduleAsync(record.EmployeeId, record.AttendanceDate, cancellationToken);
        return MapRecordedAttendance(record, MapEmployee(employee), resolvedSchedule);
    }

    public async Task<AttendanceRecordListItemDto> CreateAttendanceRecordAsync(
        SaveAttendanceRecordRequestDto request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var employeeId = request.EmployeeId!.Value;
        var attendanceDate = request.AttendanceDate!.Value;
        EnsureAttendanceDateIsNotInFuture(attendanceDate);
        ValidateSource(request.Source);

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .Include(record => record.Department)
            .Include(record => record.Branch)
            .SingleOrDefaultAsync(record => record.Id == employeeId, cancellationToken)
            ?? throw new BadRequestException("The selected employee does not exist.");

        if (await _dbContext.AttendanceRecords.AnyAsync(
                record => record.EmployeeId == employeeId && record.AttendanceDate == attendanceDate,
                cancellationToken))
        {
            throw new ConflictException("An attendance record for this employee and date already exists.");
        }

        var actualTimeIn = NormalizeTimestamp(request.ActualTimeIn);
        var actualTimeOut = NormalizeTimestamp(request.ActualTimeOut);
        var breakStartTime = NormalizeTimestamp(request.BreakStartTime);
        var breakEndTime = NormalizeTimestamp(request.BreakEndTime);
        var resolvedSchedule = await ResolveScheduleAsync(employeeId, attendanceDate, cancellationToken);
        var calculation = _attendanceCalculationService.CalculateAttendance(
            attendanceDate,
            resolvedSchedule,
            actualTimeIn,
            actualTimeOut,
            breakStartTime,
            breakEndTime);

        var record = new AttendanceRecord
        {
            EmployeeId = employeeId,
            AttendanceDate = attendanceDate,
            ActualTimeIn = actualTimeIn,
            ActualTimeOut = actualTimeOut,
            BreakStartTime = breakStartTime,
            BreakEndTime = breakEndTime,
            Source = request.Source.Trim().ToLowerInvariant(),
            Remarks = request.Remarks.Trim(),
            CreatedByUserId = actorUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        ApplyCalculation(record, calculation);

        _dbContext.AttendanceRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Attendance record {AttendanceRecordId} created for employee {EmployeeId} on {AttendanceDate}.",
            record.Id,
            employeeId,
            attendanceDate);

        return await GetAttendanceRecordByIdAsync(record.Id, cancellationToken);
    }

    public async Task<AttendanceRecordListItemDto> UpdateAttendanceRecordAsync(
        Guid attendanceRecordId,
        SaveAttendanceRecordRequestDto request,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var employeeId = request.EmployeeId!.Value;
        var attendanceDate = request.AttendanceDate!.Value;
        EnsureAttendanceDateIsNotInFuture(attendanceDate);
        ValidateSource(request.Source);

        var record = await _dbContext.AttendanceRecords
            .SingleOrDefaultAsync(item => item.Id == attendanceRecordId, cancellationToken)
            ?? throw new NotFoundException($"Attendance record '{attendanceRecordId}' was not found.");

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .Include(item => item.Department)
            .Include(item => item.Branch)
            .SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken)
            ?? throw new BadRequestException("The selected employee does not exist.");

        if (await _dbContext.AttendanceRecords.AnyAsync(
                item => item.Id != attendanceRecordId &&
                        item.EmployeeId == employeeId &&
                        item.AttendanceDate == attendanceDate,
                cancellationToken))
        {
            throw new ConflictException("An attendance record for this employee and date already exists.");
        }

        var actualTimeIn = NormalizeTimestamp(request.ActualTimeIn);
        var actualTimeOut = NormalizeTimestamp(request.ActualTimeOut);
        var breakStartTime = NormalizeTimestamp(request.BreakStartTime);
        var breakEndTime = NormalizeTimestamp(request.BreakEndTime);
        var resolvedSchedule = await ResolveScheduleAsync(employeeId, attendanceDate, cancellationToken);
        var calculation = _attendanceCalculationService.CalculateAttendance(
            attendanceDate,
            resolvedSchedule,
            actualTimeIn,
            actualTimeOut,
            breakStartTime,
            breakEndTime);

        record.EmployeeId = employeeId;
        record.AttendanceDate = attendanceDate;
        record.ActualTimeIn = actualTimeIn;
        record.ActualTimeOut = actualTimeOut;
        record.BreakStartTime = breakStartTime;
        record.BreakEndTime = breakEndTime;
        record.Source = request.Source.Trim().ToLowerInvariant();
        record.Remarks = request.Remarks.Trim();
        record.UpdatedByUserId = actorUserId;
        record.UpdatedAtUtc = DateTime.UtcNow;

        ApplyCalculation(record, calculation);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Attendance record {AttendanceRecordId} updated.", attendanceRecordId);
        return await GetAttendanceRecordByIdAsync(attendanceRecordId, cancellationToken);
    }

    public async Task DeleteAttendanceRecordAsync(Guid attendanceRecordId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.AttendanceRecords
            .SingleOrDefaultAsync(item => item.Id == attendanceRecordId, cancellationToken)
            ?? throw new NotFoundException($"Attendance record '{attendanceRecordId}' was not found.");

        _dbContext.AttendanceRecords.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Attendance record {AttendanceRecordId} deleted.", attendanceRecordId);
    }

    private async Task<IReadOnlyList<EmployeeRosterItem>> GetEmployeeRosterAsync(
        string search,
        Guid? departmentId,
        Guid? branchId,
        bool activeOnly,
        CancellationToken cancellationToken)
    {
        var source = _dbContext.Employees
            .AsNoTracking()
            .Include(record => record.Department)
            .Include(record => record.Branch)
            .AsQueryable();

        if (activeOnly)
        {
            source = source.Where(record => record.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var trimmedSearch = search.Trim();
            source = source.Where(record =>
                record.EmployeeCode.Contains(trimmedSearch) ||
                record.FirstName.Contains(trimmedSearch) ||
                record.MiddleName.Contains(trimmedSearch) ||
                record.LastName.Contains(trimmedSearch) ||
                record.Email.Contains(trimmedSearch));
        }

        if (departmentId is not null)
        {
            source = source.Where(record => record.DepartmentId == departmentId.Value);
        }

        if (branchId is not null)
        {
            source = source.Where(record => record.BranchId == branchId.Value);
        }

        var employees = await source
            .OrderBy(record => record.LastName)
            .ThenBy(record => record.FirstName)
            .ToListAsync(cancellationToken);

        return employees.Select(MapEmployee).ToArray();
    }

    private async Task<List<AttendanceRecordListItemDto>> BuildAttendanceItemsAsync(
        IReadOnlyList<EmployeeRosterItem> employees,
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken)
    {
        if (employees.Count == 0)
        {
            return [];
        }

        var employeeIds = employees.Select(record => record.Id).ToArray();
        var assignments = await _dbContext.EmployeeScheduleAssignments
            .AsNoTracking()
            .Include(record => record.WorkSchedule)
            .Include(record => record.Shift)
            .Where(record =>
                employeeIds.Contains(record.EmployeeId) &&
                record.EffectiveStartDate <= dateTo &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= dateFrom))
            .ToListAsync(cancellationToken);

        var records = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Include(record => record.CreatedByUser)
            .Include(record => record.UpdatedByUser)
            .Where(record =>
                employeeIds.Contains(record.EmployeeId) &&
                record.AttendanceDate >= dateFrom &&
                record.AttendanceDate <= dateTo)
            .ToListAsync(cancellationToken);

        var assignmentsByEmployee = assignments
            .GroupBy(record => record.EmployeeId)
            .ToDictionary(group => group.Key, group => (IReadOnlyCollection<EmployeeScheduleAssignment>)group.ToArray());

        var recordsByKey = records.ToDictionary(record => (record.EmployeeId, record.AttendanceDate));
        var items = new List<AttendanceRecordListItemDto>(employees.Count * GetInclusiveDayCount(dateFrom, dateTo));

        foreach (var employee in employees)
        {
            var employeeAssignments = assignmentsByEmployee.TryGetValue(employee.Id, out var groupedAssignments)
                ? groupedAssignments
                : Array.Empty<EmployeeScheduleAssignment>();

            for (var date = dateFrom; date <= dateTo; date = date.AddDays(1))
            {
                var resolvedSchedule = _attendanceCalculationService.ResolveSchedule(employeeAssignments, date);
                if (recordsByKey.TryGetValue((employee.Id, date), out var record))
                {
                    items.Add(MapRecordedAttendance(record, employee, resolvedSchedule));
                }
                else
                {
                    items.Add(CreateSyntheticAttendance(employee, date, resolvedSchedule));
                }
            }
        }

        return items;
    }

    private async Task<ResolvedAttendanceSchedule> ResolveScheduleAsync(
        Guid employeeId,
        DateOnly attendanceDate,
        CancellationToken cancellationToken)
    {
        var assignments = await _dbContext.EmployeeScheduleAssignments
            .AsNoTracking()
            .Include(record => record.WorkSchedule)
            .Include(record => record.Shift)
            .Where(record =>
                record.EmployeeId == employeeId &&
                record.EffectiveStartDate <= attendanceDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= attendanceDate))
            .ToListAsync(cancellationToken);

        return _attendanceCalculationService.ResolveSchedule(assignments, attendanceDate);
    }

    private static AttendanceRecordListItemDto MapRecordedAttendance(
        AttendanceRecord record,
        EmployeeRosterItem employee,
        ResolvedAttendanceSchedule resolvedSchedule)
    {
        return new AttendanceRecordListItemDto
        {
            AttendanceRecordId = record.Id,
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = employee.FullName,
            DepartmentName = employee.DepartmentName,
            BranchName = employee.BranchName,
            AttendanceDate = record.AttendanceDate,
            WorkScheduleName = resolvedSchedule.WorkScheduleName,
            ShiftName = resolvedSchedule.ShiftName,
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
            HasScheduleAssignment = resolvedSchedule.HasScheduleAssignment,
            HasBackingRecord = true,
            CreatedByDisplayName = BuildUserDisplayName(record.CreatedByUser),
            UpdatedByDisplayName = BuildUserDisplayName(record.UpdatedByUser),
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static AttendanceRecordListItemDto CreateSyntheticAttendance(
        EmployeeRosterItem employee,
        DateOnly attendanceDate,
        ResolvedAttendanceSchedule resolvedSchedule)
    {
        var status = resolvedSchedule.HasScheduleAssignment
            ? resolvedSchedule.IsRestDay
                ? AttendanceStatuses.RestDay
                : AttendanceStatuses.Absent
            : AttendanceStatuses.NoSchedule;

        return new AttendanceRecordListItemDto
        {
            AttendanceRecordId = null,
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = employee.FullName,
            DepartmentName = employee.DepartmentName,
            BranchName = employee.BranchName,
            AttendanceDate = attendanceDate,
            WorkScheduleName = resolvedSchedule.WorkScheduleName,
            ShiftName = resolvedSchedule.ShiftName,
            ScheduledStartTime = resolvedSchedule.ScheduledStartTime,
            ScheduledEndTime = resolvedSchedule.ScheduledEndTime,
            ActualTimeIn = null,
            ActualTimeOut = null,
            BreakStartTime = null,
            BreakEndTime = null,
            TotalWorkedMinutes = 0,
            LateMinutes = 0,
            UndertimeMinutes = 0,
            OvertimeMinutes = 0,
            Status = status,
            Source = AttendanceSources.System,
            Remarks = BuildSyntheticRemarks(status),
            HasScheduleAssignment = resolvedSchedule.HasScheduleAssignment,
            HasBackingRecord = false,
            CreatedByDisplayName = string.Empty,
            UpdatedByDisplayName = string.Empty,
            CreatedAtUtc = null,
            UpdatedAtUtc = null
        };
    }

    private static IReadOnlyList<AttendanceRecordListItemDto> ApplySorting(
        IReadOnlyList<AttendanceRecordListItemDto> items,
        string sortBy,
        bool descending)
    {
        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();

        return (normalizedSortBy, descending) switch
        {
            ("employee", true) => items.OrderByDescending(item => item.EmployeeFullName).ThenByDescending(item => item.AttendanceDate).ToArray(),
            ("employee", false) => items.OrderBy(item => item.EmployeeFullName).ThenByDescending(item => item.AttendanceDate).ToArray(),
            ("code", true) => items.OrderByDescending(item => item.EmployeeCode).ThenByDescending(item => item.AttendanceDate).ToArray(),
            ("code", false) => items.OrderBy(item => item.EmployeeCode).ThenByDescending(item => item.AttendanceDate).ToArray(),
            ("status", true) => items.OrderByDescending(item => item.Status).ThenByDescending(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray(),
            ("status", false) => items.OrderBy(item => item.Status).ThenByDescending(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray(),
            ("late", true) => items.OrderByDescending(item => item.LateMinutes).ThenByDescending(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray(),
            ("late", false) => items.OrderBy(item => item.LateMinutes).ThenByDescending(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray(),
            ("undertime", true) => items.OrderByDescending(item => item.UndertimeMinutes).ThenByDescending(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray(),
            ("undertime", false) => items.OrderBy(item => item.UndertimeMinutes).ThenByDescending(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray(),
            ("overtime", true) => items.OrderByDescending(item => item.OvertimeMinutes).ThenByDescending(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray(),
            ("overtime", false) => items.OrderBy(item => item.OvertimeMinutes).ThenByDescending(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray(),
            ("worked", true) => items.OrderByDescending(item => item.TotalWorkedMinutes).ThenByDescending(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray(),
            ("worked", false) => items.OrderBy(item => item.TotalWorkedMinutes).ThenByDescending(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray(),
            (_, true) => items.OrderByDescending(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray(),
            _ => items.OrderBy(item => item.AttendanceDate).ThenBy(item => item.EmployeeFullName).ToArray()
        };
    }

    private static PagedResultDto<AttendanceRecordListItemDto> ToPage(
        IReadOnlyList<AttendanceRecordListItemDto> items,
        int pageNumber,
        int pageSize)
    {
        var totalCount = items.Count;
        var pagedItems = items
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return new PagedResultDto<AttendanceRecordListItemDto>
        {
            Items = pagedItems,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static PagedResultDto<AttendanceRecordListItemDto> EmptyPage(int pageNumber, int pageSize)
    {
        return new PagedResultDto<AttendanceRecordListItemDto>
        {
            Items = [],
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = 0,
            TotalPages = 0
        };
    }

    private void ValidateDateRange(DateOnly dateFrom, DateOnly dateTo)
    {
        var inclusiveDayCount = GetInclusiveDayCount(dateFrom, dateTo);
        var maxQueryRangeDays = Math.Max(1, _options.MaxQueryRangeDays);
        if (inclusiveDayCount > maxQueryRangeDays)
        {
            throw new BadRequestException($"Attendance queries cannot span more than {maxQueryRangeDays} days.");
        }
    }

    private (DateOnly DateFrom, DateOnly DateTo) NormalizeDateRange(DateOnly? dateFrom, DateOnly? dateTo)
    {
        if (dateFrom is null && dateTo is null)
        {
            var today = _attendanceCalculationService.GetBusinessToday();
            return (today, today);
        }

        if (dateFrom is null)
        {
            return (dateTo!.Value, dateTo.Value);
        }

        if (dateTo is null)
        {
            return (dateFrom.Value, dateFrom.Value);
        }

        return (dateFrom.Value, dateTo.Value);
    }

    private void EnsureAttendanceDateIsNotInFuture(DateOnly attendanceDate)
    {
        var businessToday = _attendanceCalculationService.GetBusinessToday();
        if (attendanceDate > businessToday)
        {
            throw new BadRequestException("Attendance records cannot be created for a future date.");
        }
    }

    private static void ValidateSource(string source)
    {
        if (!AttendanceSources.All.Contains(source.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Attendance source must be manual, web_clock, import, system, or leave.");
        }
    }

    private static void ApplyCalculation(AttendanceRecord record, AttendanceCalculationResult calculation)
    {
        record.ScheduledStartTime = calculation.ScheduledStartTime;
        record.ScheduledEndTime = calculation.ScheduledEndTime;
        record.TotalWorkedMinutes = calculation.TotalWorkedMinutes;
        record.LateMinutes = calculation.LateMinutes;
        record.UndertimeMinutes = calculation.UndertimeMinutes;
        record.OvertimeMinutes = calculation.OvertimeMinutes;
        record.Status = calculation.Status;
    }

    private static DateTime? NormalizeTimestamp(DateTime? value)
    {
        return value is null
            ? null
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);
    }

    private static bool IsPresentLike(string status)
    {
        return string.Equals(status, AttendanceStatuses.Present, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, AttendanceStatuses.Late, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, AttendanceStatuses.Undertime, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, AttendanceStatuses.HalfDay, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetInclusiveDayCount(DateOnly dateFrom, DateOnly dateTo)
    {
        return dateTo.DayNumber - dateFrom.DayNumber + 1;
    }

    private static string BuildSyntheticRemarks(string status)
    {
        return status switch
        {
            AttendanceStatuses.Absent => "No attendance record found for a scheduled working day.",
            AttendanceStatuses.RestDay => "Scheduled rest day.",
            AttendanceStatuses.NoSchedule => "No active schedule assignment found for this date.",
            _ => string.Empty
        };
    }

    private static string BuildUserDisplayName(ApplicationUser? user)
    {
        return user is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.Email ?? string.Empty
                : user.DisplayName;
    }

    private static EmployeeRosterItem MapEmployee(Employee employee)
    {
        return new EmployeeRosterItem(
            employee.Id,
            employee.EmployeeCode,
            BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            employee.Department?.Name ?? string.Empty,
            employee.Branch?.Name ?? string.Empty);
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

    private sealed record EmployeeRosterItem(
        Guid Id,
        string EmployeeCode,
        string FullName,
        string DepartmentName,
        string BranchName);
}
