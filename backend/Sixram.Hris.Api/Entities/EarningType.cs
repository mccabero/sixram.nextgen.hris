namespace Sixram.Api.Entities;

public class EarningType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public bool Taxable { get; set; } = true;

    public bool Recurring { get; set; }

    public bool AffectsThirteenthMonth { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<EmployeeRecurringEarning> EmployeeRecurringEarnings { get; set; } = new List<EmployeeRecurringEarning>();

    public ICollection<PayrollEarningLine> PayrollEarningLines { get; set; } = new List<PayrollEarningLine>();

    public ICollection<PayrollAdjustment> PayrollAdjustments { get; set; } = new List<PayrollAdjustment>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

