namespace Sixram.Api.Entities;

public class NotificationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string ReferenceType { get; set; } = string.Empty;

    public string ReferenceId { get; set; } = string.Empty;

    public string ActionUrl { get; set; } = string.Empty;

    public DateTime? ReadAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
