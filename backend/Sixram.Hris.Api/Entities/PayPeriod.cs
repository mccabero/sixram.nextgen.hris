namespace Sixram.Api.Entities;

public class PayPeriod
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string PayFrequency { get; set; } = string.Empty;

    public DateOnly PeriodStartDate { get; set; }

    public DateOnly PeriodEndDate { get; set; }

    public DateOnly PayrollDate { get; set; }

    public DateOnly CutoffStartDate { get; set; }

    public DateOnly CutoffEndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    public Guid? PayPeriodTemplateId { get; set; }

    public PayPeriodTemplate? PayPeriodTemplate { get; set; }

    public ICollection<PayrollRun> PayrollRuns { get; set; } = new List<PayrollRun>();

    public ICollection<PayrollAdjustment> PayrollAdjustments { get; set; } = new List<PayrollAdjustment>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

