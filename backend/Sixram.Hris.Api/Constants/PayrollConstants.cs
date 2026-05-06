namespace Sixram.Api.Constants;

public static class PayrollPayTypes
{
    public const string Monthly = "monthly";
    public const string Daily = "daily";
    public const string Hourly = "hourly";
    public const string Project = "project";
    public const string Commission = "commission";
    public const string Other = "other";

    public static readonly IReadOnlyList<string> All =
    [
        Monthly,
        Daily,
        Hourly,
        Project,
        Commission,
        Other
    ];
}

public static class PayrollPayFrequencies
{
    public const string Weekly = "weekly";
    public const string SemiMonthly = "semi_monthly";
    public const string Monthly = "monthly";
    public const string Custom = "custom";
    public const string EveryPayroll = "every_payroll";

    public static readonly IReadOnlyList<string> Standard =
    [
        Weekly,
        SemiMonthly,
        Monthly,
        Custom
    ];

    public static readonly IReadOnlyList<string> All =
    [
        Weekly,
        SemiMonthly,
        Monthly,
        Custom,
        EveryPayroll
    ];
}

public static class PayrollRunStatuses
{
    public const string Draft = "draft";
    public const string Calculated = "calculated";
    public const string ForReview = "for_review";
    public const string Approved = "approved";
    public const string Paid = "paid";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        Calculated,
        ForReview,
        Approved,
        Paid,
        Cancelled
    ];
}

public static class PayPeriodStatuses
{
    public const string Open = "open";
    public const string Processing = "processing";
    public const string Locked = "locked";
    public const string Paid = "paid";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Open,
        Processing,
        Locked,
        Paid,
        Cancelled
    ];
}

public static class PayrollItemStatuses
{
    public const string Draft = "draft";
    public const string Reviewed = "reviewed";
    public const string Approved = "approved";
    public const string Held = "held";
    public const string Paid = "paid";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        Reviewed,
        Approved,
        Held,
        Paid
    ];
}

public static class EarningTypeCategories
{
    public const string Basic = "basic";
    public const string Allowance = "allowance";
    public const string Overtime = "overtime";
    public const string HolidayPay = "holiday_pay";
    public const string Bonus = "bonus";
    public const string Commission = "commission";
    public const string Reimbursement = "reimbursement";
    public const string Other = "other";

    public static readonly IReadOnlyList<string> All =
    [
        Basic,
        Allowance,
        Overtime,
        HolidayPay,
        Bonus,
        Commission,
        Reimbursement,
        Other
    ];
}

public static class DeductionTypeCategories
{
    public const string Government = "government";
    public const string Loan = "loan";
    public const string CashAdvance = "cash_advance";
    public const string Absence = "absence";
    public const string Late = "late";
    public const string Undertime = "undertime";
    public const string Tax = "tax";
    public const string Other = "other";

    public static readonly IReadOnlyList<string> All =
    [
        Government,
        Loan,
        CashAdvance,
        Absence,
        Late,
        Undertime,
        Tax,
        Other
    ];
}

public static class PayrollAdjustmentTypes
{
    public const string Earning = "earning";
    public const string Deduction = "deduction";

    public static readonly IReadOnlyList<string> All =
    [
        Earning,
        Deduction
    ];
}

public static class PayrollAdjustmentStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Applied = "applied";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Pending,
        Approved,
        Rejected,
        Applied,
        Cancelled
    ];
}

public static class PayrollLineSources
{
    public const string BasicSalary = "basic_salary";
    public const string Attendance = "attendance";
    public const string Overtime = "overtime";
    public const string Leave = "leave";
    public const string Manual = "manual";
    public const string System = "system";
    public const string Recurring = "recurring";
    public const string Contribution = "contribution";
    public const string Tax = "tax";
    public const string Adjustment = "adjustment";

    public static readonly IReadOnlyList<string> All =
    [
        BasicSalary,
        Attendance,
        Overtime,
        Leave,
        Manual,
        System,
        Recurring,
        Contribution,
        Tax,
        Adjustment
    ];
}

public static class PayrollSettingKeys
{
    public const string DefaultPayFrequency = "default_pay_frequency";
    public const string DefaultWorkingDaysPerMonth = "default_working_days_per_month";
    public const string DefaultWorkingHoursPerDay = "default_working_hours_per_day";
    public const string LateUndertimeDeductionPolicy = "late_undertime_deduction_policy";
    public const string AbsenceDeductionPolicy = "absence_deduction_policy";
    public const string OvertimeCalculationPolicy = "overtime_calculation_policy";
    public const string RoundingRule = "rounding_rule";
    public const string PayrollTimeZoneId = "payroll_time_zone_id";
    public const string PayslipVisibilityRule = "payslip_visibility_rule";
    public const string AllowNegativeNetPay = "allow_negative_net_pay";
    public const string DefaultCurrency = "default_currency";
}

public static class PayrollAuditEntityTypes
{
    public const string CompensationProfile = "compensation_profile";
    public const string PayPeriod = "pay_period";
    public const string PayrollRun = "payroll_run";
    public const string PayrollAdjustment = "payroll_adjustment";
    public const string PayrollRunItem = "payroll_run_item";
}

