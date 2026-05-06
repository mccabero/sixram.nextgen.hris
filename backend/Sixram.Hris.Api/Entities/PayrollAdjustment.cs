namespace Sixram.Api.Entities;

public class PayrollAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid? PayPeriodId { get; set; }

    public PayPeriod? PayPeriod { get; set; }

    public Guid? PayrollRunId { get; set; }

    public PayrollRun? PayrollRun { get; set; }

    public string AdjustmentType { get; set; } = string.Empty;

    public Guid? EarningTypeId { get; set; }

    public EarningType? EarningType { get; set; }

    public Guid? DeductionTypeId { get; set; }

    public DeductionType? DeductionType { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? RequestedByUserId { get; set; }

    public ApplicationUser? RequestedByUser { get; set; }

    public string? ApprovedByUserId { get; set; }

    public ApplicationUser? ApprovedByUser { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public DateTime? AppliedAtUtc { get; set; }

    public string DecisionRemarks { get; set; } = string.Empty;

    public ICollection<PayrollEarningLine> PayrollEarningLines { get; set; } = new List<PayrollEarningLine>();

    public ICollection<PayrollDeductionLine> PayrollDeductionLines { get; set; } = new List<PayrollDeductionLine>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

