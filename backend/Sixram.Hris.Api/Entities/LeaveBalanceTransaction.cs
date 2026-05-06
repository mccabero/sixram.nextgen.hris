namespace Sixram.Api.Entities;

public class LeaveBalanceTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid LeaveTypeId { get; set; }

    public LeaveType? LeaveType { get; set; }

    public int PeriodYear { get; set; }

    public Guid? LeaveRequestId { get; set; }

    public LeaveRequest? LeaveRequest { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal BalanceBefore { get; set; }

    public decimal BalanceAfter { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
