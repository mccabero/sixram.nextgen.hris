namespace Sixram.Api.Entities;

public class GovernmentContributionBracket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid GovernmentContributionTableId { get; set; }

    public GovernmentContributionTable? GovernmentContributionTable { get; set; }

    public decimal MinCompensation { get; set; }

    public decimal? MaxCompensation { get; set; }

    public decimal? EmployeeShareAmount { get; set; }

    public decimal? EmployeeShareRate { get; set; }

    public decimal? EmployerShareAmount { get; set; }

    public decimal? EmployerShareRate { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

