namespace Sixram.Api.Entities;

public class DocumentType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool RequiresExpiryDate { get; set; }

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<EmployeeDocument> EmployeeDocuments { get; set; } = new List<EmployeeDocument>();
}
