using Microsoft.AspNetCore.Identity;

namespace Sixram.Api.Entities;

public class ApplicationRole : IdentityRole
{
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
}
