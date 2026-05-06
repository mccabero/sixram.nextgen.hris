namespace Sixram.Api.Constants;

public static class RequestStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Cancelled = "cancelled";

    public static readonly IReadOnlyList<string> All =
    [
        Pending,
        Approved,
        Rejected,
        Cancelled
    ];
}

public static class ProfileChangeRequestTypes
{
    public const string PersonalProfileUpdate = "personal_profile_update";

    public static readonly IReadOnlyList<string> All =
    [
        PersonalProfileUpdate
    ];
}

public static class AttendanceAdjustmentRequestTypes
{
    public const string MissingTimeIn = "missing_time_in";
    public const string MissingTimeOut = "missing_time_out";
    public const string IncorrectTimeInOut = "incorrect_time_in_out";
    public const string RemarksUpdate = "remarks_update";

    public static readonly IReadOnlyList<string> All =
    [
        MissingTimeIn,
        MissingTimeOut,
        IncorrectTimeInOut,
        RemarksUpdate
    ];
}

public static class NotificationTypes
{
    public const string LeaveSubmitted = "leave_submitted";
    public const string LeaveApproved = "leave_approved";
    public const string LeaveRejected = "leave_rejected";
    public const string LeaveCancelled = "leave_cancelled";
    public const string AttendanceAdjustmentSubmitted = "attendance_adjustment_submitted";
    public const string AttendanceAdjustmentApproved = "attendance_adjustment_approved";
    public const string AttendanceAdjustmentRejected = "attendance_adjustment_rejected";
    public const string AttendanceAdjustmentCancelled = "attendance_adjustment_cancelled";
    public const string ProfileChangeSubmitted = "profile_change_submitted";
    public const string ProfileChangeApproved = "profile_change_approved";
    public const string ProfileChangeRejected = "profile_change_rejected";
    public const string ProfileChangeCancelled = "profile_change_cancelled";
    public const string PayslipAvailable = "payslip_available";
    public const string ApprovalPending = "approval_pending";

    public static readonly IReadOnlyList<string> All =
    [
        LeaveSubmitted,
        LeaveApproved,
        LeaveRejected,
        LeaveCancelled,
        AttendanceAdjustmentSubmitted,
        AttendanceAdjustmentApproved,
        AttendanceAdjustmentRejected,
        AttendanceAdjustmentCancelled,
        ProfileChangeSubmitted,
        ProfileChangeApproved,
        ProfileChangeRejected,
        ProfileChangeCancelled,
        PayslipAvailable,
        ApprovalPending
    ];
}

public static class ApprovableTypes
{
    public const string LeaveRequest = "leave_request";
    public const string AttendanceAdjustmentRequest = "attendance_adjustment_request";
    public const string ProfileChangeRequest = "profile_change_request";
    public const string PayrollAdjustment = "payroll_adjustment";
}
