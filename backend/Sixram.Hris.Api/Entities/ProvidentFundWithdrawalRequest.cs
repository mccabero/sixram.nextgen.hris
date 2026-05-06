namespace Sixram.Api.Entities;

public class ProvidentFundWithdrawalRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RequestNumber { get; set; } = string.Empty;

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid EnrollmentId { get; set; }

    public ProvidentFundEnrollment? Enrollment { get; set; }

    public DateOnly RequestDate { get; set; }

    public string WithdrawalType { get; set; } = string.Empty;

    public decimal RequestedAmount { get; set; }

    public decimal EligibleWithdrawableAmount { get; set; }

    public decimal ApprovedAmount { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string AttachmentPath { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? PaymentDate { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ApplicationUser? UpdatedByUser { get; set; }

    public ICollection<ProvidentFundWithdrawalApproval> Approvals { get; set; } = new List<ProvidentFundWithdrawalApproval>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
