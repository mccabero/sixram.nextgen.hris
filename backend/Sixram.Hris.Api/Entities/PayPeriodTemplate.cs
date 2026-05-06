namespace Sixram.Api.Entities;

public class PayPeriodTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string PayFrequency { get; set; } = string.Empty;

    public int PeriodLengthDays { get; set; }

    public int PayrollOffsetDays { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<PayPeriod> PayPeriods { get; set; } = new List<PayPeriod>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

