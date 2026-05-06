namespace Sixram.Api.Entities;

public class TaxTable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string PayFrequency { get; set; } = string.Empty;

    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<TaxBracket> Brackets { get; set; } = new List<TaxBracket>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

