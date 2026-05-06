using System.ComponentModel.DataAnnotations;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;

namespace Sixram.Api.DTOs.Reporting;

internal static class ReportingValidationRules
{
    public const int MaxDateRangeDays = 366;
}

public sealed class ReportDefinitionDto
{
    public string Key { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string RoutePath { get; init; } = string.Empty;

    public IReadOnlyList<string> AllowedRoles { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Filters { get; init; } = Array.Empty<string>();

    public bool SupportsExport { get; init; }

    public bool SupportsSavedViews { get; init; }
}

public sealed class ReportColumnDto
{
    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Alignment { get; init; } = string.Empty;
}

public sealed class ReportRowDto
{
    public string Id { get; init; } = string.Empty;

    public string LinkPath { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class ReportMetricDto
{
    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Tone { get; init; } = string.Empty;
}

public sealed class ReportResultDto
{
    public string ReportKey { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DateTime GeneratedAtUtc { get; init; }

    public IReadOnlyList<ReportColumnDto> Columns { get; init; } = Array.Empty<ReportColumnDto>();

    public IReadOnlyList<ReportRowDto> Rows { get; init; } = Array.Empty<ReportRowDto>();

    public IReadOnlyList<ReportMetricDto> Metrics { get; init; } = Array.Empty<ReportMetricDto>();

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }
}

public sealed class ReportQueryDto : PagedQueryDto, IValidatableObject
{
    public Guid? EmployeeId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid? EmploymentTypeId { get; init; }

    public Guid? EmploymentStatusId { get; init; }

    public Guid? LeaveTypeId { get; init; }

    public Guid? DocumentTypeId { get; init; }

    public Guid? PayPeriodId { get; init; }

    public Guid? PayrollRunId { get; init; }

    [MaxLength(64)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(64)]
    public string Source { get; init; } = string.Empty;

    [MaxLength(64)]
    public string IssueType { get; init; } = string.Empty;

    [MaxLength(64)]
    public string Severity { get; init; } = string.Empty;

    [MaxLength(64)]
    public string EntityType { get; init; } = string.Empty;

    [MaxLength(64)]
    public string Action { get; init; } = string.Empty;

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [Range(2000, 9999)]
    public int? Year { get; init; }

    [Range(1, 12)]
    public int? Month { get; init; }

    public bool? IncludeInactive { get; init; }

    [MaxLength(64)]
    public string SortBy { get; init; } = "name";

    public bool Descending { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom is not null && DateTo is not null && DateTo < DateFrom)
        {
            yield return new ValidationResult(
                "End date cannot be earlier than the start date.",
                [nameof(DateTo)]);
        }

        if (DateFrom is not null && DateTo is not null && DateTo.Value.DayNumber - DateFrom.Value.DayNumber > ReportingValidationRules.MaxDateRangeDays)
        {
            yield return new ValidationResult(
                $"Date range queries are limited to {ReportingValidationRules.MaxDateRangeDays} days.",
                [nameof(DateTo)]);
        }
    }
}

public sealed class ReportsCenterDto
{
    public IReadOnlyList<ReportDefinitionDto> Reports { get; init; } = Array.Empty<ReportDefinitionDto>();
}

public sealed class ReportOptionsDto
{
    public IReadOnlyList<EmployeeAttendanceOptionDto> Employees { get; init; } = Array.Empty<EmployeeAttendanceOptionDto>();

    public IReadOnlyList<LookupOptionDto> Departments { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> Branches { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> EmploymentTypes { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> EmploymentStatuses { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> LeaveTypes { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> DocumentTypes { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> PayPeriods { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> PayrollRuns { get; init; } = Array.Empty<LookupOptionDto>();
}

public sealed class SavedReportDto
{
    public Guid Id { get; init; }

    public string ReportKey { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string FiltersJson { get; init; } = "{}";

    public bool IsDefault { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class SaveSavedReportDto
{
    [Required]
    [MaxLength(128)]
    public string ReportKey { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string FiltersJson { get; init; } = "{}";

    public bool IsDefault { get; init; }
}

public sealed class ComplianceSummaryDto
{
    public int OpenIssueCount { get; init; }

    public int CriticalIssueCount { get; init; }

    public int HighIssueCount { get; init; }

    public int MissingRequiredDocumentCount { get; init; }

    public int ExpiredDocumentCount { get; init; }

    public int ExpiringSoonDocumentCount { get; init; }

    public int MissingGovernmentIdCount { get; init; }

    public int MissingScheduleAssignmentCount { get; init; }

    public int MissingCompensationProfileCount { get; init; }

    public int IncompleteAttendanceCount { get; init; }
}

public sealed class ComplianceIssueDto
{
    public string Id { get; init; } = string.Empty;

    public string IssueType { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public Guid? EmployeeId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ReferenceType { get; init; } = string.Empty;

    public string ReferenceId { get; init; } = string.Empty;

    public string LinkPath { get; init; } = string.Empty;

    public DateTime DetectedAtUtc { get; init; }
}

public sealed class ComplianceIssueQueryDto : PagedQueryDto, IValidatableObject
{
    public Guid? EmployeeId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    [MaxLength(64)]
    public string IssueType { get; init; } = string.Empty;

    [MaxLength(32)]
    public string Severity { get; init; } = string.Empty;

    [MaxLength(32)]
    public string SortBy { get; init; } = "severity";

    public bool Descending { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Severity) &&
            !string.Equals(Severity, "all", StringComparison.OrdinalIgnoreCase) &&
            !Sixram.Api.Constants.ComplianceSeverityLevels.All.Contains(Severity.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "Severity must be low, medium, high, or critical.",
                [nameof(Severity)]);
        }
    }
}

public sealed class AnalyticsSeriesPointDto
{
    public string Label { get; init; } = string.Empty;

    public decimal Value { get; init; }
}

public sealed class AnalyticsDashboardDto
{
    public IReadOnlyList<ReportMetricDto> Metrics { get; init; } = Array.Empty<ReportMetricDto>();

    public IReadOnlyList<AnalyticsSeriesPointDto> HeadcountByDepartment { get; init; } = Array.Empty<AnalyticsSeriesPointDto>();

    public IReadOnlyList<AnalyticsSeriesPointDto> HeadcountByBranch { get; init; } = Array.Empty<AnalyticsSeriesPointDto>();

    public IReadOnlyList<AnalyticsSeriesPointDto> AttendanceTrend { get; init; } = Array.Empty<AnalyticsSeriesPointDto>();

    public IReadOnlyList<AnalyticsSeriesPointDto> LeaveUsageTrend { get; init; } = Array.Empty<AnalyticsSeriesPointDto>();

    public IReadOnlyList<AnalyticsSeriesPointDto> ApprovalVolume { get; init; } = Array.Empty<AnalyticsSeriesPointDto>();

    public IReadOnlyList<AnalyticsSeriesPointDto> PayrollCostTrend { get; init; } = Array.Empty<AnalyticsSeriesPointDto>();
}

public sealed class AuditLogQueryDto : PagedQueryDto, IValidatableObject
{
    [MaxLength(64)]
    public string EntityType { get; init; } = string.Empty;

    [MaxLength(64)]
    public string Action { get; init; } = string.Empty;

    public Guid? EmployeeId { get; init; }

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "created";

    public bool Descending { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom is not null && DateTo is not null && DateTo < DateFrom)
        {
            yield return new ValidationResult(
                "End date cannot be earlier than the start date.",
                [nameof(DateTo)]);
        }

        if (DateFrom is not null && DateTo is not null && DateTo.Value.DayNumber - DateFrom.Value.DayNumber > ReportingValidationRules.MaxDateRangeDays)
        {
            yield return new ValidationResult(
                $"Date range queries are limited to {ReportingValidationRules.MaxDateRangeDays} days.",
                [nameof(DateTo)]);
        }
    }
}

public sealed class AuditLogDto
{
    public Guid Id { get; init; }

    public string ActorUserId { get; init; } = string.Empty;

    public string ActorName { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string EntityType { get; init; } = string.Empty;

    public string EntityId { get; init; } = string.Empty;

    public Guid? EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string OldValuesJson { get; init; } = string.Empty;

    public string NewValuesJson { get; init; } = string.Empty;

    public string IpAddress { get; init; } = string.Empty;

    public string UserAgent { get; init; } = string.Empty;

    public string Remarks { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}
