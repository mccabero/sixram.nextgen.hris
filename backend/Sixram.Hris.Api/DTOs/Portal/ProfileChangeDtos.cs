using System.ComponentModel.DataAnnotations;
using Sixram.Api.DTOs.Common;

namespace Sixram.Api.DTOs.Portal;

public sealed class ProfileFieldChangeDto
{
    public string FieldKey { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string OldValue { get; init; } = string.Empty;

    public string NewValue { get; init; } = string.Empty;
}

public sealed class SaveProfileChangeRequestDto
{
    [Phone]
    [MaxLength(32)]
    public string MobileNumber { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(512)]
    public string Address { get; init; } = string.Empty;

    [MaxLength(128)]
    public string CityProvince { get; init; } = string.Empty;

    [MaxLength(32)]
    public string PostalCode { get; init; } = string.Empty;

    [MaxLength(32)]
    public string CivilStatus { get; init; } = string.Empty;

    [MaxLength(64)]
    public string Nationality { get; init; } = string.Empty;

    [MaxLength(128)]
    public string EmergencyContactName { get; init; } = string.Empty;

    [MaxLength(64)]
    public string EmergencyContactRelationship { get; init; } = string.Empty;

    [Phone]
    [MaxLength(32)]
    public string EmergencyContactPhone { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;
}

public sealed class EmployeeProfileChangeRequestListQueryDto : PagedQueryDto
{
    public Guid? EmployeeId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "created";

    public bool Descending { get; init; } = true;
}

public sealed class EmployeeProfileChangeRequestDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string RequestType { get; init; } = string.Empty;

    public IReadOnlyList<ProfileFieldChangeDto> FieldChanges { get; init; } = Array.Empty<ProfileFieldChangeDto>();

    public string Reason { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string RequestedByDisplayName { get; init; } = string.Empty;

    public string ReviewedByDisplayName { get; init; } = string.Empty;

    public string ReviewerRemarks { get; init; } = string.Empty;

    public DateTime? ReviewedAtUtc { get; init; }

    public DateTime? AppliedAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}
