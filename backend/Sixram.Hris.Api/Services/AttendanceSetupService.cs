using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IAttendanceSetupService
{
    Task<AttendanceSetupSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<AttendanceListOptionsDto> GetOptionsAsync(Guid? assignmentId = null, CancellationToken cancellationToken = default);

    Task<PagedResultDto<WorkScheduleRecordDto>> GetWorkSchedulesAsync(WorkScheduleListQueryDto query, CancellationToken cancellationToken = default);

    Task<WorkScheduleRecordDto> GetWorkScheduleByIdAsync(Guid workScheduleId, CancellationToken cancellationToken = default);

    Task<WorkScheduleRecordDto> CreateWorkScheduleAsync(SaveWorkScheduleRequestDto request, CancellationToken cancellationToken = default);

    Task<WorkScheduleRecordDto> UpdateWorkScheduleAsync(Guid workScheduleId, SaveWorkScheduleRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteWorkScheduleAsync(Guid workScheduleId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ShiftRecordDto>> GetShiftsAsync(ShiftListQueryDto query, CancellationToken cancellationToken = default);

    Task<ShiftRecordDto> GetShiftByIdAsync(Guid shiftId, CancellationToken cancellationToken = default);

    Task<ShiftRecordDto> CreateShiftAsync(SaveShiftRequestDto request, CancellationToken cancellationToken = default);

    Task<ShiftRecordDto> UpdateShiftAsync(Guid shiftId, SaveShiftRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteShiftAsync(Guid shiftId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<EmployeeScheduleAssignmentRecordDto>> GetScheduleAssignmentsAsync(EmployeeScheduleAssignmentListQueryDto query, CancellationToken cancellationToken = default);

    Task<EmployeeScheduleAssignmentRecordDto> GetScheduleAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    Task<EmployeeScheduleAssignmentRecordDto> CreateScheduleAssignmentAsync(SaveEmployeeScheduleAssignmentRequestDto request, CancellationToken cancellationToken = default);

    Task<EmployeeScheduleAssignmentRecordDto> UpdateScheduleAssignmentAsync(Guid assignmentId, SaveEmployeeScheduleAssignmentRequestDto request, CancellationToken cancellationToken = default);
}

public class AttendanceSetupService : IAttendanceSetupService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAttendanceCalculationService _attendanceCalculationService;
    private readonly ILogger<AttendanceSetupService> _logger;

    public AttendanceSetupService(
        ApplicationDbContext dbContext,
        IAttendanceCalculationService attendanceCalculationService,
        ILogger<AttendanceSetupService> logger)
    {
        _dbContext = dbContext;
        _attendanceCalculationService = attendanceCalculationService;
        _logger = logger;
    }

    public async Task<AttendanceSetupSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        return new AttendanceSetupSummaryDto
        {
            WorkScheduleCount = await _dbContext.WorkSchedules.CountAsync(cancellationToken),
            ActiveWorkScheduleCount = await _dbContext.WorkSchedules.CountAsync(record => record.IsActive, cancellationToken),
            ShiftCount = await _dbContext.Shifts.CountAsync(cancellationToken),
            ActiveShiftCount = await _dbContext.Shifts.CountAsync(record => record.IsActive, cancellationToken),
            ScheduleAssignmentCount = await _dbContext.EmployeeScheduleAssignments.CountAsync(cancellationToken),
            ActiveScheduleAssignmentCount = await _dbContext.EmployeeScheduleAssignments.CountAsync(record => record.IsActive, cancellationToken)
        };
    }

    public async Task<AttendanceListOptionsDto> GetOptionsAsync(Guid? assignmentId = null, CancellationToken cancellationToken = default)
    {
        var workSchedules = await _dbContext.WorkSchedules
            .AsNoTracking()
            .Where(record => record.IsActive)
            .OrderBy(record => record.Name)
            .Select(record => new WorkScheduleOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                ScheduleType = record.ScheduleType,
                IsActive = record.IsActive
            })
            .ToListAsync(cancellationToken);

        var shifts = await _dbContext.Shifts
            .AsNoTracking()
            .Where(record => record.IsActive)
            .OrderBy(record => record.Name)
            .Select(record => new ShiftOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                StartTime = record.StartTime,
                EndTime = record.EndTime,
                IsOvernight = record.IsOvernight,
                IsActive = record.IsActive
            })
            .ToListAsync(cancellationToken);

        if (assignmentId is not null)
        {
            var assignment = await _dbContext.EmployeeScheduleAssignments
                .AsNoTracking()
                .Include(record => record.WorkSchedule)
                .Include(record => record.Shift)
                .SingleOrDefaultAsync(record => record.Id == assignmentId.Value, cancellationToken);

            if (assignment?.WorkSchedule is not null && workSchedules.All(record => record.Id != assignment.WorkScheduleId))
            {
                workSchedules =
                [
                    .. workSchedules,
                    new WorkScheduleOptionDto
                    {
                        Id = assignment.WorkSchedule.Id,
                        Code = assignment.WorkSchedule.Code,
                        Name = assignment.WorkSchedule.Name,
                        ScheduleType = assignment.WorkSchedule.ScheduleType,
                        IsActive = assignment.WorkSchedule.IsActive
                    }
                ];
            }

            if (assignment?.Shift is not null && shifts.All(record => record.Id != assignment.ShiftId))
            {
                shifts =
                [
                    .. shifts,
                    new ShiftOptionDto
                    {
                        Id = assignment.Shift.Id,
                        Code = assignment.Shift.Code,
                        Name = assignment.Shift.Name,
                        StartTime = assignment.Shift.StartTime,
                        EndTime = assignment.Shift.EndTime,
                        IsOvernight = assignment.Shift.IsOvernight,
                        IsActive = assignment.Shift.IsActive
                    }
                ];
            }
        }

        return new AttendanceListOptionsDto
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
            WorkSchedules = workSchedules,
            Shifts = shifts,
            Statuses = AttendanceStatuses.All,
            Sources = AttendanceSources.All
        };
    }

    public async Task<PagedResultDto<WorkScheduleRecordDto>> GetWorkSchedulesAsync(WorkScheduleListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.WorkSchedules
            .AsNoTracking()
            .Select(record => new WorkScheduleRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                ScheduleType = record.ScheduleType,
                RequiredWorkingMinutes = record.RequiredWorkingMinutes,
                GracePeriodMinutes = record.GracePeriodMinutes,
                BreakDurationMinutes = record.BreakDurationMinutes,
                IsActive = record.IsActive,
                AssignmentCount = record.EmployeeScheduleAssignments.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            });

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Code.Contains(search) ||
                record.Name.Contains(search) ||
                record.Description.Contains(search) ||
                record.ScheduleType.Contains(search));
        }

        if (query.IsActive is not null)
        {
            source = source.Where(record => record.IsActive == query.IsActive.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("code", true) => source.OrderByDescending(record => record.Code).ThenByDescending(record => record.Name),
            ("code", false) => source.OrderBy(record => record.Code).ThenBy(record => record.Name),
            ("assignments", true) => source.OrderByDescending(record => record.AssignmentCount).ThenByDescending(record => record.Name),
            ("assignments", false) => source.OrderBy(record => record.AssignmentCount).ThenBy(record => record.Name),
            ("created", true) => source.OrderByDescending(record => record.CreatedAtUtc).ThenByDescending(record => record.Name),
            ("created", false) => source.OrderBy(record => record.CreatedAtUtc).ThenBy(record => record.Name),
            (_, true) => source.OrderByDescending(record => record.Name).ThenByDescending(record => record.Code),
            _ => source.OrderBy(record => record.Name).ThenBy(record => record.Code)
        };

        return await ToPageAsync(source, query.PageNumber, query.PageSize, cancellationToken);
    }

    public async Task<WorkScheduleRecordDto> GetWorkScheduleByIdAsync(Guid workScheduleId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.WorkSchedules
            .AsNoTracking()
            .Where(item => item.Id == workScheduleId)
            .Select(item => new WorkScheduleRecordDto
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                ScheduleType = item.ScheduleType,
                RequiredWorkingMinutes = item.RequiredWorkingMinutes,
                GracePeriodMinutes = item.GracePeriodMinutes,
                BreakDurationMinutes = item.BreakDurationMinutes,
                IsActive = item.IsActive,
                AssignmentCount = item.EmployeeScheduleAssignments.Count,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return record ?? throw new NotFoundException($"Work schedule '{workScheduleId}' was not found.");
    }

    public async Task<WorkScheduleRecordDto> CreateWorkScheduleAsync(SaveWorkScheduleRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateScheduleType(request.ScheduleType);

        var record = new WorkSchedule
        {
            CreatedAtUtc = DateTime.UtcNow
        };

        ApplyWorkSchedule(record, request);
        await EnsureWorkScheduleUniquenessAsync(record.Code, record.Name, null, cancellationToken);

        _dbContext.WorkSchedules.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Work schedule {WorkScheduleCode} created.", record.Code);
        return await GetWorkScheduleByIdAsync(record.Id, cancellationToken);
    }

    public async Task<WorkScheduleRecordDto> UpdateWorkScheduleAsync(Guid workScheduleId, SaveWorkScheduleRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateScheduleType(request.ScheduleType);

        var record = await _dbContext.WorkSchedules.SingleOrDefaultAsync(item => item.Id == workScheduleId, cancellationToken)
            ?? throw new NotFoundException($"Work schedule '{workScheduleId}' was not found.");

        ApplyWorkSchedule(record, request);
        record.UpdatedAtUtc = DateTime.UtcNow;
        await EnsureWorkScheduleUniquenessAsync(record.Code, record.Name, workScheduleId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Work schedule {WorkScheduleId} updated.", workScheduleId);
        return await GetWorkScheduleByIdAsync(workScheduleId, cancellationToken);
    }

    public async Task DeleteWorkScheduleAsync(Guid workScheduleId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.WorkSchedules
            .Include(item => item.EmployeeScheduleAssignments)
            .SingleOrDefaultAsync(item => item.Id == workScheduleId, cancellationToken)
            ?? throw new NotFoundException($"Work schedule '{workScheduleId}' was not found.");

        if (record.EmployeeScheduleAssignments.Count > 0)
        {
            throw new BadRequestException("This work schedule is already referenced by schedule assignments. Deactivate it instead of deleting it.");
        }

        _dbContext.WorkSchedules.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Work schedule {WorkScheduleId} deleted.", workScheduleId);
    }

    public async Task<PagedResultDto<ShiftRecordDto>> GetShiftsAsync(ShiftListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.Shifts
            .AsNoTracking()
            .Select(record => new ShiftRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                StartTime = record.StartTime,
                EndTime = record.EndTime,
                BreakStartTime = record.BreakStartTime,
                BreakEndTime = record.BreakEndTime,
                RequiredWorkingMinutes = record.RequiredWorkingMinutes,
                GracePeriodMinutes = record.GracePeriodMinutes,
                IsOvernight = record.IsOvernight,
                IsActive = record.IsActive,
                AssignmentCount = record.EmployeeScheduleAssignments.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            });

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Code.Contains(search) ||
                record.Name.Contains(search));
        }

        if (query.IsActive is not null)
        {
            source = source.Where(record => record.IsActive == query.IsActive.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("code", true) => source.OrderByDescending(record => record.Code).ThenByDescending(record => record.Name),
            ("code", false) => source.OrderBy(record => record.Code).ThenBy(record => record.Name),
            ("start", true) => source.OrderByDescending(record => record.StartTime).ThenByDescending(record => record.Name),
            ("start", false) => source.OrderBy(record => record.StartTime).ThenBy(record => record.Name),
            ("created", true) => source.OrderByDescending(record => record.CreatedAtUtc).ThenByDescending(record => record.Name),
            ("created", false) => source.OrderBy(record => record.CreatedAtUtc).ThenBy(record => record.Name),
            (_, true) => source.OrderByDescending(record => record.Name).ThenByDescending(record => record.Code),
            _ => source.OrderBy(record => record.Name).ThenBy(record => record.Code)
        };

        return await ToPageAsync(source, query.PageNumber, query.PageSize, cancellationToken);
    }

    public async Task<ShiftRecordDto> GetShiftByIdAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Shifts
            .AsNoTracking()
            .Where(item => item.Id == shiftId)
            .Select(item => new ShiftRecordDto
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                BreakStartTime = item.BreakStartTime,
                BreakEndTime = item.BreakEndTime,
                RequiredWorkingMinutes = item.RequiredWorkingMinutes,
                GracePeriodMinutes = item.GracePeriodMinutes,
                IsOvernight = item.IsOvernight,
                IsActive = item.IsActive,
                AssignmentCount = item.EmployeeScheduleAssignments.Count,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return record ?? throw new NotFoundException($"Shift '{shiftId}' was not found.");
    }

    public async Task<ShiftRecordDto> CreateShiftAsync(SaveShiftRequestDto request, CancellationToken cancellationToken = default)
    {
        var record = new Shift
        {
            CreatedAtUtc = DateTime.UtcNow
        };

        ApplyShift(record, request);
        await EnsureShiftUniquenessAsync(record.Code, record.Name, null, cancellationToken);

        _dbContext.Shifts.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Shift {ShiftCode} created.", record.Code);
        return await GetShiftByIdAsync(record.Id, cancellationToken);
    }

    public async Task<ShiftRecordDto> UpdateShiftAsync(Guid shiftId, SaveShiftRequestDto request, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Shifts.SingleOrDefaultAsync(item => item.Id == shiftId, cancellationToken)
            ?? throw new NotFoundException($"Shift '{shiftId}' was not found.");

        ApplyShift(record, request);
        record.UpdatedAtUtc = DateTime.UtcNow;
        await EnsureShiftUniquenessAsync(record.Code, record.Name, shiftId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Shift {ShiftId} updated.", shiftId);
        return await GetShiftByIdAsync(shiftId, cancellationToken);
    }

    public async Task DeleteShiftAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Shifts
            .Include(item => item.EmployeeScheduleAssignments)
            .SingleOrDefaultAsync(item => item.Id == shiftId, cancellationToken)
            ?? throw new NotFoundException($"Shift '{shiftId}' was not found.");

        if (record.EmployeeScheduleAssignments.Count > 0)
        {
            throw new BadRequestException("This shift is already referenced by schedule assignments. Deactivate it instead of deleting it.");
        }

        _dbContext.Shifts.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Shift {ShiftId} deleted.", shiftId);
    }

    public async Task<PagedResultDto<EmployeeScheduleAssignmentRecordDto>> GetScheduleAssignmentsAsync(EmployeeScheduleAssignmentListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.EmployeeScheduleAssignments
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.WorkSchedule)
            .Include(record => record.Shift)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                (record.Employee != null && (
                    record.Employee.EmployeeCode.Contains(search) ||
                    record.Employee.FirstName.Contains(search) ||
                    record.Employee.MiddleName.Contains(search) ||
                    record.Employee.LastName.Contains(search))) ||
                (record.Employee != null && record.Employee.Department != null && record.Employee.Department.Name.Contains(search)) ||
                (record.Employee != null && record.Employee.Branch != null && record.Employee.Branch.Name.Contains(search)) ||
                (record.WorkSchedule != null && record.WorkSchedule.Name.Contains(search)) ||
                (record.Shift != null && record.Shift.Name.Contains(search)));
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.BranchId == query.BranchId.Value);
        }

        if (query.IsActive is not null)
        {
            source = source.Where(record => record.IsActive == query.IsActive.Value);
        }

        if (query.Date is not null)
        {
            var date = query.Date.Value;
            source = source.Where(record =>
                record.EffectiveStartDate <= date &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= date));
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("start", true) => source.OrderByDescending(record => record.EffectiveStartDate).ThenByDescending(record => record.Employee != null ? record.Employee.LastName : string.Empty),
            ("start", false) => source.OrderBy(record => record.EffectiveStartDate).ThenBy(record => record.Employee != null ? record.Employee.LastName : string.Empty),
            ("schedule", true) => source.OrderByDescending(record => record.WorkSchedule != null ? record.WorkSchedule.Name : string.Empty).ThenByDescending(record => record.Employee != null ? record.Employee.LastName : string.Empty),
            ("schedule", false) => source.OrderBy(record => record.WorkSchedule != null ? record.WorkSchedule.Name : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.LastName : string.Empty),
            (_, true) => source.OrderByDescending(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenByDescending(record => record.EffectiveStartDate),
            _ => source.OrderBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenByDescending(record => record.EffectiveStartDate)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<EmployeeScheduleAssignmentRecordDto>
        {
            Items = items.Select(MapScheduleAssignment).ToArray(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<EmployeeScheduleAssignmentRecordDto> GetScheduleAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.EmployeeScheduleAssignments
            .AsNoTracking()
            .Include(item => item.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(item => item.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(item => item.WorkSchedule)
            .Include(item => item.Shift)
            .SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken);

        return record is null
            ? throw new NotFoundException($"Schedule assignment '{assignmentId}' was not found.")
            : MapScheduleAssignment(record);
    }

    public async Task<EmployeeScheduleAssignmentRecordDto> CreateScheduleAssignmentAsync(SaveEmployeeScheduleAssignmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var employee = await _dbContext.Employees.SingleOrDefaultAsync(record => record.Id == request.EmployeeId, cancellationToken)
            ?? throw new BadRequestException("The selected employee does not exist.");

        if (!employee.IsActive)
        {
            throw new BadRequestException("Inactive employees cannot receive new schedule assignments.");
        }

        var workSchedule = await _dbContext.WorkSchedules.SingleOrDefaultAsync(record => record.Id == request.WorkScheduleId, cancellationToken)
            ?? throw new BadRequestException("The selected work schedule does not exist.");

        var shift = await ResolveShiftAsync(request.ShiftId, cancellationToken);
        ValidateAssignmentShape(workSchedule, shift);

        var record = new EmployeeScheduleAssignment
        {
            CreatedAtUtc = DateTime.UtcNow
        };

        ApplyAssignment(record, request);
        await EnsureAssignmentDoesNotOverlapAsync(record.EmployeeId, record.EffectiveStartDate, record.EffectiveEndDate, null, cancellationToken);

        _dbContext.EmployeeScheduleAssignments.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncEmployeeWorkScheduleAsync(record.EmployeeId, cancellationToken);
        _logger.LogInformation("Schedule assignment {AssignmentId} created for employee {EmployeeId}.", record.Id, record.EmployeeId);
        return await GetScheduleAssignmentByIdAsync(record.Id, cancellationToken);
    }

    public async Task<EmployeeScheduleAssignmentRecordDto> UpdateScheduleAssignmentAsync(Guid assignmentId, SaveEmployeeScheduleAssignmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.EmployeeScheduleAssignments.SingleOrDefaultAsync(item => item.Id == assignmentId, cancellationToken)
            ?? throw new NotFoundException($"Schedule assignment '{assignmentId}' was not found.");

        var employee = await _dbContext.Employees.SingleOrDefaultAsync(item => item.Id == request.EmployeeId, cancellationToken)
            ?? throw new BadRequestException("The selected employee does not exist.");

        if (!employee.IsActive)
        {
            throw new BadRequestException("Inactive employees cannot receive schedule assignments.");
        }

        var workSchedule = await _dbContext.WorkSchedules.SingleOrDefaultAsync(item => item.Id == request.WorkScheduleId, cancellationToken)
            ?? throw new BadRequestException("The selected work schedule does not exist.");

        var shift = await ResolveShiftAsync(request.ShiftId, cancellationToken);
        ValidateAssignmentShape(workSchedule, shift);

        ApplyAssignment(record, request);
        record.UpdatedAtUtc = DateTime.UtcNow;
        await EnsureAssignmentDoesNotOverlapAsync(record.EmployeeId, record.EffectiveStartDate, record.EffectiveEndDate, assignmentId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await SyncEmployeeWorkScheduleAsync(record.EmployeeId, cancellationToken);
        _logger.LogInformation("Schedule assignment {AssignmentId} updated.", assignmentId);
        return await GetScheduleAssignmentByIdAsync(assignmentId, cancellationToken);
    }

    private async Task<Shift?> ResolveShiftAsync(Guid? shiftId, CancellationToken cancellationToken)
    {
        if (shiftId is null)
        {
            return null;
        }

        return await _dbContext.Shifts.SingleOrDefaultAsync(record => record.Id == shiftId.Value, cancellationToken)
            ?? throw new BadRequestException("The selected shift does not exist.");
    }

    private void ValidateAssignmentShape(WorkSchedule workSchedule, Shift? shift)
    {
        if (!AttendanceScheduleTypes.All.Contains(workSchedule.ScheduleType, StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("The selected work schedule has an invalid schedule type.");
        }

        if (!workSchedule.IsActive)
        {
            throw new BadRequestException("The selected work schedule is inactive.");
        }

        if ((string.Equals(workSchedule.ScheduleType, AttendanceScheduleTypes.Fixed, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(workSchedule.ScheduleType, AttendanceScheduleTypes.Shifting, StringComparison.OrdinalIgnoreCase)) &&
            shift is null)
        {
            throw new BadRequestException("Fixed and shifting schedules require a shift assignment.");
        }

        if (shift is not null && !shift.IsActive)
        {
            throw new BadRequestException("The selected shift is inactive.");
        }
    }

    private async Task EnsureAssignmentDoesNotOverlapAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly? endDate,
        Guid? existingAssignmentId,
        CancellationToken cancellationToken)
    {
        var overlappingAssignmentExists = await _dbContext.EmployeeScheduleAssignments.AnyAsync(
            record => record.EmployeeId == employeeId &&
                      record.Id != existingAssignmentId &&
                      record.EffectiveStartDate <= (endDate ?? DateOnly.MaxValue) &&
                      (record.EffectiveEndDate ?? DateOnly.MaxValue) >= startDate,
            cancellationToken);

        if (overlappingAssignmentExists)
        {
            throw new ConflictException("This employee already has an overlapping schedule assignment for the selected date range.");
        }
    }

    private async Task EnsureWorkScheduleUniquenessAsync(string code, string name, Guid? existingId, CancellationToken cancellationToken)
    {
        if (await _dbContext.WorkSchedules.AnyAsync(record => record.Code == code && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A work schedule with code '{code}' already exists.");
        }

        if (await _dbContext.WorkSchedules.AnyAsync(record => record.Name == name && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A work schedule named '{name}' already exists.");
        }
    }

    private async Task EnsureShiftUniquenessAsync(string code, string name, Guid? existingId, CancellationToken cancellationToken)
    {
        if (await _dbContext.Shifts.AnyAsync(record => record.Code == code && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A shift with code '{code}' already exists.");
        }

        if (await _dbContext.Shifts.AnyAsync(record => record.Name == name && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A shift named '{name}' already exists.");
        }
    }

    private static async Task<PagedResultDto<T>> ToPageAsync<T>(
        IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static void ValidateScheduleType(string scheduleType)
    {
        if (!AttendanceScheduleTypes.All.Contains(scheduleType.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Schedule type must be fixed, flexible, or shifting.");
        }
    }

    private void ApplyAssignment(EmployeeScheduleAssignment record, SaveEmployeeScheduleAssignmentRequestDto request)
    {
        var effectiveStartDate = request.EffectiveStartDate!.Value;
        var effectiveEndDate = request.EffectiveEndDate;

        if (!request.IsActive && effectiveEndDate is null)
        {
            var businessToday = _attendanceCalculationService.GetBusinessToday();
            effectiveEndDate = businessToday < effectiveStartDate ? effectiveStartDate : businessToday;
        }

        record.EmployeeId = request.EmployeeId!.Value;
        record.WorkScheduleId = request.WorkScheduleId!.Value;
        record.ShiftId = request.ShiftId;
        record.EffectiveStartDate = effectiveStartDate;
        record.EffectiveEndDate = effectiveEndDate;
        record.RestDays = _attendanceCalculationService.SerializeRestDayValues(request.RestDayValues);
        record.IsActive = request.IsActive;
    }

    private static void ApplyWorkSchedule(WorkSchedule record, SaveWorkScheduleRequestDto request)
    {
        record.Code = request.Code.Trim().ToUpperInvariant();
        record.Name = request.Name.Trim();
        record.Description = request.Description.Trim();
        record.ScheduleType = request.ScheduleType.Trim().ToLowerInvariant();
        record.RequiredWorkingMinutes = request.RequiredWorkingMinutes;
        record.GracePeriodMinutes = request.GracePeriodMinutes;
        record.BreakDurationMinutes = request.BreakDurationMinutes;
        record.IsActive = request.IsActive;
    }

    private static void ApplyShift(Shift record, SaveShiftRequestDto request)
    {
        record.Code = request.Code.Trim().ToUpperInvariant();
        record.Name = request.Name.Trim();
        record.StartTime = request.StartTime!.Value;
        record.EndTime = request.EndTime!.Value;
        record.BreakStartTime = request.BreakStartTime;
        record.BreakEndTime = request.BreakEndTime;
        record.RequiredWorkingMinutes = request.RequiredWorkingMinutes;
        record.GracePeriodMinutes = request.GracePeriodMinutes;
        record.IsOvernight = request.IsOvernight;
        record.IsActive = request.IsActive;
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

    private static string ToRestDayLabel(int value)
    {
        return value switch
        {
            0 => "Sunday",
            1 => "Monday",
            2 => "Tuesday",
            3 => "Wednesday",
            4 => "Thursday",
            5 => "Friday",
            6 => "Saturday",
            _ => "Unknown"
        };
    }

    private EmployeeScheduleAssignmentRecordDto MapScheduleAssignment(EmployeeScheduleAssignment record)
    {
        var restDayValues = _attendanceCalculationService.ParseRestDayValues(record.RestDays);

        return new EmployeeScheduleAssignmentRecordDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = record.Employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = record.Employee is null
                ? string.Empty
                : BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
            DepartmentName = record.Employee?.Department?.Name ?? string.Empty,
            BranchName = record.Employee?.Branch?.Name ?? string.Empty,
            WorkScheduleId = record.WorkScheduleId,
            WorkScheduleCode = record.WorkSchedule?.Code ?? string.Empty,
            WorkScheduleName = record.WorkSchedule?.Name ?? string.Empty,
            WorkScheduleType = record.WorkSchedule?.ScheduleType ?? string.Empty,
            WorkScheduleIsActive = record.WorkSchedule?.IsActive ?? false,
            ShiftId = record.ShiftId,
            ShiftCode = record.Shift?.Code ?? string.Empty,
            ShiftName = record.Shift?.Name ?? string.Empty,
            ShiftIsActive = record.Shift?.IsActive ?? false,
            EffectiveStartDate = record.EffectiveStartDate,
            EffectiveEndDate = record.EffectiveEndDate,
            RestDayValues = restDayValues,
            RestDayLabels = restDayValues.Select(ToRestDayLabel).ToArray(),
            IsActive = record.IsActive,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private async Task SyncEmployeeWorkScheduleAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees.SingleOrDefaultAsync(record => record.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            return;
        }

        var today = _attendanceCalculationService.GetBusinessToday();
        var assignments = await _dbContext.EmployeeScheduleAssignments
            .AsNoTracking()
            .Include(record => record.WorkSchedule)
            .Include(record => record.Shift)
            .Where(record => record.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);

        var resolvedSchedule = _attendanceCalculationService.ResolveSchedule(assignments, today);
        employee.WorkSchedule = !resolvedSchedule.HasScheduleAssignment
            ? string.Empty
            : string.IsNullOrWhiteSpace(resolvedSchedule.ShiftName)
                ? resolvedSchedule.WorkScheduleName
                : $"{resolvedSchedule.WorkScheduleName} ({resolvedSchedule.ShiftName})";
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
