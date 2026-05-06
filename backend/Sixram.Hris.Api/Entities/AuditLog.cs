namespace Sixram.Api.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? ActorUserId { get; set; }

    public ApplicationUser? ActorUser { get; set; }

    public string ActorNameSnapshot { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public Guid? EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public string OldValuesJson { get; set; } = string.Empty;

    public string NewValuesJson { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public string UserAgent { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
