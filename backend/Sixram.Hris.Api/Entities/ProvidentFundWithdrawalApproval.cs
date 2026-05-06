namespace Sixram.Api.Entities;

public class ProvidentFundWithdrawalApproval
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WithdrawalRequestId { get; set; }

    public ProvidentFundWithdrawalRequest? WithdrawalRequest { get; set; }

    public string StepName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? ActorUserId { get; set; }

    public ApplicationUser? ActorUser { get; set; }

    public string ActorNameSnapshot { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
