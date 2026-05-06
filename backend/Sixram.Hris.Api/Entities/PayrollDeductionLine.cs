namespace Sixram.Api.Entities;

public class PayrollDeductionLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PayrollRunItemId { get; set; }

    public PayrollRunItem? PayrollRunItem { get; set; }

    public Guid? DeductionTypeId { get; set; }

    public DeductionType? DeductionType { get; set; }

    public string DeductionTypeCodeSnapshot { get; set; } = string.Empty;

    public string DeductionTypeNameSnapshot { get; set; } = string.Empty;

    public string DeductionCategorySnapshot { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Source { get; set; } = string.Empty;

    public bool PreTax { get; set; }

    public bool IsManual { get; set; }

    public Guid? PayrollAdjustmentId { get; set; }

    public PayrollAdjustment? PayrollAdjustment { get; set; }

    public Guid? EmployeeRecurringDeductionId { get; set; }

    public EmployeeRecurringDeduction? EmployeeRecurringDeduction { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

