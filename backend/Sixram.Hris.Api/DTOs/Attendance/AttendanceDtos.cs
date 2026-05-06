using System.ComponentModel.DataAnnotations;
using Sixram.Api.DTOs.Common;

namespace Sixram.Api.DTOs.Attendance;

public sealed class AttendanceSetupSummaryDto
{
    public int WorkScheduleCount { get; init; }

    public int ActiveWorkScheduleCount { get; init; }

    public int ShiftCount { get; init; }

    public int ActiveShiftCount { get; init; }

    public int ScheduleAssignmentCount { get; init; }

    public int ActiveScheduleAssignmentCount { get; init; }
}

public sealed class AttendanceDashboardSummaryDto
{
    public DateOnly AttendanceDate { get; init; }

    public int PresentCount { get; init; }

    public int LateCount { get; init; }

    public int AbsentCount { get; init; }

    public int IncompleteCount { get; init; }

    public int RestDayCount { get; init; }

    public int NoScheduleCount { get; init; }

    public int UndertimeCount { get; init; }

    public int PendingAdjustmentRequestCount { get; init; }

    public int EmployeesWithoutScheduleAssignmentCount { get; init; }

    public IReadOnlyList<AttendanceTrendPointDto> Trend { get; init; } = Array.Empty<AttendanceTrendPointDto>();
}

public sealed class AttendanceTrendPointDto
{
    public DateOnly Date { get; init; }

    public int PresentCount { get; init; }

    public int LateCount { get; init; }

    public int AbsentCount { get; init; }

    public int IncompleteCount { get; init; }
}

public sealed class EmployeeAttendanceOptionDto
{
    public Guid Id { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class WorkScheduleOptionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string ScheduleType { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class ShiftOptionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }

    public bool IsOvernight { get; init; }

    public bool IsActive { get; init; }
}

public sealed class AttendanceListOptionsDto
{
    public IReadOnlyList<EmployeeAttendanceOptionDto> Employees { get; init; } = Array.Empty<EmployeeAttendanceOptionDto>();

