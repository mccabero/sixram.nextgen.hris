namespace Sixram.Api.Entities;

public class EmployeeProfileChangeRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public string RequestedByUserId { get; set; } = string.Empty;

    public ApplicationUser? RequestedByUser { get; set; }

    public string RequestType { get; set; } = string.Empty;

    public string FieldChangesJson { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string? ReviewedByUserId { get; set; }

    public ApplicationUser? ReviewedByUser { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }

    public string ReviewerRemarks { get; set; } = string.Empty;

    public DateTime? AppliedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
