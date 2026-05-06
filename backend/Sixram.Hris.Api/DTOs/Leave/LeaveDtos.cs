using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;

namespace Sixram.Api.DTOs.Leave;

public sealed class LeaveDashboardSummaryDto
{
    public DateOnly BusinessDate { get; init; }

    public int PendingLeaveRequestCount { get; init; }

    public int ApprovedLeavesTodayCount { get; init; }

    public int EmployeesOnLeaveTodayCount { get; init; }

    public int LowBalanceCount { get; init; }

    public int NegativeBalanceCount { get; init; }

    public int UpcomingApprovedLeaveCount { get; init; }

    public int AttendanceConflictCount { get; init; }
}

public sealed class LeaveTypeOptionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool AllowHalfDay { get; init; }

    public bool RequiresAttachment { get; init; }

    public bool RequiresReason { get; init; }

    public bool AllowNegativeBalance { get; init; }

    public decimal? DefaultAnnualCredits { get; init; }

    public bool IsActive { get; init; }
}

public sealed class LeaveTypeRecordDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool IsPaid { get; init; }

    public bool RequiresAttachment { get; init; }

    public bool RequiresReason { get; init; }

    public bool AllowHalfDay { get; init; }

    public bool AllowNegativeBalance { get; init; }

    public decimal? DefaultAnnualCredits { get; init; }

    public decimal? MaxDaysPerRequest { get; init; }

    public int? MinDaysBeforeFiling { get; init; }

    public string GenderRestriction { get; init; } = string.Empty;

    public IReadOnlyList<Guid> EmploymentTypeRestrictionIds { get; init; } = Array.Empty<Guid>();

    public bool CountsRestDays { get; init; }

    public bool CountsHolidays { get; init; }

    public bool AllowDuringProbationaryPeriod { get; init; }

    public bool IsActive { get; init; }

    public int EmployeeCount { get; init; }

    public int PendingRequestCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class LeaveTypeListQueryDto : PagedQueryDto
{
    public bool? IsActive { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "name";

    public bool Descending { get; init; }
}

public sealed class SaveLeaveTypeRequestDto : IValidatableObject
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

    public bool IsPaid { get; init; } = true;

    public bool RequiresAttachment { get; init; }

    public bool RequiresReason { get; init; }

    public bool AllowHalfDay { get; init; } = true;

    public bool AllowNegativeBalance { get; init; }

    [Range(0, 366)]
    public decimal? DefaultAnnualCredits { get; init; }

    [Range(0.5, 366)]
    public decimal? MaxDaysPerRequest { get; init; }

    [Range(0, 366)]
    public int? MinDaysBeforeFiling { get; init; }

    [MaxLength(32)]
    public string GenderRestriction { get; init; } = string.Empty;

    public IReadOnlyList<Guid> EmploymentTypeRestrictionIds { get; init; } = Array.Empty<Guid>();

    public bool CountsRestDays { get; init; }

    public bool CountsHolidays { get; init; }

    public bool AllowDuringProbationaryPeriod { get; init; }

    public bool IsActive { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EmploymentTypeRestrictionIds.Count != EmploymentTypeRestrictionIds.Distinct().Count())
        {
            yield return new ValidationResult(
                "Employment type restrictions cannot contain duplicates.",
                [nameof(EmploymentTypeRestrictionIds)]);
        }
    }
}

public sealed class LeaveManagementOptionsDto
{
    public IReadOnlyList<EmployeeAttendanceOptionDto> Employees { get; init; } = Array.Empty<EmployeeAttendanceOptionDto>();

