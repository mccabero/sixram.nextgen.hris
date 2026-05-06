using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Reporting;

namespace Sixram.Api.Services;

public interface IAnalyticsService
{
    Task<AnalyticsDashboardDto> GetDashboardAsync(string? actorUserId, CancellationToken cancellationToken = default);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserAccessService _userAccessService;
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceCalculationService _attendanceCalculationService;
    private readonly IComplianceService _complianceService;

    public AnalyticsService(
        ApplicationDbContext dbContext,
        IUserAccessService userAccessService,
        IAttendanceService attendanceService,
        IAttendanceCalculationService attendanceCalculationService,
        IComplianceService complianceService)
    {
        _dbContext = dbContext;
        _userAccessService = userAccessService;
        _attendanceService = attendanceService;
        _attendanceCalculationService = attendanceCalculationService;
        _complianceService = complianceService;
    }

    public async Task<AnalyticsDashboardDto> GetDashboardAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanAccessReports(actor);

        var today = _attendanceCalculationService.GetBusinessToday();
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var scopedEmployees = await BuildScopedEmployeeQuery(actor)
            .Include(record => record.Department)
            .Include(record => record.Branch)
            .ToListAsync(cancellationToken);

        var scopedEmployeeIds = scopedEmployees.Select(record => record.Id).ToList();
        var attendanceToday = await _attendanceService.GetAttendanceRecordsAsync(new Sixram.Api.DTOs.Attendance.AttendanceRecordListQueryDto
        {
            EmployeeIds = actor.IsManager && !actor.IsAdministrator && !actor.IsHumanResources ? actor.ManagedEmployeeIds : [],
            DateFrom = today,
            DateTo = today,
            PageNumber = 1,
            PageSize = 5000,
            SortBy = "date",
            Descending = true
        }, cancellationToken);

        var complianceSummary = await _complianceService.GetSummaryAsync(actorUserId, cancellationToken);
        var pendingLeaveApprovals = await _dbContext.LeaveRequests.CountAsync(
            record =>
                scopedEmployeeIds.Contains(record.EmployeeId) &&
                record.Status == LeaveRequestStatuses.Pending,
            cancellationToken);
        var pendingAttendanceApprovals = await _dbContext.AttendanceAdjustmentRequests.CountAsync(
            record =>
                scopedEmployeeIds.Contains(record.EmployeeId) &&
                record.Status == RequestStatuses.Pending,
            cancellationToken);
        var pendingProfileChanges = await _dbContext.EmployeeProfileChangeRequests.CountAsync(
            record =>
                scopedEmployeeIds.Contains(record.EmployeeId) &&
                record.Status == RequestStatuses.Pending,
            cancellationToken);

        var metrics = new List<ReportMetricDto>
        {
            Metric("activeEmployees", "Active employees", scopedEmployees.Count(record => record.IsActive), "success"),
            Metric("newHires", "New hires this month", scopedEmployees.Count(record => record.DateHired >= monthStart && record.DateHired <= today)),
            Metric("separations", "Separations this month", scopedEmployees.Count(record => record.DateSeparated >= monthStart && record.DateSeparated <= today), scopedEmployees.Any(record => record.DateSeparated >= monthStart && record.DateSeparated <= today) ? "warning" : "default"),
            Metric("present", "Present today", attendanceToday.Items.Count(item => item.Status == AttendanceStatuses.Present), "success"),
            Metric("late", "Late today", attendanceToday.Items.Count(item => item.Status == AttendanceStatuses.Late), attendanceToday.Items.Any(item => item.Status == AttendanceStatuses.Late) ? "warning" : "default"),
            Metric("absent", "Absent today", attendanceToday.Items.Count(item => item.Status == AttendanceStatuses.Absent), attendanceToday.Items.Any(item => item.Status == AttendanceStatuses.Absent) ? "danger" : "default"),
            Metric("onLeave", "On leave today", attendanceToday.Items.Count(item => item.Status == AttendanceStatuses.OnLeave)),
            Metric("pendingApprovals", "Pending approvals", pendingLeaveApprovals + pendingAttendanceApprovals + pendingProfileChanges, pendingLeaveApprovals + pendingAttendanceApprovals + pendingProfileChanges > 0 ? "warning" : "default"),
            Metric("missingDocuments", "Missing required docs", complianceSummary.MissingRequiredDocumentCount, complianceSummary.MissingRequiredDocumentCount > 0 ? "warning" : "default"),
            Metric("expiredDocuments", "Expired documents", complianceSummary.ExpiredDocumentCount, complianceSummary.ExpiredDocumentCount > 0 ? "danger" : "default"),
            Metric("missingCompensation", "Missing compensation", complianceSummary.MissingCompensationProfileCount, complianceSummary.MissingCompensationProfileCount > 0 ? "warning" : "default")
        };

        if (actor.IsAdministrator || actor.IsPayrollOfficer)
        {
            var pendingPayrollRuns = await _dbContext.PayrollRuns.CountAsync(record => record.Status == PayrollRunStatuses.ForReview, cancellationToken);
            metrics.Add(Metric("payrollForReview", "Payroll pending approval", pendingPayrollRuns, pendingPayrollRuns > 0 ? "warning" : "default"));
        }

