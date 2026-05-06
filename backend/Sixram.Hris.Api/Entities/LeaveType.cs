namespace Sixram.Api.Entities;

public class LeaveType
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsPaid { get; set; } = true;

    public bool RequiresAttachment { get; set; }

    public bool RequiresReason { get; set; }

    public bool AllowHalfDay { get; set; } = true;

    public bool AllowNegativeBalance { get; set; }

    public decimal? DefaultAnnualCredits { get; set; }

    public decimal? MaxDaysPerRequest { get; set; }

    public int? MinDaysBeforeFiling { get; set; }

    public string GenderRestriction { get; set; } = string.Empty;

    public string EmploymentTypeRestrictions { get; set; } = string.Empty;

    public bool CountsRestDays { get; set; }

    public bool CountsHolidays { get; set; }

    public bool AllowDuringProbationaryPeriod { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<EmployeeLeaveBalance> EmployeeLeaveBalances { get; set; } = new List<EmployeeLeaveBalance>();

    public ICollection<LeaveBalanceTransaction> LeaveBalanceTransactions { get; set; } = new List<LeaveBalanceTransaction>();

    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
