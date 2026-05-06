namespace Sixram.Api.Entities;

public class PayrollAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? PayrollRunId { get; set; }

    public PayrollRun? PayrollRun { get; set; }

    public Guid? PayrollRunItemId { get; set; }

    public PayrollRunItem? PayrollRunItem { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string? ActorUserId { get; set; }

    public ApplicationUser? ActorUser { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
