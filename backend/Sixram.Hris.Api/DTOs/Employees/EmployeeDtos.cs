using System.ComponentModel.DataAnnotations;
using Sixram.Api.DTOs.Common;

namespace Sixram.Api.DTOs.Employees;

public sealed class EmployeeSummaryDto
{
    public Guid Id { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string MobileNumber { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string PositionName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string EmploymentTypeName { get; init; } = string.Empty;

    public string EmploymentStatusName { get; init; } = string.Empty;

    public string ManagerName { get; init; } = string.Empty;

    public DateOnly? DateHired { get; init; }

    public bool IsActive { get; init; }
}

public sealed class EmployeeDetailDto
{
    public Guid Id { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string MiddleName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Suffix { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Gender { get; init; } = string.Empty;

    public DateOnly? BirthDate { get; init; }

    public string CivilStatus { get; init; } = string.Empty;

    public string Nationality { get; init; } = string.Empty;

    public string MobileNumber { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public string CityProvince { get; init; } = string.Empty;

    public string PostalCode { get; init; } = string.Empty;

    public string EmergencyContactName { get; init; } = string.Empty;

    public string EmergencyContactRelationship { get; init; } = string.Empty;

    public string EmergencyContactPhone { get; init; } = string.Empty;

    public Guid? DepartmentId { get; init; }

    public string DepartmentName { get; init; } = string.Empty;

    public bool DepartmentIsActive { get; init; }

    public Guid? PositionId { get; init; }

    public string PositionName { get; init; } = string.Empty;

    public bool PositionIsActive { get; init; }

    public Guid? BranchId { get; init; }

    public string BranchName { get; init; } = string.Empty;

    public bool BranchIsActive { get; init; }

    public Guid? EmploymentTypeId { get; init; }

    public string EmploymentTypeName { get; init; } = string.Empty;

    public bool EmploymentTypeIsActive { get; init; }

    public Guid? EmploymentStatusId { get; init; }

    public string EmploymentStatusName { get; init; } = string.Empty;

    public bool EmploymentStatusIsActive { get; init; }

    public Guid? ManagerId { get; init; }

    public string ManagerName { get; init; } = string.Empty;

    public string WorkSchedule { get; init; } = string.Empty;

    public DateOnly? DateHired { get; init; }

    public DateOnly? DateRegularized { get; init; }

    public DateOnly? DateSeparated { get; init; }

    public string SssNumber { get; init; } = string.Empty;

    public string PhilHealthNumber { get; init; } = string.Empty;

    public string PagIbigNumber { get; init; } = string.Empty;

    public string TinNumber { get; init; } = string.Empty;

    public string OtherGovernmentId { get; init; } = string.Empty;

    public string UserId { get; init; } = string.Empty;

    public string LinkedUserEmail { get; init; } = string.Empty;

    public string LinkedUserDisplayName { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class EmployeeEditorOptionsDto
{
    public IReadOnlyList<LookupOptionDto> Departments { get; set; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> Positions { get; set; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> Branches { get; set; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> EmploymentTypes { get; set; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> EmploymentStatuses { get; set; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<EmployeeManagerOptionDto> Managers { get; set; } = Array.Empty<EmployeeManagerOptionDto>();

    public IReadOnlyList<UserOptionDto> UserAccounts { get; set; } = Array.Empty<UserOptionDto>();
}

public sealed class EmployeeManagerOptionDto
{
    public Guid Id { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class EmployeeListQueryDto : PagedQueryDto
{
    public Guid? DepartmentId { get; init; }

    public Guid? PositionId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid? EmploymentTypeId { get; init; }

    public Guid? EmploymentStatusId { get; init; }

    public bool? IsActive { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "name";

    public bool Descending { get; init; }
}

public sealed class SaveEmployeeRequestDto : IValidatableObject
{
    [Required]
    [MaxLength(32)]
    [RegularExpression("^[A-Za-z0-9._/-]+$", ErrorMessage = "Employee codes may contain letters, numbers, periods, underscores, slashes, and hyphens only.")]
    public string EmployeeCode { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string FirstName { get; init; } = string.Empty;

    [MaxLength(128)]
    public string MiddleName { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string LastName { get; init; } = string.Empty;

    [MaxLength(32)]
    public string Suffix { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string Gender { get; init; } = string.Empty;

    public DateOnly? BirthDate { get; init; }

    [MaxLength(32)]
    public string CivilStatus { get; init; } = string.Empty;

    [MaxLength(64)]
    public string Nationality { get; init; } = string.Empty;

    [MaxLength(32)]
    [RegularExpression(@"^[0-9+() \-]*$", ErrorMessage = "Mobile numbers may contain digits, spaces, parentheses, plus signs, and hyphens only.")]
    public string MobileNumber { get; init; } = string.Empty;

    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [MaxLength(512)]
    public string Address { get; init; } = string.Empty;

    [MaxLength(128)]
    public string CityProvince { get; init; } = string.Empty;

    [MaxLength(32)]
    public string PostalCode { get; init; } = string.Empty;

    [MaxLength(128)]
    public string EmergencyContactName { get; init; } = string.Empty;

    [MaxLength(64)]
    public string EmergencyContactRelationship { get; init; } = string.Empty;

    [MaxLength(32)]
    [RegularExpression(@"^[0-9+() \-]*$", ErrorMessage = "Emergency contact phone may contain digits, spaces, parentheses, plus signs, and hyphens only.")]
    public string EmergencyContactPhone { get; init; } = string.Empty;

    [Required]
    public Guid? DepartmentId { get; init; }

    [Required]
    public Guid? PositionId { get; init; }

    [Required]
    public Guid? BranchId { get; init; }

    [Required]
    public Guid? EmploymentTypeId { get; init; }

    [Required]
    public Guid? EmploymentStatusId { get; init; }

    public Guid? ManagerId { get; init; }

    [MaxLength(128)]
    public string WorkSchedule { get; init; } = string.Empty;

    [Required]
    public DateOnly? DateHired { get; init; }

    public DateOnly? DateRegularized { get; init; }

    public DateOnly? DateSeparated { get; init; }

    [MaxLength(32)]
    public string SssNumber { get; init; } = string.Empty;

    [MaxLength(32)]
    public string PhilHealthNumber { get; init; } = string.Empty;

    [MaxLength(32)]
    public string PagIbigNumber { get; init; } = string.Empty;

    [MaxLength(32)]
    public string TinNumber { get; init; } = string.Empty;

    [MaxLength(64)]
    public string OtherGovernmentId { get; init; } = string.Empty;

    public string? UserId { get; init; }

    public bool IsActive { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateRegularized is not null && DateHired is not null && DateRegularized < DateHired)
        {
            yield return new ValidationResult(
                "Regularization date cannot be earlier than the hire date.",
                [nameof(DateRegularized)]);
        }

        if (DateSeparated is not null && DateHired is not null && DateSeparated < DateHired)
        {
            yield return new ValidationResult(
                "Separation date cannot be earlier than the hire date.",
                [nameof(DateSeparated)]);
        }
    }
}
