using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Payroll;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IPayrollService
{
    Task<PayrollDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    Task<PagedResultDto<PayPeriodRecordDto>> GetPayPeriodsAsync(PayPeriodListQueryDto query, CancellationToken cancellationToken = default);

    Task<PayPeriodRecordDto> CreatePayPeriodAsync(SavePayPeriodRequestDto request, CancellationToken cancellationToken = default);

    Task<PayPeriodRecordDto> UpdatePayPeriodAsync(Guid payPeriodId, SavePayPeriodRequestDto request, CancellationToken cancellationToken = default);

    Task DeletePayPeriodAsync(Guid payPeriodId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<PayrollRunSummaryDto>> GetPayrollRunsAsync(PayrollRunListQueryDto query, CancellationToken cancellationToken = default);

    Task<PayrollRunDetailDto> GetPayrollRunByIdAsync(Guid payrollRunId, CancellationToken cancellationToken = default);

    Task<PayrollRunDetailDto> GeneratePayrollRunAsync(GeneratePayrollRunRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PayrollRunDetailDto> RecalculatePayrollRunAsync(Guid payrollRunId, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PayrollRunDetailDto> SubmitPayrollRunForReviewAsync(Guid payrollRunId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PayrollRunDetailDto> ApprovePayrollRunAsync(Guid payrollRunId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PayrollRunDetailDto> MarkPayrollRunAsPaidAsync(Guid payrollRunId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PayrollRunDetailDto> CancelPayrollRunAsync(Guid payrollRunId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<PayrollAdjustmentRecordDto>> GetPayrollAdjustmentsAsync(PayrollAdjustmentListQueryDto query, CancellationToken cancellationToken = default);

    Task<PayrollAdjustmentRecordDto> CreatePayrollAdjustmentAsync(SavePayrollAdjustmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PayrollAdjustmentRecordDto> UpdatePayrollAdjustmentAsync(Guid payrollAdjustmentId, SavePayrollAdjustmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task DeletePayrollAdjustmentAsync(Guid payrollAdjustmentId, CancellationToken cancellationToken = default);

    Task<PayrollAdjustmentRecordDto> ApprovePayrollAdjustmentAsync(Guid payrollAdjustmentId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PayrollAdjustmentRecordDto> RejectPayrollAdjustmentAsync(Guid payrollAdjustmentId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PayrollAdjustmentRecordDto> CancelPayrollAdjustmentAsync(Guid payrollAdjustmentId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PayrollReportsDto> GetReportsAsync(PayrollReportQueryDto query, CancellationToken cancellationToken = default);

    Task<PayslipDto> GetPayslipAsync(Guid payrollRunItemId, CancellationToken cancellationToken = default);
}

public class PayrollService : IPayrollService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IAttendanceCalculationService _attendanceCalculationService;
    private readonly IPayrollSetupService _payrollSetupService;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<PayrollService> _logger;

    public PayrollService(
        ApplicationDbContext dbContext,
        IAttendanceCalculationService attendanceCalculationService,
        IPayrollSetupService payrollSetupService,
        IAuditLogService auditLogService,
        INotificationService notificationService,
        ILogger<PayrollService> logger)
    {
        _dbContext = dbContext;
        _attendanceCalculationService = attendanceCalculationService;
        _payrollSetupService = payrollSetupService;
        _auditLogService = auditLogService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<PayrollDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var businessDate = _attendanceCalculationService.GetBusinessToday();
        var currentOpenPayPeriod = await _dbContext.PayPeriods
            .AsNoTracking()
            .Where(record =>
                record.Status == PayPeriodStatuses.Open ||
                record.Status == PayPeriodStatuses.Processing ||
                record.Status == PayPeriodStatuses.Locked)
            .OrderBy(record => record.PeriodStartDate)
            .ThenBy(record => record.PayrollDate)
            .Select(record => new PayPeriodOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                PayFrequency = record.PayFrequency,
                PeriodStartDate = record.PeriodStartDate,
                PeriodEndDate = record.PeriodEndDate,
                PayrollDate = record.PayrollDate,
                Status = record.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        var nonCancelledRuns = _dbContext.PayrollRuns.AsNoTracking().Where(record => record.Status != PayrollRunStatuses.Cancelled);
        var recentRuns = await nonCancelledRuns
            .OrderByDescending(record => record.GeneratedAtUtc)
            .Take(5)
            .Select(record => new PayrollRunSummaryDto
            {
                Id = record.Id,
                PayPeriodId = record.PayPeriodId,
                PayPeriodCode = record.PayPeriod != null ? record.PayPeriod.Code : string.Empty,
                PayPeriodName = record.PayPeriod != null ? record.PayPeriod.Name : string.Empty,
                ReferenceNumber = record.ReferenceNumber,
                Name = record.Name,
                Status = record.Status,
                EmployeeCount = record.Items.Count,
                HoldCount = record.Items.Count(item => item.Status == PayrollItemStatuses.Held),
                CriticalIssueCount = record.Items.Count(item => item.HasCriticalIssues),
                TotalGrossPay = record.Items.Sum(item => item.GrossPay),
                TotalDeductions = record.Items.Sum(item => item.TotalDeductions),
                TotalNetPay = record.Items.Sum(item => item.NetPay),
                GeneratedByDisplayName = record.GeneratedByUser != null
                    ? (!string.IsNullOrWhiteSpace(record.GeneratedByUser.DisplayName) ? record.GeneratedByUser.DisplayName : record.GeneratedByUser.Email ?? string.Empty)
                    : string.Empty,
                GeneratedAtUtc = record.GeneratedAtUtc,
                ApprovedByDisplayName = record.ApprovedByUser != null
                    ? (!string.IsNullOrWhiteSpace(record.ApprovedByUser.DisplayName) ? record.ApprovedByUser.DisplayName : record.ApprovedByUser.Email ?? string.Empty)
                    : string.Empty,
                ApprovedAtUtc = record.ApprovedAtUtc,
                PaidAtUtc = record.PaidAtUtc,
                Remarks = record.Remarks,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var employeesMissingCompensationProfileCount = await _dbContext.Employees
            .AsNoTracking()
            .CountAsync(
                employee =>
                    employee.IsActive &&
                    !_dbContext.CompensationProfiles.Any(
                        compensation =>
                            compensation.EmployeeId == employee.Id &&
                            compensation.IsActive &&
                            compensation.EffectiveStartDate <= businessDate &&
                            (!compensation.EffectiveEndDate.HasValue || compensation.EffectiveEndDate.Value >= businessDate)),
                cancellationToken);

        var holdItems = await _dbContext.PayrollRunItems
            .AsNoTracking()
            .CountAsync(record => record.Status == PayrollItemStatuses.Held || record.HasCriticalIssues, cancellationToken);

        return new PayrollDashboardSummaryDto
        {
            BusinessDate = businessDate,
            CurrentOpenPayPeriod = currentOpenPayPeriod,
            DraftRunCount = await _dbContext.PayrollRuns.CountAsync(record => record.Status == PayrollRunStatuses.Draft || record.Status == PayrollRunStatuses.Calculated, cancellationToken),
            ForReviewRunCount = await _dbContext.PayrollRuns.CountAsync(record => record.Status == PayrollRunStatuses.ForReview, cancellationToken),
            ApprovedRunCount = await _dbContext.PayrollRuns.CountAsync(record => record.Status == PayrollRunStatuses.Approved, cancellationToken),
            EmployeesMissingCompensationProfileCount = employeesMissingCompensationProfileCount,
            EmployeesWithAttendanceIssuesCount = holdItems,
            PendingPayrollAdjustmentCount = await _dbContext.PayrollAdjustments.CountAsync(record => record.Status == PayrollAdjustmentStatuses.Pending, cancellationToken),
            PayrollItemsOnHoldCount = holdItems,
            TotalGrossPay = await _dbContext.PayrollRunItems.Where(record => record.PayrollRun != null && record.PayrollRun.Status != PayrollRunStatuses.Cancelled).SumAsync(record => record.GrossPay, cancellationToken),
            TotalDeductions = await _dbContext.PayrollRunItems.Where(record => record.PayrollRun != null && record.PayrollRun.Status != PayrollRunStatuses.Cancelled).SumAsync(record => record.TotalDeductions, cancellationToken),
            TotalNetPay = await _dbContext.PayrollRunItems.Where(record => record.PayrollRun != null && record.PayrollRun.Status != PayrollRunStatuses.Cancelled).SumAsync(record => record.NetPay, cancellationToken),
            RecentRuns = recentRuns
        };
    }

    public async Task<PagedResultDto<PayPeriodRecordDto>> GetPayPeriodsAsync(PayPeriodListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.PayPeriods
            .AsNoTracking()
            .Include(record => record.PayPeriodTemplate)
            .Include(record => record.PayrollRuns)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record => record.Code.Contains(search) || record.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(query.PayFrequency))
        {
            source = source.Where(record => record.PayFrequency == query.PayFrequency.Trim().ToLowerInvariant());
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(record => record.PeriodEndDate >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(record => record.PeriodStartDate <= query.DateTo.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("payroll_date", true) => source.OrderByDescending(record => record.PayrollDate).ThenByDescending(record => record.PeriodStartDate),
            ("payroll_date", false) => source.OrderBy(record => record.PayrollDate).ThenBy(record => record.PeriodStartDate),
            ("status", true) => source.OrderByDescending(record => record.Status).ThenByDescending(record => record.PeriodStartDate),
            ("status", false) => source.OrderBy(record => record.Status).ThenByDescending(record => record.PeriodStartDate),
            (_, true) => source.OrderByDescending(record => record.PeriodStartDate).ThenByDescending(record => record.PayrollDate),
            _ => source.OrderBy(record => record.PeriodStartDate).ThenBy(record => record.PayrollDate)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ToPage(records.Select(MapPayPeriod).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<PayPeriodRecordDto> CreatePayPeriodAsync(SavePayPeriodRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatePayFrequency(request.PayFrequency);
        var computedDates = await ResolvePayPeriodDatesAsync(request, cancellationToken);
        await EnsureUniquePayPeriodCodeAsync(request.Code, null, cancellationToken);

        var record = new PayPeriod
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            PayFrequency = request.PayFrequency.Trim().ToLowerInvariant(),
            PeriodStartDate = computedDates.PeriodStartDate,
            PeriodEndDate = computedDates.PeriodEndDate,
            PayrollDate = computedDates.PayrollDate,
            CutoffStartDate = computedDates.CutoffStartDate,
            CutoffEndDate = computedDates.CutoffEndDate,
            Status = NormalizePayPeriodStatus(request.Status),
            Remarks = request.Remarks.Trim(),
            PayPeriodTemplateId = request.PayPeriodTemplateId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.PayPeriods.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetPayPeriodByIdAsync(record.Id, cancellationToken);
    }

    public async Task<PayPeriodRecordDto> UpdatePayPeriodAsync(Guid payPeriodId, SavePayPeriodRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatePayFrequency(request.PayFrequency);
        var record = await _dbContext.PayPeriods
            .Include(item => item.PayrollRuns)
            .SingleOrDefaultAsync(item => item.Id == payPeriodId, cancellationToken)
            ?? throw new NotFoundException($"Pay period '{payPeriodId}' was not found.");

        if (record.Status == PayPeriodStatuses.Locked || record.Status == PayPeriodStatuses.Paid)
        {
            throw new ConflictException("Locked or paid pay periods cannot be edited.");
        }

        if (record.PayrollRuns.Any(run => run.Status == PayrollRunStatuses.Approved || run.Status == PayrollRunStatuses.Paid))
        {
            throw new ConflictException("This pay period already has approved payroll activity and can no longer be edited.");
        }

        var computedDates = await ResolvePayPeriodDatesAsync(request, cancellationToken);
        await EnsureUniquePayPeriodCodeAsync(request.Code, payPeriodId, cancellationToken);

        record.Code = request.Code.Trim().ToUpperInvariant();
        record.Name = request.Name.Trim();
        record.PayFrequency = request.PayFrequency.Trim().ToLowerInvariant();
        record.PeriodStartDate = computedDates.PeriodStartDate;
        record.PeriodEndDate = computedDates.PeriodEndDate;
        record.PayrollDate = computedDates.PayrollDate;
        record.CutoffStartDate = computedDates.CutoffStartDate;
        record.CutoffEndDate = computedDates.CutoffEndDate;
        record.Status = NormalizePayPeriodStatus(request.Status);
        record.Remarks = request.Remarks.Trim();
        record.PayPeriodTemplateId = request.PayPeriodTemplateId;
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetPayPeriodByIdAsync(record.Id, cancellationToken);
    }

    public async Task DeletePayPeriodAsync(Guid payPeriodId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.PayPeriods
            .Include(item => item.PayrollRuns)
            .SingleOrDefaultAsync(item => item.Id == payPeriodId, cancellationToken)
            ?? throw new NotFoundException($"Pay period '{payPeriodId}' was not found.");

        if (record.PayrollRuns.Count > 0)
        {
            throw new ConflictException("This pay period already has payroll runs. Cancel them or keep the pay period for history.");
        }

        _dbContext.PayPeriods.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<PayrollRunSummaryDto>> GetPayrollRunsAsync(PayrollRunListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.PayrollRuns
            .AsNoTracking()
            .Include(record => record.PayPeriod)
            .Include(record => record.GeneratedByUser)
            .Include(record => record.ApprovedByUser)
            .Include(record => record.Items)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.ReferenceNumber.Contains(search) ||
                record.Name.Contains(search) ||
                (record.PayPeriod != null && record.PayPeriod.Code.Contains(search)) ||
                (record.PayPeriod != null && record.PayPeriod.Name.Contains(search)));
        }

        if (query.PayPeriodId is not null)
        {
            source = source.Where(record => record.PayPeriodId == query.PayPeriodId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim().ToLowerInvariant());
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(record => record.PayPeriod != null && record.PayPeriod.PeriodEndDate >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(record => record.PayPeriod != null && record.PayPeriod.PeriodStartDate <= query.DateTo.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Items.Any(item => item.Employee != null && item.Employee.DepartmentId == query.DepartmentId.Value));
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.Items.Any(item => item.Employee != null && item.Employee.BranchId == query.BranchId.Value));
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("status", true) => source.OrderByDescending(record => record.Status).ThenByDescending(record => record.GeneratedAtUtc),
            ("status", false) => source.OrderBy(record => record.Status).ThenByDescending(record => record.GeneratedAtUtc),
            ("payroll_date", true) => source.OrderByDescending(record => record.PayPeriod != null ? record.PayPeriod.PayrollDate : DateOnly.MinValue).ThenByDescending(record => record.GeneratedAtUtc),
            ("payroll_date", false) => source.OrderBy(record => record.PayPeriod != null ? record.PayPeriod.PayrollDate : DateOnly.MinValue).ThenByDescending(record => record.GeneratedAtUtc),
            (_, true) => source.OrderByDescending(record => record.GeneratedAtUtc),
            _ => source.OrderBy(record => record.GeneratedAtUtc)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ToPage(records.Select(MapPayrollRunSummary).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<PayrollRunDetailDto> GetPayrollRunByIdAsync(Guid payrollRunId, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.PayrollRuns
            .AsNoTracking()
            .Include(record => record.PayPeriod)
                .ThenInclude(payPeriod => payPeriod!.PayPeriodTemplate)
            .Include(record => record.GeneratedByUser)
            .Include(record => record.ApprovedByUser)
            .Include(record => record.Items)
                .ThenInclude(item => item.EarningLines)
            .Include(record => record.Items)
                .ThenInclude(item => item.DeductionLines)
            .Include(record => record.AuditLogs)
                .ThenInclude(log => log.ActorUser)
            .SingleOrDefaultAsync(record => record.Id == payrollRunId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run '{payrollRunId}' was not found.");

        return new PayrollRunDetailDto
        {
            Run = MapPayrollRunSummary(run),
            PayPeriod = MapPayPeriod(run.PayPeriod ?? throw new NotFoundException("The pay period linked to this payroll run could not be found.")),
            Items = run.Items
                .OrderBy(item => item.EmployeeNameSnapshot)
                .Select(MapPayrollRunItem)
                .ToList(),
            AuditLogs = run.AuditLogs
                .OrderByDescending(log => log.CreatedAtUtc)
                .Select(MapAuditLog)
                .ToList()
        };
    }

    public async Task<PayrollRunDetailDto> GeneratePayrollRunAsync(GeneratePayrollRunRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var payPeriod = await _dbContext.PayPeriods
            .SingleOrDefaultAsync(record => record.Id == request.PayPeriodId!.Value, cancellationToken)
            ?? throw new BadRequestException("The selected pay period does not exist.");

        if (payPeriod.Status == PayPeriodStatuses.Cancelled || payPeriod.Status == PayPeriodStatuses.Paid)
        {
            throw new ConflictException("Payroll cannot be generated for a cancelled or paid pay period.");
        }

        var existingRun = await _dbContext.PayrollRuns.AnyAsync(
            record => record.PayPeriodId == payPeriod.Id && record.Status != PayrollRunStatuses.Cancelled,
            cancellationToken);

        if (existingRun)
        {
            throw new ConflictException("A payroll run already exists for this pay period.");
        }

        await EnsureUniquePayrollRunReferenceAsync(request.ReferenceNumber, null, cancellationToken);

        var employeeIds = await ResolveIncludedEmployeeIdsAsync(payPeriod, request, cancellationToken);
        if (employeeIds.Count == 0)
        {
            throw new BadRequestException("No employees matched the selected payroll scope.");
        }

        var run = new PayrollRun
        {
            PayPeriodId = payPeriod.Id,
            ReferenceNumber = request.ReferenceNumber.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Status = PayrollRunStatuses.Draft,
            GeneratedByUserId = NormalizeUserId(actorUserId),
            GeneratedAtUtc = DateTime.UtcNow,
            Remarks = request.Remarks.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.PayrollRuns.Add(run);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await BuildPayrollRunItemsAsync(run, payPeriod, employeeIds, actorUserId, releaseExistingAdjustments: false, cancellationToken);
        payPeriod.Status = PayPeriodStatuses.Processing;
        payPeriod.UpdatedAtUtc = DateTime.UtcNow;
        await LogAuditAsync(run.Id, null, PayrollAuditEntityTypes.PayrollRun, run.Id.ToString(), "generated", "Payroll run generated from selected payroll scope.", actorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payroll run {PayrollRunId} generated for pay period {PayPeriodId}.", run.Id, payPeriod.Id);
        return await GetPayrollRunByIdAsync(run.Id, cancellationToken);
    }

    public async Task<PayrollRunDetailDto> RecalculatePayrollRunAsync(Guid payrollRunId, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.PayrollRuns
            .Include(record => record.PayPeriod)
            .Include(record => record.Items)
                .ThenInclude(item => item.EarningLines)
            .Include(record => record.Items)
                .ThenInclude(item => item.DeductionLines)
            .SingleOrDefaultAsync(record => record.Id == payrollRunId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run '{payrollRunId}' was not found.");

        EnsureRunEditable(run);

        var employeeIds = run.Items.Select(item => item.EmployeeId).Distinct().ToList();
        await BuildPayrollRunItemsAsync(run, run.PayPeriod ?? throw new NotFoundException("The pay period linked to this payroll run could not be found."), employeeIds, actorUserId, releaseExistingAdjustments: true, cancellationToken);
        run.Status = PayrollRunStatuses.Calculated;
        run.UpdatedAtUtc = DateTime.UtcNow;
        await LogAuditAsync(run.Id, null, PayrollAuditEntityTypes.PayrollRun, run.Id.ToString(), "recalculated", "Payroll run recalculated.", actorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payroll run {PayrollRunId} recalculated.", run.Id);
        return await GetPayrollRunByIdAsync(run.Id, cancellationToken);
    }

    public async Task<PayrollRunDetailDto> SubmitPayrollRunForReviewAsync(Guid payrollRunId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.PayrollRuns
            .Include(record => record.Items)
            .SingleOrDefaultAsync(record => record.Id == payrollRunId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run '{payrollRunId}' was not found.");

        EnsureRunEditable(run);
        run.Status = PayrollRunStatuses.ForReview;
        run.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? run.Remarks : request.Remarks.Trim();
        run.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var item in run.Items)
        {
            item.Status = item.HasCriticalIssues ? PayrollItemStatuses.Held : PayrollItemStatuses.Reviewed;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }

        await LogAuditAsync(run.Id, null, PayrollAuditEntityTypes.PayrollRun, run.Id.ToString(), "for_review", string.IsNullOrWhiteSpace(request.Remarks) ? "Payroll run submitted for review." : request.Remarks.Trim(), actorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetPayrollRunByIdAsync(run.Id, cancellationToken);
    }

    public async Task<PayrollRunDetailDto> ApprovePayrollRunAsync(Guid payrollRunId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.PayrollRuns
            .Include(record => record.PayPeriod)
            .Include(record => record.Items)
            .SingleOrDefaultAsync(record => record.Id == payrollRunId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run '{payrollRunId}' was not found.");

        if (run.Status == PayrollRunStatuses.Paid || run.Status == PayrollRunStatuses.Cancelled)
        {
            throw new ConflictException("Paid or cancelled payroll runs cannot be approved.");
        }

        if (run.Items.Count == 0)
        {
            throw new ConflictException("Payroll runs without items cannot be approved.");
        }

        if (run.Items.Any(item => item.HasCriticalIssues || item.Status == PayrollItemStatuses.Held))
        {
            throw new ConflictException("Payroll runs with critical issues or held items cannot be approved.");
        }

        run.Status = PayrollRunStatuses.Approved;
        run.ApprovedByUserId = NormalizeUserId(actorUserId);
        run.ApprovedAtUtc = DateTime.UtcNow;
        run.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? run.Remarks : request.Remarks.Trim();
        run.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var item in run.Items)
        {
            item.Status = PayrollItemStatuses.Approved;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (run.PayPeriod is not null)
        {
            run.PayPeriod.Status = PayPeriodStatuses.Locked;
            run.PayPeriod.UpdatedAtUtc = DateTime.UtcNow;
        }

        await LogAuditAsync(run.Id, null, PayrollAuditEntityTypes.PayrollRun, run.Id.ToString(), "approved", string.IsNullOrWhiteSpace(request.Remarks) ? "Payroll run approved." : request.Remarks.Trim(), actorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyPayslipAvailabilityAsync(run, approvedOnly: true, cancellationToken);
        return await GetPayrollRunByIdAsync(run.Id, cancellationToken);
    }

    public async Task<PayrollRunDetailDto> MarkPayrollRunAsPaidAsync(Guid payrollRunId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.PayrollRuns
            .Include(record => record.PayPeriod)
            .Include(record => record.Items)
                .ThenInclude(item => item.DeductionLines)
            .SingleOrDefaultAsync(record => record.Id == payrollRunId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run '{payrollRunId}' was not found.");

        if (run.Status != PayrollRunStatuses.Approved)
        {
            throw new ConflictException("Only approved payroll runs can be marked as paid.");
        }

        foreach (var item in run.Items)
        {
            item.Status = PayrollItemStatuses.Paid;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }

        run.Status = PayrollRunStatuses.Paid;
        run.PaidAtUtc = DateTime.UtcNow;
        run.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? run.Remarks : request.Remarks.Trim();
        run.UpdatedAtUtc = DateTime.UtcNow;

        if (run.PayPeriod is not null)
        {
            run.PayPeriod.Status = PayPeriodStatuses.Paid;
            run.PayPeriod.UpdatedAtUtc = DateTime.UtcNow;
        }

        await ApplyPaidRunRecurringBalancesAsync(run, cancellationToken);
        await ApplyPaidRunAdjustmentsAsync(run, cancellationToken);
        await LogAuditAsync(run.Id, null, PayrollAuditEntityTypes.PayrollRun, run.Id.ToString(), "paid", string.IsNullOrWhiteSpace(request.Remarks) ? "Payroll run marked as paid." : request.Remarks.Trim(), actorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyPayslipAvailabilityAsync(run, approvedOnly: false, cancellationToken);
        return await GetPayrollRunByIdAsync(run.Id, cancellationToken);
    }

    public async Task<PayrollRunDetailDto> CancelPayrollRunAsync(Guid payrollRunId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var run = await _dbContext.PayrollRuns
            .Include(record => record.PayPeriod)
            .SingleOrDefaultAsync(record => record.Id == payrollRunId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run '{payrollRunId}' was not found.");

        if (run.Status == PayrollRunStatuses.Paid)
        {
            throw new ConflictException("Paid payroll runs cannot be cancelled.");
        }

        run.Status = PayrollRunStatuses.Cancelled;
        run.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? "Cancelled payroll run." : request.Remarks.Trim();
        run.UpdatedAtUtc = DateTime.UtcNow;

        if (run.PayPeriod is not null && run.PayPeriod.Status != PayPeriodStatuses.Paid)
        {
            run.PayPeriod.Status = PayPeriodStatuses.Open;
            run.PayPeriod.UpdatedAtUtc = DateTime.UtcNow;
        }

        await ReleaseAssignedAdjustmentsAsync(run.Id, cancellationToken);
        await LogAuditAsync(run.Id, null, PayrollAuditEntityTypes.PayrollRun, run.Id.ToString(), "cancelled", run.Remarks, actorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetPayrollRunByIdAsync(run.Id, cancellationToken);
    }

    public async Task<PagedResultDto<PayrollAdjustmentRecordDto>> GetPayrollAdjustmentsAsync(PayrollAdjustmentListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.PayrollAdjustments
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.PayPeriod)
            .Include(record => record.PayrollRun)
            .Include(record => record.EarningType)
            .Include(record => record.DeductionType)
            .Include(record => record.RequestedByUser)
            .Include(record => record.ApprovedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Employee != null &&
                (record.Employee.EmployeeCode.Contains(search) ||
                 record.Employee.FirstName.Contains(search) ||
                 record.Employee.MiddleName.Contains(search) ||
                 record.Employee.LastName.Contains(search) ||
                 record.Reason.Contains(search)));
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (query.PayPeriodId is not null)
        {
            source = source.Where(record => record.PayPeriodId == query.PayPeriodId.Value);
        }

        if (query.PayrollRunId is not null)
        {
            source = source.Where(record => record.PayrollRunId == query.PayrollRunId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.BranchId == query.BranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(query.AdjustmentType))
        {
            source = source.Where(record => record.AdjustmentType == query.AdjustmentType.Trim().ToLowerInvariant());
        }

        if (query.DateFrom is not null)
        {
            var fromDate = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            source = source.Where(record => record.CreatedAtUtc >= fromDate);
        }

        if (query.DateTo is not null)
        {
            var toDateExclusive = query.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            source = source.Where(record => record.CreatedAtUtc < toDateExclusive);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("amount", true) => source.OrderByDescending(record => record.Amount).ThenByDescending(record => record.CreatedAtUtc),
            ("amount", false) => source.OrderBy(record => record.Amount).ThenByDescending(record => record.CreatedAtUtc),
            ("employee", true) => source.OrderByDescending(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenByDescending(record => record.CreatedAtUtc),
            ("employee", false) => source.OrderBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenByDescending(record => record.CreatedAtUtc),
            (_, true) => source.OrderByDescending(record => record.CreatedAtUtc),
            _ => source.OrderBy(record => record.CreatedAtUtc)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ToPage(records.Select(MapAdjustment).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<PayrollAdjustmentRecordDto> CreatePayrollAdjustmentAsync(SavePayrollAdjustmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        await ValidateAdjustmentAsync(request, cancellationToken);

        var record = new PayrollAdjustment
        {
            EmployeeId = request.EmployeeId!.Value,
            PayPeriodId = request.PayPeriodId,
            PayrollRunId = request.PayrollRunId,
            AdjustmentType = request.AdjustmentType.Trim().ToLowerInvariant(),
            EarningTypeId = request.EarningTypeId,
            DeductionTypeId = request.DeductionTypeId,
            Amount = request.Amount,
            Reason = request.Reason.Trim(),
            Status = PayrollAdjustmentStatuses.Pending,
            RequestedByUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.PayrollAdjustments.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(null, null, PayrollAuditEntityTypes.PayrollAdjustment, record.Id.ToString(), "created", "Payroll adjustment created.", actorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdjustmentByIdAsync(record.Id, cancellationToken);
    }

    public async Task<PayrollAdjustmentRecordDto> UpdatePayrollAdjustmentAsync(Guid payrollAdjustmentId, SavePayrollAdjustmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        await ValidateAdjustmentAsync(request, cancellationToken);

        var record = await _dbContext.PayrollAdjustments.SingleOrDefaultAsync(item => item.Id == payrollAdjustmentId, cancellationToken)
            ?? throw new NotFoundException($"Payroll adjustment '{payrollAdjustmentId}' was not found.");

        if (record.Status != PayrollAdjustmentStatuses.Pending)
        {
            throw new ConflictException("Only pending payroll adjustments can be edited.");
        }

        record.EmployeeId = request.EmployeeId!.Value;
        record.PayPeriodId = request.PayPeriodId;
        record.PayrollRunId = request.PayrollRunId;
        record.AdjustmentType = request.AdjustmentType.Trim().ToLowerInvariant();
        record.EarningTypeId = request.EarningTypeId;
        record.DeductionTypeId = request.DeductionTypeId;
        record.Amount = request.Amount;
        record.Reason = request.Reason.Trim();
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(null, null, PayrollAuditEntityTypes.PayrollAdjustment, record.Id.ToString(), "updated", "Payroll adjustment updated.", actorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdjustmentByIdAsync(record.Id, cancellationToken);
    }

    public async Task DeletePayrollAdjustmentAsync(Guid payrollAdjustmentId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.PayrollAdjustments.SingleOrDefaultAsync(item => item.Id == payrollAdjustmentId, cancellationToken)
            ?? throw new NotFoundException($"Payroll adjustment '{payrollAdjustmentId}' was not found.");

        if (record.Status == PayrollAdjustmentStatuses.Applied)
        {
            throw new ConflictException("Applied payroll adjustments cannot be deleted.");
        }

        _dbContext.PayrollAdjustments.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PayrollAdjustmentRecordDto> ApprovePayrollAdjustmentAsync(Guid payrollAdjustmentId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.PayrollAdjustments.SingleOrDefaultAsync(item => item.Id == payrollAdjustmentId, cancellationToken)
            ?? throw new NotFoundException($"Payroll adjustment '{payrollAdjustmentId}' was not found.");

        if (record.Status != PayrollAdjustmentStatuses.Pending)
        {
            throw new ConflictException("Only pending payroll adjustments can be approved.");
        }

        record.Status = PayrollAdjustmentStatuses.Approved;
        record.ApprovedByUserId = NormalizeUserId(actorUserId);
        record.ApprovedAtUtc = DateTime.UtcNow;
        record.DecisionRemarks = request.Remarks.Trim();
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(null, null, PayrollAuditEntityTypes.PayrollAdjustment, record.Id.ToString(), "approved", string.IsNullOrWhiteSpace(request.Remarks) ? "Payroll adjustment approved." : request.Remarks.Trim(), actorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdjustmentByIdAsync(record.Id, cancellationToken);
    }

    public async Task<PayrollAdjustmentRecordDto> RejectPayrollAdjustmentAsync(Guid payrollAdjustmentId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.PayrollAdjustments.SingleOrDefaultAsync(item => item.Id == payrollAdjustmentId, cancellationToken)
            ?? throw new NotFoundException($"Payroll adjustment '{payrollAdjustmentId}' was not found.");

        if (record.Status != PayrollAdjustmentStatuses.Pending)
        {
            throw new ConflictException("Only pending payroll adjustments can be rejected.");
        }

        record.Status = PayrollAdjustmentStatuses.Rejected;
        record.ApprovedByUserId = NormalizeUserId(actorUserId);
        record.ApprovedAtUtc = DateTime.UtcNow;
        record.DecisionRemarks = request.Remarks.Trim();
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(null, null, PayrollAuditEntityTypes.PayrollAdjustment, record.Id.ToString(), "rejected", string.IsNullOrWhiteSpace(request.Remarks) ? "Payroll adjustment rejected." : request.Remarks.Trim(), actorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdjustmentByIdAsync(record.Id, cancellationToken);
    }

    public async Task<PayrollAdjustmentRecordDto> CancelPayrollAdjustmentAsync(Guid payrollAdjustmentId, PayrollRunActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.PayrollAdjustments.SingleOrDefaultAsync(item => item.Id == payrollAdjustmentId, cancellationToken)
            ?? throw new NotFoundException($"Payroll adjustment '{payrollAdjustmentId}' was not found.");

        if (record.Status == PayrollAdjustmentStatuses.Applied)
        {
            throw new ConflictException("Applied payroll adjustments cannot be cancelled.");
        }

        record.Status = PayrollAdjustmentStatuses.Cancelled;
        record.DecisionRemarks = request.Remarks.Trim();
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(null, null, PayrollAuditEntityTypes.PayrollAdjustment, record.Id.ToString(), "cancelled", string.IsNullOrWhiteSpace(request.Remarks) ? "Payroll adjustment cancelled." : request.Remarks.Trim(), actorUserId, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdjustmentByIdAsync(record.Id, cancellationToken);
    }

    public async Task<PayrollReportsDto> GetReportsAsync(PayrollReportQueryDto query, CancellationToken cancellationToken = default)
    {
        var items = await FilterPayrollRunItemsForReportsAsync(query, cancellationToken);
        var adjustments = await FilterPayrollAdjustmentsForReportsAsync(query, cancellationToken);

        return new PayrollReportsDto
        {
            TotalGrossPay = items.Sum(item => item.GrossPay),
            TotalDeductions = items.Sum(item => item.TotalDeductions),
            TotalNetPay = items.Sum(item => item.NetPay),
            Register = items.Select(MapPayrollRunItem).ToList(),
            ByDepartment = items
                .GroupBy(item => item.DepartmentSnapshot)
                .OrderBy(group => group.Key)
                .Select(group => new PayrollReportGroupDto
                {
                    Label = string.IsNullOrWhiteSpace(group.Key) ? "Unassigned" : group.Key,
                    Count = group.Count(),
                    GrossPay = group.Sum(item => item.GrossPay),
                    Deductions = group.Sum(item => item.TotalDeductions),
                    NetPay = group.Sum(item => item.NetPay)
                })
                .ToList(),
            ByBranch = items
                .GroupBy(item => item.BranchSnapshot)
                .OrderBy(group => group.Key)
                .Select(group => new PayrollReportGroupDto
                {
                    Label = string.IsNullOrWhiteSpace(group.Key) ? "Unassigned" : group.Key,
                    Count = group.Count(),
                    GrossPay = group.Sum(item => item.GrossPay),
                    Deductions = group.Sum(item => item.TotalDeductions),
                    NetPay = group.Sum(item => item.NetPay)
                })
                .ToList(),
            Earnings = items
                .SelectMany(item => item.EarningLines)
                .GroupBy(line => line.EarningTypeNameSnapshot)
                .OrderBy(group => group.Key)
                .Select(group => new PayrollReportLineDto
                {
                    Label = group.Key,
                    Amount = group.Sum(line => line.Amount)
                })
                .ToList(),
            Deductions = items
                .SelectMany(item => item.DeductionLines)
                .GroupBy(line => line.DeductionTypeNameSnapshot)
                .OrderBy(group => group.Key)
                .Select(group => new PayrollReportLineDto
                {
                    Label = group.Key,
                    Amount = group.Sum(line => line.Amount)
                })
                .ToList(),
            GovernmentContributions = items
                .SelectMany(item => item.DeductionLines)
                .Where(line => line.DeductionCategorySnapshot == DeductionTypeCategories.Government)
                .GroupBy(line => line.DeductionTypeNameSnapshot)
                .OrderBy(group => group.Key)
                .Select(group => new PayrollReportLineDto
                {
                    Label = group.Key,
                    Amount = group.Sum(line => line.Amount)
                })
                .ToList(),
            Adjustments = adjustments.Select(MapAdjustment).ToList()
        };
    }

    public async Task<PayslipDto> GetPayslipAsync(Guid payrollRunItemId, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.PayrollRunItems
            .AsNoTracking()
            .Include(record => record.PayrollRun)
                .ThenInclude(run => run!.PayPeriod)
            .Include(record => record.EarningLines)
            .Include(record => record.DeductionLines)
            .SingleOrDefaultAsync(record => record.Id == payrollRunItemId, cancellationToken)
            ?? throw new NotFoundException($"Payroll run item '{payrollRunItemId}' was not found.");

        var run = item.PayrollRun ?? throw new NotFoundException("The payroll run linked to this payslip could not be found.");
        var payPeriod = run.PayPeriod ?? throw new NotFoundException("The pay period linked to this payslip could not be found.");

        await _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Action = "view",
                EntityType = AuditEntityTypes.Payslip,
                EntityId = item.Id.ToString(),
                EmployeeId = item.EmployeeId,
                Remarks = $"Viewed payslip '{run.ReferenceNumber}' for employee '{item.EmployeeCodeSnapshot}'."
            },
            cancellationToken);

        return new PayslipDto
        {
            PayrollRunItemId = item.Id,
            CompanyName = "Sixram HRIS",
            PayrollRunReferenceNumber = run.ReferenceNumber,
            PayrollRunName = run.Name,
            PayPeriodName = payPeriod.Name,
            PeriodStartDate = payPeriod.PeriodStartDate,
            PeriodEndDate = payPeriod.PeriodEndDate,
            PayrollDate = payPeriod.PayrollDate,
            EmployeeCode = item.EmployeeCodeSnapshot,
            EmployeeName = item.EmployeeNameSnapshot,
            DepartmentName = item.DepartmentSnapshot,
            PositionName = item.PositionSnapshot,
            BranchName = item.BranchSnapshot,
            Currency = item.CurrencySnapshot,
            RegularWorkedDays = item.RegularWorkedDays,
            RegularWorkedHours = item.RegularWorkedHours,
            PaidLeaveDays = item.PaidLeaveDays,
            UnpaidLeaveDays = item.UnpaidLeaveDays,
            AbsentDays = item.AbsentDays,
            LateMinutes = item.LateMinutes,
            UndertimeMinutes = item.UndertimeMinutes,
            OvertimeMinutes = item.OvertimeMinutes,
            GrossPay = item.GrossPay,
            TotalDeductions = item.TotalDeductions,
            NetPay = item.NetPay,
            EmployerContributionTotal = item.EmployerContributionTotal,
            Remarks = item.Remarks,
            Issues = SplitIssues(item.IssueSummary),
            Earnings = item.EarningLines.OrderBy(line => line.EarningTypeNameSnapshot).Select(MapEarningLine).ToList(),
            Deductions = item.DeductionLines.OrderBy(line => line.DeductionTypeNameSnapshot).Select(MapDeductionLine).ToList(),
            GeneratedAtUtc = run.GeneratedAtUtc
        };
    }

    private async Task<ResolvedPayPeriodDates> ResolvePayPeriodDatesAsync(SavePayPeriodRequestDto request, CancellationToken cancellationToken)
    {
        if (request.PayPeriodTemplateId is not null && request.PeriodStartDate is not null && request.PeriodEndDate is null)
        {
            var template = await _dbContext.PayPeriodTemplates
                .AsNoTracking()
                .SingleOrDefaultAsync(record => record.Id == request.PayPeriodTemplateId.Value, cancellationToken)
                ?? throw new BadRequestException("The selected pay period template does not exist.");

            var periodStartDate = request.PeriodStartDate.Value;
            var periodEndDate = periodStartDate.AddDays(template.PeriodLengthDays - 1);
            var payrollDate = periodEndDate.AddDays(template.PayrollOffsetDays);

            return new ResolvedPayPeriodDates(
                periodStartDate,
                periodEndDate,
                request.CutoffStartDate ?? periodStartDate,
                request.CutoffEndDate ?? periodEndDate,
                request.PayrollDate ?? payrollDate);
        }

        if (request.PeriodStartDate is null || request.PeriodEndDate is null || request.CutoffStartDate is null || request.CutoffEndDate is null || request.PayrollDate is null)
        {
            throw new BadRequestException("Period start, period end, cutoff start, cutoff end, and payroll date are required when no template-driven default can be applied.");
        }

        return new ResolvedPayPeriodDates(
            request.PeriodStartDate.Value,
            request.PeriodEndDate.Value,
            request.CutoffStartDate.Value,
            request.CutoffEndDate.Value,
            request.PayrollDate.Value);
    }

    private async Task EnsureUniquePayPeriodCodeAsync(string code, Guid? existingId, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var exists = await _dbContext.PayPeriods.AnyAsync(
            record => record.Code == normalized && (!existingId.HasValue || record.Id != existingId.Value),
            cancellationToken);

        if (exists)
        {
            throw BuildValidationException("The pay period code is already in use.", nameof(SavePayPeriodRequestDto.Code));
        }
    }

    private async Task EnsureUniquePayrollRunReferenceAsync(string referenceNumber, Guid? existingId, CancellationToken cancellationToken)
    {
        var normalized = referenceNumber.Trim().ToUpperInvariant();
        var exists = await _dbContext.PayrollRuns.AnyAsync(
            record => record.ReferenceNumber == normalized && (!existingId.HasValue || record.Id != existingId.Value),
            cancellationToken);

        if (exists)
        {
            throw BuildValidationException("The payroll run reference number is already in use.", nameof(GeneratePayrollRunRequestDto.ReferenceNumber));
        }
    }

    private async Task NotifyPayslipAvailabilityAsync(PayrollRun run, bool approvedOnly, CancellationToken cancellationToken)
    {
        var settings = await _payrollSetupService.GetSettingsAsync(cancellationToken);
        var visibilityRule = settings.PayslipVisibilityRule.Trim().ToLowerInvariant();

        if (approvedOnly)
        {
            if (visibilityRule != "approved_or_paid" && visibilityRule != "approved_only")
            {
                return;
            }
        }
        else if (visibilityRule != "paid_only")
        {
            return;
        }

        if (run.PayPeriod is null || run.Items.Count == 0)
        {
            return;
        }

        var userIdsByEmployee = await _notificationService.GetUserIdsForEmployeesAsync(run.Items.Select(item => item.EmployeeId), cancellationToken);
        var notifications = run.Items
            .Where(item => item.Status != PayrollItemStatuses.Held && userIdsByEmployee.ContainsKey(item.EmployeeId))
            .Select(item => new NotificationDraft(
                userIdsByEmployee[item.EmployeeId],
                approvedOnly ? "Payslip is ready" : "Payroll has been marked as paid",
                approvedOnly
                    ? $"Your payslip for {run.PayPeriod.Name} is now available."
                    : $"Your payroll for {run.PayPeriod.Name} has been marked as paid.",
                NotificationTypes.PayslipAvailable,
                "payroll_run_item",
                item.Id.ToString(),
                $"/me/payslips/{item.Id}"))
            .ToList();

        await _notificationService.CreateManyAsync(notifications, cancellationToken);
    }

    private async Task<List<Guid>> ResolveIncludedEmployeeIdsAsync(PayPeriod payPeriod, GeneratePayrollRunRequestDto request, CancellationToken cancellationToken)
    {
        var source = _dbContext.Employees
            .AsNoTracking()
            .Where(record =>
                record.DateHired == null || record.DateHired <= payPeriod.PeriodEndDate);

        source = source.Where(record =>
            record.IsActive ||
            (record.DateSeparated != null && record.DateSeparated >= payPeriod.PeriodStartDate));

        if (request.DepartmentId is not null)
        {
            source = source.Where(record => record.DepartmentId == request.DepartmentId.Value);
        }

        if (request.BranchId is not null)
        {
            source = source.Where(record => record.BranchId == request.BranchId.Value);
        }

        if (request.EmploymentTypeId is not null)
        {
            source = source.Where(record => record.EmploymentTypeId == request.EmploymentTypeId.Value);
        }

        if (request.EmploymentStatusId is not null)
        {
            source = source.Where(record => record.EmploymentStatusId == request.EmploymentStatusId.Value);
        }

        if (request.SelectedEmployeeIds.Count > 0)
        {
            source = source.Where(record => request.SelectedEmployeeIds.Contains(record.Id));
        }

        return await source
            .OrderBy(record => record.LastName)
            .ThenBy(record => record.FirstName)
            .Select(record => record.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task BuildPayrollRunItemsAsync(
        PayrollRun run,
        PayPeriod payPeriod,
        IReadOnlyCollection<Guid> employeeIds,
        string? actorUserId,
        bool releaseExistingAdjustments,
        CancellationToken cancellationToken)
    {
        if (releaseExistingAdjustments)
        {
            await ReleaseAssignedAdjustmentsAsync(run.Id, cancellationToken);
            await RemoveRunItemsAsync(run.Id, cancellationToken);
        }

        var settings = await _payrollSetupService.GetSettingsAsync(cancellationToken);
        var activeContributionTypes = await _dbContext.ContributionTypes
            .AsNoTracking()
            .Where(record => record.IsActive)
            .OrderBy(record => record.Name)
            .ToListAsync(cancellationToken);
        var basicEarningType = await GetOrCreateSystemEarningTypeAsync("BASIC", "Basic Pay", EarningTypeCategories.Basic, taxable: true, cancellationToken);
        var leaveEarningType = await GetOrCreateSystemEarningTypeAsync("PAID-LEAVE", "Paid Leave", EarningTypeCategories.Other, taxable: true, cancellationToken);
        var overtimeEarningType = await GetOrCreateSystemEarningTypeAsync("OVERTIME", "Overtime Pay", EarningTypeCategories.Overtime, taxable: true, cancellationToken);
        var unpaidLeaveDeductionType = await GetOrCreateSystemDeductionTypeAsync("UNPAID-LEAVE", "Unpaid Leave", DeductionTypeCategories.Absence, preTax: false, cancellationToken);
        var absenceDeductionType = await GetOrCreateSystemDeductionTypeAsync("ABSENCE", "Absence", DeductionTypeCategories.Absence, preTax: false, cancellationToken);
        var lateDeductionType = await GetOrCreateSystemDeductionTypeAsync("LATE", "Late", DeductionTypeCategories.Late, preTax: false, cancellationToken);
        var undertimeDeductionType = await GetOrCreateSystemDeductionTypeAsync("UNDERTIME", "Undertime", DeductionTypeCategories.Undertime, preTax: false, cancellationToken);
        var taxDeductionType = await GetOrCreateSystemDeductionTypeAsync("TAX", "Tax", DeductionTypeCategories.Tax, preTax: false, cancellationToken);

        var items = new List<PayrollRunItem>();
        foreach (var employeeId in employeeIds)
        {
            var item = await BuildPayrollRunItemAsync(
                run,
                payPeriod,
                employeeId,
                settings,
                activeContributionTypes,
                basicEarningType,
                leaveEarningType,
                overtimeEarningType,
                absenceDeductionType,
                unpaidLeaveDeductionType,
                lateDeductionType,
                undertimeDeductionType,
                taxDeductionType,
                cancellationToken);

            items.Add(item);
        }

        _dbContext.PayrollRunItems.AddRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var item in items)
        {
            await LogAuditAsync(run.Id, item.Id, PayrollAuditEntityTypes.PayrollRunItem, item.Id.ToString(), "calculated", "Payroll item calculated and snapshotted.", actorUserId, cancellationToken);
        }

        run.Status = PayrollRunStatuses.Calculated;
        run.GeneratedAtUtc = DateTime.UtcNow;
        run.UpdatedAtUtc = DateTime.UtcNow;
    }

    private async Task<PayrollRunItem> BuildPayrollRunItemAsync(
        PayrollRun run,
        PayPeriod payPeriod,
        Guid employeeId,
        PayrollSettingsDto settings,
        IReadOnlyList<ContributionType> activeContributionTypes,
        EarningType basicEarningType,
        EarningType leaveEarningType,
        EarningType overtimeEarningType,
        DeductionType absenceDeductionType,
        DeductionType unpaidLeaveDeductionType,
        DeductionType lateDeductionType,
        DeductionType undertimeDeductionType,
        DeductionType taxDeductionType,
        CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees
            .AsNoTracking()
            .Include(record => record.Department)
            .Include(record => record.Position)
            .Include(record => record.Branch)
            .SingleOrDefaultAsync(record => record.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Employee '{employeeId}' was not found.");

        var compensationProfile = await _dbContext.CompensationProfiles
            .AsNoTracking()
            .Where(record =>
                record.EmployeeId == employeeId &&
                record.IsActive &&
                record.EffectiveStartDate <= payPeriod.CutoffEndDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= payPeriod.CutoffStartDate))
            .OrderByDescending(record => record.EffectiveStartDate)
            .ThenByDescending(record => record.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var issueSet = new LinkedHashSet<string>();
        if (compensationProfile is null)
        {
            issueSet.Add("No active compensation profile covers the pay period.");
        }

        var periodStart = employee.DateHired is not null && employee.DateHired > payPeriod.CutoffStartDate
            ? employee.DateHired.Value
            : payPeriod.CutoffStartDate;
        var periodEnd = employee.DateSeparated is not null && employee.DateSeparated < payPeriod.CutoffEndDate
            ? employee.DateSeparated.Value
            : payPeriod.CutoffEndDate;

        if (periodEnd < periodStart)
        {
            issueSet.Add("Employee is outside the effective employment dates for this pay period.");
        }

        var assignments = await _dbContext.EmployeeScheduleAssignments
            .AsNoTracking()
            .Include(record => record.WorkSchedule)
            .Include(record => record.Shift)
            .Where(record =>
                record.EmployeeId == employeeId &&
                record.EffectiveStartDate <= payPeriod.CutoffEndDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= payPeriod.CutoffStartDate))
            .ToListAsync(cancellationToken);

        var attendanceRecords = await _dbContext.AttendanceRecords
            .AsNoTracking()
            .Where(record =>
                record.EmployeeId == employeeId &&
                record.AttendanceDate >= payPeriod.CutoffStartDate &&
                record.AttendanceDate <= payPeriod.CutoffEndDate)
            .ToDictionaryAsync(record => record.AttendanceDate, cancellationToken);

        var leaveRequests = await _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(record => record.LeaveType)
            .Where(record =>
                record.EmployeeId == employeeId &&
                record.Status == LeaveRequestStatuses.Approved &&
                record.StartDate <= payPeriod.CutoffEndDate &&
                record.EndDate >= payPeriod.CutoffStartDate)
            .ToListAsync(cancellationToken);

        var leaveAllocations = await BuildLeaveAllocationsAsync(employeeId, leaveRequests, assignments, payPeriod.CutoffStartDate, payPeriod.CutoffEndDate, cancellationToken);

        var regularWorkedDays = 0m;
        var regularWorkedHours = 0m;
        var paidLeaveDays = 0m;
        var unpaidLeaveDays = 0m;
        var absentDays = 0m;
        var lateMinutes = 0;
        var undertimeMinutes = 0;
        var overtimeMinutes = 0;
        var noScheduleDays = 0;
        var scheduledWorkDays = 0m;

        if (periodEnd >= periodStart)
        {
            for (var date = periodStart; date <= periodEnd; date = date.AddDays(1))
            {
                var resolvedSchedule = _attendanceCalculationService.ResolveSchedule(assignments, date);
                var hasWorkSchedule = resolvedSchedule.HasScheduleAssignment && !resolvedSchedule.IsRestDay;
                if (hasWorkSchedule)
                {
                    scheduledWorkDays += 1m;
                }

                if (!resolvedSchedule.HasScheduleAssignment)
                {
                    noScheduleDays++;
                }

                var leaveAllocation = leaveAllocations.GetValueOrDefault(date);
                var hasActualAttendance = attendanceRecords.TryGetValue(date, out var attendance) &&
                                          !string.Equals(attendance.Status, AttendanceStatuses.OnLeave, StringComparison.OrdinalIgnoreCase);

                if (leaveAllocation.TotalDays > 0m && hasActualAttendance)
                {
                    issueSet.Add($"Approved leave conflicts with actual attendance on {date:yyyy-MM-dd}.");
                    leaveAllocation = leaveAllocation with { PaidDays = 0m, UnpaidDays = 0m };
                }

                if (leaveAllocation.PaidDays > 0m)
                {
                    paidLeaveDays += leaveAllocation.PaidDays;
                }

                if (leaveAllocation.UnpaidDays > 0m)
                {
                    unpaidLeaveDays += leaveAllocation.UnpaidDays;
                }

                if (attendance is not null && !string.Equals(attendance.Status, AttendanceStatuses.OnLeave, StringComparison.OrdinalIgnoreCase))
                {
                    var attendanceFraction = ResolveAttendanceFraction(attendance, resolvedSchedule);
                    regularWorkedDays += attendanceFraction;
                    regularWorkedHours += Math.Round(attendance.TotalWorkedMinutes / 60m, 2);
                    lateMinutes += attendance.LateMinutes;
                    undertimeMinutes += attendance.UndertimeMinutes;
                    overtimeMinutes += attendance.OvertimeMinutes;

                    if (string.Equals(attendance.Status, AttendanceStatuses.Incomplete, StringComparison.OrdinalIgnoreCase))
                    {
                        issueSet.Add($"Incomplete attendance exists on {date:yyyy-MM-dd}.");
                    }

                    if (string.Equals(attendance.Status, AttendanceStatuses.NoSchedule, StringComparison.OrdinalIgnoreCase))
                    {
                        issueSet.Add($"Attendance exists without an assigned schedule on {date:yyyy-MM-dd}.");
                    }

                    continue;
                }

                if (hasWorkSchedule)
                {
                    var uncovered = Math.Max(0m, 1m - leaveAllocation.TotalDays);
                    if (uncovered > 0m)
                    {
                        absentDays += uncovered;
                        issueSet.Add($"Missing attendance for a scheduled working day on {date:yyyy-MM-dd}.");
                    }
                }
            }
        }

        if (noScheduleDays > 0)
        {
            issueSet.Add("No schedule assignment exists for one or more dates in this pay period.");
        }

        var currency = compensationProfile?.Currency ?? settings.DefaultCurrency;
        var basicSalary = compensationProfile?.BasicSalary ?? 0m;
        var dailyRate = ResolveDailyRate(compensationProfile, settings);
        var hourlyRate = ResolveHourlyRate(compensationProfile, settings, dailyRate);
        var payType = compensationProfile?.PayType ?? PayrollPayTypes.Other;

        var earnings = new List<PayrollEarningLine>();
        var deductions = new List<PayrollDeductionLine>();

        var basicPay = payType == PayrollPayTypes.Hourly
            ? RoundMoney(regularWorkedHours * hourlyRate, settings)
            : RoundMoney(regularWorkedDays * dailyRate, settings);
        var leavePay = payType == PayrollPayTypes.Hourly
            ? RoundMoney(paidLeaveDays * settings.DefaultWorkingHoursPerDay * hourlyRate, settings)
            : RoundMoney(paidLeaveDays * dailyRate, settings);
        var overtimePay = settings.OvertimeCalculationPolicy.Equals("off", StringComparison.OrdinalIgnoreCase)
            ? 0m
            : RoundMoney((overtimeMinutes / 60m) * hourlyRate, settings);

        if (basicPay > 0m)
        {
            earnings.Add(CreateEarningLine(basicEarningType, "Basic pay", basicPay, regularWorkedDays, payType == PayrollPayTypes.Hourly ? hourlyRate : dailyRate, PayrollLineSources.BasicSalary, taxable: true));
        }

        if (leavePay > 0m)
        {
            earnings.Add(CreateEarningLine(leaveEarningType, "Paid leave", leavePay, paidLeaveDays, payType == PayrollPayTypes.Hourly ? hourlyRate * settings.DefaultWorkingHoursPerDay : dailyRate, PayrollLineSources.Leave, taxable: true));
        }

        if (overtimePay > 0m)
        {
            earnings.Add(CreateEarningLine(overtimeEarningType, "Preliminary overtime pay", overtimePay, Math.Round(overtimeMinutes / 60m, 2), hourlyRate, PayrollLineSources.Overtime, taxable: true));
        }

        var recurringEarnings = await GetActiveRecurringEarningsAsync(employeeId, payPeriod, cancellationToken);
        foreach (var recurring in recurringEarnings)
        {
            var amount = RoundMoney(ConvertAmountForPayFrequency(recurring.Amount, recurring.Frequency, payPeriod.PayFrequency), settings);
            if (amount <= 0m)
            {
                continue;
            }

            var type = recurring.EarningType ?? throw new NotFoundException("Recurring earning type could not be loaded.");
            earnings.Add(new PayrollEarningLine
            {
                EarningTypeId = type.Id,
                EarningTypeCodeSnapshot = type.Code,
                EarningTypeNameSnapshot = type.Name,
                Description = string.IsNullOrWhiteSpace(recurring.Remarks) ? $"Recurring earning: {type.Name}" : recurring.Remarks,
                Amount = amount,
                Quantity = 1m,
                Rate = recurring.Amount,
                Source = PayrollLineSources.Recurring,
                Taxable = type.Taxable,
                IsManual = false,
                EmployeeRecurringEarningId = recurring.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        var recurringDeductions = await GetActiveRecurringDeductionsAsync(employeeId, payPeriod, cancellationToken);
        foreach (var recurring in recurringDeductions)
        {
            var type = recurring.DeductionType ?? throw new NotFoundException("Recurring deduction type could not be loaded.");
            var amount = RoundMoney(ConvertAmountForPayFrequency(recurring.Amount, recurring.Frequency, payPeriod.PayFrequency), settings);
            if (recurring.Balance is not null)
            {
                amount = Math.Min(amount, recurring.Balance.Value);
            }

            if (amount <= 0m)
            {
                continue;
            }

            deductions.Add(new PayrollDeductionLine
            {
                DeductionTypeId = type.Id,
                DeductionTypeCodeSnapshot = type.Code,
                DeductionTypeNameSnapshot = type.Name,
                DeductionCategorySnapshot = type.Category,
                Description = string.IsNullOrWhiteSpace(recurring.Remarks) ? $"Recurring deduction: {type.Name}" : recurring.Remarks,
                Amount = amount,
                Source = PayrollLineSources.Recurring,
                PreTax = type.PreTax,
                IsManual = false,
                EmployeeRecurringDeductionId = recurring.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        var payrollAdjustments = await GetApplicableApprovedAdjustmentsAsync(employeeId, payPeriod.Id, run.Id, cancellationToken);
        foreach (var adjustment in payrollAdjustments)
        {
            adjustment.PayrollRunId = run.Id;
            adjustment.UpdatedAtUtc = DateTime.UtcNow;

            if (adjustment.AdjustmentType == PayrollAdjustmentTypes.Earning)
            {
                var type = adjustment.EarningType ?? throw new NotFoundException("Payroll adjustment earning type could not be loaded.");
                earnings.Add(new PayrollEarningLine
                {
                    EarningTypeId = type.Id,
                    EarningTypeCodeSnapshot = type.Code,
                    EarningTypeNameSnapshot = type.Name,
                    Description = adjustment.Reason,
                    Amount = RoundMoney(adjustment.Amount, settings),
                    Quantity = 1m,
                    Rate = adjustment.Amount,
                    Source = PayrollLineSources.Adjustment,
                    Taxable = type.Taxable,
                    IsManual = true,
                    PayrollAdjustmentId = adjustment.Id,
                    Remarks = adjustment.Reason,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                var type = adjustment.DeductionType ?? throw new NotFoundException("Payroll adjustment deduction type could not be loaded.");
                deductions.Add(new PayrollDeductionLine
                {
                    DeductionTypeId = type.Id,
                    DeductionTypeCodeSnapshot = type.Code,
                    DeductionTypeNameSnapshot = type.Name,
                    DeductionCategorySnapshot = type.Category,
                    Description = adjustment.Reason,
                    Amount = RoundMoney(adjustment.Amount, settings),
                    Source = PayrollLineSources.Adjustment,
                    PreTax = type.PreTax,
                    IsManual = true,
                    PayrollAdjustmentId = adjustment.Id,
                    Remarks = adjustment.Reason,
                    CreatedAtUtc = DateTime.UtcNow
                });
                if (type.Category == DeductionTypeCategories.Loan)
                {
                    issueSet.Add("This payroll item includes a loan-related manual adjustment.");
                }
            }
        }

        var grossPay = RoundMoney(earnings.Sum(line => line.Amount), settings);
        var taxableIncome = grossPay - deductions.Where(line => line.PreTax).Sum(line => line.Amount);
        if (taxableIncome < 0m)
        {
            taxableIncome = 0m;
        }

        foreach (var contributionType in activeContributionTypes)
        {
            var applicableTable = await GetApplicableContributionTableAsync(contributionType.Id, payPeriod.CutoffEndDate, cancellationToken);
            if (applicableTable is null)
            {
                issueSet.Add($"No active contribution table is configured for {contributionType.Name}.");
                continue;
            }

            var bracket = applicableTable.Brackets
                .OrderBy(item => item.MinCompensation)
                .FirstOrDefault(item => grossPay >= item.MinCompensation && (!item.MaxCompensation.HasValue || grossPay <= item.MaxCompensation.Value));

            if (bracket is null)
            {
                issueSet.Add($"No contribution bracket matched {contributionType.Name} for the computed compensation.");
                continue;
            }

            var deductionType = await GetOrCreateSystemDeductionTypeAsync(contributionType.Code, contributionType.Name, DeductionTypeCategories.Government, preTax: false, cancellationToken);
            var employeeShare = contributionType.EmployeeShareApplicable
                ? RoundMoney(ResolveBracketAmount(grossPay, bracket.EmployeeShareAmount, bracket.EmployeeShareRate), settings)
                : 0m;
            var employerShare = contributionType.EmployerShareApplicable
                ? RoundMoney(ResolveBracketAmount(grossPay, bracket.EmployerShareAmount, bracket.EmployerShareRate), settings)
                : 0m;

            if (employeeShare > 0m)
            {
                deductions.Add(new PayrollDeductionLine
                {
                    DeductionTypeId = deductionType.Id,
                    DeductionTypeCodeSnapshot = deductionType.Code,
                    DeductionTypeNameSnapshot = deductionType.Name,
                    DeductionCategorySnapshot = deductionType.Category,
                    Description = contributionType.Name,
                    Amount = employeeShare,
                    Source = PayrollLineSources.Contribution,
                    PreTax = deductionType.PreTax,
                    IsManual = false,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            if (employerShare > 0m)
            {
                // Stored later on the item snapshot.
            }
        }

        var taxTable = await GetApplicableTaxTableAsync(payPeriod.PayFrequency, payPeriod.CutoffEndDate, cancellationToken);
        if (taxTable is null)
        {
            issueSet.Add($"No active tax table is configured for the {payPeriod.PayFrequency} payroll frequency.");
        }
        else
        {
            var bracket = taxTable.Brackets
                .OrderBy(item => item.MinTaxableIncome)
                .FirstOrDefault(item => taxableIncome >= item.MinTaxableIncome && (!item.MaxTaxableIncome.HasValue || taxableIncome <= item.MaxTaxableIncome.Value));

            if (bracket is null)
            {
                issueSet.Add("No tax bracket matched the taxable income for this payroll item.");
            }
            else
            {
                var tax = RoundMoney(bracket.BaseTax + Math.Max(0m, taxableIncome - bracket.ExcessOver) * bracket.TaxRate, settings);
                if (tax > 0m)
                {
                    deductions.Add(new PayrollDeductionLine
                    {
                        DeductionTypeId = taxDeductionType.Id,
                        DeductionTypeCodeSnapshot = taxDeductionType.Code,
                        DeductionTypeNameSnapshot = taxDeductionType.Name,
                        DeductionCategorySnapshot = taxDeductionType.Category,
                        Description = taxTable.Name,
                        Amount = tax,
                        Source = PayrollLineSources.Tax,
                        PreTax = false,
                        IsManual = false,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }
        }

        var unpaidLeaveDeductionAmount = payType == PayrollPayTypes.Monthly
            ? RoundMoney(unpaidLeaveDays * dailyRate, settings)
            : 0m;
        if (unpaidLeaveDeductionAmount > 0m)
        {
            deductions.Add(CreateDeductionLine(unpaidLeaveDeductionType, "Unpaid leave", unpaidLeaveDeductionAmount, PayrollLineSources.Leave));
        }

        var absenceDeductionAmount = payType == PayrollPayTypes.Monthly
            ? RoundMoney(absentDays * dailyRate, settings)
            : 0m;
        if (absenceDeductionAmount > 0m)
        {
            deductions.Add(CreateDeductionLine(absenceDeductionType, "Absence", absenceDeductionAmount, PayrollLineSources.Attendance));
        }

        var lateDeductionAmount = settings.LateUndertimeDeductionPolicy.Equals("minute_based", StringComparison.OrdinalIgnoreCase)
            ? RoundMoney((lateMinutes / 60m) * hourlyRate, settings)
            : 0m;
        if (lateDeductionAmount > 0m)
        {
            deductions.Add(CreateDeductionLine(lateDeductionType, "Late deduction", lateDeductionAmount, PayrollLineSources.Attendance));
        }

        var undertimeDeductionAmount = settings.LateUndertimeDeductionPolicy.Equals("minute_based", StringComparison.OrdinalIgnoreCase)
            ? RoundMoney((undertimeMinutes / 60m) * hourlyRate, settings)
            : 0m;
        if (undertimeDeductionAmount > 0m)
        {
            deductions.Add(CreateDeductionLine(undertimeDeductionType, "Undertime deduction", undertimeDeductionAmount, PayrollLineSources.Attendance));
        }

        grossPay = RoundMoney(earnings.Sum(line => line.Amount), settings);
        var totalDeductions = RoundMoney(deductions.Sum(line => line.Amount), settings);
        var netPay = RoundMoney(grossPay - totalDeductions, settings);

        if (!settings.AllowNegativeNetPay && netPay < 0m)
        {
            issueSet.Add("Net pay is negative and the current payroll settings do not allow negative net pay.");
        }

        var employerContributionTotal = RoundMoney(await CalculateEmployerContributionTotalAsync(activeContributionTypes, payPeriod.CutoffEndDate, grossPay, settings, cancellationToken), settings);
        var governmentDeductionsTotal = RoundMoney(deductions.Where(line => line.DeductionCategorySnapshot == DeductionTypeCategories.Government).Sum(line => line.Amount), settings);
        var taxDeduction = RoundMoney(deductions.Where(line => line.DeductionCategorySnapshot == DeductionTypeCategories.Tax).Sum(line => line.Amount), settings);
        var loanDeduction = RoundMoney(deductions.Where(line => line.DeductionCategorySnapshot == DeductionTypeCategories.Loan).Sum(line => line.Amount), settings);
        var otherDeductionTotal = RoundMoney(deductions.Where(line =>
            line.DeductionCategorySnapshot != DeductionTypeCategories.Government &&
            line.DeductionCategorySnapshot != DeductionTypeCategories.Tax &&
            line.DeductionCategorySnapshot != DeductionTypeCategories.Loan &&
            line.DeductionCategorySnapshot != DeductionTypeCategories.Absence &&
            line.DeductionCategorySnapshot != DeductionTypeCategories.Late &&
            line.DeductionCategorySnapshot != DeductionTypeCategories.Undertime).Sum(line => line.Amount), settings);

        var item = new PayrollRunItem
        {
            PayrollRunId = run.Id,
            EmployeeId = employee.Id,
            CompensationProfileId = compensationProfile?.Id,
            EmployeeCodeSnapshot = employee.EmployeeCode,
            EmployeeNameSnapshot = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            DepartmentSnapshot = employee.Department?.Name ?? string.Empty,
            PositionSnapshot = employee.Position?.Name ?? string.Empty,
            BranchSnapshot = employee.Branch?.Name ?? string.Empty,
            PayTypeSnapshot = payType,
            CurrencySnapshot = currency,
            BasicSalarySnapshot = basicSalary,
            DailyRateSnapshot = dailyRate > 0m ? dailyRate : null,
            HourlyRateSnapshot = hourlyRate > 0m ? hourlyRate : null,
            RegularWorkedDays = RoundDecimal(regularWorkedDays),
            RegularWorkedHours = RoundDecimal(regularWorkedHours),
            PaidLeaveDays = RoundDecimal(paidLeaveDays),
            UnpaidLeaveDays = RoundDecimal(unpaidLeaveDays),
            AbsentDays = RoundDecimal(absentDays),
            LateMinutes = lateMinutes,
            UndertimeMinutes = undertimeMinutes,
            OvertimeMinutes = overtimeMinutes,
            BasicPay = RoundMoney(earnings.Where(line => line.EarningTypeId == basicEarningType.Id).Sum(line => line.Amount), settings),
            AllowanceTotal = RoundMoney(earnings.Where(line => line.EarningTypeId != basicEarningType.Id && line.EarningTypeId != leaveEarningType.Id && line.EarningTypeId != overtimeEarningType.Id && line.EarningType != null && line.EarningType.Category == EarningTypeCategories.Allowance).Sum(line => line.Amount), settings),
            OvertimePay = RoundMoney(earnings.Where(line => line.EarningTypeId == overtimeEarningType.Id).Sum(line => line.Amount), settings),
            HolidayPay = 0m,
            LeavePay = RoundMoney(earnings.Where(line => line.EarningTypeId == leaveEarningType.Id).Sum(line => line.Amount), settings),
            BonusTotal = RoundMoney(earnings.Where(line => line.EarningType != null && line.EarningType.Category == EarningTypeCategories.Bonus).Sum(line => line.Amount), settings),
            OtherEarningsTotal = RoundMoney(earnings.Where(line =>
                line.EarningTypeId != basicEarningType.Id &&
                line.EarningTypeId != leaveEarningType.Id &&
                line.EarningTypeId != overtimeEarningType.Id &&
                (line.EarningType == null || (line.EarningType.Category != EarningTypeCategories.Allowance && line.EarningType.Category != EarningTypeCategories.Bonus))).Sum(line => line.Amount), settings),
            GrossPay = grossPay,
            GovernmentDeductionsTotal = governmentDeductionsTotal,
            TaxDeduction = taxDeduction,
            AbsenceDeduction = absenceDeductionAmount + unpaidLeaveDeductionAmount,
            LateDeduction = lateDeductionAmount,
            UndertimeDeduction = undertimeDeductionAmount,
            LoanDeduction = loanDeduction,
            OtherDeductionsTotal = otherDeductionTotal,
            TotalDeductions = totalDeductions,
            NetPay = netPay,
            EmployerContributionTotal = employerContributionTotal,
            Status = issueSet.Count > 0 ? PayrollItemStatuses.Held : PayrollItemStatuses.Draft,
            Remarks = issueSet.Count > 0 ? "Payroll item contains warnings or blocking issues." : "Payroll item calculated successfully.",
            HasCriticalIssues = issueSet.Count > 0,
            IssueSummary = string.Join('\n', issueSet),
            EarningLines = earnings,
            DeductionLines = deductions,
            CreatedAtUtc = DateTime.UtcNow
        };

        return item;
    }

    private Task<Dictionary<DateOnly, LeaveAllocation>> BuildLeaveAllocationsAsync(
        Guid employeeId,
        IReadOnlyList<LeaveRequest> leaveRequests,
        IReadOnlyList<EmployeeScheduleAssignment> assignments,
        DateOnly cutoffStartDate,
        DateOnly cutoffEndDate,
        CancellationToken cancellationToken)
    {
        _ = employeeId;
        _ = cancellationToken;

        var allocations = new Dictionary<DateOnly, LeaveAllocation>();
        foreach (var leaveRequest in leaveRequests)
        {
            var leaveType = leaveRequest.LeaveType ?? throw new NotFoundException("The leave type linked to an approved leave request could not be found.");
            var effectiveStart = leaveRequest.StartDate > cutoffStartDate ? leaveRequest.StartDate : cutoffStartDate;
            var effectiveEnd = leaveRequest.EndDate < cutoffEndDate ? leaveRequest.EndDate : cutoffEndDate;
            for (var date = effectiveStart; date <= effectiveEnd; date = date.AddDays(1))
            {
                var resolvedSchedule = _attendanceCalculationService.ResolveSchedule(assignments, date);
                if (!leaveType.CountsRestDays && resolvedSchedule.HasScheduleAssignment && resolvedSchedule.IsRestDay)
                {
                    continue;
                }

                var fraction = ResolveLeaveFraction(date, leaveRequest.StartDate, leaveRequest.EndDate, leaveRequest.StartDayType, leaveRequest.EndDayType);
                if (fraction <= 0m)
                {
                    continue;
                }

                var existing = allocations.GetValueOrDefault(date);
                allocations[date] = leaveType.IsPaid
                    ? existing with { PaidDays = existing.PaidDays + fraction }
                    : existing with { UnpaidDays = existing.UnpaidDays + fraction };
            }
        }

        return Task.FromResult(allocations);
    }

    private async Task<PayPeriodRecordDto> GetPayPeriodByIdAsync(Guid payPeriodId, CancellationToken cancellationToken)
    {
        return await _dbContext.PayPeriods
            .AsNoTracking()
            .Include(record => record.PayPeriodTemplate)
            .Include(record => record.PayrollRuns)
            .Where(record => record.Id == payPeriodId)
            .Select(record => new PayPeriodRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                PayFrequency = record.PayFrequency,
                PeriodStartDate = record.PeriodStartDate,
                PeriodEndDate = record.PeriodEndDate,
                PayrollDate = record.PayrollDate,
                CutoffStartDate = record.CutoffStartDate,
                CutoffEndDate = record.CutoffEndDate,
                Status = record.Status,
                Remarks = record.Remarks,
                PayPeriodTemplateId = record.PayPeriodTemplateId,
                PayPeriodTemplateName = record.PayPeriodTemplate != null ? record.PayPeriodTemplate.Name : string.Empty,
                PayrollRunCount = record.PayrollRuns.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Pay period '{payPeriodId}' was not found.");
    }

    private async Task<List<EmployeeRecurringEarning>> GetActiveRecurringEarningsAsync(Guid employeeId, PayPeriod payPeriod, CancellationToken cancellationToken)
    {
        return await _dbContext.EmployeeRecurringEarnings
            .AsNoTracking()
            .Include(record => record.EarningType)
            .Where(record =>
                record.EmployeeId == employeeId &&
                record.IsActive &&
                record.EffectiveStartDate <= payPeriod.CutoffEndDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= payPeriod.CutoffStartDate))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<EmployeeRecurringDeduction>> GetActiveRecurringDeductionsAsync(Guid employeeId, PayPeriod payPeriod, CancellationToken cancellationToken)
    {
        return await _dbContext.EmployeeRecurringDeductions
            .AsNoTracking()
            .Include(record => record.DeductionType)
            .Where(record =>
                record.EmployeeId == employeeId &&
                record.IsActive &&
                record.EffectiveStartDate <= payPeriod.CutoffEndDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= payPeriod.CutoffStartDate))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<PayrollAdjustment>> GetApplicableApprovedAdjustmentsAsync(Guid employeeId, Guid payPeriodId, Guid payrollRunId, CancellationToken cancellationToken)
    {
        return await _dbContext.PayrollAdjustments
            .Include(record => record.EarningType)
            .Include(record => record.DeductionType)
            .Where(record =>
                record.EmployeeId == employeeId &&
                record.Status == PayrollAdjustmentStatuses.Approved &&
                (!record.PayPeriodId.HasValue || record.PayPeriodId == payPeriodId) &&
                (!record.PayrollRunId.HasValue || record.PayrollRunId == payrollRunId))
            .ToListAsync(cancellationToken);
    }

    private async Task<GovernmentContributionTable?> GetApplicableContributionTableAsync(Guid contributionTypeId, DateOnly effectiveDate, CancellationToken cancellationToken)
    {
        return await _dbContext.GovernmentContributionTables
            .AsNoTracking()
            .Include(record => record.Brackets)
            .Where(record =>
                record.ContributionTypeId == contributionTypeId &&
                record.IsActive &&
                record.EffectiveStartDate <= effectiveDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= effectiveDate))
            .OrderByDescending(record => record.EffectiveStartDate)
            .ThenByDescending(record => record.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<TaxTable?> GetApplicableTaxTableAsync(string payFrequency, DateOnly effectiveDate, CancellationToken cancellationToken)
    {
        var normalizedFrequency = payFrequency.Trim().ToLowerInvariant();
        return await _dbContext.TaxTables
            .AsNoTracking()
            .Include(record => record.Brackets)
            .Where(record =>
                record.IsActive &&
                record.PayFrequency == normalizedFrequency &&
                record.EffectiveStartDate <= effectiveDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= effectiveDate))
            .OrderByDescending(record => record.EffectiveStartDate)
            .ThenByDescending(record => record.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<decimal> CalculateEmployerContributionTotalAsync(
        IReadOnlyList<ContributionType> contributionTypes,
        DateOnly effectiveDate,
        decimal grossPay,
        PayrollSettingsDto settings,
        CancellationToken cancellationToken)
    {
        var total = 0m;
        foreach (var contributionType in contributionTypes)
        {
            var table = await GetApplicableContributionTableAsync(contributionType.Id, effectiveDate, cancellationToken);
            if (table is null)
            {
                continue;
            }

            var bracket = table.Brackets
                .OrderBy(item => item.MinCompensation)
                .FirstOrDefault(item => grossPay >= item.MinCompensation && (!item.MaxCompensation.HasValue || grossPay <= item.MaxCompensation.Value));

            if (bracket is null)
            {
                continue;
            }

            total += ResolveBracketAmount(grossPay, bracket.EmployerShareAmount, bracket.EmployerShareRate);
        }

        return RoundMoney(total, settings);
    }

    private async Task<EarningType> GetOrCreateSystemEarningTypeAsync(string code, string name, string category, bool taxable, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var normalizedName = name.Trim();
        var record = await _dbContext.EarningTypes.SingleOrDefaultAsync(item => item.Code == normalizedCode, cancellationToken);
        if (record is not null)
        {
            return record;
        }

        record = await _dbContext.EarningTypes.SingleOrDefaultAsync(item => item.Name == normalizedName, cancellationToken);
        if (record is not null)
        {
            record.Category = category;
            record.Taxable = taxable;
            record.IsActive = true;
            record.UpdatedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return record;
        }

        record = new EarningType
        {
            Code = normalizedCode,
            Name = normalizedName,
            Description = $"System-created earning type for {name}.",
            Category = category,
            Taxable = taxable,
            Recurring = false,
            AffectsThirteenthMonth = category == EarningTypeCategories.Basic,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.EarningTypes.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    private async Task<DeductionType> GetOrCreateSystemDeductionTypeAsync(string code, string name, string category, bool preTax, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var normalizedName = name.Trim();
        var record = await _dbContext.DeductionTypes.SingleOrDefaultAsync(item => item.Code == normalizedCode, cancellationToken);
        if (record is not null)
        {
            return record;
        }

        record = await _dbContext.DeductionTypes.SingleOrDefaultAsync(item => item.Name == normalizedName, cancellationToken);
        if (record is not null)
        {
            record.Category = category;
            record.PreTax = preTax;
            record.IsActive = true;
            record.UpdatedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return record;
        }

        record = new DeductionType
        {
            Code = normalizedCode,
            Name = normalizedName,
            Description = $"System-created deduction type for {name}.",
            Category = category,
            PreTax = preTax,
            Recurring = false,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.DeductionTypes.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    private async Task RemoveRunItemsAsync(Guid payrollRunId, CancellationToken cancellationToken)
    {
        var items = await _dbContext.PayrollRunItems
            .Include(item => item.EarningLines)
            .Include(item => item.DeductionLines)
            .Where(item => item.PayrollRunId == payrollRunId)
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return;
        }

        _dbContext.PayrollEarningLines.RemoveRange(items.SelectMany(item => item.EarningLines));
        _dbContext.PayrollDeductionLines.RemoveRange(items.SelectMany(item => item.DeductionLines));
        _dbContext.PayrollRunItems.RemoveRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ReleaseAssignedAdjustmentsAsync(Guid payrollRunId, CancellationToken cancellationToken)
    {
        var adjustments = await _dbContext.PayrollAdjustments
            .Where(record => record.PayrollRunId == payrollRunId && record.Status == PayrollAdjustmentStatuses.Approved)
            .ToListAsync(cancellationToken);

        foreach (var adjustment in adjustments)
        {
            adjustment.PayrollRunId = null;
            adjustment.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (adjustments.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ApplyPaidRunAdjustmentsAsync(PayrollRun run, CancellationToken cancellationToken)
    {
        var adjustmentIds = await _dbContext.PayrollEarningLines
            .Where(line => line.PayrollRunItem != null && line.PayrollRunItem.PayrollRunId == run.Id && line.PayrollAdjustmentId != null)
            .Select(line => line.PayrollAdjustmentId!.Value)
            .Union(
                _dbContext.PayrollDeductionLines
                    .Where(line => line.PayrollRunItem != null && line.PayrollRunItem.PayrollRunId == run.Id && line.PayrollAdjustmentId != null)
                    .Select(line => line.PayrollAdjustmentId!.Value))
            .Distinct()
            .ToListAsync(cancellationToken);

        if (adjustmentIds.Count == 0)
        {
            return;
        }

        var adjustments = await _dbContext.PayrollAdjustments
            .Where(record => adjustmentIds.Contains(record.Id))
            .ToListAsync(cancellationToken);

        foreach (var adjustment in adjustments)
        {
            adjustment.Status = PayrollAdjustmentStatuses.Applied;
            adjustment.PayrollRunId = run.Id;
            adjustment.AppliedAtUtc = DateTime.UtcNow;
            adjustment.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private async Task ApplyPaidRunRecurringBalancesAsync(PayrollRun run, CancellationToken cancellationToken)
    {
        var recurringDeductions = await _dbContext.PayrollDeductionLines
            .Where(line =>
                line.PayrollRunItem != null &&
                line.PayrollRunItem.PayrollRunId == run.Id &&
                line.EmployeeRecurringDeductionId != null)
            .Select(line => new
            {
                line.EmployeeRecurringDeductionId,
                line.Amount
            })
            .ToListAsync(cancellationToken);

        if (recurringDeductions.Count == 0)
        {
            return;
        }

        var recurringIds = recurringDeductions
            .Where(item => item.EmployeeRecurringDeductionId.HasValue)
            .Select(item => item.EmployeeRecurringDeductionId!.Value)
            .Distinct()
            .ToList();

        var recurringRecords = await _dbContext.EmployeeRecurringDeductions
            .Where(record => recurringIds.Contains(record.Id))
            .ToDictionaryAsync(record => record.Id, cancellationToken);

        foreach (var applied in recurringDeductions)
        {
            if (!applied.EmployeeRecurringDeductionId.HasValue)
            {
                continue;
            }

            if (!recurringRecords.TryGetValue(applied.EmployeeRecurringDeductionId.Value, out var recurringRecord) || recurringRecord.Balance is null)
            {
                continue;
            }

            recurringRecord.Balance = Math.Max(0m, recurringRecord.Balance.Value - applied.Amount);
            recurringRecord.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private async Task ValidateAdjustmentAsync(SavePayrollAdjustmentRequestDto request, CancellationToken cancellationToken)
    {
        _ = await _dbContext.Employees.SingleOrDefaultAsync(record => record.Id == request.EmployeeId!.Value, cancellationToken)
            ?? throw new BadRequestException("The selected employee does not exist.");

        if (!PayrollAdjustmentTypes.All.Contains(request.AdjustmentType.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Payroll adjustment type must be earning or deduction.");
        }

        if (string.Equals(request.AdjustmentType, PayrollAdjustmentTypes.Earning, StringComparison.OrdinalIgnoreCase))
        {
            if (request.EarningTypeId is null)
            {
                throw BuildValidationException("An earning type is required for earning adjustments.", nameof(SavePayrollAdjustmentRequestDto.EarningTypeId));
            }

            _ = await _dbContext.EarningTypes.SingleOrDefaultAsync(record => record.Id == request.EarningTypeId.Value, cancellationToken)
                ?? throw new BadRequestException("The selected earning type does not exist.");
        }
        else
        {
            if (request.DeductionTypeId is null)
            {
                throw BuildValidationException("A deduction type is required for deduction adjustments.", nameof(SavePayrollAdjustmentRequestDto.DeductionTypeId));
            }

            _ = await _dbContext.DeductionTypes.SingleOrDefaultAsync(record => record.Id == request.DeductionTypeId.Value, cancellationToken)
                ?? throw new BadRequestException("The selected deduction type does not exist.");
        }

        if (request.PayPeriodId is not null)
        {
            _ = await _dbContext.PayPeriods.SingleOrDefaultAsync(record => record.Id == request.PayPeriodId.Value, cancellationToken)
                ?? throw new BadRequestException("The selected pay period does not exist.");
        }

        if (request.PayrollRunId is not null)
        {
            _ = await _dbContext.PayrollRuns.SingleOrDefaultAsync(record => record.Id == request.PayrollRunId.Value, cancellationToken)
                ?? throw new BadRequestException("The selected payroll run does not exist.");
        }
    }

    private async Task<PayrollAdjustmentRecordDto> GetAdjustmentByIdAsync(Guid payrollAdjustmentId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.PayrollAdjustments
            .AsNoTracking()
            .Include(item => item.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(item => item.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(item => item.PayPeriod)
            .Include(item => item.PayrollRun)
            .Include(item => item.EarningType)
            .Include(item => item.DeductionType)
            .Include(item => item.RequestedByUser)
            .Include(item => item.ApprovedByUser)
            .SingleOrDefaultAsync(item => item.Id == payrollAdjustmentId, cancellationToken)
            ?? throw new NotFoundException($"Payroll adjustment '{payrollAdjustmentId}' was not found.");

        return MapAdjustment(record);
    }

    private async Task<List<PayrollRunItem>> FilterPayrollRunItemsForReportsAsync(PayrollReportQueryDto query, CancellationToken cancellationToken)
    {
        var source = _dbContext.PayrollRunItems
            .AsNoTracking()
            .Include(item => item.PayrollRun)
                .ThenInclude(run => run!.PayPeriod)
            .Include(item => item.Employee)
            .Include(item => item.EarningLines)
            .Include(item => item.DeductionLines)
            .AsQueryable();

        if (query.PayPeriodId is not null)
        {
            source = source.Where(item => item.PayrollRun != null && item.PayrollRun.PayPeriodId == query.PayPeriodId.Value);
        }

        if (query.PayrollRunId is not null)
        {
            source = source.Where(item => item.PayrollRunId == query.PayrollRunId.Value);
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(item => item.EmployeeId == query.EmployeeId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(item => item.Employee != null && item.Employee.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(item => item.Employee != null && item.Employee.BranchId == query.BranchId.Value);
        }

        if (query.EmploymentTypeId is not null)
        {
            source = source.Where(item => item.Employee != null && item.Employee.EmploymentTypeId == query.EmploymentTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(item => item.PayrollRun != null && item.PayrollRun.Status == query.Status.Trim().ToLowerInvariant());
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(item => item.PayrollRun != null && item.PayrollRun.PayPeriod != null && item.PayrollRun.PayPeriod.PeriodEndDate >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(item => item.PayrollRun != null && item.PayrollRun.PayPeriod != null && item.PayrollRun.PayPeriod.PeriodStartDate <= query.DateTo.Value);
        }

        return await source
            .OrderBy(item => item.EmployeeNameSnapshot)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<PayrollAdjustment>> FilterPayrollAdjustmentsForReportsAsync(PayrollReportQueryDto query, CancellationToken cancellationToken)
    {
        var source = _dbContext.PayrollAdjustments
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.PayPeriod)
            .Include(record => record.PayrollRun)
            .Include(record => record.EarningType)
            .Include(record => record.DeductionType)
            .Include(record => record.RequestedByUser)
            .Include(record => record.ApprovedByUser)
            .AsQueryable();

        if (query.PayPeriodId is not null)
        {
            source = source.Where(record => record.PayPeriodId == query.PayPeriodId.Value);
        }

        if (query.PayrollRunId is not null)
        {
            source = source.Where(record => record.PayrollRunId == query.PayrollRunId.Value);
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.BranchId == query.BranchId.Value);
        }

        if (query.DateFrom is not null)
        {
            var fromDate = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            source = source.Where(record => record.CreatedAtUtc >= fromDate);
        }

        if (query.DateTo is not null)
        {
            var toDateExclusive = query.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            source = source.Where(record => record.CreatedAtUtc < toDateExclusive);
        }

        return await source
            .OrderByDescending(record => record.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    private async Task LogAuditAsync(Guid? payrollRunId, Guid? payrollRunItemId, string entityType, string entityId, string action, string summary, string? actorUserId, CancellationToken cancellationToken)
    {
        _dbContext.PayrollAuditLogs.Add(new PayrollAuditLog
        {
            PayrollRunId = payrollRunId,
            PayrollRunItemId = payrollRunItemId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Summary = summary,
            ActorUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow
        });

        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static string NormalizePayPeriodStatus(string status)
    {
        var normalized = string.IsNullOrWhiteSpace(status) ? PayPeriodStatuses.Open : status.Trim().ToLowerInvariant();
        if (!PayPeriodStatuses.All.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Invalid pay period status.");
        }

        return normalized;
    }

    private static void ValidatePayFrequency(string payFrequency)
    {
        if (!PayrollPayFrequencies.Standard.Contains(payFrequency.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Pay frequency must be weekly, semi_monthly, monthly, or custom.");
        }
    }

    private static void EnsureRunEditable(PayrollRun run)
    {
        if (run.Status == PayrollRunStatuses.Approved || run.Status == PayrollRunStatuses.Paid || run.Status == PayrollRunStatuses.Cancelled)
        {
            throw new ConflictException("Approved, paid, or cancelled payroll runs cannot be recalculated.");
        }
    }

    private static decimal ResolveDailyRate(CompensationProfile? compensationProfile, PayrollSettingsDto settings)
    {
        if (compensationProfile?.DailyRate is > 0m)
        {
            return compensationProfile.DailyRate.Value;
        }

        if (compensationProfile?.BasicSalary is > 0m)
        {
            return compensationProfile.BasicSalary / settings.DefaultWorkingDaysPerMonth;
        }

        if (compensationProfile?.HourlyRate is > 0m)
        {
            return compensationProfile.HourlyRate.Value * settings.DefaultWorkingHoursPerDay;
        }

        return 0m;
    }

    private static decimal ResolveHourlyRate(CompensationProfile? compensationProfile, PayrollSettingsDto settings, decimal dailyRate)
    {
        if (compensationProfile?.HourlyRate is > 0m)
        {
            return compensationProfile.HourlyRate.Value;
        }

        if (compensationProfile?.DailyRate is > 0m)
        {
            return compensationProfile.DailyRate.Value / settings.DefaultWorkingHoursPerDay;
        }

        if (dailyRate > 0m)
        {
            return dailyRate / settings.DefaultWorkingHoursPerDay;
        }

        return 0m;
    }

    private static decimal ConvertAmountForPayFrequency(decimal amount, string sourceFrequency, string targetFrequency)
    {
        var normalizedSource = sourceFrequency.Trim().ToLowerInvariant();
        var normalizedTarget = targetFrequency.Trim().ToLowerInvariant();

        if (normalizedSource == normalizedTarget || normalizedSource == PayrollPayFrequencies.EveryPayroll || normalizedSource == PayrollPayFrequencies.Custom)
        {
            return amount;
        }

        return (normalizedSource, normalizedTarget) switch
        {
            (PayrollPayFrequencies.Monthly, PayrollPayFrequencies.SemiMonthly) => amount / 2m,
            (PayrollPayFrequencies.Monthly, PayrollPayFrequencies.Weekly) => amount * 12m / 52m,
            (PayrollPayFrequencies.SemiMonthly, PayrollPayFrequencies.Monthly) => amount * 2m,
            (PayrollPayFrequencies.SemiMonthly, PayrollPayFrequencies.Weekly) => amount * 24m / 52m,
            (PayrollPayFrequencies.Weekly, PayrollPayFrequencies.Monthly) => amount * 52m / 12m,
            (PayrollPayFrequencies.Weekly, PayrollPayFrequencies.SemiMonthly) => amount * 52m / 24m,
            _ => amount
        };
    }

    private static decimal ResolveBracketAmount(decimal baseAmount, decimal? fixedAmount, decimal? rate)
    {
        if (fixedAmount is > 0m)
        {
            return fixedAmount.Value;
        }

        return rate is > 0m ? baseAmount * rate.Value : 0m;
    }

    private static PayrollEarningLine CreateEarningLine(EarningType type, string description, decimal amount, decimal? quantity, decimal? rate, string source, bool taxable)
    {
        return new PayrollEarningLine
        {
            EarningTypeId = type.Id,
            EarningTypeCodeSnapshot = type.Code,
            EarningTypeNameSnapshot = type.Name,
            Description = description,
            Amount = amount,
            Quantity = quantity,
            Rate = rate,
            Source = source,
            Taxable = taxable,
            IsManual = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static PayrollDeductionLine CreateDeductionLine(DeductionType type, string description, decimal amount, string source)
    {
        return new PayrollDeductionLine
        {
            DeductionTypeId = type.Id,
            DeductionTypeCodeSnapshot = type.Code,
            DeductionTypeNameSnapshot = type.Name,
            DeductionCategorySnapshot = type.Category,
            Description = description,
            Amount = amount,
            Source = source,
            PreTax = type.PreTax,
            IsManual = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static decimal ResolveAttendanceFraction(AttendanceRecord attendance, ResolvedAttendanceSchedule resolvedSchedule)
    {
        if (string.Equals(attendance.Status, AttendanceStatuses.HalfDay, StringComparison.OrdinalIgnoreCase))
        {
            return 0.5m;
        }

        if (string.Equals(attendance.Status, AttendanceStatuses.Incomplete, StringComparison.OrdinalIgnoreCase))
        {
            if (attendance.TotalWorkedMinutes > 0 && resolvedSchedule.RequiredWorkingMinutes > 0)
            {
                return Math.Min(1m, Math.Round(attendance.TotalWorkedMinutes / (decimal)resolvedSchedule.RequiredWorkingMinutes, 2));
            }

            return attendance.ActualTimeIn is not null ? 0.5m : 0m;
        }

        if (string.Equals(attendance.Status, AttendanceStatuses.Absent, StringComparison.OrdinalIgnoreCase))
        {
            return 0m;
        }

        if (attendance.ActualTimeIn is not null || attendance.ActualTimeOut is not null)
        {
            return 1m;
        }

        return 0m;
    }

    private static decimal ResolveLeaveFraction(DateOnly date, DateOnly startDate, DateOnly endDate, string startDayType, string endDayType)
    {
        var normalizedStart = startDayType.Trim().ToLowerInvariant();
        var normalizedEnd = endDayType.Trim().ToLowerInvariant();

        if (startDate == endDate)
        {
            return (normalizedStart, normalizedEnd) switch
            {
                (LeaveDayTypes.FullDay, LeaveDayTypes.FullDay) => 1m,
                (LeaveDayTypes.FirstHalf, LeaveDayTypes.FirstHalf) => 0.5m,
                (LeaveDayTypes.SecondHalf, LeaveDayTypes.SecondHalf) => 0.5m,
                (LeaveDayTypes.FirstHalf, LeaveDayTypes.SecondHalf) => 1m,
                _ => 0m
            };
        }

        if (date == startDate)
        {
            return normalizedStart == LeaveDayTypes.FullDay ? 1m : 0.5m;
        }

        if (date == endDate)
        {
            return normalizedEnd == LeaveDayTypes.FullDay ? 1m : 0.5m;
        }

        return 1m;
    }

    private static decimal RoundMoney(decimal amount, PayrollSettingsDto settings)
    {
        var normalizedRule = settings.RoundingRule.Trim().ToLowerInvariant();
        return normalizedRule switch
        {
            "round_0" => Math.Round(amount, 0, MidpointRounding.AwayFromZero),
            "truncate_2" => Math.Truncate(amount * 100m) / 100m,
            _ => Math.Round(amount, 2, MidpointRounding.AwayFromZero)
        };
    }

    private static decimal RoundDecimal(decimal amount)
    {
        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    private static string BuildFullName(string firstName, string middleName, string lastName, string suffix)
    {
        return string.Join(
            " ",
            new[] { firstName.Trim(), middleName.Trim(), lastName.Trim(), suffix.Trim() }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string BuildUserDisplayName(ApplicationUser? user)
    {
        return user is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.Email ?? string.Empty
                : user.DisplayName;
    }

    private static string? NormalizeUserId(string? userId)
    {
        return string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
    }

    private static IReadOnlyList<string> SplitIssues(string issueSummary)
    {
        return string.IsNullOrWhiteSpace(issueSummary)
            ? Array.Empty<string>()
            : issueSummary
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
    }

    private static PayPeriodRecordDto MapPayPeriod(PayPeriod record)
    {
        return new PayPeriodRecordDto
        {
            Id = record.Id,
            Code = record.Code,
            Name = record.Name,
            PayFrequency = record.PayFrequency,
            PeriodStartDate = record.PeriodStartDate,
            PeriodEndDate = record.PeriodEndDate,
            PayrollDate = record.PayrollDate,
            CutoffStartDate = record.CutoffStartDate,
            CutoffEndDate = record.CutoffEndDate,
            Status = record.Status,
            Remarks = record.Remarks,
            PayPeriodTemplateId = record.PayPeriodTemplateId,
            PayPeriodTemplateName = record.PayPeriodTemplate?.Name ?? string.Empty,
            PayrollRunCount = record.PayrollRuns.Count,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static PayrollRunSummaryDto MapPayrollRunSummary(PayrollRun record)
    {
        return new PayrollRunSummaryDto
        {
            Id = record.Id,
            PayPeriodId = record.PayPeriodId,
            PayPeriodCode = record.PayPeriod?.Code ?? string.Empty,
            PayPeriodName = record.PayPeriod?.Name ?? string.Empty,
            ReferenceNumber = record.ReferenceNumber,
            Name = record.Name,
            Status = record.Status,
            EmployeeCount = record.Items.Count,
            HoldCount = record.Items.Count(item => item.Status == PayrollItemStatuses.Held),
            CriticalIssueCount = record.Items.Count(item => item.HasCriticalIssues),
            TotalGrossPay = record.Items.Sum(item => item.GrossPay),
            TotalDeductions = record.Items.Sum(item => item.TotalDeductions),
            TotalNetPay = record.Items.Sum(item => item.NetPay),
            GeneratedByDisplayName = BuildUserDisplayName(record.GeneratedByUser),
            GeneratedAtUtc = record.GeneratedAtUtc,
            ApprovedByDisplayName = BuildUserDisplayName(record.ApprovedByUser),
            ApprovedAtUtc = record.ApprovedAtUtc,
            PaidAtUtc = record.PaidAtUtc,
            Remarks = record.Remarks,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static PayrollRunItemDto MapPayrollRunItem(PayrollRunItem record)
    {
        return new PayrollRunItemDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = record.EmployeeCodeSnapshot,
            EmployeeName = record.EmployeeNameSnapshot,
            DepartmentName = record.DepartmentSnapshot,
            PositionName = record.PositionSnapshot,
            BranchName = record.BranchSnapshot,
            PayType = record.PayTypeSnapshot,
            Currency = record.CurrencySnapshot,
            BasicSalary = record.BasicSalarySnapshot,
            DailyRate = record.DailyRateSnapshot,
            HourlyRate = record.HourlyRateSnapshot,
            RegularWorkedDays = record.RegularWorkedDays,
            RegularWorkedHours = record.RegularWorkedHours,
            PaidLeaveDays = record.PaidLeaveDays,
            UnpaidLeaveDays = record.UnpaidLeaveDays,
            AbsentDays = record.AbsentDays,
            LateMinutes = record.LateMinutes,
            UndertimeMinutes = record.UndertimeMinutes,
            OvertimeMinutes = record.OvertimeMinutes,
            BasicPay = record.BasicPay,
            AllowanceTotal = record.AllowanceTotal,
            OvertimePay = record.OvertimePay,
            HolidayPay = record.HolidayPay,
            LeavePay = record.LeavePay,
            BonusTotal = record.BonusTotal,
            OtherEarningsTotal = record.OtherEarningsTotal,
            GrossPay = record.GrossPay,
            GovernmentDeductionsTotal = record.GovernmentDeductionsTotal,
            TaxDeduction = record.TaxDeduction,
            AbsenceDeduction = record.AbsenceDeduction,
            LateDeduction = record.LateDeduction,
            UndertimeDeduction = record.UndertimeDeduction,
            LoanDeduction = record.LoanDeduction,
            OtherDeductionsTotal = record.OtherDeductionsTotal,
            TotalDeductions = record.TotalDeductions,
            NetPay = record.NetPay,
            EmployerContributionTotal = record.EmployerContributionTotal,
            Status = record.Status,
            Remarks = record.Remarks,
            HasCriticalIssues = record.HasCriticalIssues,
            Issues = SplitIssues(record.IssueSummary),
            Earnings = record.EarningLines.OrderBy(line => line.EarningTypeNameSnapshot).Select(MapEarningLine).ToList(),
            Deductions = record.DeductionLines.OrderBy(line => line.DeductionTypeNameSnapshot).Select(MapDeductionLine).ToList(),
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static PayrollEarningLineDto MapEarningLine(PayrollEarningLine record)
    {
        return new PayrollEarningLineDto
        {
            Id = record.Id,
            EarningTypeId = record.EarningTypeId,
            EarningTypeCode = record.EarningTypeCodeSnapshot,
            EarningTypeName = record.EarningTypeNameSnapshot,
            Description = record.Description,
            Amount = record.Amount,
            Quantity = record.Quantity,
            Rate = record.Rate,
            Source = record.Source,
            Taxable = record.Taxable,
            IsManual = record.IsManual,
            Remarks = record.Remarks
        };
    }

    private static PayrollDeductionLineDto MapDeductionLine(PayrollDeductionLine record)
    {
        return new PayrollDeductionLineDto
        {
            Id = record.Id,
            DeductionTypeId = record.DeductionTypeId,
            DeductionTypeCode = record.DeductionTypeCodeSnapshot,
            DeductionTypeName = record.DeductionTypeNameSnapshot,
            DeductionCategory = record.DeductionCategorySnapshot,
            Description = record.Description,
            Amount = record.Amount,
            Source = record.Source,
            PreTax = record.PreTax,
            IsManual = record.IsManual,
            Remarks = record.Remarks
        };
    }

    private static PayrollAdjustmentRecordDto MapAdjustment(PayrollAdjustment record)
    {
        var employee = record.Employee ?? throw new NotFoundException("The employee linked to this payroll adjustment could not be found.");

        return new PayrollAdjustmentRecordDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            DepartmentName = employee.Department?.Name ?? string.Empty,
            BranchName = employee.Branch?.Name ?? string.Empty,
            PayPeriodId = record.PayPeriodId,
            PayPeriodName = record.PayPeriod?.Name ?? string.Empty,
            PayrollRunId = record.PayrollRunId,
            PayrollRunReferenceNumber = record.PayrollRun?.ReferenceNumber ?? string.Empty,
            AdjustmentType = record.AdjustmentType,
            EarningTypeId = record.EarningTypeId,
            EarningTypeName = record.EarningType?.Name ?? string.Empty,
            DeductionTypeId = record.DeductionTypeId,
            DeductionTypeName = record.DeductionType?.Name ?? string.Empty,
            Amount = record.Amount,
            Reason = record.Reason,
            Status = record.Status,
            RequestedByDisplayName = BuildUserDisplayName(record.RequestedByUser),
            ApprovedByDisplayName = BuildUserDisplayName(record.ApprovedByUser),
            ApprovedAtUtc = record.ApprovedAtUtc,
            AppliedAtUtc = record.AppliedAtUtc,
            DecisionRemarks = record.DecisionRemarks,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static PayrollAuditLogDto MapAuditLog(PayrollAuditLog record)
    {
        return new PayrollAuditLogDto
        {
            Id = record.Id,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            Action = record.Action,
            Summary = record.Summary,
            ActorDisplayName = BuildUserDisplayName(record.ActorUser),
            CreatedAtUtc = record.CreatedAtUtc
        };
    }

    private static PagedResultDto<T> ToPage<T>(IReadOnlyList<T> items, int pageNumber, int pageSize, int totalCount)
    {
        return new PagedResultDto<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static BadRequestException BuildValidationException(string message, string fieldName)
    {
        return new BadRequestException(message, new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }

    private sealed record ResolvedPayPeriodDates(
        DateOnly PeriodStartDate,
        DateOnly PeriodEndDate,
        DateOnly CutoffStartDate,
        DateOnly CutoffEndDate,
        DateOnly PayrollDate);

    private readonly record struct LeaveAllocation(DateOnly Date, decimal PaidDays, decimal UnpaidDays)
    {
        public decimal TotalDays => PaidDays + UnpaidDays;
    }

    private sealed class LinkedHashSet<T> : IEnumerable<T>
    {
        private readonly HashSet<T> _seen = new();
        private readonly List<T> _ordered = [];

        public int Count => _ordered.Count;

        public void Add(T value)
        {
            if (_seen.Add(value))
            {
                _ordered.Add(value);
            }
        }

        public IEnumerator<T> GetEnumerator() => _ordered.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
