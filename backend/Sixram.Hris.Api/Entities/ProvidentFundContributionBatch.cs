namespace Sixram.Api.Entities;

public class ProvidentFundContributionBatch
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string BatchNumber { get; set; } = string.Empty;

    public int Month { get; set; }

    public int Year { get; set; }

    public Guid? PolicyId { get; set; }

    public ProvidentFundPolicy? Policy { get; set; }

    public bool IsSupplemental { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public string? ReviewedByUserId { get; set; }

    public ApplicationUser? ReviewedByUser { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string? PostedByUserId { get; set; }

    public ApplicationUser? PostedByUser { get; set; }

    public DateTime? PostingDate { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public ICollection<ProvidentFundContributionBatchLine> Lines { get; set; } = new List<ProvidentFundContributionBatchLine>();

    public ICollection<ProvidentFundLedgerTransaction> LedgerTransactions { get; set; } = new List<ProvidentFundLedgerTransaction>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
