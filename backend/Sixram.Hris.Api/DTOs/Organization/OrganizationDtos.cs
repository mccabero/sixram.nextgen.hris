using System.ComponentModel.DataAnnotations;
using Sixram.Api.DTOs.Common;

namespace Sixram.Api.DTOs.Organization;

public sealed class OrganizationSummaryDto
{
    public int DepartmentCount { get; init; }

    public int ActiveDepartmentCount { get; init; }

    public int PositionCount { get; init; }

    public int ActivePositionCount { get; init; }

    public int BranchCount { get; init; }

    public int ActiveBranchCount { get; init; }

    public int EmploymentTypeCount { get; init; }

    public int ActiveEmploymentTypeCount { get; init; }

    public int EmploymentStatusCount { get; init; }

    public int ActiveEmploymentStatusCount { get; init; }

    public int EmployeeCount { get; init; }

    public int ActiveEmployeeCount { get; init; }
}

public sealed class OrganizationRecordDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public Guid? DepartmentId { get; init; }

    public string DepartmentName { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public int EmployeeCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class OrganizationOptionsDto
{
    public IReadOnlyList<LookupOptionDto> Departments { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> Positions { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> Branches { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> EmploymentTypes { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> EmploymentStatuses { get; init; } = Array.Empty<LookupOptionDto>();
}

public sealed class OrganizationListQueryDto : PagedQueryDto
{
    public bool? IsActive { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "name";

    public bool Descending { get; init; }
}

public abstract class SaveOrganizationRecordRequestDto
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

    public bool IsActive { get; init; } = true;
}

public sealed class SaveDepartmentRequestDto : SaveOrganizationRecordRequestDto;

public sealed class SaveEmploymentTypeRequestDto : SaveOrganizationRecordRequestDto;

public sealed class SaveEmploymentStatusRequestDto : SaveOrganizationRecordRequestDto;

public sealed class SavePositionRequestDto : SaveOrganizationRecordRequestDto
{
    public Guid? DepartmentId { get; init; }
}

public sealed class SaveBranchRequestDto : SaveOrganizationRecordRequestDto
{
    [MaxLength(512)]
    public string Address { get; init; } = string.Empty;
}
