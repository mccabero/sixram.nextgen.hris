namespace Sixram.Api.Entities;

public class ProvidentFundPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string PolicyName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string EligibilityRules { get; set; } = string.Empty;

    public string EmployeeContributionType { get; set; } = string.Empty;

    public decimal EmployeeContributionValue { get; set; }

    public string EmployerContributionType { get; set; } = string.Empty;

    public decimal EmployerContributionValue { get; set; }

    public string ContributionFrequency { get; set; } = "monthly";

    public DateOnly EffectiveDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool AllowVoluntaryContribution { get; set; }

    public bool AllowWithdrawal { get; set; }

    public bool AllowLoan { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ApplicationUser? UpdatedByUser { get; set; }

    public ICollection<ProvidentFundVestingRule> VestingRules { get; set; } = new List<ProvidentFundVestingRule>();

    public ICollection<ProvidentFundEnrollment> Enrollments { get; set; } = new List<ProvidentFundEnrollment>();

    public ICollection<ProvidentFundContributionBatch> ContributionBatches { get; set; } = new List<ProvidentFundContributionBatch>();

    public ICollection<ProvidentFundLedgerTransaction> LedgerTransactions { get; set; } = new List<ProvidentFundLedgerTransaction>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
