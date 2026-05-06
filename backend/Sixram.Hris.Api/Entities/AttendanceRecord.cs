namespace Sixram.Api.Entities;

public class AttendanceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public DateTime? ScheduledStartTime { get; set; }

    public DateTime? ScheduledEndTime { get; set; }

    public DateTime? ActualTimeIn { get; set; }

    public DateTime? ActualTimeOut { get; set; }

    public DateTime? BreakStartTime { get; set; }

    public DateTime? BreakEndTime { get; set; }

    public int TotalWorkedMinutes { get; set; }

    public int LateMinutes { get; set; }

    public int UndertimeMinutes { get; set; }

    public int OvertimeMinutes { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    public Guid? LeaveRequestId { get; set; }

    public LeaveRequest? LeaveRequest { get; set; }

    public ICollection<AttendanceAdjustmentRequest> AdjustmentRequests { get; set; } = new List<AttendanceAdjustmentRequest>();

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ApplicationUser? UpdatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
