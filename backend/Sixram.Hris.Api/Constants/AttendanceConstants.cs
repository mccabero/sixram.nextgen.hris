namespace Sixram.Api.Constants;

public static class AttendanceScheduleTypes
{
    public const string Fixed = "fixed";
    public const string Flexible = "flexible";
    public const string Shifting = "shifting";

    public static readonly IReadOnlyList<string> All = [Fixed, Flexible, Shifting];
}

public static class AttendanceStatuses
{
    public const string Present = "Present";
    public const string Late = "Late";
    public const string Undertime = "Undertime";
    public const string HalfDay = "Half Day";
    public const string Absent = "Absent";
    public const string RestDay = "Rest Day";
    public const string Holiday = "Holiday";
    public const string OnLeave = "On Leave";
    public const string Incomplete = "Incomplete";
    public const string NoSchedule = "No Schedule";

    public static readonly IReadOnlyList<string> All =
    [
        Present,
        Late,
        Undertime,
        HalfDay,
        Absent,
        RestDay,
        Holiday,
        OnLeave,
        Incomplete,
        NoSchedule
    ];
}

public static class AttendanceSources
{
    public const string Manual = "manual";
    public const string WebClock = "web_clock";
    public const string Import = "import";
    public const string System = "system";
    public const string Leave = "leave";

    public static readonly IReadOnlyList<string> All = [Manual, WebClock, Import, System, Leave];
}
