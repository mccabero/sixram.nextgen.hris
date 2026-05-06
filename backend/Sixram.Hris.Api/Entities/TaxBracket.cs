namespace Sixram.Api.Entities;

public class TaxBracket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TaxTableId { get; set; }

    public TaxTable? TaxTable { get; set; }

    public decimal MinTaxableIncome { get; set; }

    public decimal? MaxTaxableIncome { get; set; }

    public decimal BaseTax { get; set; }

    public decimal TaxRate { get; set; }

    public decimal ExcessOver { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

