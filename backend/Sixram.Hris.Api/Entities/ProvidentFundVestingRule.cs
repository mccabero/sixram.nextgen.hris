namespace Sixram.Api.Entities;

public class ProvidentFundVestingRule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PolicyId { get; set; }

    public ProvidentFundPolicy? Policy { get; set; }

    public int YearsOfService { get; set; }

    public decimal VestedPercentage { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
