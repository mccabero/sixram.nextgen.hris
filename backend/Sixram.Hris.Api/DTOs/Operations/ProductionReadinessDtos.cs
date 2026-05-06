using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Sixram.Api.DTOs.Operations;

public static class ProductionReadinessStates
{
    public const string Ready = "ready";
    public const string Attention = "attention";
    public const string Blocked = "blocked";
    public const string Manual = "manual";
}

public static class DataImportTypes
{
    public const string Employees = "employees";
    public const string Departments = "departments";
    public const string Positions = "positions";
    public const string Branches = "branches";
    public const string EmploymentTypes = "employment_types";
    public const string EmploymentStatuses = "employment_statuses";
    public const string LeaveBalances = "leave_balances";
    public const string CompensationProfiles = "compensation_profiles";

    public static readonly IReadOnlyList<string> All =
    [
        Employees,
        Departments,
        Positions,
        Branches,
        EmploymentTypes,
        EmploymentStatuses,
        LeaveBalances,
        CompensationProfiles
    ];
}

public sealed class ProductionReadinessOverviewDto
{
    public DateTime GeneratedAtUtc { get; init; }

    public int ReadinessPercent { get; init; }

    public int ReadyItemCount { get; init; }

    public int AttentionItemCount { get; init; }

    public int BlockedItemCount { get; init; }

    public IReadOnlyList<ProductionReadinessSectionDto> Sections { get; init; } = Array.Empty<ProductionReadinessSectionDto>();

    public IReadOnlyList<DataImportDefinitionDto> AvailableImports { get; init; } = Array.Empty<DataImportDefinitionDto>();

    public IReadOnlyList<OperationalGuidanceItemDto> OperationalGuidance { get; init; } = Array.Empty<OperationalGuidanceItemDto>();
}

public sealed class ProductionReadinessSectionDto
{
    public string Key { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<ProductionReadinessItemDto> Items { get; init; } = Array.Empty<ProductionReadinessItemDto>();
}

public sealed class ProductionReadinessItemDto
{
    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string Detail { get; init; } = string.Empty;

    public string ActionUrl { get; init; } = string.Empty;
}

public sealed class OperationalGuidanceItemDto
{
    public string Key { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}

public sealed class DataImportDefinitionDto
{
    public string Key { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string SampleFileName { get; init; } = string.Empty;

    public IReadOnlyList<string> RequiredColumns { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> OptionalColumns { get; init; } = Array.Empty<string>();
}

public sealed class DataImportPreviewRowDto
{
    public int RowNumber { get; init; }

    public string Identifier { get; init; } = string.Empty;

    public string Operation { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public IReadOnlyList<string> Messages { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, string> Values { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class DataImportPreviewDto
{
    public string ImportType { get; init; } = string.Empty;

    public string ImportName { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public int TotalRows { get; init; }

    public int ValidRowCount { get; init; }

    public int InvalidRowCount { get; init; }

    public bool CanApply { get; init; }

    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();

    public IReadOnlyList<DataImportPreviewRowDto> Rows { get; init; } = Array.Empty<DataImportPreviewRowDto>();
}

public sealed class DataImportApplyResultDto
{
    public string ImportType { get; init; } = string.Empty;

    public string ImportName { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public int ProcessedCount { get; init; }

    public int CreatedCount { get; init; }

    public int UpdatedCount { get; init; }

    public int SkippedCount { get; init; }

    public int ErrorCount { get; init; }

    public DateTime AppliedAtUtc { get; init; }

    public IReadOnlyList<DataImportPreviewRowDto> Rows { get; init; } = Array.Empty<DataImportPreviewRowDto>();
}

public sealed class PreviewDataImportRequestDto
{
    [Required]
    [MaxLength(64)]
    public string ImportType { get; init; } = string.Empty;

    [Required]
    public IFormFile? File { get; init; }
}

public sealed class ApplyDataImportRequestDto
{
    [Required]
    [MaxLength(64)]
    public string ImportType { get; init; } = string.Empty;

    [Required]
    public IFormFile? File { get; init; }
}
