namespace Sixram.Api.Entities;

public class ContributionType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool EmployeeShareApplicable { get; set; } = true;

    public bool EmployerShareApplicable { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public ICollection<GovernmentContributionTable> GovernmentContributionTables { get; set; } = new List<GovernmentContributionTable>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

