using Microsoft.Extensions.Options;
using Sixram.Api.Configuration;
using Sixram.Api.Constants;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public sealed record ResolvedAttendanceSchedule(
    EmployeeScheduleAssignment? Assignment,
    WorkSchedule? WorkSchedule,
    Shift? Shift,
    bool HasScheduleAssignment,
    bool IsRestDay,
    string WorkScheduleName,
    string ShiftName,
    int RequiredWorkingMinutes,
    int GracePeriodMinutes,
    int BreakDurationMinutes,
    DateTime? ScheduledStartTime,
    DateTime? ScheduledEndTime);

public sealed record AttendanceCalculationResult(
    DateTime? ScheduledStartTime,
    DateTime? ScheduledEndTime,
    int TotalWorkedMinutes,
    int LateMinutes,
    int UndertimeMinutes,
    int OvertimeMinutes,
    string Status);

public interface IAttendanceCalculationService
{
    DateOnly GetBusinessToday();

    DateTime GetBusinessNow();

    IReadOnlyList<int> ParseRestDayValues(string? restDays);

    string SerializeRestDayValues(IEnumerable<int> restDayValues);

    ResolvedAttendanceSchedule ResolveSchedule(IReadOnlyCollection<EmployeeScheduleAssignment> assignments, DateOnly attendanceDate);

    AttendanceCalculationResult CalculateAttendance(
        DateOnly attendanceDate,
        ResolvedAttendanceSchedule resolvedSchedule,
        DateTime? actualTimeIn,
        DateTime? actualTimeOut,
        DateTime? breakStartTime,
        DateTime? breakEndTime);
}

public class AttendanceCalculationService : IAttendanceCalculationService
{
    private readonly TimeZoneInfo _timeZone;

    public AttendanceCalculationService(IOptions<AttendanceOptions> options)
    {
        _timeZone = ResolveTimeZone(options.Value.TimeZoneId);
    }

    public DateOnly GetBusinessToday()
    {
        return DateOnly.FromDateTime(GetBusinessNow());
    }

