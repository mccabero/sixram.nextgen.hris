namespace Sixram.Api.Entities;

public class ProvidentFundEnrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid PolicyId { get; set; }

    public ProvidentFundPolicy? Policy { get; set; }

    public DateOnly EnrollmentDate { get; set; }

    public DateOnly VestingStartDate { get; set; }

    public string EmployeeContributionOverrideType { get; set; } = string.Empty;

    public decimal? EmployeeContributionOverrideValue { get; set; }

    public string EmployerContributionOverrideType { get; set; } = string.Empty;

    public decimal? EmployerContributionOverrideValue { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ApplicationUser? UpdatedByUser { get; set; }

    public DateTime? ClosedAtUtc { get; set; }

    public ICollection<ProvidentFundContributionBatchLine> ContributionBatchLines { get; set; } = new List<ProvidentFundContributionBatchLine>();

    public ICollection<ProvidentFundLedgerTransaction> LedgerTransactions { get; set; } = new List<ProvidentFundLedgerTransaction>();

    public ICollection<ProvidentFundWithdrawalRequest> WithdrawalRequests { get; set; } = new List<ProvidentFundWithdrawalRequest>();

    public ICollection<ProvidentFundAdjustment> Adjustments { get; set; } = new List<ProvidentFundAdjustment>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
