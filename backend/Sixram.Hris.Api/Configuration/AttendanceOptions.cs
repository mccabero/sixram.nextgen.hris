namespace Sixram.Api.Configuration;

public sealed class AttendanceOptions
{
    public const string SectionName = "Attendance";

    public string TimeZoneId { get; set; } = "Singapore Standard Time";

    public int DashboardTrendDays { get; set; } = 7;

    public int MaxQueryRangeDays { get; set; } = 31;
}
