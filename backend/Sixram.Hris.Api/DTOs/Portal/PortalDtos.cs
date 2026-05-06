using System.ComponentModel.DataAnnotations;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Documents;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.DTOs.Notifications;

namespace Sixram.Api.DTOs.Portal;

public sealed class ApprovalActionRequestDto
{
    [MaxLength(1000)]
    public string Remarks { get; init; } = string.Empty;
}

public sealed class EmployeeSelfProfileDto
{
    public Guid Id { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string MiddleName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Suffix { get; init; } = string.Empty;

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

    public string DepartmentName { get; init; } = string.Empty;

    public string PositionName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string EmploymentTypeName { get; init; } = string.Empty;

    public string EmploymentStatusName { get; init; } = string.Empty;

    public string ManagerName { get; init; } = string.Empty;

    public string WorkSchedule { get; init; } = string.Empty;

    public DateOnly? DateHired { get; init; }

    public DateOnly? DateRegularized { get; init; }

    public DateOnly? DateSeparated { get; init; }

    public string SssNumberMasked { get; init; } = string.Empty;

    public string PhilHealthNumberMasked { get; init; } = string.Empty;

    public string PagIbigNumberMasked { get; init; } = string.Empty;

    public string TinNumberMasked { get; init; } = string.Empty;

    public string OtherGovernmentIdMasked { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }
}

public sealed class PayslipSummaryDto
{
    public Guid PayrollRunItemId { get; init; }

    public string PayrollRunReferenceNumber { get; init; } = string.Empty;

    public string PayPeriodName { get; init; } = string.Empty;

    public DateOnly PeriodStartDate { get; init; }

    public DateOnly PeriodEndDate { get; init; }

    public DateOnly PayrollDate { get; init; }

    public string Currency { get; init; } = string.Empty;

    public decimal GrossPay { get; init; }

    public decimal NetPay { get; init; }

    public string Status { get; init; } = string.Empty;
}

public sealed class EmployeePortalDashboardDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public int ProfileCompletionPercent { get; init; }

    public AttendanceRecordListItemDto? TodayAttendance { get; init; }

    public AttendanceRecordListItemDto? LastAttendance { get; init; }

    public IReadOnlyList<EmployeeLeaveBalanceDto> LeaveBalances { get; init; } = Array.Empty<EmployeeLeaveBalanceDto>();

    public int PendingLeaveRequestCount { get; init; }

    public int PendingAttendanceAdjustmentRequestCount { get; init; }

    public int PendingProfileChangeRequestCount { get; init; }

    public IReadOnlyList<LeaveRequestListItemDto> UpcomingApprovedLeaves { get; init; } = Array.Empty<LeaveRequestListItemDto>();

    public PayslipSummaryDto? LatestPayslip { get; init; }

    public EmployeeDocumentComplianceSummaryDto DocumentSummary { get; init; } = new();

    public IReadOnlyList<UserNotificationDto> Notifications { get; init; } = Array.Empty<UserNotificationDto>();
}

public sealed class EmployeeRequestHistoryItemDto
{
    public string RequestType { get; init; } = string.Empty;

    public string RequestLabel { get; init; } = string.Empty;

    public string RequestId { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string CurrentApproverDisplayName { get; init; } = string.Empty;

    public DateTime SubmittedAtUtc { get; init; }

    public DateTime LastUpdatedAtUtc { get; init; }

    public bool CanCancel { get; init; }
}

public sealed class MyPayslipListQueryDto : PagedQueryDto
{
    [Range(2000, 9999)]
    public int? Year { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "payroll_date";

    public bool Descending { get; init; } = true;
}

public sealed class ManagerDashboardDto
{
    public Guid ManagerEmployeeId { get; init; }

    public int DirectReportCount { get; init; }

    public int PresentTodayCount { get; init; }

    public int LateTodayCount { get; init; }

    public int AbsentTodayCount { get; init; }

    public int OnLeaveTodayCount { get; init; }

    public int IncompleteLogCount { get; init; }

    public int EmployeesWithoutScheduleCount { get; init; }

    public int PendingApprovalCount { get; init; }

    public int UpcomingTeamLeaveCount { get; init; }

    public IReadOnlyList<UserNotificationDto> Notifications { get; init; } = Array.Empty<UserNotificationDto>();
}

public sealed class ManagerPortalOptionsDto
{
    public IReadOnlyList<EmployeeAttendanceOptionDto> Employees { get; init; } = Array.Empty<EmployeeAttendanceOptionDto>();

    public IReadOnlyList<LookupOptionDto> Departments { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> Branches { get; init; } = Array.Empty<LookupOptionDto>();
}

public sealed class ManagerTeamMemberListQueryDto : PagedQueryDto
{
    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "name";

    public bool Descending { get; init; }
}

public sealed class ManagerTeamMemberDto
{
    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string PositionName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string EmploymentStatusName { get; init; } = string.Empty;

    public string MobileNumber { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string TodayAttendanceStatus { get; init; } = string.Empty;

    public string TodayAttendanceTimeInLabel { get; init; } = string.Empty;

    public string LeaveStatus { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class ApprovalCenterSummaryDto
{
    public int PendingLeaveRequestCount { get; init; }

    public int PendingAttendanceAdjustmentRequestCount { get; init; }

    public int PendingProfileChangeRequestCount { get; init; }

    public int PendingPayrollAdjustmentCount { get; init; }

    public int TotalPendingCount { get; init; }
}

public sealed class ApprovalCenterOptionsDto
{
    public IReadOnlyList<LookupOptionDto> Departments { get; init; } = Array.Empty<LookupOptionDto>();

    public IReadOnlyList<LookupOptionDto> Branches { get; init; } = Array.Empty<LookupOptionDto>();
}

public sealed class ApprovalCenterInboxItemDto
{
    public string ApprovalType { get; init; } = string.Empty;

    public string ApprovalTypeLabel { get; init; } = string.Empty;

    public string RequestId { get; init; } = string.Empty;

    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeFullName { get; init; } = string.Empty;

    public string DepartmentName { get; init; } = string.Empty;

    public string BranchName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public string CurrentApproverDisplayName { get; init; } = string.Empty;

    public DateTime SubmittedAtUtc { get; init; }

    public DateTime LastUpdatedAtUtc { get; init; }
}

public sealed class ApprovalCenterQueryDto : PagedQueryDto, IValidatableObject
{
    [MaxLength(32)]
    public string Type { get; init; } = string.Empty;

    [MaxLength(32)]
    public string Status { get; init; } = string.Empty;

    public Guid? DepartmentId { get; init; }

    public Guid? BranchId { get; init; }

    public DateOnly? DateFrom { get; init; }

    public DateOnly? DateTo { get; init; }

    [MaxLength(32)]
    public string SortBy { get; init; } = "submitted";

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
