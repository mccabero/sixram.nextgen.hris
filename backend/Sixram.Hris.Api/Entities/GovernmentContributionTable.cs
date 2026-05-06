namespace Sixram.Api.Entities;

public class GovernmentContributionTable
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContributionTypeId { get; set; }

    public ContributionType? ContributionType { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<GovernmentContributionBracket> Brackets { get; set; } = new List<GovernmentContributionBracket>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

