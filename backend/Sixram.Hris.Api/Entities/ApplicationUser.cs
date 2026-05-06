using Microsoft.AspNetCore.Identity;

namespace Sixram.Api.Entities;

public class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<NotificationRecord> Notifications { get; set; } = new List<NotificationRecord>();

    public ICollection<EmployeeDocument> UploadedEmployeeDocuments { get; set; } = new List<EmployeeDocument>();

    public ICollection<AttendanceRecord> CreatedAttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public ICollection<AttendanceRecord> UpdatedAttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public ICollection<AttendanceAdjustmentRequest> RequestedAttendanceAdjustmentRequests { get; set; } = new List<AttendanceAdjustmentRequest>();

    public ICollection<AttendanceAdjustmentRequest> CurrentApproverAttendanceAdjustmentRequests { get; set; } = new List<AttendanceAdjustmentRequest>();

    public ICollection<AttendanceAdjustmentRequest> ReviewedAttendanceAdjustmentRequests { get; set; } = new List<AttendanceAdjustmentRequest>();

    public ICollection<LeaveRequest> CreatedLeaveRequests { get; set; } = new List<LeaveRequest>();

    public ICollection<LeaveRequest> UpdatedLeaveRequests { get; set; } = new List<LeaveRequest>();

    public ICollection<LeaveRequest> CurrentApproverLeaveRequests { get; set; } = new List<LeaveRequest>();

    public ICollection<LeaveBalanceTransaction> CreatedLeaveBalanceTransactions { get; set; } = new List<LeaveBalanceTransaction>();

    public ICollection<EmployeeProfileChangeRequest> RequestedProfileChangeRequests { get; set; } = new List<EmployeeProfileChangeRequest>();

    public ICollection<EmployeeProfileChangeRequest> ReviewedProfileChangeRequests { get; set; } = new List<EmployeeProfileChangeRequest>();

    public ICollection<CompensationProfile> CreatedCompensationProfiles { get; set; } = new List<CompensationProfile>();

    public ICollection<CompensationProfile> UpdatedCompensationProfiles { get; set; } = new List<CompensationProfile>();

    public ICollection<PayrollRun> GeneratedPayrollRuns { get; set; } = new List<PayrollRun>();

    public ICollection<PayrollRun> ApprovedPayrollRuns { get; set; } = new List<PayrollRun>();

    public ICollection<PayrollAdjustment> RequestedPayrollAdjustments { get; set; } = new List<PayrollAdjustment>();

    public ICollection<PayrollAdjustment> ApprovedPayrollAdjustments { get; set; } = new List<PayrollAdjustment>();

    public ICollection<PayrollAuditLog> PayrollAuditLogs { get; set; } = new List<PayrollAuditLog>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public ICollection<SavedReport> SavedReports { get; set; } = new List<SavedReport>();
}