    public IReadOnlyList<LookupOptionDto> Departments { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> Branches { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> EmploymentTypes { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LeaveTypeOptionDto> LeaveTypes { get; init; } = Array.Empty<LeaveTypeOptionDto>();

    public IReadOnlyList<string> Statuses { get; init; } = Array.Empty<string>();

    public IReadOnlyList<int> PeriodYears { get; init; } = Array.Empty<int>();
}

public sealed class EmployeeLeaveBalanceDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public Guid LeaveTypeId { get; init; }

    public string LeaveTypeCode { get; init; } = string.Empty;

    public string LeaveTypeName { get; init; } = string.Empty;

    public bool LeaveTypeIsPaid { get; init; }

    public int PeriodYear { get; init; }

    public decimal OpeningBalance { get; init; }

    public decimal Accrued { get; init; }

    public decimal Used { get; init; }

    public decimal Pending { get; init; }

    public decimal Adjusted { get; init; }

    public decimal CarriedForward { get; init; }

    public decimal AvailableBalance { get; init; }

    public bool IsLowBalance { get; init; }

    public bool IsNegativeBalance { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class LeaveBalanceTransactionDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public Guid LeaveTypeId { get; init; }

    public int PeriodYear { get; init; }

    public Guid? LeaveRequestId { get; init; }

    public string TransactionType { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public decimal BalanceBefore { get; init; }

    public decimal BalanceAfter { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public string CreatedByDisplayName { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class LeaveBalanceListQueryDto : PagedQueryDto
{
    public Guid? EmployeeId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid? LeaveTypeId { get; init; }

    public int? PeriodYear { get; init; }

    public bool? LowBalanceOnly { get; init; }

    public bool? NegativeBalanceOnly { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "employee";

    public bool Descending { get; init; }
}

public sealed class LeaveRequestListQueryDto : PagedQueryDto, IValidatableObject
{
    public Guid? EmployeeId { get; init; }

    public IReadOnlyList<Guid> EmployeeIds { get; init; } = Array.Empty<Guid>();

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid? LeaveTypeId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    public string ApproverId { get; init; } = string.Empty;

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "submitted";

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

public sealed class LeaveRequestListItemDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public Guid LeaveTypeId { get; init; }

    public string LeaveTypeCode { get; init; } = string.Empty;

    public string LeaveTypeName { get; init; } = string.Empty;

    public bool LeaveTypeIsPaid { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public string StartDayType { get; init; } = string.Empty;

    public string EndDayType { get; init; } = string.Empty;

    public decimal TotalLeaveDays { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime? SubmittedAtUtc { get; init; }

    public DateTime? ApprovedAtUtc { get; init; }

    public DateTime? RejectedAtUtc { get; init; }

    public DateTime? CancelledAtUtc { get; init; }

    public string CurrentApproverDisplayName { get; init; } = string.Empty;

    public string DecisionRemarks { get; init; } = string.Empty;

    public bool HasAttachment { get; init; }

    public string AttachmentOriginalFileName { get; init; } = string.Empty;

    public long? AttachmentFileSize { get; init; }

    public bool HasAttendanceConflict { get; init; }

    public int AttendanceConflictCount { get; init; }

    public decimal AvailableBalanceAfterApproval { get; init; }

    public string CreatedByDisplayName { get; init; } = string.Empty;

    public string UpdatedByDisplayName { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class LeaveCalendarEntryDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string LeaveTypeName { get; init; } = string.Empty;

    public bool LeaveTypeIsPaid { get; init; }

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public decimal TotalLeaveDays { get; init; }

    public string Status { get; init; } = string.Empty;
}

public sealed class LeaveCalendarQueryDto
{
    [Range(1, 9999)]
    public int Year { get; init; }

    [Range(1, 12)]
    public int Month { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid? EmployeeId { get; init; }

    public IReadOnlyList<Guid> EmployeeIds { get; init; } = Array.Empty<Guid>();

    public Guid? LeaveTypeId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;
}

public sealed class LeaveCalendarResponseDto
{
    public int Year { get; init; }

    public int Month { get; init; }

    public IReadOnlyList<LeaveCalendarEntryDto> Entries { get; init; } = Array.Empty<LeaveCalendarEntryDto>();
}

public sealed class SaveLeaveRequestDto : IValidatableObject
{
    [Required]
    public Guid? EmployeeId { get; init; }

    [Required]
    public Guid? LeaveTypeId { get; init; }

    [Required]
    public DateOnly? StartDate { get; init; }

    [Required]
    public DateOnly? EndDate { get; init; }

    [Required]
    [MaxLength(32)]
    public string StartDayType { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string EndDayType { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;

    public IFormFile? Attachment { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate is not null && EndDate is not null && EndDate < StartDate)
        {
            yield return new ValidationResult(
                "End date cannot be earlier than the start date.",
                [nameof(EndDate)]);
        }
    }
}

public sealed class LeaveActionRequestDto
{
    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public sealed class LeaveBalanceAdjustmentRequestDto
{
    [Required]
    public Guid? EmployeeId { get; init; }

    [Required]
    public Guid? LeaveTypeId { get; init; }

    [Required]
    public int? PeriodYear { get; init; }

    [Range(-366, 366)]
    public decimal Amount { get; init; }

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;

    public DateOnly? EffectiveDate { get; init; }
}

public sealed class EmployeeLeaveProfileSummaryDto
{
    public int PendingRequestCount { get; init; }

    public int ApprovedRequestCount { get; init; }

    public int RejectedOrCancelledRequestCount { get; init; }

    public int LowBalanceCount { get; init; }

    public int NegativeBalanceCount { get; init; }
}

public sealed class EmployeeLeaveProfileDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public EmployeeLeaveProfileSummaryDto Summary { get; init; } = new();

    public IReadOnlyList<EmployeeLeaveBalanceDto> Balances { get; init; } = Array.Empty<EmployeeLeaveBalanceDto>();

    public IReadOnlyList<LeaveRequestListItemDto> PendingRequests { get; init; } = Array.Empty<LeaveRequestListItemDto>();

    public IReadOnlyList<LeaveRequestListItemDto> History { get; init; } = Array.Empty<LeaveRequestListItemDto>();

    public IReadOnlyList<LeaveBalanceTransactionDto> Ledger { get; init; } = Array.Empty<LeaveBalanceTransactionDto>();
}
