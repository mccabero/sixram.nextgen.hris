using System.ComponentModel.DataAnnotations;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;

namespace Sixram.Api.DTOs.Payroll;

public sealed class PayrollDashboardSummaryDto
{
    public DateOnly BusinessDate { get; init; }

    public PayPeriodOptionDto? CurrentOpenPayPeriod { get; init; }

    public int DraftRunCount { get; init; }

    public int ForReviewRunCount { get; init; }

    public int ApprovedRunCount { get; init; }

    public int EmployeesMissingCompensationProfileCount { get; init; }

    public int EmployeesWithAttendanceIssuesCount { get; init; }

    public int PendingPayrollAdjustmentCount { get; init; }

    public int PayrollItemsOnHoldCount { get; init; }

    public decimal TotalGrossPay { get; init; }

    public decimal TotalDeductions { get; init; }

    public decimal TotalNetPay { get; init; }

    public IReadOnlyList<PayrollRunSummaryDto> RecentRuns { get; init; } = Array.Empty<PayrollRunSummaryDto>();
}

public sealed class PayrollOptionsDto
{
    public IReadOnlyList<EmployeeAttendanceOptionDto> Employees { get; init; } = Array.Empty<EmployeeAttendanceOptionDto>();

    public IReadOnlyList<LookupOptionDto> Departments { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> Branches { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> EmploymentTypes { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> EmploymentStatuses { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<PayPeriodOptionDto> PayPeriods { get; init; } = Array.Empty<PayPeriodOptionDto>();

    public IReadOnlyList<PayPeriodTemplateOptionDto> PayPeriodTemplates { get; init; } = Array.Empty<PayPeriodTemplateOptionDto>();

    public IReadOnlyList<EarningTypeOptionDto> EarningTypes { get; init; } = Array.Empty<EarningTypeOptionDto>();

    public IReadOnlyList<DeductionTypeOptionDto> DeductionTypes { get; init; } = Array.Empty<DeductionTypeOptionDto>();

    public IReadOnlyList<ContributionTypeOptionDto> ContributionTypes { get; init; } = Array.Empty<ContributionTypeOptionDto>();

    public IReadOnlyList<TaxTableOptionDto> TaxTables { get; init; } = Array.Empty<TaxTableOptionDto>();

    public IReadOnlyList<string> PayTypes { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> PayFrequencies { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> RunStatuses { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> AdjustmentStatuses { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> AdjustmentTypes { get; init; } = Array.Empty<string>();
}

public sealed class PayrollSetupSummaryDto
{
    public int PayPeriodTemplateCount { get; init; }

    public int ActivePayPeriodTemplateCount { get; init; }

    public int EarningTypeCount { get; init; }

    public int ActiveEarningTypeCount { get; init; }

    public int DeductionTypeCount { get; init; }

    public int ActiveDeductionTypeCount { get; init; }

    public int ContributionTypeCount { get; init; }

    public int ActiveContributionTypeCount { get; init; }

    public int GovernmentContributionTableCount { get; init; }

    public int ActiveGovernmentContributionTableCount { get; init; }

    public int TaxTableCount { get; init; }

    public int ActiveTaxTableCount { get; init; }
}

public sealed class PayrollSettingsDto
{
    [Required]
    [MaxLength(32)]
    public string DefaultPayFrequency { get; init; } = string.Empty;

    [Range(1, 31)]
    public decimal DefaultWorkingDaysPerMonth { get; init; }

    [Range(1, 24)]
    public decimal DefaultWorkingHoursPerDay { get; init; }

    [Required]
    [MaxLength(64)]
    public string LateUndertimeDeductionPolicy { get; init; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string AbsenceDeductionPolicy { get; init; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string OvertimeCalculationPolicy { get; init; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string RoundingRule { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string PayrollTimeZoneId { get; init; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string PayslipVisibilityRule { get; init; } = string.Empty;

    public bool AllowNegativeNetPay { get; init; }

    [Required]
    [MaxLength(8)]
    public string DefaultCurrency { get; init; } = "PHP";
}

public sealed class PayPeriodTemplateOptionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string PayFrequency { get; init; } = string.Empty;

    public int PeriodLengthDays { get; init; }

    public bool IsActive { get; init; }
}

public sealed class PayPeriodTemplateRecordDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string PayFrequency { get; init; } = string.Empty;

    public int PeriodLengthDays { get; init; }

    public int PayrollOffsetDays { get; init; }

    public bool IsActive { get; init; }

    public int PayPeriodCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class PayrollSetupListQueryDto : PagedQueryDto
{
    public bool? IsActive { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "name";

    public bool Descending { get; init; }
}

public sealed class SavePayPeriodTemplateRequestDto
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

    [Required]
    [MaxLength(32)]
    public string PayFrequency { get; init; } = string.Empty;

    [Range(1, 62)]
    public int PeriodLengthDays { get; init; }

    [Range(0, 31)]
    public int PayrollOffsetDays { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class EarningTypeOptionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public bool Taxable { get; init; }

    public bool IsActive { get; init; }
}

public sealed class EarningTypeRecordDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public bool Taxable { get; init; }

    public bool Recurring { get; init; }

    public bool AffectsThirteenthMonth { get; init; }

    public bool IsActive { get; init; }

    public int EmployeeRecurringCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class SaveEarningTypeRequestDto
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

    [Required]
    [MaxLength(32)]
    public string Category { get; init; } = string.Empty;

    public bool Taxable { get; init; } = true;

    public bool Recurring { get; init; }

    public bool AffectsThirteenthMonth { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class DeductionTypeOptionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public bool PreTax { get; init; }

    public bool IsActive { get; init; }
}

public sealed class DeductionTypeRecordDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public bool PreTax { get; init; }

    public bool Recurring { get; init; }

    public bool IsActive { get; init; }

    public int EmployeeRecurringCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class SaveDeductionTypeRequestDto
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

    [Required]
    [MaxLength(32)]
    public string Category { get; init; } = string.Empty;

    public bool PreTax { get; init; }

    public bool Recurring { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class ContributionTypeOptionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class ContributionTypeRecordDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool EmployeeShareApplicable { get; init; }

    public bool EmployerShareApplicable { get; init; }

    public bool IsActive { get; init; }

    public int TableCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class SaveContributionTypeRequestDto
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

    public bool EmployeeShareApplicable { get; init; } = true;

    public bool EmployerShareApplicable { get; init; } = true;

    public bool IsActive { get; init; } = true;
}

public sealed class GovernmentContributionBracketDto
{
    public Guid Id { get; init; }

    public decimal MinCompensation { get; init; }

    public decimal? MaxCompensation { get; init; }

    public decimal? EmployeeShareAmount { get; init; }

    public decimal? EmployeeShareRate { get; init; }

    public decimal? EmployerShareAmount { get; init; }

    public decimal? EmployerShareRate { get; init; }

    public string Remarks { get; init; } = string.Empty;
}

public sealed class SaveGovernmentContributionBracketRequestDto : IValidatableObject
{
    public Guid? Id { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal MinCompensation { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal? MaxCompensation { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal? EmployeeShareAmount { get; init; }

    [Range(0, 1)]
    public decimal? EmployeeShareRate { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal? EmployerShareAmount { get; init; }

    [Range(0, 1)]
    public decimal? EmployerShareRate { get; init; }

    [MaxLength(512)]
    public string Remarks { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxCompensation is not null && MaxCompensation < MinCompensation)
        {
            yield return new ValidationResult(
                "Maximum compensation cannot be earlier than the minimum compensation value.",
                [nameof(MaxCompensation)]);
        }
    }
}

public sealed class GovernmentContributionTableRecordDto
{
    public Guid Id { get; init; }

    public Guid ContributionTypeId { get; init; }

    public string ContributionTypeCode { get; init; } = string.Empty;

    public string ContributionTypeName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public DateOnly EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public bool IsActive { get; init; }

    public int BracketCount { get; init; }

    public IReadOnlyList<GovernmentContributionBracketDto> Brackets { get; init; } = Array.Empty<GovernmentContributionBracketDto>();

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class SaveGovernmentContributionTableRequestDto : IValidatableObject
{
    [Required]
    public Guid? ContributionTypeId { get; init; }

    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public DateOnly? EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<SaveGovernmentContributionBracketRequestDto> Brackets { get; init; } = Array.Empty<SaveGovernmentContributionBracketRequestDto>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EffectiveStartDate is not null && EffectiveEndDate is not null && EffectiveEndDate < EffectiveStartDate)
        {
            yield return new ValidationResult(
                "Effective end date cannot be earlier than the effective start date.",
                [nameof(EffectiveEndDate)]);
        }
    }
}

public sealed class TaxTableOptionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string PayFrequency { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class TaxBracketDto
{
    public Guid Id { get; init; }

    public decimal MinTaxableIncome { get; init; }

    public decimal? MaxTaxableIncome { get; init; }

    public decimal BaseTax { get; init; }

    public decimal TaxRate { get; init; }

    public decimal ExcessOver { get; init; }
}

public sealed class SaveTaxBracketRequestDto : IValidatableObject
{
    public Guid? Id { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal MinTaxableIncome { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal? MaxTaxableIncome { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal BaseTax { get; init; }

    [Range(0, 1)]
    public decimal TaxRate { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal ExcessOver { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MaxTaxableIncome is not null && MaxTaxableIncome < MinTaxableIncome)
        {
            yield return new ValidationResult(
                "Maximum taxable income cannot be earlier than the minimum taxable income value.",
                [nameof(MaxTaxableIncome)]);
        }
    }
}

public sealed class TaxTableRecordDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string PayFrequency { get; init; } = string.Empty;

    public DateOnly EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public bool IsActive { get; init; }

    public IReadOnlyList<TaxBracketDto> Brackets { get; init; } = Array.Empty<TaxBracketDto>();

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class SaveTaxTableRequestDto : IValidatableObject
{
    [Required]
    [MaxLength(32)]
    [RegularExpression("^[A-Za-z0-9._/-]+$", ErrorMessage = "Codes may contain letters, numbers, periods, underscores, slashes, and hyphens only.")]
    public string Code { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string PayFrequency { get; init; } = string.Empty;

    [Required]
    public DateOnly? EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<SaveTaxBracketRequestDto> Brackets { get; init; } = Array.Empty<SaveTaxBracketRequestDto>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EffectiveStartDate is not null && EffectiveEndDate is not null && EffectiveEndDate < EffectiveStartDate)
        {
            yield return new ValidationResult(
                "Effective end date cannot be earlier than the effective start date.",
                [nameof(EffectiveEndDate)]);
        }
    }
}

public sealed class PayPeriodOptionDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string PayFrequency { get; init; } = string.Empty;

    public DateOnly PeriodStartDate { get; init; }

    public DateOnly PeriodEndDate { get; init; }

    public DateOnly PayrollDate { get; init; }

    public string Status { get; init; } = string.Empty;
}

public sealed class PayPeriodRecordDto
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string PayFrequency { get; init; } = string.Empty;

    public DateOnly PeriodStartDate { get; init; }

    public DateOnly PeriodEndDate { get; init; }

    public DateOnly PayrollDate { get; init; }

    public DateOnly CutoffStartDate { get; init; }

    public DateOnly CutoffEndDate { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Remarks { get; init; } = string.Empty;

    public Guid? PayPeriodTemplateId { get; init; }

    public string PayPeriodTemplateName { get; init; } = string.Empty;

    public int PayrollRunCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class PayPeriodListQueryDto : PagedQueryDto, IValidatableObject
{
    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(32)]
    public string PayFrequency { get; init; } = string.Empty;

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "period_start";

    public bool Descending { get; init; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom is not null && DateTo is not null && DateTo < DateFrom)
        {
            yield return new ValidationResult(
                "End date cannot be earlier than the start date.",
                [nameof(DateTo)]);
        }

        if (DateFrom is not null && DateTo is not null && DateTo.Value.DayNumber - DateFrom.Value.DayNumber > 366)
        {
            yield return new ValidationResult(
                "Payroll report date range queries are limited to 366 days.",
                [nameof(DateTo)]);
        }
    }
}

public sealed class SavePayPeriodRequestDto : IValidatableObject
{
    [Required]
    [MaxLength(32)]
    [RegularExpression("^[A-Za-z0-9._/-]+$", ErrorMessage = "Codes may contain letters, numbers, periods, underscores, slashes, and hyphens only.")]
    public string Code { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string PayFrequency { get; init; } = string.Empty;

    [Required]
    public DateOnly? PeriodStartDate { get; init; }

    public DateOnly? PeriodEndDate { get; init; }

    public DateOnly? PayrollDate { get; init; }

    public DateOnly? CutoffStartDate { get; init; }

    public DateOnly? CutoffEndDate { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;

    public Guid? PayPeriodTemplateId { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PeriodStartDate is not null && PeriodEndDate is not null && PeriodEndDate < PeriodStartDate)
        {
            yield return new ValidationResult(
                "Period end date cannot be earlier than the period start date.",
                [nameof(PeriodEndDate)]);
        }

        if (CutoffStartDate is not null && CutoffEndDate is not null && CutoffEndDate < CutoffStartDate)
        {
            yield return new ValidationResult(
                "Cutoff end date cannot be earlier than the cutoff start date.",
                [nameof(CutoffEndDate)]);
        }
    }
}

public sealed class CompensationProfileRecordDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string PayType { get; init; } = string.Empty;

    public string PayFrequency { get; init; } = string.Empty;

    public decimal BasicSalary { get; init; }

    public decimal? DailyRate { get; init; }

    public decimal? HourlyRate { get; init; }

    public string Currency { get; init; } = string.Empty;

    public DateOnly EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public bool IsActive { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public string CreatedByDisplayName { get; init; } = string.Empty;

    public string UpdatedByDisplayName { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class CompensationProfileListQueryDto : PagedQueryDto
{
    public Guid? EmployeeId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public bool? IsActive { get; init; }

    public DateOnly? EffectiveDate { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "effective_start";

    public bool Descending { get; init; } = true;
}

public sealed class SaveCompensationProfileRequestDto : IValidatableObject
{
    [Required]
    public Guid? EmployeeId { get; init; }

    [Required]
    [MaxLength(32)]
    public string PayType { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string PayFrequency { get; init; } = string.Empty;

    [Range(0, 9_999_999_999d)]
    public decimal BasicSalary { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal? DailyRate { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal? HourlyRate { get; init; }

    [Required]
    [MaxLength(8)]
    public string Currency { get; init; } = "PHP";

    [Required]
    public DateOnly? EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public bool IsActive { get; init; } = true;

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EffectiveStartDate is not null && EffectiveEndDate is not null && EffectiveEndDate < EffectiveStartDate)
        {
            yield return new ValidationResult(
                "Effective end date cannot be earlier than the effective start date.",
                [nameof(EffectiveEndDate)]);
        }
    }
}

public sealed class EmployeeRecurringEarningRecordDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public Guid EarningTypeId { get; init; }

    public string EarningTypeCode { get; init; } = string.Empty;

    public string EarningTypeName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Frequency { get; init; } = string.Empty;

    public DateOnly EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public bool IsActive { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class SaveEmployeeRecurringEarningRequestDto : IValidatableObject
{
    [Required]
    public Guid? EmployeeId { get; init; }

    [Required]
    public Guid? EarningTypeId { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal Amount { get; init; }

    [Required]
    [MaxLength(32)]
    public string Frequency { get; init; } = string.Empty;

    [Required]
    public DateOnly? EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public bool IsActive { get; init; } = true;

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EffectiveStartDate is not null && EffectiveEndDate is not null && EffectiveEndDate < EffectiveStartDate)
        {
            yield return new ValidationResult(
                "Effective end date cannot be earlier than the effective start date.",
                [nameof(EffectiveEndDate)]);
        }
    }
}

public sealed class EmployeeRecurringDeductionRecordDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public Guid DeductionTypeId { get; init; }

    public string DeductionTypeCode { get; init; } = string.Empty;

    public string DeductionTypeName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Frequency { get; init; } = string.Empty;

    public decimal? Balance { get; init; }

    public DateOnly EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public bool IsActive { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class SaveEmployeeRecurringDeductionRequestDto : IValidatableObject
{
    [Required]
    public Guid? EmployeeId { get; init; }

    [Required]
    public Guid? DeductionTypeId { get; init; }

    [Range(0, 9_999_999_999d)]
    public decimal Amount { get; init; }

    [Required]
    [MaxLength(32)]
    public string Frequency { get; init; } = string.Empty;

    [Range(0, 9_999_999_999d)]
    public decimal? Balance { get; init; }

    [Required]
    public DateOnly? EffectiveStartDate { get; init; }

    public DateOnly? EffectiveEndDate { get; init; }

    public bool IsActive { get; init; } = true;

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EffectiveStartDate is not null && EffectiveEndDate is not null && EffectiveEndDate < EffectiveStartDate)
        {
            yield return new ValidationResult(
                "Effective end date cannot be earlier than the effective start date.",
                [nameof(EffectiveEndDate)]);
        }
    }
}

public sealed class RecurringPayrollComponentListQueryDto : PagedQueryDto
{
    public Guid? EmployeeId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public bool? IsActive { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "effective_start";

    public bool Descending { get; init; } = true;
}

public sealed class PayrollRunSummaryDto
{
    public Guid Id { get; init; }

    public Guid PayPeriodId { get; init; }

    public string PayPeriodCode { get; init; } = string.Empty;

    public string PayPeriodName { get; init; } = string.Empty;

    public string ReferenceNumber { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public int EmployeeCount { get; init; }

    public int HoldCount { get; init; }

    public int CriticalIssueCount { get; init; }

    public decimal TotalGrossPay { get; init; }

    public decimal TotalDeductions { get; init; }

    public decimal TotalNetPay { get; init; }

    public string GeneratedByDisplayName { get; init; } = string.Empty;

    public DateTime GeneratedAtUtc { get; init; }

    public string ApprovedByDisplayName { get; init; } = string.Empty;

    public DateTime? ApprovedAtUtc { get; init; }

    public DateTime? PaidAtUtc { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class PayrollRunListQueryDto : PagedQueryDto
{
    public Guid? PayPeriodId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "generated";

    public bool Descending { get; init; } = true;
}

public sealed class GeneratePayrollRunRequestDto
{
    [Required]
    public Guid? PayPeriodId { get; init; }

    [Required]
    [MaxLength(64)]
    public string ReferenceNumber { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string Name { get; init; } = string.Empty;

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid? EmploymentTypeId { get; init; }

    public Guid? EmploymentStatusId { get; init; }

    public IReadOnlyList<Guid> SelectedEmployeeIds { get; init; } = Array.Empty<Guid>();

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public sealed class PayrollRunActionRequestDto
{
    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public sealed class PayrollEarningLineDto
{
    public Guid Id { get; init; }

    public Guid? EarningTypeId { get; init; }

    public string EarningTypeCode { get; init; } = string.Empty;

    public string EarningTypeName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public decimal? Quantity { get; init; }

    public decimal? Rate { get; init; }

    public string Source { get; init; } = string.Empty;

    public bool Taxable { get; init; }

    public bool IsManual { get; init; }

    public string Remarks { get; init; } = string.Empty;
}

public sealed class PayrollDeductionLineDto
{
    public Guid Id { get; init; }

    public Guid? DeductionTypeId { get; init; }

    public string DeductionTypeCode { get; init; } = string.Empty;

    public string DeductionTypeName { get; init; } = string.Empty;

    public string DeductionCategory { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Source { get; init; } = string.Empty;

    public bool PreTax { get; init; }

    public bool IsManual { get; init; }

    public string Remarks { get; init; } = string.Empty;
}

public sealed class PayrollRunItemDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string PositionName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string PayType { get; init; } = string.Empty;

    public string Currency { get; init; } = string.Empty;

    public decimal BasicSalary { get; init; }

    public decimal? DailyRate { get; init; }

    public decimal? HourlyRate { get; init; }

    public decimal RegularWorkedDays { get; init; }

    public decimal RegularWorkedHours { get; init; }

    public decimal PaidLeaveDays { get; init; }

    public decimal UnpaidLeaveDays { get; init; }

    public decimal AbsentDays { get; init; }

    public int LateMinutes { get; init; }

    public int UndertimeMinutes { get; init; }

    public int OvertimeMinutes { get; init; }

    public decimal BasicPay { get; init; }

    public decimal AllowanceTotal { get; init; }

    public decimal OvertimePay { get; init; }

    public decimal HolidayPay { get; init; }

    public decimal LeavePay { get; init; }

    public decimal BonusTotal { get; init; }

    public decimal OtherEarningsTotal { get; init; }

    public decimal GrossPay { get; init; }

    public decimal GovernmentDeductionsTotal { get; init; }

    public decimal TaxDeduction { get; init; }

    public decimal AbsenceDeduction { get; init; }

    public decimal LateDeduction { get; init; }

    public decimal UndertimeDeduction { get; init; }

    public decimal LoanDeduction { get; init; }

    public decimal OtherDeductionsTotal { get; init; }

    public decimal TotalDeductions { get; init; }

    public decimal NetPay { get; init; }

    public decimal EmployerContributionTotal { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Remarks { get; init; } = string.Empty;

    public bool HasCriticalIssues { get; init; }

    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();

    public IReadOnlyList<PayrollEarningLineDto> Earnings { get; init; } = Array.Empty<PayrollEarningLineDto>();

    public IReadOnlyList<PayrollDeductionLineDto> Deductions { get; init; } = Array.Empty<PayrollDeductionLineDto>();

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class PayrollAuditLogDto
{
    public Guid Id { get; init; }

    public string EntityType { get; init; } = string.Empty;

    public string EntityId { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public string ActorDisplayName { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class PayrollRunDetailDto
{
    public PayrollRunSummaryDto Run { get; init; } = new();

    public PayPeriodRecordDto PayPeriod { get; init; } = new();

    public IReadOnlyList<PayrollRunItemDto> Items { get; init; } = Array.Empty<PayrollRunItemDto>();

    public IReadOnlyList<PayrollAuditLogDto> AuditLogs { get; init; } = Array.Empty<PayrollAuditLogDto>();
}

public sealed class PayrollAdjustmentRecordDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public Guid? PayPeriodId { get; init; }

    public string PayPeriodName { get; init; } = string.Empty;

    public Guid? PayrollRunId { get; init; }

    public string PayrollRunReferenceNumber { get; init; } = string.Empty;

    public string AdjustmentType { get; init; } = string.Empty;

    public Guid? EarningTypeId { get; init; }

    public string EarningTypeName { get; init; } = string.Empty;

    public Guid? DeductionTypeId { get; init; }

    public string DeductionTypeName { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string RequestedByDisplayName { get; init; } = string.Empty;

    public string ApprovedByDisplayName { get; init; } = string.Empty;

    public DateTime? ApprovedAtUtc { get; init; }

    public DateTime? AppliedAtUtc { get; init; }

    public string DecisionRemarks { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class PayrollAdjustmentListQueryDto : PagedQueryDto, IValidatableObject
{
    public Guid? EmployeeId { get; init; }

    public Guid? PayPeriodId { get; init; }

    public Guid? PayrollRunId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(32)]
    public string AdjustmentType { get; init; } = string.Empty;

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
    }
}

public sealed class SavePayrollAdjustmentRequestDto
{
    [Required]
    public Guid? EmployeeId { get; init; }

    public Guid? PayPeriodId { get; init; }

    public Guid? PayrollRunId { get; init; }

    [Required]
    [MaxLength(32)]
    public string AdjustmentType { get; init; } = string.Empty;

    public Guid? EarningTypeId { get; init; }

    public Guid? DeductionTypeId { get; init; }

    [Range(0.01, 9_999_999_999d)]
    public decimal Amount { get; init; }

    [Required]
    [MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;
}

public sealed class EmployeePayrollProfileDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public IReadOnlyList<CompensationProfileRecordDto> CompensationProfiles { get; init; } = Array.Empty<CompensationProfileRecordDto>();

    public IReadOnlyList<EmployeeRecurringEarningRecordDto> RecurringEarnings { get; init; } = Array.Empty<EmployeeRecurringEarningRecordDto>();

    public IReadOnlyList<EmployeeRecurringDeductionRecordDto> RecurringDeductions { get; init; } = Array.Empty<EmployeeRecurringDeductionRecordDto>();

    public IReadOnlyList<PayrollRunItemDto> PayrollHistory { get; init; } = Array.Empty<PayrollRunItemDto>();
}

public sealed class PayrollReportGroupDto
{
    public string Label { get; init; } = string.Empty;

    public int Count { get; init; }

    public decimal GrossPay { get; init; }

    public decimal Deductions { get; init; }

    public decimal NetPay { get; init; }
}

public sealed class PayrollReportLineDto
{
    public string Label { get; init; } = string.Empty;

    public decimal Amount { get; init; }
}

public sealed class PayrollReportQueryDto : IValidatableObject
{
    public Guid? PayPeriodId { get; init; }

    public Guid? PayrollRunId { get; init; }

    public Guid? EmployeeId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public Guid? EmploymentTypeId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom is not null && DateTo is not null && DateTo < DateFrom)
        {
            yield return new ValidationResult(
                "End date cannot be earlier than the start date.",
                [nameof(DateTo)]);
        }
    }
}

public sealed class PayrollReportsDto
{
    public decimal TotalGrossPay { get; init; }

    public decimal TotalDeductions { get; init; }

    public decimal TotalNetPay { get; init; }

    public IReadOnlyList<PayrollRunItemDto> Register { get; init; } = Array.Empty<PayrollRunItemDto>();

    public IReadOnlyList<PayrollReportGroupDto> ByDepartment { get; init; } = Array.Empty<PayrollReportGroupDto>();

    public IReadOnlyList<PayrollReportGroupDto> ByBranch { get; init; } = Array.Empty<PayrollReportGroupDto>();

    public IReadOnlyList<PayrollReportLineDto> Earnings { get; init; } = Array.Empty<PayrollReportLineDto>();

    public IReadOnlyList<PayrollReportLineDto> Deductions { get; init; } = Array.Empty<PayrollReportLineDto>();

    public IReadOnlyList<PayrollReportLineDto> GovernmentContributions { get; init; } = Array.Empty<PayrollReportLineDto>();

    public IReadOnlyList<PayrollAdjustmentRecordDto> Adjustments { get; init; } = Array.Empty<PayrollAdjustmentRecordDto>();
}

public sealed class PayslipDto
{
    public Guid PayrollRunItemId { get; init; }

    public string CompanyName { get; init; } = string.Empty;

    public string PayrollRunReferenceNumber { get; init; } = string.Empty;

    public string PayrollRunName { get; init; } = string.Empty;

    public string PayPeriodName { get; init; } = string.Empty;

    public DateOnly PeriodStartDate { get; init; }

    public DateOnly PeriodEndDate { get; init; }

    public DateOnly PayrollDate { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string PositionName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string Currency { get; init; } = string.Empty;

    public decimal RegularWorkedDays { get; init; }

    public decimal RegularWorkedHours { get; init; }

    public decimal PaidLeaveDays { get; init; }

    public decimal UnpaidLeaveDays { get; init; }

    public decimal AbsentDays { get; init; }

    public int LateMinutes { get; init; }

    public int UndertimeMinutes { get; init; }

    public int OvertimeMinutes { get; init; }

    public decimal GrossPay { get; init; }

    public decimal TotalDeductions { get; init; }

    public decimal NetPay { get; init; }

    public decimal EmployerContributionTotal { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();

    public IReadOnlyList<PayrollEarningLineDto> Earnings { get; init; } = Array.Empty<PayrollEarningLineDto>();

    public IReadOnlyList<PayrollDeductionLineDto> Deductions { get; init; } = Array.Empty<PayrollDeductionLineDto>();

    public DateTime GeneratedAtUtc { get; init; }
}
