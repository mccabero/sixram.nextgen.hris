namespace Sixram.Api.Entities;

public class EmployeeDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid DocumentTypeId { get; set; }

    public DocumentType? DocumentType { get; set; }

    public string Title { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public long FileSize { get; set; }

    public string MimeType { get; set; } = string.Empty;

    public DateOnly? IssueDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public string? UploadedByUserId { get; set; }

    public ApplicationUser? UploadedByUser { get; set; }

    public bool IsArchived { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
