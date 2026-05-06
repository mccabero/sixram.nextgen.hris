using System.ComponentModel.DataAnnotations;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;

namespace Sixram.Api.DTOs.ProvidentFund;

public sealed class ProvidentFundOptionsDto
{
    public IReadOnlyList<EmployeeAttendanceOptionDto> Employees { get; init; } = Array.Empty<EmployeeAttendanceOptionDto>();

    public IReadOnlyList<LookupOptionDto> Departments { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<ProvidentFundPolicyOptionDto> Policies { get; init; } = Array.Empty<ProvidentFundPolicyOptionDto>();

    public IReadOnlyList<string> ContributionTypes { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> PolicyStatuses { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> EnrollmentStatuses { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> BatchStatuses { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> LedgerTransactionTypes { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> WithdrawalStatuses { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> WithdrawalTypes { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> AdjustmentStatuses { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> AdjustmentTypes { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> ShareTypes { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}

public sealed class ProvidentFundDashboardDto
{
    public decimal TotalFundValue { get; init; }

    public decimal TotalEmployeeContributions { get; init; }

    public decimal TotalEmployerContributions { get; init; }

    public int PendingWithdrawalRequestCount { get; init; }

    public string CurrentMonthContributionStatus { get; init; } = string.Empty;

    public int EmployeesEnrolled { get; init; }

    public int EmployeesNotEnrolled { get; init; }

    public decimal TotalWithdrawalsThisMonth { get; init; }

    public IReadOnlyList<ProvidentFundBalanceTrendPointDto> FundBalanceTrend { get; init; } = Array.Empty<ProvidentFundBalanceTrendPointDto>();
}

public sealed class ProvidentFundBalanceTrendPointDto
{
    public string Period { get; init; } = string.Empty;

    public decimal Balance { get; init; }
}

public sealed class ProvidentFundPolicyOptionDto
{
    public Guid Id { get; init; }

    public string PolicyName { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public bool AllowVoluntaryContribution { get; init; }

    public bool AllowWithdrawal { get; init; }
}

public sealed class ProvidentFundPolicyRecordDto
{
    public Guid Id { get; init; }

    public string PolicyName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string EligibilityRules { get; init; } = string.Empty;

    public string EmployeeContributionType { get; init; } = string.Empty;

    public decimal EmployeeContributionValue { get; init; }

    public string EmployerContributionType { get; init; } = string.Empty;

    public decimal EmployerContributionValue { get; init; }

    public string ContributionFrequency { get; init; } = string.Empty;

    public DateOnly EffectiveDate { get; init; }

    public string Status { get; init; } = string.Empty;

    public bool AllowVoluntaryContribution { get; init; }

    public bool AllowWithdrawal { get; init; }

    public bool AllowLoan { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public int VestingRuleCount { get; init; }

    public int ActiveEnrollmentCount { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class ProvidentFundPolicyListQueryDto : PagedQueryDto
{
    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(32)]
    public string SortBy { get; init; } = "name";

    public bool Descending { get; init; }
}

public sealed class SaveProvidentFundPolicyRequestDto
{
    [Required]
    [MaxLength(160)]
    public string PolicyName { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string EligibilityRules { get; init; } = string.Empty;

    [Required]
    [MaxLength(32)]
    public string EmployeeContributionType { get; init; } = string.Empty;

    [Range(0, 999999999)]
    public decimal EmployeeContributionValue { get; init; }

    [Required]
    [MaxLength(32)]
    public string EmployerContributionType { get; init; } = string.Empty;

    [Range(0, 999999999)]
    public decimal EmployerContributionValue { get; init; }

    [Required]
    [MaxLength(32)]
    public string ContributionFrequency { get; init; } = "monthly";

    [Required]
    public DateOnly? EffectiveDate { get; init; }

    [Required]
    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    public bool AllowVoluntaryContribution { get; init; }

    public bool AllowWithdrawal { get; init; }

    public bool AllowLoan { get; init; }

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public sealed class ProvidentFundVestingRuleDto
{
    public Guid Id { get; init; }

    public Guid PolicyId { get; init; }

    public string PolicyName { get; init; } = string.Empty;

    public int YearsOfService { get; init; }

    public decimal VestedPercentage { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class ProvidentFundVestingRuleListQueryDto : PagedQueryDto
{
    public Guid? PolicyId { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "years";

    public bool Descending { get; init; }
}

public sealed class SaveProvidentFundVestingRuleRequestDto
{
    [Required]
    public Guid? PolicyId { get; init; }

    [Range(0, 80)]
    public int YearsOfService { get; init; }

    [Range(0, 100)]
    public decimal VestedPercentage { get; init; }

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public sealed class ProvidentFundEnrollmentRecordDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public Guid PolicyId { get; init; }

    public string PolicyName { get; init; } = string.Empty;

    public DateOnly EnrollmentDate { get; init; }

    public DateOnly VestingStartDate { get; init; }

    public string EmployeeContributionOverrideType { get; init; } = string.Empty;

    public decimal? EmployeeContributionOverrideValue { get; init; }

    public string EmployerContributionOverrideType { get; init; } = string.Empty;

    public decimal? EmployerContributionOverrideValue { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Remarks { get; init; } = string.Empty;

    public decimal GrossBalance { get; init; }

    public decimal WithdrawableBalance { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class ProvidentFundEnrollmentListQueryDto : PagedQueryDto
{
    public Guid? EmployeeId { get; init; }

    public Guid? PolicyId { get; init; }

    public Guid? DepartmentId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(32)]
    public string SortBy { get; init; } = "created";

    public bool Descending { get; init; } = true;
}

public sealed class SaveProvidentFundEnrollmentRequestDto
{
    [Required]
    public Guid? EmployeeId { get; init; }

    [Required]
    public Guid? PolicyId { get; init; }

    [Required]
    public DateOnly? EnrollmentDate { get; init; }

    [Required]
    public DateOnly? VestingStartDate { get; init; }

    [MaxLength(32)]
    public string EmployeeContributionOverrideType { get; init; } = string.Empty;

    [Range(0, 999999999)]
    public decimal? EmployeeContributionOverrideValue { get; init; }

    [MaxLength(32)]
    public string EmployerContributionOverrideType { get; init; } = string.Empty;

    [Range(0, 999999999)]
    public decimal? EmployerContributionOverrideValue { get; init; }

    [Required]
    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public sealed class GenerateProvidentFundContributionBatchRequestDto
{
    [Range(1, 12)]
    public int Month { get; init; }

    [Range(2000, 2200)]
    public int Year { get; init; }

    public Guid? PolicyId { get; init; }

    public bool IsSupplemental { get; init; }

    [MaxLength(64)]
    public string BatchNumber { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;

    public IReadOnlyList<ProvidentFundManualContributionInputDto> ManualLines { get; init; } = Array.Empty<ProvidentFundManualContributionInputDto>();
}

public sealed class ProvidentFundManualContributionInputDto
{
    public Guid EmployeeId { get; init; }

    [Range(0, 999999999)]
    public decimal? BasicSalary { get; init; }

    [Range(0, 999999999)]
    public decimal? EmployeeContribution { get; init; }

    [Range(0, 999999999)]
    public decimal? EmployerContribution { get; init; }

    [Range(0, 999999999)]
    public decimal? VoluntaryContribution { get; init; }
}

public sealed class ProvidentFundContributionBatchSummaryDto
{
    public Guid Id { get; init; }

    public string BatchNumber { get; init; } = string.Empty;

    public int Month { get; init; }

    public int Year { get; init; }

    public Guid? PolicyId { get; init; }

    public string PolicyName { get; init; } = string.Empty;

    public bool IsSupplemental { get; init; }

    public string Status { get; init; } = string.Empty;

    public int LineCount { get; init; }

    public decimal TotalEmployeeContribution { get; init; }

    public decimal TotalEmployerContribution { get; init; }

    public decimal TotalVoluntaryContribution { get; init; }

    public decimal TotalContribution { get; init; }

    public string CreatedByDisplayName { get; init; } = string.Empty;

    public string ReviewedByDisplayName { get; init; } = string.Empty;

    public string PostedByDisplayName { get; init; } = string.Empty;

    public DateTime? PostingDate { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class ProvidentFundContributionBatchDetailDto
{
    public ProvidentFundContributionBatchSummaryDto Batch { get; init; } = new();

    public IReadOnlyList<ProvidentFundContributionBatchLineDto> Lines { get; init; } = Array.Empty<ProvidentFundContributionBatchLineDto>();
}

public sealed class ProvidentFundContributionBatchLineDto
{
    public Guid Id { get; init; }

    public Guid BatchId { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public Guid EnrollmentId { get; init; }

    public decimal BasicSalary { get; init; }

    public decimal EmployeeContribution { get; init; }

    public decimal EmployerContribution { get; init; }

    public decimal VoluntaryContribution { get; init; }

    public decimal TotalContribution { get; init; }

    public string Status { get; init; } = string.Empty;

    public string Remarks { get; init; } = string.Empty;
}

public sealed class ProvidentFundContributionBatchListQueryDto : PagedQueryDto
{
    public int? Month { get; init; }

    public int? Year { get; init; }

    public Guid? PolicyId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(32)]
    public string SortBy { get; init; } = "created";

    public bool Descending { get; init; } = true;
}

public sealed class ProvidentFundContributionBatchActionRequestDto
{
    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public sealed class ProvidentFundLedgerTransactionDto
{
    public Guid Id { get; init; }

    public string TransactionNumber { get; init; } = string.Empty;

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public Guid EnrollmentId { get; init; }

    public Guid PolicyId { get; init; }

    public string PolicyName { get; init; } = string.Empty;

    public DateOnly TransactionDate { get; init; }

    public string TransactionType { get; init; } = string.Empty;

    public string SourceType { get; init; } = string.Empty;

    public string SourceReferenceId { get; init; } = string.Empty;

    public decimal EmployeeShareAmount { get; init; }

    public decimal EmployerShareAmount { get; init; }

    public decimal VoluntaryShareAmount { get; init; }

    public decimal InterestAmount { get; init; }

    public decimal DebitAmount { get; init; }

    public decimal CreditAmount { get; init; }

    public decimal? RunningBalance { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public string CreatedByDisplayName { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public bool IsReversed { get; init; }

    public Guid? ReversalReferenceId { get; init; }
}

public sealed class ProvidentFundLedgerListQueryDto : PagedQueryDto
{
    public Guid? EmployeeId { get; init; }

    public Guid? PolicyId { get; init; }

    public Guid? DepartmentId { get; init; }

    [MaxLength(64)]
    public string TransactionType { get; init; } = string.Empty;

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "date";

    public bool Descending { get; init; } = true;
}

public sealed class ProvidentFundBalanceDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public Guid? EnrollmentId { get; init; }

    public string EnrollmentStatus { get; init; } = string.Empty;

    public Guid? PolicyId { get; init; }

    public string PolicyName { get; init; } = string.Empty;

    public DateOnly? EnrollmentDate { get; init; }

    public DateOnly? VestingStartDate { get; init; }

    public decimal VestingPercentage { get; init; }

    public decimal TotalEmployeeContribution { get; init; }

    public decimal TotalEmployerContribution { get; init; }

    public decimal TotalVoluntaryContribution { get; init; }

    public decimal TotalInterest { get; init; }

    public decimal TotalWithdrawals { get; init; }

    public decimal TotalAdjustments { get; init; }

    public decimal GrossFundBalance { get; init; }

    public decimal VestedEmployerBalance { get; init; }

    public decimal NonVestedEmployerBalance { get; init; }

    public decimal WithdrawableBalance { get; init; }

    public DateOnly? LatestTransactionDate { get; init; }
}

public sealed class ProvidentFundWithdrawalRequestDto
{
    public Guid Id { get; init; }

    public string RequestNumber { get; init; } = string.Empty;

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public Guid EnrollmentId { get; init; }

    public DateOnly RequestDate { get; init; }

    public string WithdrawalType { get; init; } = string.Empty;

    public decimal RequestedAmount { get; init; }

    public decimal EligibleWithdrawableAmount { get; init; }

    public decimal ApprovedAmount { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string AttachmentPath { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTime? PaymentDate { get; init; }

    public string Remarks { get; init; } = string.Empty;

    public IReadOnlyList<ProvidentFundApprovalHistoryDto> Approvals { get; init; } = Array.Empty<ProvidentFundApprovalHistoryDto>();

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class ProvidentFundApprovalHistoryDto
{
    public Guid Id { get; init; }

    public string StepName { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;

    public string ActorName { get; init; } = string.Empty;

    public string Remarks { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}

public sealed class ProvidentFundWithdrawalListQueryDto : PagedQueryDto
{
    public Guid? EmployeeId { get; init; }

    public Guid? DepartmentId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(32)]
    public string WithdrawalType { get; init; } = string.Empty;

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "created";

    public bool Descending { get; init; } = true;
}

public sealed class SaveProvidentFundWithdrawalRequestDto
{
    public Guid? EmployeeId { get; init; }

    public Guid? EnrollmentId { get; init; }

    [Required]
    public DateOnly? RequestDate { get; init; }

    [Required]
    [MaxLength(32)]
    public string WithdrawalType { get; init; } = string.Empty;

    [Range(0.01, 999999999)]
    public decimal RequestedAmount { get; init; }

    [MaxLength(2000)]
    public string Reason { get; init; } = string.Empty;

    [MaxLength(512)]
    public string AttachmentPath { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public sealed class ProvidentFundWithdrawalActionRequestDto
{
    [Range(0, 999999999)]
    public decimal? ApprovedAmount { get; init; }

    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;

    public bool CloseEnrollment { get; init; }
}

public sealed class ProvidentFundAdjustmentDto
{
    public Guid Id { get; init; }

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public Guid EnrollmentId { get; init; }

    public string AdjustmentType { get; init; } = string.Empty;

    public DateOnly AdjustmentDate { get; init; }

    public decimal Amount { get; init; }

    public string ShareAffected { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public string AttachmentPath { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string RequestedByDisplayName { get; init; } = string.Empty;

    public string ApprovedByDisplayName { get; init; } = string.Empty;

    public DateTime? ApprovedAtUtc { get; init; }

    public DateTime? PostedAtUtc { get; init; }

    public string DecisionRemarks { get; init; } = string.Empty;

    public IReadOnlyList<ProvidentFundApprovalHistoryDto> Approvals { get; init; } = Array.Empty<ProvidentFundApprovalHistoryDto>();

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class ProvidentFundAdjustmentListQueryDto : PagedQueryDto
{
    public Guid? EmployeeId { get; init; }

    public Guid? DepartmentId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(32)]
    public string AdjustmentType { get; init; } = string.Empty;

    [MaxLength(32)]
    public string ShareAffected { get; init; } = string.Empty;

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "created";

    public bool Descending { get; init; } = true;
}

public sealed class SaveProvidentFundAdjustmentRequestDto
{
    [Required]
    public Guid? EmployeeId { get; init; }

    public Guid? EnrollmentId { get; init; }

    [Required]
    [MaxLength(32)]
    public string AdjustmentType { get; init; } = string.Empty;

    [Required]
    public DateOnly? AdjustmentDate { get; init; }

    [Range(0.01, 999999999)]
    public decimal Amount { get; init; }

    [Required]
    [MaxLength(32)]
    public string ShareAffected { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Reason { get; init; } = string.Empty;

    [MaxLength(512)]
    public string AttachmentPath { get; init; } = string.Empty;
}

public sealed class ProvidentFundActionRequestDto
{
    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public sealed class ProvidentFundReportQueryDto
{
    public int? Month { get; init; }

    public int? Year { get; init; }

    public Guid? PolicyId { get; init; }

    public Guid? DepartmentId { get; init; }

    public Guid? EmployeeId { get; init; }

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    [MaxLength(32)]
    public string WithdrawalType { get; init; } = string.Empty;

    [MaxLength(64)]
    public string TransactionType { get; init; } = string.Empty;

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    public DateOnly? AsOfDate { get; init; }

    [MaxLength(64)]
    public string EmploymentStatus { get; init; } = string.Empty;
}

public sealed class ProvidentFundReportsDto
{
    public IReadOnlyList<ProvidentFundContributionReportRowDto> Contributions { get; init; } = Array.Empty<ProvidentFundContributionReportRowDto>();

    public IReadOnlyList<ProvidentFundBalanceReportRowDto> Balances { get; init; } = Array.Empty<ProvidentFundBalanceReportRowDto>();

    public IReadOnlyList<ProvidentFundWithdrawalReportRowDto> Withdrawals { get; init; } = Array.Empty<ProvidentFundWithdrawalReportRowDto>();

    public IReadOnlyList<ProvidentFundLedgerTransactionDto> Ledger { get; init; } = Array.Empty<ProvidentFundLedgerTransactionDto>();
}

public sealed class ProvidentFundContributionReportRowDto
{
    public string EmployeeNumber { get; init; } = string.Empty;

    public string EmployeeName { get; init; } = string.Empty;

    public string Department { get; init; } = string.Empty;

    public decimal BasicSalary { get; init; }

    public decimal EmployeeContribution { get; init; }

    public decimal EmployerContribution { get; init; }

    public decimal VoluntaryContribution { get; init; }

    public decimal TotalContribution { get; init; }

    public string BatchStatus { get; init; } = string.Empty;
}

public sealed class ProvidentFundBalanceReportRowDto
{
    public string EmployeeNumber { get; init; } = string.Empty;

    public string EmployeeName { get; init; } = string.Empty;

    public decimal TotalEmployeeShare { get; init; }

    public decimal TotalEmployerShare { get; init; }

    public decimal VestedEmployerShare { get; init; }

    public decimal NonVestedEmployerShare { get; init; }

    public decimal Interest { get; init; }

    public decimal Withdrawals { get; init; }

    public decimal CurrentBalance { get; init; }

    public decimal WithdrawableBalance { get; init; }
}

public sealed class ProvidentFundWithdrawalReportRowDto
{
    public string RequestNumber { get; init; } = string.Empty;

    public string Employee { get; init; } = string.Empty;

    public DateOnly RequestDate { get; init; }

    public string WithdrawalType { get; init; } = string.Empty;

    public decimal RequestedAmount { get; init; }

    public decimal ApprovedAmount { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime? PaymentDate { get; init; }
}
