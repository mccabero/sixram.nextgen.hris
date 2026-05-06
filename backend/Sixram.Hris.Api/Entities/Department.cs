namespace Sixram.Api.Entities;

public class Department
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Position> Positions { get; set; } = new List<Position>();

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
