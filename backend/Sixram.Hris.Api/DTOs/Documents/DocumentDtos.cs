using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Sixram.Api.DTOs.Common;

namespace Sixram.Api.DTOs.Documents;

public sealed class DocumentTypeRecordDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool RequiresExpiryDate { get; init; }

    public bool IsRequired { get; init; }

    public bool IsActive { get; init; }

    public int DocumentCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class DocumentTypeOptionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool RequiresExpiryDate { get; init; }

    public bool IsRequired { get; init; }

    public bool IsActive { get; init; }
}

public sealed class DocumentTypeListQueryDto : PagedQueryDto
{
    public bool? IsActive { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "name";

    public bool Descending { get; init; }
}

public sealed class SaveDocumentTypeRequestDto
{
    [Required]
    [MaxLength(32)]
    [RegularExpression("^[A-Za-z0-9._/-]+$", ErrorMessage = "Codes may contain letters, numbers, periods, underscores, slashes, and hyphens only.")]
    public string Code { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(512)]
    public string Description { get; init; } = string.Empty;

    public bool RequiresExpiryDate { get; init; }

    public bool IsRequired { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class DocumentComplianceSummaryDto
{
    public int TotalDocuments { get; init; }

    public int ArchivedDocuments { get; init; }

    public int ExpiredDocuments { get; init; }

    public int ExpiringSoonDocuments { get; init; }

    public int MissingRequiredDocuments { get; init; }

    public int EmployeesWithIncompleteDocuments { get; init; }

    public int EmployeesWithExpiringDocuments { get; init; }

    public int RequiredDocumentTypes { get; init; }
}

public sealed class EmployeeDocumentComplianceSummaryDto
{
    public int TotalDocuments { get; init; }

    public int ActiveDocuments { get; init; }

    public int ArchivedDocuments { get; init; }

    public int MissingRequiredDocuments { get; init; }

    public int ExpiredDocuments { get; init; }

    public int ExpiringSoonDocuments { get; init; }

    public int RequiredDocumentTypes { get; init; }

    public int SubmittedRequiredDocumentTypes { get; init; }

    public bool HasIssues { get; init; }
}

public sealed class MissingRequiredDocumentDto
{
    public Guid DocumentTypeId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool RequiresExpiryDate { get; init; }
}

public sealed class EmployeeDocumentListItemDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public Guid DocumentTypeId { get; init; }

    public string DocumentTypeCode { get; init; } = string.Empty;

    public string DocumentTypeName { get; init; } = string.Empty;

    public bool DocumentTypeIsActive { get; init; }

    public bool DocumentTypeRequiresExpiryDate { get; init; }

    public bool DocumentTypeIsRequired { get; init; }

    public string Title { get; init; } = string.Empty;

    public string OriginalFileName { get; init; } = string.Empty;

    public long FileSize { get; init; }

    public string MimeType { get; init; } = string.Empty;

    public DateOnly? IssueDate { get; init; }

    public DateOnly? ExpiryDate { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public string UploadedByDisplayName { get; init; } = string.Empty;

    public string UploadedByEmail { get; init; } = string.Empty;

    public bool IsArchived { get; init; }

    public string StatusCode { get; init; } = string.Empty;

    public string StatusLabel { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class EmployeeDocumentProfileDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public EmployeeDocumentComplianceSummaryDto Summary { get; init; } = new();

    public IReadOnlyList<DocumentTypeOptionDto> AvailableDocumentTypes { get; init; } = Array.Empty<DocumentTypeOptionDto>();

    public IReadOnlyList<MissingRequiredDocumentDto> MissingRequiredDocuments { get; init; } = Array.Empty<MissingRequiredDocumentDto>();

    public IReadOnlyList<EmployeeDocumentListItemDto> Documents { get; init; } = Array.Empty<EmployeeDocumentListItemDto>();
}

public sealed class EmployeeDocumentListOptionsDto
{
    public IReadOnlyList<DocumentTypeOptionDto> DocumentTypes { get; init; } = Array.Empty<DocumentTypeOptionDto>();

    public IReadOnlyList<LookupOptionDto> Departments { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> Branches { get; init; } = Array.Empty<LookupOptionDto>();
}

public sealed class EmployeeDocumentListQueryDto : PagedQueryDto
{
    public Guid? EmployeeId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid? DocumentTypeId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(32)]
    public string SortBy { get; init; } = "uploaded";

    public bool Descending { get; init; } = true;
}

public sealed class SaveEmployeeDocumentRequestDto : IValidatableObject
{
    [Required]
    public Guid? DocumentTypeId { get; init; }

    [Required]
    [MaxLength(160)]
    public string Title { get; init; } = string.Empty;

    [Required]
    public IFormFile? File { get; init; }

    public DateOnly? IssueDate { get; init; }

    public DateOnly? ExpiryDate { get; init; }

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExpiryDate is not null && IssueDate is not null && ExpiryDate < IssueDate)
        {
            yield return new ValidationResult(
                "Expiry date cannot be earlier than the issue date.",
                [nameof(ExpiryDate)]);
        }
    }
}

public sealed class UpdateEmployeeDocumentMetadataRequestDto : IValidatableObject
{
    [Required]
    public Guid? DocumentTypeId { get; init; }

    [Required]
    [MaxLength(160)]
    public string Title { get; init; } = string.Empty;

    public DateOnly? IssueDate { get; init; }

    public DateOnly? ExpiryDate { get; init; }

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ExpiryDate is not null && IssueDate is not null && ExpiryDate < IssueDate)
        {
            yield return new ValidationResult(
                "Expiry date cannot be earlier than the issue date.",
                [nameof(ExpiryDate)]);
        }
    }
}

public sealed class ReplaceEmployeeDocumentFileRequestDto
{
    [Required]
    public IFormFile? File { get; init; }
}

public sealed class SetEmployeeDocumentArchiveStateRequestDto
{
    public bool IsArchived { get; init; }
}
