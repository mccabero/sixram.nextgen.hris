namespace Sixram.Api.Constants;

public static class ReportCategories
{
    public const string Employee = "employee";
    public const string Organization = "organization";
    public const string DocumentCompliance = "document_compliance";
    public const string Attendance = "attendance";
    public const string Leave = "leave";
    public const string Payroll = "payroll";
    public const string Approval = "approval";
    public const string Audit = "audit";
}

public static class ReportKeys
{
    public const string EmployeeMasterList = "employee_master_list";
    public const string EmployeeProfileCompleteness = "employee_profile_completeness";
    public const string DepartmentHeadcount = "department_headcount";
    public const string BranchHeadcount = "branch_headcount";
    public const string DocumentComplianceIssues = "document_compliance_issues";
    public const string AttendanceDaily = "attendance_daily";
    public const string AttendanceSummary = "attendance_summary";
    public const string LeaveUsage = "leave_usage";
    public const string LeaveBalances = "leave_balances";
    public const string PayrollRegister = "payroll_register";
    public const string ApprovalAging = "approval_aging";
    public const string AuditActivity = "audit_activity";
}

public static class ReportFilterKeys
{
    public const string Search = "search";
    public const string Employee = "employeeId";
    public const string Department = "departmentId";
    public const string Branch = "branchId";
    public const string EmploymentType = "employmentTypeId";
    public const string EmploymentStatus = "employmentStatusId";
    public const string LeaveType = "leaveTypeId";
    public const string DocumentType = "documentTypeId";
    public const string PayPeriod = "payPeriodId";
    public const string PayrollRun = "payrollRunId";
    public const string Status = "status";
    public const string Source = "source";
    public const string IssueType = "issueType";
    public const string Severity = "severity";
    public const string EntityType = "entityType";
    public const string Action = "action";
    public const string DateFrom = "dateFrom";
    public const string DateTo = "dateTo";
    public const string Year = "year";
    public const string Month = "month";
    public const string IncludeInactive = "includeInactive";
}

public static class ReportColumnAlignment
{
    public const string Left = "left";
    public const string Right = "right";
    public const string Center = "center";
}

public static class ComplianceIssueTypes
{
    public const string MissingRequiredDocument = "missing_required_document";
    public const string ExpiredDocument = "expired_document";
    public const string ExpiringSoonDocument = "expiring_soon_document";
    public const string MissingGovernmentId = "missing_government_id";
    public const string MissingEmergencyContact = "missing_emergency_contact";
    public const string MissingScheduleAssignment = "missing_schedule_assignment";
    public const string MissingCompensationProfile = "missing_compensation_profile";
    public const string IncompleteAttendance = "incomplete_attendance";
    public const string PendingProfileChange = "pending_profile_change";
    public const string PendingAttendanceAdjustment = "pending_attendance_adjustment";
    public const string PendingLeaveRequest = "pending_leave_request";

    public static readonly IReadOnlyList<string> All =
    [
        MissingRequiredDocument,
        ExpiredDocument,
        ExpiringSoonDocument,
        MissingGovernmentId,
        MissingEmergencyContact,
        MissingScheduleAssignment,
        MissingCompensationProfile,
        IncompleteAttendance,
        PendingProfileChange,
        PendingAttendanceAdjustment,
        PendingLeaveRequest
    ];
}

public static class ComplianceSeverityLevels
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";

    public static readonly IReadOnlyList<string> All = [Low, Medium, High, Critical];
}

public static class AuditEntityTypes
{
    public const string Employee = "employee";
    public const string Department = "department";
    public const string Position = "position";
    public const string Branch = "branch";
    public const string EmploymentType = "employment_type";
    public const string EmploymentStatus = "employment_status";
    public const string DocumentType = "document_type";
    public const string EmployeeDocument = "employee_document";
    public const string AttendanceRecord = "attendance_record";
    public const string AttendanceAdjustmentRequest = "attendance_adjustment_request";
    public const string LeaveRequest = "leave_request";
    public const string LeaveBalance = "leave_balance";
    public const string ProfileChangeRequest = "profile_change_request";
    public const string CompensationProfile = "compensation_profile";
    public const string RecurringEarning = "recurring_earning";
    public const string RecurringDeduction = "recurring_deduction";
    public const string PayrollRun = "payroll_run";
    public const string PayrollAdjustment = "payroll_adjustment";
    public const string User = "user";
    public const string RoleAssignment = "role_assignment";
    public const string Payslip = "payslip";
    public const string Report = "report";
    public const string DataImport = "data_import";
}
