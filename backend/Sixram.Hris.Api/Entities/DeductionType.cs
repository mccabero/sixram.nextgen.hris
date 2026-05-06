namespace Sixram.Api.Entities;

public class DeductionType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public bool PreTax { get; set; }

    public bool Recurring { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<EmployeeRecurringDeduction> EmployeeRecurringDeductions { get; set; } = new List<EmployeeRecurringDeduction>();

    public ICollection<PayrollDeductionLine> PayrollDeductionLines { get; set; } = new List<PayrollDeductionLine>();

    public ICollection<PayrollAdjustment> PayrollAdjustments { get; set; } = new List<PayrollAdjustment>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

