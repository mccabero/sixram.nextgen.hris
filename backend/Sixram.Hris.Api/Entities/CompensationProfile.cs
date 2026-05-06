namespace Sixram.Api.Entities;

public class CompensationProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public string PayType { get; set; } = string.Empty;

    public string PayFrequency { get; set; } = string.Empty;

    public decimal BasicSalary { get; set; }

    public decimal? DailyRate { get; set; }

    public decimal? HourlyRate { get; set; }

    public string Currency { get; set; } = "PHP";

    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public string Remarks { get; set; } = string.Empty;

    public string? CreatedByUserId { get; set; }

    public ApplicationUser? CreatedByUser { get; set; }

    public string? UpdatedByUserId { get; set; }

    public ApplicationUser? UpdatedByUser { get; set; }

    public ICollection<PayrollRunItem> PayrollRunItems { get; set; } = new List<PayrollRunItem>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

