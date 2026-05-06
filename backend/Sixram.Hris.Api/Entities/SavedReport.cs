namespace Sixram.Api.Entities;

public class SavedReport
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser? User { get; set; }

    public string ReportKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string FiltersJson { get; set; } = "{}";

    public bool IsDefault { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
