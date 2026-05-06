namespace Sixram.Api.Entities;

public class PayrollRunItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PayrollRunId { get; set; }

    public PayrollRun? PayrollRun { get; set; }

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid? CompensationProfileId { get; set; }

    public CompensationProfile? CompensationProfile { get; set; }

    public string EmployeeCodeSnapshot { get; set; } = string.Empty;

    public string EmployeeNameSnapshot { get; set; } = string.Empty;

    public string DepartmentSnapshot { get; set; } = string.Empty;

    public string PositionSnapshot { get; set; } = string.Empty;

    public string BranchSnapshot { get; set; } = string.Empty;

    public string PayTypeSnapshot { get; set; } = string.Empty;

    public string CurrencySnapshot { get; set; } = "PHP";

    public decimal BasicSalarySnapshot { get; set; }

    public decimal? DailyRateSnapshot { get; set; }

    public decimal? HourlyRateSnapshot { get; set; }

    public decimal RegularWorkedDays { get; set; }

    public decimal RegularWorkedHours { get; set; }

    public decimal PaidLeaveDays { get; set; }

    public decimal UnpaidLeaveDays { get; set; }

    public decimal AbsentDays { get; set; }

    public int LateMinutes { get; set; }

    public int UndertimeMinutes { get; set; }

    public int OvertimeMinutes { get; set; }

    public decimal BasicPay { get; set; }

    public decimal AllowanceTotal { get; set; }

    public decimal OvertimePay { get; set; }

    public decimal HolidayPay { get; set; }

    public decimal LeavePay { get; set; }

    public decimal BonusTotal { get; set; }

    public decimal OtherEarningsTotal { get; set; }

    public decimal GrossPay { get; set; }

    public decimal GovernmentDeductionsTotal { get; set; }

    public decimal TaxDeduction { get; set; }

    public decimal AbsenceDeduction { get; set; }

    public decimal LateDeduction { get; set; }

    public decimal UndertimeDeduction { get; set; }

    public decimal LoanDeduction { get; set; }

    public decimal OtherDeductionsTotal { get; set; }

    public decimal TotalDeductions { get; set; }

    public decimal NetPay { get; set; }

    public decimal EmployerContributionTotal { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Remarks { get; set; } = string.Empty;

    public bool HasCriticalIssues { get; set; }

    public string IssueSummary { get; set; } = string.Empty;

    public ICollection<PayrollEarningLine> EarningLines { get; set; } = new List<PayrollEarningLine>();

    public ICollection<PayrollDeductionLine> DeductionLines { get; set; } = new List<PayrollDeductionLine>();

    public ICollection<PayrollAuditLog> AuditLogs { get; set; } = new List<PayrollAuditLog>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