    public DateTime GetBusinessNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);
    }

    public IReadOnlyList<int> ParseRestDayValues(string? restDays)
    {
        if (string.IsNullOrWhiteSpace(restDays))
        {
            return Array.Empty<int>();
        }

        return restDays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => int.TryParse(value, out var parsedValue) ? parsedValue : -1)
            .Where(value => value >= 0 && value <= 6)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
    }

    public string SerializeRestDayValues(IEnumerable<int> restDayValues)
    {
        return string.Join(
            ",",
            restDayValues
                .Where(value => value >= 0 && value <= 6)
                .Distinct()
                .OrderBy(value => value));
    }

    public ResolvedAttendanceSchedule ResolveSchedule(IReadOnlyCollection<EmployeeScheduleAssignment> assignments, DateOnly attendanceDate)
    {
        var assignment = assignments
            .Where(record =>
                record.EffectiveStartDate <= attendanceDate &&
                (record.EffectiveEndDate is null || record.EffectiveEndDate >= attendanceDate))
            .OrderByDescending(record => record.EffectiveStartDate)
            .ThenByDescending(record => record.CreatedAtUtc)
            .FirstOrDefault();

        if (assignment is null || assignment.WorkSchedule is null)
        {
            return new ResolvedAttendanceSchedule(
                Assignment: null,
                WorkSchedule: null,
                Shift: null,
                HasScheduleAssignment: false,
                IsRestDay: false,
                WorkScheduleName: string.Empty,
                ShiftName: string.Empty,
                RequiredWorkingMinutes: 0,
                GracePeriodMinutes: 0,
                BreakDurationMinutes: 0,
                ScheduledStartTime: null,
                ScheduledEndTime: null);
        }

        var workSchedule = assignment.WorkSchedule;
        var shift = assignment.Shift;
        var restDayValues = ParseRestDayValues(assignment.RestDays);
        var dayOfWeek = (int)attendanceDate.DayOfWeek;
        var isRestDay = restDayValues.Contains(dayOfWeek);

        var requiredWorkingMinutes = shift?.RequiredWorkingMinutes > 0
            ? shift.RequiredWorkingMinutes
            : workSchedule.RequiredWorkingMinutes;

        var gracePeriodMinutes = shift is not null
            ? shift.GracePeriodMinutes
            : workSchedule.GracePeriodMinutes;

        var breakDurationMinutes = ResolveBreakDurationMinutes(workSchedule, shift);

        var scheduledStartTime = shift is null
            ? (DateTime?)null
            : CreateLocalDateTime(attendanceDate, shift.StartTime);

        DateTime? scheduledEndTime = null;
        if (shift is not null)
        {
            scheduledEndTime = CreateLocalDateTime(attendanceDate, shift.EndTime);
            if (shift.IsOvernight || shift.EndTime <= shift.StartTime)
            {
                scheduledEndTime = scheduledEndTime.Value.AddDays(1);
            }
        }

        return new ResolvedAttendanceSchedule(
            Assignment: assignment,
            WorkSchedule: workSchedule,
            Shift: shift,
            HasScheduleAssignment: true,
            IsRestDay: isRestDay,
            WorkScheduleName: workSchedule.Name,
            ShiftName: shift?.Name ?? string.Empty,
            RequiredWorkingMinutes: requiredWorkingMinutes,
            GracePeriodMinutes: gracePeriodMinutes,
            BreakDurationMinutes: breakDurationMinutes,
            ScheduledStartTime: scheduledStartTime,
            ScheduledEndTime: scheduledEndTime);
    }

    public AttendanceCalculationResult CalculateAttendance(
        DateOnly attendanceDate,
        ResolvedAttendanceSchedule resolvedSchedule,
        DateTime? actualTimeIn,
        DateTime? actualTimeOut,
        DateTime? breakStartTime,
        DateTime? breakEndTime)
    {
        ValidateTimePair(actualTimeIn, actualTimeOut, nameof(actualTimeOut), "Actual time out");
        ValidateTimePair(breakStartTime, breakEndTime, nameof(breakEndTime), "Break end time");

        if (breakStartTime is not null && actualTimeIn is null)
        {
            throw BuildValidationException("Break times require an actual time in.", nameof(breakStartTime));
        }

        if (breakStartTime is not null && actualTimeOut is not null)
        {
            if (breakStartTime < actualTimeIn || breakEndTime > actualTimeOut)
            {
                throw BuildValidationException("Break times must fall within the actual working interval.", nameof(breakStartTime));
            }
        }

        var scheduledStartTime = resolvedSchedule.ScheduledStartTime;
        var scheduledEndTime = resolvedSchedule.ScheduledEndTime;

        var totalWorkedMinutes = 0;
        var lateMinutes = 0;
        var undertimeMinutes = 0;
        var overtimeMinutes = 0;

        if (actualTimeIn is not null && actualTimeOut is not null)
        {
            var breakDurationMinutes = breakStartTime is not null && breakEndTime is not null
                ? (int)Math.Floor((breakEndTime.Value - breakStartTime.Value).TotalMinutes)
                : resolvedSchedule.BreakDurationMinutes;

            var totalSpanMinutes = (int)Math.Floor((actualTimeOut.Value - actualTimeIn.Value).TotalMinutes);
            if (breakDurationMinutes > totalSpanMinutes)
            {
                throw BuildValidationException("Break duration cannot be longer than the total worked interval.", nameof(breakStartTime));
            }

            totalWorkedMinutes = Math.Max(0, totalSpanMinutes - breakDurationMinutes);

            if (scheduledStartTime is not null)
            {
                var graceCutoff = scheduledStartTime.Value.AddMinutes(resolvedSchedule.GracePeriodMinutes);
                lateMinutes = Math.Max(0, (int)Math.Floor((actualTimeIn.Value - graceCutoff).TotalMinutes));
            }

            if (scheduledEndTime is not null)
            {
                undertimeMinutes = Math.Max(0, (int)Math.Floor((scheduledEndTime.Value - actualTimeOut.Value).TotalMinutes));
                overtimeMinutes = Math.Max(0, (int)Math.Floor((actualTimeOut.Value - scheduledEndTime.Value).TotalMinutes));
            }
            else if (resolvedSchedule.RequiredWorkingMinutes > 0)
            {
                overtimeMinutes = Math.Max(0, totalWorkedMinutes - resolvedSchedule.RequiredWorkingMinutes);
            }
        }

        var status = ResolveStatus(
            resolvedSchedule,
            actualTimeIn,
            actualTimeOut,
            totalWorkedMinutes,
            lateMinutes,
            undertimeMinutes);

        return new AttendanceCalculationResult(
            ScheduledStartTime: scheduledStartTime,
            ScheduledEndTime: scheduledEndTime,
            TotalWorkedMinutes: totalWorkedMinutes,
            LateMinutes: lateMinutes,
            UndertimeMinutes: undertimeMinutes,
            OvertimeMinutes: overtimeMinutes,
            Status: status);
    }

    private static string ResolveStatus(
        ResolvedAttendanceSchedule resolvedSchedule,
        DateTime? actualTimeIn,
        DateTime? actualTimeOut,
        int totalWorkedMinutes,
        int lateMinutes,
        int undertimeMinutes)
    {
        if (!resolvedSchedule.HasScheduleAssignment)
        {
            return AttendanceStatuses.NoSchedule;
        }

        if (actualTimeIn is null && actualTimeOut is null)
        {
            return resolvedSchedule.IsRestDay
                ? AttendanceStatuses.RestDay
                : AttendanceStatuses.Absent;
        }

        if (actualTimeIn is null || actualTimeOut is null)
        {
            return resolvedSchedule.IsRestDay
                ? AttendanceStatuses.RestDay
                : AttendanceStatuses.Incomplete;
        }

        if (resolvedSchedule.IsRestDay)
        {
            return AttendanceStatuses.RestDay;
        }

        if (resolvedSchedule.RequiredWorkingMinutes > 0 && totalWorkedMinutes > 0 && totalWorkedMinutes < (resolvedSchedule.RequiredWorkingMinutes / 2d))
        {
            return AttendanceStatuses.HalfDay;
        }

        if (lateMinutes > 0)
        {
            return AttendanceStatuses.Late;
        }

        if (undertimeMinutes > 0)
        {
            return AttendanceStatuses.Undertime;
        }

        return AttendanceStatuses.Present;
    }

    private static int ResolveBreakDurationMinutes(WorkSchedule workSchedule, Shift? shift)
    {
        if (shift?.BreakStartTime is not null && shift.BreakEndTime is not null)
        {
            return Math.Max(0, (int)Math.Floor((shift.BreakEndTime.Value - shift.BreakStartTime.Value).TotalMinutes));
        }

        return workSchedule.BreakDurationMinutes;
    }

    private static DateTime CreateLocalDateTime(DateOnly date, TimeOnly time)
    {
        return date.ToDateTime(time, DateTimeKind.Unspecified);
    }

    private static void ValidateTimePair(DateTime? start, DateTime? end, string fieldName, string fieldLabel)
    {
        if (start is not null && end is not null && end < start)
        {
            throw BuildValidationException($"{fieldLabel} cannot be earlier than the paired start time.", fieldName);
        }
    }

    private static BadRequestException BuildValidationException(string message, string fieldName)
    {
        return new BadRequestException(message, new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }

    private static TimeZoneInfo ResolveTimeZone(string configuredTimeZoneId)
    {
        foreach (var candidate in new[] { configuredTimeZoneId, "Singapore Standard Time", "Asia/Manila" })
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
