namespace Sixram.Api.Entities;

public class AttendanceAdjustmentRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid? AttendanceRecordId { get; set; }

    public AttendanceRecord? AttendanceRecord { get; set; }

    public string RequestedByUserId { get; set; } = string.Empty;

    public ApplicationUser? RequestedByUser { get; set; }

    public string RequestType { get; set; } = string.Empty;

    public DateOnly AttendanceDate { get; set; }

    public DateTime? RequestedTimeIn { get; set; }

    public DateTime? RequestedTimeOut { get; set; }

    public string RequestedRemarks { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? CurrentApproverUserId { get; set; }

    public ApplicationUser? CurrentApproverUser { get; set; }

    public string? ReviewedByUserId { get; set; }

    public ApplicationUser? ReviewedByUser { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string ReviewerRemarks { get; set; } = string.Empty;

    public DateTime? AppliedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
