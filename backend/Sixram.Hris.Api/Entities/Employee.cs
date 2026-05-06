namespace Sixram.Api.Entities;

public class Employee
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string EmployeeCode { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string MiddleName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Suffix { get; set; } = string.Empty;

    public string Gender { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public string CivilStatus { get; set; } = string.Empty;

    public string Nationality { get; set; } = string.Empty;

    public string MobileNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string CityProvince { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string EmergencyContactName { get; set; } = string.Empty;

    public string EmergencyContactRelationship { get; set; } = string.Empty;

    public string EmergencyContactPhone { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public Guid? PositionId { get; set; }

    public Position? Position { get; set; }

    public Guid? BranchId { get; set; }

    public Branch? Branch { get; set; }

    public Guid? EmploymentTypeId { get; set; }

    public EmploymentType? EmploymentType { get; set; }

    public Guid? EmploymentStatusId { get; set; }

    public EmploymentStatus? EmploymentStatus { get; set; }

    public Guid? ManagerId { get; set; }

    public Employee? Manager { get; set; }

    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    public string WorkSchedule { get; set; } = string.Empty;

    public DateOnly? DateHired { get; set; }

    public DateOnly? DateRegularized { get; set; }

    public DateOnly? DateSeparated { get; set; }

    public string SssNumber { get; set; } = string.Empty;

    public string PhilHealthNumber { get; set; } = string.Empty;

    public string PagIbigNumber { get; set; } = string.Empty;

    public string TinNumber { get; set; } = string.Empty;

    public string OtherGovernmentId { get; set; } = string.Empty;

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public ICollection<EmployeeDocument> Documents { get; set; } = new List<EmployeeDocument>();

    public ICollection<EmployeeScheduleAssignment> ScheduleAssignments { get; set; } = new List<EmployeeScheduleAssignment>();

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    public ICollection<AttendanceAdjustmentRequest> AttendanceAdjustmentRequests { get; set; } = new List<AttendanceAdjustmentRequest>();

    public ICollection<EmployeeLeaveBalance> LeaveBalances { get; set; } = new List<EmployeeLeaveBalance>();

    public ICollection<LeaveBalanceTransaction> LeaveBalanceTransactions { get; set; } = new List<LeaveBalanceTransaction>();

    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    public ICollection<EmployeeProfileChangeRequest> ProfileChangeRequests { get; set; } = new List<EmployeeProfileChangeRequest>();

    public ICollection<CompensationProfile> CompensationProfiles { get; set; } = new List<CompensationProfile>();

    public ICollection<EmployeeRecurringEarning> RecurringEarnings { get; set; } = new List<EmployeeRecurringEarning>();

    public ICollection<EmployeeRecurringDeduction> RecurringDeductions { get; set; } = new List<EmployeeRecurringDeduction>();

    public ICollection<PayrollAdjustment> PayrollAdjustments { get; set; } = new List<PayrollAdjustment>();

    public ICollection<PayrollRunItem> PayrollRunItems { get; set; } = new List<PayrollRunItem>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
