namespace Sixram.Api.Entities;

public class ProvidentFundLedgerTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string TransactionNumber { get; set; } = string.Empty;

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid EnrollmentId { get; set; }

    public ProvidentFundEnrollment? Enrollment { get; set; }

    public Guid PolicyId { get; set; }

    public ProvidentFundPolicy? Policy { get; set; }

    public Guid? ContributionBatchId { get; set; }

    public ProvidentFundContributionBatch? ContributionBatch { get; set; }

    public Guid? ContributionBatchLineId { get; set; }

    public ProvidentFundContributionBatchLine? ContributionBatchLine { get; set; }

    public DateOnly TransactionDate { get; set; }

    public string TransactionType { get; set; } = string.Empty;

    public string SourceType { get; set; } = string.Empty;

    public string SourceReferenceId { get; set; } = string.Empty;

    public decimal EmployeeShareAmount { get; set; }

    public decimal EmployerShareAmount { get; set; }

    public decimal VoluntaryShareAmount { get; set; }

    public decimal InterestAmount { get; set; }

    public decimal DebitAmount { get; set; }

    public decimal CreditAmount { get; set; }

    public decimal? RunningBalance { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsReversed { get; set; }

    public Guid? ReversalReferenceId { get; set; }

    public ProvidentFundLedgerTransaction? ReversalReference { get; set; }

    public ICollection<ProvidentFundLedgerTransaction> Reversals { get; set; } = new List<ProvidentFundLedgerTransaction>();
}
