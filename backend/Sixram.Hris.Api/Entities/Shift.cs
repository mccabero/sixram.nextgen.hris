namespace Sixram.Api.Entities;

public class Shift
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public TimeOnly? BreakStartTime { get; set; }

    public TimeOnly? BreakEndTime { get; set; }

    public int RequiredWorkingMinutes { get; set; }

    public int GracePeriodMinutes { get; set; }

    public bool IsOvernight { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<EmployeeScheduleAssignment> EmployeeScheduleAssignments { get; set; } = new List<EmployeeScheduleAssignment>();
}
