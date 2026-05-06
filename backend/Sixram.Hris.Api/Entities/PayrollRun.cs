namespace Sixram.Api.Entities;

public class PayrollRun
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PayPeriodId { get; set; }

    public PayPeriod? PayPeriod { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? GeneratedByUserId { get; set; }

    public ApplicationUser? GeneratedByUser { get; set; }

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public string? ApprovedByUserId { get; set; }

    public ApplicationUser? ApprovedByUser { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public DateTime? PaidAtUtc { get; set; }

    public string Remarks { get; set; } = string.Empty;

    public ICollection<PayrollRunItem> Items { get; set; } = new List<PayrollRunItem>();

    public ICollection<PayrollAdjustment> PayrollAdjustments { get; set; } = new List<PayrollAdjustment>();

    public ICollection<PayrollAuditLog> AuditLogs { get; set; } = new List<PayrollAuditLog>();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}

