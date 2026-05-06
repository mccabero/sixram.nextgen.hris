namespace Sixram.Api.Constants;

public static class LeaveRequestStatuses
{
    public const string Draft = "Draft";
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Draft,
        Pending,
        Approved,
        Rejected,
        Cancelled
    ];
}

public static class LeaveDayTypes
{
    public const string FullDay = "full_day";
    public const string FirstHalf = "first_half";
    public const string SecondHalf = "second_half";

    public static readonly IReadOnlyList<string> All =
    [
        FullDay,
        FirstHalf,
        SecondHalf
    ];
}

public static class LeaveBalanceTransactionTypes
{
    public const string Grant = "grant";
    public const string Accrual = "accrual";
    public const string Usage = "usage";
    public const string Adjustment = "adjustment";
    public const string Cancellation = "cancellation";
    public const string CarryForward = "carry_forward";

    public static readonly IReadOnlyList<string> All =
    [
        Grant,
        Accrual,
        Usage,
        Adjustment,
        Cancellation,
        CarryForward
    ];
}
