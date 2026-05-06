namespace Sixram.Api.Entities;

public class EmployeeScheduleAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public Guid WorkScheduleId { get; set; }

    public WorkSchedule? WorkSchedule { get; set; }

    public Guid? ShiftId { get; set; }

    public Shift? Shift { get; set; }

    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    public string RestDays { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }
}
