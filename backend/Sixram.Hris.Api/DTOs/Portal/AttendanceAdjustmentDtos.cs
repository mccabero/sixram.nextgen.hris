using System.ComponentModel.DataAnnotations;
using Sixram.Api.DTOs.Common;

namespace Sixram.Api.DTOs.Portal;

public sealed class AttendanceAdjustmentRequestListQueryDto : PagedQueryDto, IValidatableObject
{
    public Guid? EmployeeId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "created";

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

public sealed class SaveAttendanceAdjustmentRequestDto : IValidatableObject
{
    public Guid? AttendanceRecordId { get; init; }

    public DateOnly? AttendanceDate { get; init; }

    [Required]
    [MaxLength(32)]
    public string RequestType { get; init; } = string.Empty;

    public DateTime? RequestedTimeIn { get; init; }

    public DateTime? RequestedTimeOut { get; init; }

    [MaxLength(1000)]
    public string RequestedRemarks { get; init; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AttendanceRecordId is null && AttendanceDate is null)
        {
            yield return new ValidationResult(
                "Either an attendance record or attendance date is required.",
                [nameof(AttendanceRecordId), nameof(AttendanceDate)]);
        }

        if (RequestedTimeIn is not null && RequestedTimeOut is not null && RequestedTimeOut < RequestedTimeIn)
        {
            yield return new ValidationResult(
                "Requested time out cannot be earlier than requested time in.",
                [nameof(RequestedTimeOut)]);
        }
    }
}

public sealed class AttendanceAdjustmentRequestDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public Guid? AttendanceRecordId { get; init; }

    public DateOnly AttendanceDate { get; init; }

    public string RequestType { get; init; } = string.Empty;

    public DateTime? CurrentTimeIn { get; init; }

    public DateTime? CurrentTimeOut { get; init; }

    public string CurrentRemarks { get; init; } = string.Empty;

    public DateTime? RequestedTimeIn { get; init; }

    public DateTime? RequestedTimeOut { get; init; }

    public string RequestedRemarks { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string CurrentApproverDisplayName { get; init; } = string.Empty;

    public string RequestedByDisplayName { get; init; } = string.Empty;

    public string ReviewedByDisplayName { get; init; } = string.Empty;

    public string ReviewerRemarks { get; init; } = string.Empty;

    public DateTime? ReviewedAtUtc { get; init; }

    public DateTime? AppliedAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}
