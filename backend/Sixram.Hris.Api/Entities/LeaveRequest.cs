namespace Sixram.Api.Entities;

public class LeaveRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid LeaveTypeId { get; set; }

    public LeaveType? LeaveType { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string StartDayType { get; set; } = string.Empty;

    public string EndDayType { get; set; } = string.Empty;

    public decimal TotalLeaveDays { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string AttachmentOriginalFileName { get; set; } = string.Empty;

    public string AttachmentPath { get; set; } = string.Empty;

    public string AttachmentMimeType { get; set; } = string.Empty;

    public long? AttachmentFileSize { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? SubmittedAtUtc { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public DateTime? RejectedAtUtc { get; set; }

    public DateTime? CancelledAtUtc { get; set; }

    public string? CurrentApproverUserId { get; set; }

    public ApplicationUser? CurrentApproverUser { get; set; }

    public string DecisionRemarks { get; set; } = string.Empty;

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ApplicationUser? UpdatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<LeaveBalanceTransaction> LeaveBalanceTransactions { get; set; } = new List<LeaveBalanceTransaction>();

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
