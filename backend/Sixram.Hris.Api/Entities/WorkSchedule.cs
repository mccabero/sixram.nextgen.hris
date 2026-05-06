namespace Sixram.Api.Entities;

public class WorkSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ScheduleType { get; set; } = string.Empty;

    public int RequiredWorkingMinutes { get; set; }

    public int GracePeriodMinutes { get; set; }

    public int BreakDurationMinutes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<EmployeeScheduleAssignment> EmployeeScheduleAssignments { get; set; } = new List<EmployeeScheduleAssignment>();
}
