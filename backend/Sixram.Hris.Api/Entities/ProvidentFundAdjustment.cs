namespace Sixram.Api.Entities;

public class ProvidentFundAdjustment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid EnrollmentId { get; set; }

    public ProvidentFundEnrollment? Enrollment { get; set; }

    public string AdjustmentType { get; set; } = string.Empty;

    public DateOnly AdjustmentDate { get; set; }

    public decimal Amount { get; set; }

    public string ShareAffected { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string AttachmentPath { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? RequestedByUserId { get; set; }

    public ApplicationUser? RequestedByUser { get; set; }

    public string? ApprovedByUserId { get; set; }

    public ApplicationUser? ApprovedByUser { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public DateTime? PostedAtUtc { get; set; }

    public string DecisionRemarks { get; set; } = string.Empty;

    public ICollection<ProvidentFundAdjustmentApproval> Approvals { get; set; } = new List<ProvidentFundAdjustmentApproval>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
