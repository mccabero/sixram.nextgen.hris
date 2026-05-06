namespace Sixram.Api.Entities;

public class EmployeeRecurringEarning
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid EarningTypeId { get; set; }

    public EarningType? EarningType { get; set; }

    public decimal Amount { get; set; }

    public string Frequency { get; set; } = string.Empty;

    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public string Remarks { get; set; } = string.Empty;

    public ICollection<PayrollEarningLine> PayrollEarningLines { get; set; } = new List<PayrollEarningLine>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

