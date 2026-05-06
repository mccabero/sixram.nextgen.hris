namespace Sixram.Api.Entities;

public class PayrollEarningLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PayrollRunItemId { get; set; }

    public PayrollRunItem? PayrollRunItem { get; set; }

    public Guid? EarningTypeId { get; set; }

    public EarningType? EarningType { get; set; }

    public string EarningTypeCodeSnapshot { get; set; } = string.Empty;

    public string EarningTypeNameSnapshot { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? Rate { get; set; }

    public string Source { get; set; } = string.Empty;

    public bool Taxable { get; set; } = true;

    public bool IsManual { get; set; }

    public Guid? PayrollAdjustmentId { get; set; }

    public PayrollAdjustment? PayrollAdjustment { get; set; }

    public Guid? EmployeeRecurringEarningId { get; set; }

    public EmployeeRecurringEarning? EmployeeRecurringEarning { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

