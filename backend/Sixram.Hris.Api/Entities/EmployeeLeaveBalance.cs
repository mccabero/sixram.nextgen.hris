namespace Sixram.Api.Entities;

public class EmployeeLeaveBalance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid LeaveTypeId { get; set; }

    public LeaveType? LeaveType { get; set; }

    public int PeriodYear { get; set; }

    public decimal OpeningBalance { get; set; }

    public decimal Accrued { get; set; }

    public decimal Used { get; set; }

    public decimal Pending { get; set; }

    public decimal Adjusted { get; set; }

    public decimal CarriedForward { get; set; }

    public decimal AvailableBalance { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
