namespace Sixram.Api.Entities;

public class ProvidentFundContributionBatchLine
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BatchId { get; set; }

    public ProvidentFundContributionBatch? Batch { get; set; }

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid EnrollmentId { get; set; }

    public ProvidentFundEnrollment? Enrollment { get; set; }

    public decimal BasicSalary { get; set; }

    public decimal EmployeeContribution { get; set; }

    public decimal EmployerContribution { get; set; }

    public decimal VoluntaryContribution { get; set; }

    public decimal TotalContribution { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    public ICollection<ProvidentFundLedgerTransaction> LedgerTransactions { get; set; } = new List<ProvidentFundLedgerTransaction>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