    public IReadOnlyList<LookupOptionDto> Departments { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> Branches { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<WorkScheduleOptionDto> WorkSchedules { get; init; } = Array.Empty<WorkScheduleOptionDto>();

    public IReadOnlyList<ShiftOptionDto> Shifts { get; init; } = Array.Empty<ShiftOptionDto>();

    public IReadOnlyList<string> Statuses { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Sources { get; init; } = Array.Empty<string>();
}

public sealed class WorkScheduleRecordDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ScheduleType { get; init; } = string.Empty;

    public int RequiredWorkingMinutes { get; init; }

    public int GracePeriodMinutes { get; init; }

    public int BreakDurationMinutes { get; init; }

    public bool IsActive { get; init; }

    public int AssignmentCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class ShiftRecordDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public TimeOnly StartTime { get; init; }

    public TimeOnly EndTime { get; init; }

    public TimeOnly? BreakStartTime { get; init; }

    public TimeOnly? BreakEndTime { get; init; }

    public int RequiredWorkingMinutes { get; init; }

    public int GracePeriodMinutes { get; init; }

    public bool IsOvernight { get; init; }

    public bool IsActive { get; init; }

    public int AssignmentCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class EmployeeScheduleAssignmentRecordDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public Guid WorkScheduleId { get; init; }

    public string WorkScheduleCode { get; init; } = string.Empty;

    public string WorkScheduleName { get; init; } = string.Empty;

    public string WorkScheduleType { get; init; } = string.Empty;

    public bool WorkScheduleIsActive { get; init; }

    public Guid? ShiftId { get; init; }

    public string ShiftCode { get; init; } = string.Empty;

    public string ShiftName { get; init; } = string.Empty;

    public bool ShiftIsActive { get; init; }

    public DateOnly EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public IReadOnlyList<int> RestDayValues { get; init; } = Array.Empty<int>();

    public IReadOnlyList<string> RestDayLabels { get; init; } = Array.Empty<string>();

    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class AttendanceRecordListItemDto
{
    public Guid? AttendanceRecordId { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public DateOnly AttendanceDate { get; init; }

    public string WorkScheduleName { get; init; } = string.Empty;

    public string ShiftName { get; init; } = string.Empty;

    public DateTime? ScheduledStartTime { get; init; }

    public DateTime? ScheduledEndTime { get; init; }

    public DateTime? ActualTimeIn { get; init; }

    public DateTime? ActualTimeOut { get; init; }

    public DateTime? BreakStartTime { get; init; }

    public DateTime? BreakEndTime { get; init; }

    public int TotalWorkedMinutes { get; init; }

    public int LateMinutes { get; init; }

    public int UndertimeMinutes { get; init; }

    public int OvertimeMinutes { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public string Remarks { get; init; } = string.Empty;

    public bool HasScheduleAssignment { get; init; }

    public bool HasBackingRecord { get; init; }

    public string CreatedByDisplayName { get; init; } = string.Empty;

    public string UpdatedByDisplayName { get; init; } = string.Empty;

    public DateTime? CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class WorkScheduleListQueryDto : PagedQueryDto
{
    public bool? IsActive { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "name";

    public bool Descending { get; init; }
}

public sealed class ShiftListQueryDto : PagedQueryDto
{
    public bool? IsActive { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "name";

    public bool Descending { get; init; }
}

public sealed class EmployeeScheduleAssignmentListQueryDto : PagedQueryDto, IValidatableObject
{
    public Guid? EmployeeId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public bool? IsActive { get; init; }

    public DateOnly? Date { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "employee";

    public bool Descending { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PageSize > 100)
        {
            yield return new ValidationResult("Page size cannot exceed 100.", [nameof(PageSize)]);
        }
    }
}

public sealed class AttendanceRecordListQueryDto : PagedQueryDto, IValidatableObject
{
    public Guid? EmployeeId { get; init; }

    public IReadOnlyList<Guid> EmployeeIds { get; init; } = Array.Empty<Guid>();

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(32)]
    public string Source { get; init; } = string.Empty;

    [MaxLength(32)]
    public string SortBy { get; init; } = "date";

    public bool Descending { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom is not null && DateTo is not null && DateTo < DateFrom)
        {
            yield return new ValidationResult(
                "End date cannot be earlier than the start date.",
                [nameof(DateTo)]);
        }
    }
}

public sealed class SaveWorkScheduleRequestDto
{
    [Required]
    [MaxLength(32)]
    [RegularExpression("^[A-Za-z0-9._/-]+$", ErrorMessage = "Codes may contain letters, numbers, periods, underscores, slashes, and hyphens only.")]
    public string Code { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(512)]
    public string Description { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string ScheduleType { get; init; } = string.Empty;

    [Range(1, 1_440)]
    public int RequiredWorkingMinutes { get; init; }

    [Range(0, 720)]
    public int GracePeriodMinutes { get; init; }

    [Range(0, 720)]
    public int BreakDurationMinutes { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class SaveShiftRequestDto : IValidatableObject
{
    [Required]
    [MaxLength(32)]
    [RegularExpression("^[A-Za-z0-9._/-]+$", ErrorMessage = "Codes may contain letters, numbers, periods, underscores, slashes, and hyphens only.")]
    public string Code { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public TimeOnly? StartTime { get; init; }

    [Required]
    public TimeOnly? EndTime { get; init; }

    public TimeOnly? BreakStartTime { get; init; }

    public TimeOnly? BreakEndTime { get; init; }

    [Range(1, 1_440)]
    public int RequiredWorkingMinutes { get; init; }

    [Range(0, 720)]
    public int GracePeriodMinutes { get; init; }

    public bool IsOvernight { get; init; }

    public bool IsActive { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((BreakStartTime is null) != (BreakEndTime is null))
        {
            yield return new ValidationResult(
                "Both break start and break end must be provided together.",
                [nameof(BreakStartTime), nameof(BreakEndTime)]);
        }

        if (BreakStartTime is not null && BreakEndTime is not null && BreakEndTime <= BreakStartTime)
        {
            yield return new ValidationResult(
                "Break end time must be later than break start time.",
                [nameof(BreakEndTime)]);
        }
    }
}

public sealed class SaveEmployeeScheduleAssignmentRequestDto : IValidatableObject
{
    [Required]
    public Guid? EmployeeId { get; init; }

    [Required]
    public Guid? WorkScheduleId { get; init; }

    public Guid? ShiftId { get; init; }

    [Required]
    public DateOnly? EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public IReadOnlyList<int> RestDayValues { get; init; } = Array.Empty<int>();

    public bool IsActive { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EffectiveEndDate is not null && EffectiveStartDate is not null && EffectiveEndDate < EffectiveStartDate)
        {
            yield return new ValidationResult(
                "Effective end date cannot be earlier than the effective start date.",
                [nameof(EffectiveEndDate)]);
        }

        if (RestDayValues.Any(value => value < 0 || value > 6))
        {
            yield return new ValidationResult(
                "Rest day values must be between 0 (Sunday) and 6 (Saturday).",
                [nameof(RestDayValues)]);
        }

        if (RestDayValues.Count != RestDayValues.Distinct().Count())
        {
            yield return new ValidationResult(
                "Rest day values cannot contain duplicates.",
                [nameof(RestDayValues)]);
        }
    }
}

public sealed class SaveAttendanceRecordRequestDto : IValidatableObject
{
    [Required]
    public Guid? EmployeeId { get; init; }

    [Required]
    public DateOnly? AttendanceDate { get; init; }

    public DateTime? ActualTimeIn { get; init; }

    public DateTime? ActualTimeOut { get; init; }

    public DateTime? BreakStartTime { get; init; }

    public DateTime? BreakEndTime { get; init; }

    [Required]
    [MaxLength(32)]
    public string Source { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ActualTimeIn is not null && ActualTimeOut is not null && ActualTimeOut < ActualTimeIn)
        {
            yield return new ValidationResult(
                "Actual time out cannot be earlier than actual time in.",
                [nameof(ActualTimeOut)]);
        }

        if ((BreakStartTime is null) != (BreakEndTime is null))
        {
            yield return new ValidationResult(
                "Both break start and break end must be provided together.",
                [nameof(BreakStartTime), nameof(BreakEndTime)]);
        }

        if (BreakStartTime is not null && BreakEndTime is not null && BreakEndTime < BreakStartTime)
        {
            yield return new ValidationResult(
                "Break end time cannot be earlier than break start time.",
                [nameof(BreakEndTime)]);
        }

        if ((BreakStartTime is not null || BreakEndTime is not null) && ActualTimeIn is null)
        {
            yield return new ValidationResult(
                "Break times require an actual time in.",
                [nameof(BreakStartTime), nameof(BreakEndTime)]);
        }
    }
}
