namespace Sixram.Api.Constants;

public static class ProvidentFundPermissions
{
    public const string PolicyView = "ProvidentFund.Policy.View";
    public const string PolicyManage = "ProvidentFund.Policy.Manage";
    public const string EnrollmentView = "ProvidentFund.Enrollment.View";
    public const string EnrollmentManage = "ProvidentFund.Enrollment.Manage";
    public const string ContributionView = "ProvidentFund.Contribution.View";
    public const string ContributionProcess = "ProvidentFund.Contribution.Process";
    public const string ContributionPost = "ProvidentFund.Contribution.Post";
    public const string LedgerView = "ProvidentFund.Ledger.View";
    public const string WithdrawalView = "ProvidentFund.Withdrawal.View";
    public const string WithdrawalRequest = "ProvidentFund.Withdrawal.Request";
    public const string WithdrawalApprove = "ProvidentFund.Withdrawal.Approve";
    public const string AdjustmentManage = "ProvidentFund.Adjustment.Manage";
    public const string ReportView = "ProvidentFund.Report.View";
    public const string EmployeeSelfServiceView = "ProvidentFund.EmployeeSelfService.View";

    public static readonly IReadOnlyList<string> All =
    [
        PolicyView,
        PolicyManage,
        EnrollmentView,
        EnrollmentManage,
        ContributionView,
        ContributionProcess,
        ContributionPost,
        LedgerView,
        WithdrawalView,
        WithdrawalRequest,
        WithdrawalApprove,
        AdjustmentManage,
        ReportView,
        EmployeeSelfServiceView
    ];
}

public static class ProvidentFundContributionTypes
{
    public const string Percentage = "percentage";
    public const string FixedAmount = "fixed_amount";

    public static readonly IReadOnlyList<string> All = [Percentage, FixedAmount];
}

public static class ProvidentFundPolicyStatuses
{
    public const string Active = "active";
    public const string Inactive = "inactive";

    public static readonly IReadOnlyList<string> All = [Active, Inactive];
}

public static class ProvidentFundEnrollmentStatuses
{
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Closed = "closed";

    public static readonly IReadOnlyList<string> All = [Active, Suspended, Closed];
}

public static class ProvidentFundContributionBatchStatuses
{
    public const string Draft = "draft";
    public const string Reviewed = "reviewed";
    public const string Posted = "posted";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All = [Draft, Reviewed, Posted, Cancelled];
}

public static class ProvidentFundContributionLineStatuses
{
    public const string Draft = "draft";
    public const string Reviewed = "reviewed";
    public const string Posted = "posted";
    public const string Held = "held";

    public static readonly IReadOnlyList<string> All = [Draft, Reviewed, Posted, Held];
}

public static class ProvidentFundLedgerTransactionTypes
{
    public const string EmployeeContribution = "employee_contribution";
    public const string EmployerContribution = "employer_contribution";
    public const string VoluntaryContribution = "voluntary_contribution";
    public const string Withdrawal = "withdrawal";
    public const string Interest = "interest";
    public const string Adjustment = "adjustment";
    public const string Reversal = "reversal";
    public const string Forfeiture = "forfeiture";
    public const string FinalSettlement = "final_settlement";

    public static readonly IReadOnlyList<string> All =
    [
        EmployeeContribution,
        EmployerContribution,
        VoluntaryContribution,
        Withdrawal,
        Interest,
        Adjustment,
        Reversal,
        Forfeiture,
        FinalSettlement
    ];
}

public static class ProvidentFundLedgerSourceTypes
{
    public const string ContributionBatch = "contribution_batch";
    public const string Withdrawal = "withdrawal";
    public const string Adjustment = "adjustment";
    public const string Manual = "manual";
    public const string Settlement = "settlement";
}

public static class ProvidentFundWithdrawalStatuses
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";
    public const string HrReviewed = "hr_reviewed";
    public const string FinanceReviewed = "finance_reviewed";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Paid = "paid";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        Submitted,
        HrReviewed,
        FinanceReviewed,
        Approved,
        Rejected,
        Paid,
        Cancelled
    ];
}

public static class ProvidentFundWithdrawalTypes
{
    public const string Partial = "partial";
    public const string Full = "full";
    public const string Retirement = "retirement";
    public const string Resignation = "resignation";
    public const string Emergency = "emergency";
    public const string Other = "other";

    public static readonly IReadOnlyList<string> All = [Partial, Full, Retirement, Resignation, Emergency, Other];
}

public static class ProvidentFundAdjustmentStatuses
{
    public const string Draft = "draft";
    public const string Approved = "approved";
    public const string Posted = "posted";
    public const string Rejected = "rejected";

    public static readonly IReadOnlyList<string> All = [Draft, Approved, Posted, Rejected];
}

public static class ProvidentFundAdjustmentTypes
{
    public const string Credit = "credit";
    public const string Debit = "debit";

    public static readonly IReadOnlyList<string> All = [Credit, Debit];
}

public static class ProvidentFundShareTypes
{
    public const string Employee = "employee";
    public const string Employer = "employer";
    public const string Voluntary = "voluntary";
    public const string Interest = "interest";

    public static readonly IReadOnlyList<string> All = [Employee, Employer, Voluntary, Interest];
}

public static class ProvidentFundAuditEntityTypes
{
    public const string Policy = "provident_fund_policy";
    public const string VestingRule = "provident_fund_vesting_rule";
    public const string Enrollment = "provident_fund_enrollment";
    public const string ContributionBatch = "provident_fund_contribution_batch";
    public const string LedgerTransaction = "provident_fund_ledger_transaction";
    public const string Withdrawal = "provident_fund_withdrawal";
    public const string Adjustment = "provident_fund_adjustment";
}

public static class ProvidentFundNotificationTypes
{
    public const string EnrollmentCreated = "provident_fund_enrollment_created";
    public const string ContributionBatchPosted = "provident_fund_contribution_batch_posted";
    public const string WithdrawalSubmitted = "provident_fund_withdrawal_submitted";
    public const string WithdrawalApproved = "provident_fund_withdrawal_approved";
    public const string WithdrawalRejected = "provident_fund_withdrawal_rejected";
    public const string WithdrawalPaid = "provident_fund_withdrawal_paid";
    public const string AdjustmentPosted = "provident_fund_adjustment_posted";
}
