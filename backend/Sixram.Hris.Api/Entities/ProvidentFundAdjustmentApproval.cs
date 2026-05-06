namespace Sixram.Api.Entities;

public class ProvidentFundAdjustmentApproval
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AdjustmentId { get; set; }

    public ProvidentFundAdjustment? Adjustment { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? ActorUserId { get; set; }

    public ApplicationUser? ActorUser { get; set; }

    public string ActorNameSnapshot { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
