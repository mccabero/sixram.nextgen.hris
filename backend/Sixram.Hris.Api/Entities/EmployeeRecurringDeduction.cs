namespace Sixram.Api.Entities;

public class EmployeeRecurringDeduction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid DeductionTypeId { get; set; }

    public DeductionType? DeductionType { get; set; }

    public decimal Amount { get; set; }

    public string Frequency { get; set; } = string.Empty;

    public decimal? Balance { get; set; }

    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public string Remarks { get; set; } = string.Empty;

    public ICollection<PayrollDeductionLine> PayrollDeductionLines { get; set; } = new List<PayrollDeductionLine>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