        return new AnalyticsDashboardDto
        {
            Metrics = metrics,
            HeadcountByDepartment = scopedEmployees
                .GroupBy(record => record.Department?.Name ?? "Unassigned")
                .Select(group => new AnalyticsSeriesPointDto
                {
                    Label = group.Key,
                    Value = group.Count()
                })
                .OrderByDescending(item => item.Value)
                .Take(8)
                .ToList(),
            HeadcountByBranch = scopedEmployees
                .GroupBy(record => record.Branch?.Name ?? "Unassigned")
                .Select(group => new AnalyticsSeriesPointDto
                {
                    Label = group.Key,
                    Value = group.Count()
                })
                .OrderByDescending(item => item.Value)
                .Take(8)
                .ToList(),
            AttendanceTrend = await BuildAttendanceTrendAsync(actor, today, cancellationToken),
            LeaveUsageTrend = await BuildLeaveTrendAsync(actor, today, cancellationToken),
            ApprovalVolume =
            [
                new AnalyticsSeriesPointDto { Label = "Leave", Value = pendingLeaveApprovals },
                new AnalyticsSeriesPointDto { Label = "Attendance", Value = pendingAttendanceApprovals },
                new AnalyticsSeriesPointDto { Label = "Profile", Value = pendingProfileChanges }
            ],
            PayrollCostTrend = actor.IsAdministrator || actor.IsPayrollOfficer
                ? await BuildPayrollTrendAsync(today, cancellationToken)
                : []
        };
    }

    private IQueryable<Sixram.Api.Entities.Employee> BuildScopedEmployeeQuery(PortalActorContext actor)
    {
        var source = _dbContext.Employees.AsNoTracking().AsQueryable();
        if (actor.IsAdministrator || actor.IsHumanResources || actor.IsPayrollOfficer)
        {
            return source;
        }

        return source.Where(record => actor.ManagedEmployeeIds.Contains(record.Id));
    }

    private async Task<IReadOnlyList<AnalyticsSeriesPointDto>> BuildAttendanceTrendAsync(PortalActorContext actor, DateOnly today, CancellationToken cancellationToken)
    {
        var trendStart = today.AddDays(-6);
        var attendance = await _attendanceService.GetAttendanceRecordsAsync(new Sixram.Api.DTOs.Attendance.AttendanceRecordListQueryDto
        {
            EmployeeIds = actor.IsManager && !actor.IsAdministrator && !actor.IsHumanResources ? actor.ManagedEmployeeIds : [],
            DateFrom = trendStart,
            DateTo = today,
            PageNumber = 1,
            PageSize = 5000,
            SortBy = "date",
            Descending = false
        }, cancellationToken);

        return attendance.Items
            .GroupBy(item => item.AttendanceDate)
            .OrderBy(group => group.Key)
            .Select(group => new AnalyticsSeriesPointDto
            {
                Label = group.Key.ToString("MM-dd"),
                Value = group.Count(item =>
                    item.Status == AttendanceStatuses.Present ||
                    item.Status == AttendanceStatuses.Late ||
                    item.Status == AttendanceStatuses.Undertime ||
                    item.Status == AttendanceStatuses.HalfDay)
            })
            .ToList();
    }

    private async Task<IReadOnlyList<AnalyticsSeriesPointDto>> BuildLeaveTrendAsync(PortalActorContext actor, DateOnly today, CancellationToken cancellationToken)
    {
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var monthStart = currentMonthStart.AddMonths(-5);
        var employeeIds = actor.IsAdministrator || actor.IsHumanResources || actor.IsPayrollOfficer
            ? await _dbContext.Employees.AsNoTracking().Select(record => record.Id).ToListAsync(cancellationToken)
            : actor.ManagedEmployeeIds.ToList();

        var leaves = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Where(record =>
                employeeIds.Contains(record.EmployeeId) &&
                record.Status == LeaveRequestStatuses.Approved &&
                record.StartDate >= monthStart &&
                record.StartDate <= today)
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, 6)
            .Select(index => monthStart.AddMonths(index))
            .Select(month => new AnalyticsSeriesPointDto
            {
                Label = month.ToString("yyyy-MM"),
                Value = leaves.Where(record => record.StartDate.Year == month.Year && record.StartDate.Month == month.Month).Sum(record => record.TotalLeaveDays)
            })
            .ToList();
    }

    private async Task<IReadOnlyList<AnalyticsSeriesPointDto>> BuildPayrollTrendAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var currentMonthStart = new DateOnly(today.Year, today.Month, 1);
        var monthStart = currentMonthStart.AddMonths(-5);
        var runItems = await _dbContext.PayrollRunItems
            .AsNoTracking()
            .Include(record => record.PayrollRun)
                .ThenInclude(run => run!.PayPeriod)
            .Where(record =>
                record.PayrollRun != null &&
                record.PayrollRun.PayPeriod != null &&
                record.PayrollRun.Status != PayrollRunStatuses.Cancelled &&
                record.PayrollRun.PayPeriod.PayrollDate >= monthStart &&
                record.PayrollRun.PayPeriod.PayrollDate <= today)
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, 6)
            .Select(index => monthStart.AddMonths(index))
            .Select(month => new AnalyticsSeriesPointDto
            {
                Label = month.ToString("yyyy-MM"),
                Value = runItems
                    .Where(record => record.PayrollRun?.PayPeriod?.PayrollDate.Year == month.Year && record.PayrollRun.PayPeriod.PayrollDate.Month == month.Month)
                    .Sum(record => record.NetPay)
            })
            .ToList();
    }

    private static ReportMetricDto Metric(string key, string label, object value, string tone = "default")
    {
        return new ReportMetricDto
        {
            Key = key,
            Label = label,
            Value = value.ToString() ?? string.Empty,
            Tone = tone
        };
    }
}
