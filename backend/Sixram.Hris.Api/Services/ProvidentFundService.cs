using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.ProvidentFund;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IProvidentFundService
{
    Task<ProvidentFundOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);

    Task<ProvidentFundDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<PagedResultDto<ProvidentFundPolicyRecordDto>> GetPoliciesAsync(ProvidentFundPolicyListQueryDto query, CancellationToken cancellationToken = default);

    Task<ProvidentFundPolicyRecordDto> CreatePolicyAsync(SaveProvidentFundPolicyRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundPolicyRecordDto> UpdatePolicyAsync(Guid policyId, SaveProvidentFundPolicyRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ProvidentFundVestingRuleDto>> GetVestingRulesAsync(ProvidentFundVestingRuleListQueryDto query, CancellationToken cancellationToken = default);

    Task<ProvidentFundVestingRuleDto> CreateVestingRuleAsync(SaveProvidentFundVestingRuleRequestDto request, CancellationToken cancellationToken = default);

    Task<ProvidentFundVestingRuleDto> UpdateVestingRuleAsync(Guid vestingRuleId, SaveProvidentFundVestingRuleRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteVestingRuleAsync(Guid vestingRuleId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ProvidentFundEnrollmentRecordDto>> GetEnrollmentsAsync(ProvidentFundEnrollmentListQueryDto query, CancellationToken cancellationToken = default);

    Task<ProvidentFundEnrollmentRecordDto> CreateEnrollmentAsync(SaveProvidentFundEnrollmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundEnrollmentRecordDto> UpdateEnrollmentAsync(Guid enrollmentId, SaveProvidentFundEnrollmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ProvidentFundContributionBatchSummaryDto>> GetContributionBatchesAsync(ProvidentFundContributionBatchListQueryDto query, CancellationToken cancellationToken = default);

    Task<ProvidentFundContributionBatchDetailDto> GetContributionBatchByIdAsync(Guid batchId, CancellationToken cancellationToken = default);

    Task<ProvidentFundContributionBatchDetailDto> GenerateContributionBatchAsync(GenerateProvidentFundContributionBatchRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundContributionBatchDetailDto> ReviewContributionBatchAsync(Guid batchId, ProvidentFundContributionBatchActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundContributionBatchDetailDto> PostContributionBatchAsync(Guid batchId, ProvidentFundContributionBatchActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundContributionBatchDetailDto> CancelContributionBatchAsync(Guid batchId, ProvidentFundContributionBatchActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ProvidentFundLedgerTransactionDto>> GetLedgerAsync(ProvidentFundLedgerListQueryDto query, CancellationToken cancellationToken = default);

    Task<ProvidentFundLedgerTransactionDto> ReverseLedgerTransactionAsync(Guid ledgerTransactionId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundBalanceDto> GetEmployeeBalanceAsync(Guid employeeId, DateOnly? asOfDate = null, CancellationToken cancellationToken = default);

    Task<ProvidentFundBalanceDto> GetMyBalanceAsync(string? actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ProvidentFundWithdrawalRequestDto>> GetWithdrawalsAsync(ProvidentFundWithdrawalListQueryDto query, CancellationToken cancellationToken = default);

    Task<ProvidentFundWithdrawalRequestDto> CreateWithdrawalAsync(SaveProvidentFundWithdrawalRequestDto request, string? actorUserId, bool ownOnly, CancellationToken cancellationToken = default);

    Task<ProvidentFundWithdrawalRequestDto> SubmitWithdrawalAsync(Guid withdrawalId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundWithdrawalRequestDto> ApproveWithdrawalAsync(Guid withdrawalId, ProvidentFundWithdrawalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundWithdrawalRequestDto> RejectWithdrawalAsync(Guid withdrawalId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundWithdrawalRequestDto> MarkWithdrawalPaidAsync(Guid withdrawalId, ProvidentFundWithdrawalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ProvidentFundAdjustmentDto>> GetAdjustmentsAsync(ProvidentFundAdjustmentListQueryDto query, CancellationToken cancellationToken = default);

    Task<ProvidentFundAdjustmentDto> CreateAdjustmentAsync(SaveProvidentFundAdjustmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundAdjustmentDto> ApproveAdjustmentAsync(Guid adjustmentId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundAdjustmentDto> RejectAdjustmentAsync(Guid adjustmentId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<ProvidentFundAdjustmentDto> PostAdjustmentAsync(Guid adjustmentId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProvidentFundContributionReportRowDto>> GetContributionReportAsync(ProvidentFundReportQueryDto query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProvidentFundBalanceReportRowDto>> GetBalanceReportAsync(ProvidentFundReportQueryDto query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProvidentFundWithdrawalReportRowDto>> GetWithdrawalReportAsync(ProvidentFundReportQueryDto query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProvidentFundLedgerTransactionDto>> GetLedgerReportAsync(ProvidentFundReportQueryDto query, CancellationToken cancellationToken = default);
}

public class ProvidentFundService : IProvidentFundService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IUserAccessService _userAccessService;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<ProvidentFundService> _logger;

    public ProvidentFundService(
        ApplicationDbContext dbContext,
        IUserAccessService userAccessService,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        ILogger<ProvidentFundService> logger)
    {
        _dbContext = dbContext;
        _userAccessService = userAccessService;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<ProvidentFundOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        var employees = await _dbContext.Employees
            .AsNoTracking()
            .Include(record => record.Department)
            .Include(record => record.Branch)
            .Where(record => record.IsActive)
            .OrderBy(record => record.LastName)
            .ThenBy(record => record.FirstName)
            .Select(record => new EmployeeAttendanceOptionDto
            {
                Id = record.Id,
                EmployeeCode = record.EmployeeCode,
                FullName = BuildFullName(record.FirstName, record.MiddleName, record.LastName, record.Suffix),
                DepartmentName = record.Department != null ? record.Department.Name : string.Empty,
                BranchName = record.Branch != null ? record.Branch.Name : string.Empty,
                IsActive = record.IsActive
            })
            .ToListAsync(cancellationToken);

        var departments = await _dbContext.Departments
            .AsNoTracking()
            .OrderBy(record => record.Name)
            .Select(record => new LookupOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                IsActive = record.IsActive
            })
            .ToListAsync(cancellationToken);

        var policies = await _dbContext.ProvidentFundPolicies
            .AsNoTracking()
            .OrderBy(record => record.PolicyName)
            .Select(record => new ProvidentFundPolicyOptionDto
            {
                Id = record.Id,
                PolicyName = record.PolicyName,
                Status = record.Status,
                AllowVoluntaryContribution = record.AllowVoluntaryContribution,
                AllowWithdrawal = record.AllowWithdrawal
            })
            .ToListAsync(cancellationToken);

        return new ProvidentFundOptionsDto
        {
            Employees = employees,
            Departments = departments,
            Policies = policies,
            ContributionTypes = ProvidentFundContributionTypes.All,
            PolicyStatuses = ProvidentFundPolicyStatuses.All,
            EnrollmentStatuses = ProvidentFundEnrollmentStatuses.All,
            BatchStatuses = ProvidentFundContributionBatchStatuses.All,
            LedgerTransactionTypes = ProvidentFundLedgerTransactionTypes.All,
            WithdrawalStatuses = ProvidentFundWithdrawalStatuses.All,
            WithdrawalTypes = ProvidentFundWithdrawalTypes.All,
            AdjustmentStatuses = ProvidentFundAdjustmentStatuses.All,
            AdjustmentTypes = ProvidentFundAdjustmentTypes.All,
            ShareTypes = ProvidentFundShareTypes.All,
            Permissions = ProvidentFundPermissions.All
        };
    }

    public async Task<ProvidentFundDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var nextMonthStart = monthStart.AddMonths(1);

        var ledger = _dbContext.ProvidentFundLedgerTransactions.AsNoTracking();
        var grossBalance = await ledger.SumAsync(record => record.CreditAmount - record.DebitAmount, cancellationToken);
        var totalEmployee = await ledger
            .Where(record => record.TransactionType == ProvidentFundLedgerTransactionTypes.EmployeeContribution)
            .SumAsync(record => record.EmployeeShareAmount, cancellationToken);
        var totalEmployer = await ledger
            .Where(record => record.TransactionType == ProvidentFundLedgerTransactionTypes.EmployerContribution)
            .SumAsync(record => record.EmployerShareAmount, cancellationToken);
        var withdrawalsThisMonth = await ledger
            .Where(record =>
                (record.TransactionType == ProvidentFundLedgerTransactionTypes.Withdrawal ||
                 record.TransactionType == ProvidentFundLedgerTransactionTypes.FinalSettlement) &&
                record.TransactionDate >= monthStart &&
                record.TransactionDate < nextMonthStart)
            .SumAsync(record => record.DebitAmount, cancellationToken);

        var activeEmployeeCount = await _dbContext.Employees.CountAsync(record => record.IsActive, cancellationToken);
        var activeEnrolledEmployeeCount = await _dbContext.ProvidentFundEnrollments
            .Where(record => record.Status == ProvidentFundEnrollmentStatuses.Active)
            .Select(record => record.EmployeeId)
            .Distinct()
            .CountAsync(cancellationToken);

        var currentBatch = await _dbContext.ProvidentFundContributionBatches
            .AsNoTracking()
            .Where(record => record.Month == today.Month && record.Year == today.Year && record.Status != ProvidentFundContributionBatchStatuses.Cancelled)
            .OrderByDescending(record => record.CreatedAtUtc)
            .Select(record => record.Status)
            .FirstOrDefaultAsync(cancellationToken);

        var trendStart = monthStart.AddMonths(-5);
        var trendRows = await ledger
            .Where(record => record.TransactionDate >= trendStart)
            .Select(record => new
            {
                record.TransactionDate,
                Amount = record.CreditAmount - record.DebitAmount
            })
            .ToListAsync(cancellationToken);

        var trend = Enumerable.Range(0, 6)
            .Select(offset => trendStart.AddMonths(offset))
            .Select(period => new ProvidentFundBalanceTrendPointDto
            {
                Period = $"{period:yyyy-MM}",
                Balance = trendRows
                    .Where(row => row.TransactionDate < period.AddMonths(1))
                    .Sum(row => row.Amount)
            })
            .ToList();

        return new ProvidentFundDashboardDto
        {
            TotalFundValue = grossBalance,
            TotalEmployeeContributions = totalEmployee,
            TotalEmployerContributions = totalEmployer,
            PendingWithdrawalRequestCount = await _dbContext.ProvidentFundWithdrawalRequests.CountAsync(
                record =>
                    record.Status == ProvidentFundWithdrawalStatuses.Submitted ||
                    record.Status == ProvidentFundWithdrawalStatuses.HrReviewed ||
                    record.Status == ProvidentFundWithdrawalStatuses.FinanceReviewed ||
                    record.Status == ProvidentFundWithdrawalStatuses.Approved,
                cancellationToken),
            CurrentMonthContributionStatus = currentBatch ?? "not_started",
            EmployeesEnrolled = activeEnrolledEmployeeCount,
            EmployeesNotEnrolled = Math.Max(activeEmployeeCount - activeEnrolledEmployeeCount, 0),
            TotalWithdrawalsThisMonth = withdrawalsThisMonth,
            FundBalanceTrend = trend
        };
    }

    public async Task<PagedResultDto<ProvidentFundPolicyRecordDto>> GetPoliciesAsync(ProvidentFundPolicyListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.ProvidentFundPolicies
            .AsNoTracking()
            .Include(record => record.VestingRules)
            .Include(record => record.Enrollments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record => record.PolicyName.Contains(search) || record.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = NormalizeStatus(query.Status);
            source = source.Where(record => record.Status == status);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("effective", true) => source.OrderByDescending(record => record.EffectiveDate).ThenBy(record => record.PolicyName),
            ("effective", false) => source.OrderBy(record => record.EffectiveDate).ThenBy(record => record.PolicyName),
            ("status", true) => source.OrderByDescending(record => record.Status).ThenBy(record => record.PolicyName),
            ("status", false) => source.OrderBy(record => record.Status).ThenBy(record => record.PolicyName),
            (_, true) => source.OrderByDescending(record => record.PolicyName),
            _ => source.OrderBy(record => record.PolicyName)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ToPage(records.Select(MapPolicy).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<ProvidentFundPolicyRecordDto> CreatePolicyAsync(SaveProvidentFundPolicyRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        ValidatePolicy(request);
        await EnsureUniquePolicyNameAsync(request.PolicyName, null, cancellationToken);

        var record = new ProvidentFundPolicy
        {
            PolicyName = request.PolicyName.Trim(),
            Description = request.Description.Trim(),
            EligibilityRules = request.EligibilityRules.Trim(),
            EmployeeContributionType = NormalizeContributionType(request.EmployeeContributionType),
            EmployeeContributionValue = RoundMoney(request.EmployeeContributionValue),
            EmployerContributionType = NormalizeContributionType(request.EmployerContributionType),
            EmployerContributionValue = RoundMoney(request.EmployerContributionValue),
            ContributionFrequency = "monthly",
            EffectiveDate = request.EffectiveDate!.Value,
            Status = NormalizePolicyStatus(request.Status),
            AllowVoluntaryContribution = request.AllowVoluntaryContribution,
            AllowWithdrawal = request.AllowWithdrawal,
            AllowLoan = request.AllowLoan,
            Remarks = request.Remarks.Trim(),
            CreatedByUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ProvidentFundPolicies.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapPolicy(record);
    }

    public async Task<ProvidentFundPolicyRecordDto> UpdatePolicyAsync(Guid policyId, SaveProvidentFundPolicyRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        ValidatePolicy(request);
        var record = await _dbContext.ProvidentFundPolicies
            .Include(item => item.VestingRules)
            .Include(item => item.Enrollments)
            .SingleOrDefaultAsync(item => item.Id == policyId, cancellationToken)
            ?? throw new NotFoundException($"Provident fund policy '{policyId}' was not found.");

        await EnsureUniquePolicyNameAsync(request.PolicyName, policyId, cancellationToken);

        record.PolicyName = request.PolicyName.Trim();
        record.Description = request.Description.Trim();
        record.EligibilityRules = request.EligibilityRules.Trim();
        record.EmployeeContributionType = NormalizeContributionType(request.EmployeeContributionType);
        record.EmployeeContributionValue = RoundMoney(request.EmployeeContributionValue);
        record.EmployerContributionType = NormalizeContributionType(request.EmployerContributionType);
        record.EmployerContributionValue = RoundMoney(request.EmployerContributionValue);
        record.ContributionFrequency = "monthly";
        record.EffectiveDate = request.EffectiveDate!.Value;
        record.Status = NormalizePolicyStatus(request.Status);
        record.AllowVoluntaryContribution = request.AllowVoluntaryContribution;
        record.AllowWithdrawal = request.AllowWithdrawal;
        record.AllowLoan = request.AllowLoan;
        record.Remarks = request.Remarks.Trim();
        record.UpdatedByUserId = NormalizeUserId(actorUserId);
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapPolicy(record);
    }

    public async Task<PagedResultDto<ProvidentFundVestingRuleDto>> GetVestingRulesAsync(ProvidentFundVestingRuleListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.ProvidentFundVestingRules
            .AsNoTracking()
            .Include(record => record.Policy)
            .AsQueryable();

        if (query.PolicyId is not null)
        {
            source = source.Where(record => record.PolicyId == query.PolicyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record => record.Remarks.Contains(search) || (record.Policy != null && record.Policy.PolicyName.Contains(search)));
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("percentage", true) => source.OrderByDescending(record => record.VestedPercentage).ThenBy(record => record.YearsOfService),
            ("percentage", false) => source.OrderBy(record => record.VestedPercentage).ThenBy(record => record.YearsOfService),
            (_, true) => source.OrderByDescending(record => record.YearsOfService),
            _ => source.OrderBy(record => record.YearsOfService)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return ToPage(records.Select(MapVestingRule).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<ProvidentFundVestingRuleDto> CreateVestingRuleAsync(SaveProvidentFundVestingRuleRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateVestingRule(request);
        await EnsurePolicyExistsAsync(request.PolicyId!.Value, cancellationToken);
        await EnsureUniqueVestingThresholdAsync(request.PolicyId.Value, request.YearsOfService, null, cancellationToken);

        var record = new ProvidentFundVestingRule
        {
            PolicyId = request.PolicyId.Value,
            YearsOfService = request.YearsOfService,
            VestedPercentage = RoundPercentage(request.VestedPercentage),
            Remarks = request.Remarks.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ProvidentFundVestingRules.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _dbContext.Entry(record).Reference(item => item.Policy).LoadAsync(cancellationToken);
        return MapVestingRule(record);
    }

    public async Task<ProvidentFundVestingRuleDto> UpdateVestingRuleAsync(Guid vestingRuleId, SaveProvidentFundVestingRuleRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateVestingRule(request);
        var record = await _dbContext.ProvidentFundVestingRules
            .Include(item => item.Policy)
            .SingleOrDefaultAsync(item => item.Id == vestingRuleId, cancellationToken)
            ?? throw new NotFoundException($"Vesting rule '{vestingRuleId}' was not found.");

        await EnsurePolicyExistsAsync(request.PolicyId!.Value, cancellationToken);
        await EnsureUniqueVestingThresholdAsync(request.PolicyId.Value, request.YearsOfService, vestingRuleId, cancellationToken);

        record.PolicyId = request.PolicyId.Value;
        record.YearsOfService = request.YearsOfService;
        record.VestedPercentage = RoundPercentage(request.VestedPercentage);
        record.Remarks = request.Remarks.Trim();
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _dbContext.Entry(record).Reference(item => item.Policy).LoadAsync(cancellationToken);
        return MapVestingRule(record);
    }

    public async Task DeleteVestingRuleAsync(Guid vestingRuleId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ProvidentFundVestingRules.SingleOrDefaultAsync(item => item.Id == vestingRuleId, cancellationToken)
            ?? throw new NotFoundException($"Vesting rule '{vestingRuleId}' was not found.");

        _dbContext.ProvidentFundVestingRules.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<ProvidentFundEnrollmentRecordDto>> GetEnrollmentsAsync(ProvidentFundEnrollmentListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = IncludeEnrollmentGraph(_dbContext.ProvidentFundEnrollments.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Employee != null &&
                (record.Employee.EmployeeCode.Contains(search) ||
                 record.Employee.FirstName.Contains(search) ||
                 record.Employee.MiddleName.Contains(search) ||
                 record.Employee.LastName.Contains(search)) ||
                record.Policy != null && record.Policy.PolicyName.Contains(search));
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (query.PolicyId is not null)
        {
            source = source.Where(record => record.PolicyId == query.PolicyId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.DepartmentId == query.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == NormalizeEnrollmentStatus(query.Status));
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("employee", true) => source.OrderByDescending(record => record.Employee!.LastName).ThenByDescending(record => record.Employee!.FirstName),
            ("employee", false) => source.OrderBy(record => record.Employee!.LastName).ThenBy(record => record.Employee!.FirstName),
            ("status", true) => source.OrderByDescending(record => record.Status).ThenBy(record => record.Employee!.LastName),
            ("status", false) => source.OrderBy(record => record.Status).ThenBy(record => record.Employee!.LastName),
            (_, false) => source.OrderBy(record => record.CreatedAtUtc),
            _ => source.OrderByDescending(record => record.CreatedAtUtc)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        var items = new List<ProvidentFundEnrollmentRecordDto>();
        foreach (var record in records)
        {
            var balance = await ComputeBalanceAsync(record.EmployeeId, DateOnly.FromDateTime(DateTime.UtcNow), record.Id, cancellationToken);
            items.Add(MapEnrollment(record, balance));
        }

        return ToPage(items, query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<ProvidentFundEnrollmentRecordDto> CreateEnrollmentAsync(SaveProvidentFundEnrollmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateEnrollment(request);
        var employee = await _dbContext.Employees.SingleOrDefaultAsync(record => record.Id == request.EmployeeId!.Value, cancellationToken)
            ?? throw new BadRequestException("The selected employee does not exist.");
        var policy = await _dbContext.ProvidentFundPolicies.SingleOrDefaultAsync(record => record.Id == request.PolicyId!.Value, cancellationToken)
            ?? throw new BadRequestException("The selected provident fund policy does not exist.");

        if (policy.Status != ProvidentFundPolicyStatuses.Active)
        {
            throw new ConflictException("Only active provident fund policies can be assigned to employees.");
        }

        await EnsureNoDuplicateActiveEnrollmentAsync(employee.Id, null, NormalizeEnrollmentStatus(request.Status), cancellationToken);

        var record = new ProvidentFundEnrollment
        {
            EmployeeId = employee.Id,
            PolicyId = policy.Id,
            EnrollmentDate = request.EnrollmentDate!.Value,
            VestingStartDate = request.VestingStartDate!.Value,
            EmployeeContributionOverrideType = NormalizeOptionalContributionType(request.EmployeeContributionOverrideType),
            EmployeeContributionOverrideValue = request.EmployeeContributionOverrideValue.HasValue ? RoundMoney(request.EmployeeContributionOverrideValue.Value) : null,
            EmployerContributionOverrideType = NormalizeOptionalContributionType(request.EmployerContributionOverrideType),
            EmployerContributionOverrideValue = request.EmployerContributionOverrideValue.HasValue ? RoundMoney(request.EmployerContributionOverrideValue.Value) : null,
            Status = NormalizeEnrollmentStatus(request.Status),
            Remarks = request.Remarks.Trim(),
            CreatedByUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ProvidentFundEnrollments.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyEmployeeAsync(employee.Id, "Provident fund enrollment", $"You have been enrolled in {policy.PolicyName}.", ProvidentFundNotificationTypes.EnrollmentCreated, record.Id, "/me/provident-fund", cancellationToken);
        await _auditLogService.WriteAsync(new AuditLogEntry
        {
            Action = "enroll",
            EntityType = ProvidentFundAuditEntityTypes.Enrollment,
            EntityId = record.Id.ToString(),
            EmployeeId = employee.Id,
            Remarks = "Employee enrolled in provident fund policy."
        }, cancellationToken);

        var loaded = await IncludeEnrollmentGraph(_dbContext.ProvidentFundEnrollments).SingleAsync(item => item.Id == record.Id, cancellationToken);
        return MapEnrollment(loaded, await ComputeBalanceAsync(employee.Id, DateOnly.FromDateTime(DateTime.UtcNow), loaded.Id, cancellationToken));
    }

    public async Task<ProvidentFundEnrollmentRecordDto> UpdateEnrollmentAsync(Guid enrollmentId, SaveProvidentFundEnrollmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateEnrollment(request);
        var record = await _dbContext.ProvidentFundEnrollments.SingleOrDefaultAsync(item => item.Id == enrollmentId, cancellationToken)
            ?? throw new NotFoundException($"Provident fund enrollment '{enrollmentId}' was not found.");
        var requestedEmployeeId = request.EmployeeId!.Value;
        var requestedPolicyId = request.PolicyId!.Value;

        var policy = await _dbContext.ProvidentFundPolicies.SingleOrDefaultAsync(item => item.Id == requestedPolicyId, cancellationToken)
            ?? throw new BadRequestException("The selected provident fund policy does not exist.");

        if (policy.Status != ProvidentFundPolicyStatuses.Active && requestedPolicyId != record.PolicyId)
        {
            throw new ConflictException("Only active provident fund policies can be assigned to employees.");
        }

        var newStatus = NormalizeEnrollmentStatus(request.Status);
        await EnsureNoDuplicateActiveEnrollmentAsync(requestedEmployeeId, enrollmentId, newStatus, cancellationToken);

        record.EmployeeId = requestedEmployeeId;
        record.PolicyId = requestedPolicyId;
        record.EnrollmentDate = request.EnrollmentDate!.Value;
        record.VestingStartDate = request.VestingStartDate!.Value;
        record.EmployeeContributionOverrideType = NormalizeOptionalContributionType(request.EmployeeContributionOverrideType);
        record.EmployeeContributionOverrideValue = request.EmployeeContributionOverrideValue.HasValue ? RoundMoney(request.EmployeeContributionOverrideValue.Value) : null;
        record.EmployerContributionOverrideType = NormalizeOptionalContributionType(request.EmployerContributionOverrideType);
        record.EmployerContributionOverrideValue = request.EmployerContributionOverrideValue.HasValue ? RoundMoney(request.EmployerContributionOverrideValue.Value) : null;
        record.Status = newStatus;
        record.Remarks = request.Remarks.Trim();
        record.ClosedAtUtc = newStatus == ProvidentFundEnrollmentStatuses.Closed ? DateTime.UtcNow : null;
        record.UpdatedByUserId = NormalizeUserId(actorUserId);
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var loaded = await IncludeEnrollmentGraph(_dbContext.ProvidentFundEnrollments).SingleAsync(item => item.Id == record.Id, cancellationToken);
        return MapEnrollment(loaded, await ComputeBalanceAsync(loaded.EmployeeId, DateOnly.FromDateTime(DateTime.UtcNow), loaded.Id, cancellationToken));
    }

    public async Task<PagedResultDto<ProvidentFundContributionBatchSummaryDto>> GetContributionBatchesAsync(ProvidentFundContributionBatchListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.ProvidentFundContributionBatches
            .AsNoTracking()
            .Include(record => record.Policy)
            .Include(record => record.CreatedByUser)
            .Include(record => record.ReviewedByUser)
            .Include(record => record.PostedByUser)
            .Include(record => record.Lines)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record => record.BatchNumber.Contains(search) || record.Remarks.Contains(search));
        }

        if (query.Month is not null)
        {
            source = source.Where(record => record.Month == query.Month.Value);
        }

        if (query.Year is not null)
        {
            source = source.Where(record => record.Year == query.Year.Value);
        }

        if (query.PolicyId is not null)
        {
            source = source.Where(record => record.PolicyId == query.PolicyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == NormalizeBatchStatus(query.Status));
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("period", true) => source.OrderByDescending(record => record.Year).ThenByDescending(record => record.Month),
            ("period", false) => source.OrderBy(record => record.Year).ThenBy(record => record.Month),
            ("status", true) => source.OrderByDescending(record => record.Status).ThenByDescending(record => record.CreatedAtUtc),
            ("status", false) => source.OrderBy(record => record.Status).ThenByDescending(record => record.CreatedAtUtc),
            (_, false) => source.OrderBy(record => record.CreatedAtUtc),
            _ => source.OrderByDescending(record => record.CreatedAtUtc)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return ToPage(records.Select(MapContributionBatch).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<ProvidentFundContributionBatchDetailDto> GetContributionBatchByIdAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        var batch = await LoadContributionBatchAsync(batchId, tracking: false, cancellationToken)
            ?? throw new NotFoundException($"Provident fund contribution batch '{batchId}' was not found.");

        return MapContributionBatchDetail(batch);
    }

    public async Task<ProvidentFundContributionBatchDetailDto> GenerateContributionBatchAsync(GenerateProvidentFundContributionBatchRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateMonthYear(request.Month, request.Year);
        await EnsureNoDuplicatePostedContributionBatchAsync(request.Month, request.Year, request.PolicyId, request.IsSupplemental, cancellationToken);

        var periodStart = new DateOnly(request.Year, request.Month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var manualByEmployee = request.ManualLines.ToDictionary(item => item.EmployeeId);

        var enrollments = await IncludeEnrollmentGraph(_dbContext.ProvidentFundEnrollments)
            .Where(record =>
                record.Status == ProvidentFundEnrollmentStatuses.Active &&
                record.EnrollmentDate <= periodEnd &&
                record.Policy != null &&
                record.Policy.Status == ProvidentFundPolicyStatuses.Active &&
                (request.PolicyId == null || record.PolicyId == request.PolicyId.Value))
            .OrderBy(record => record.Employee!.LastName)
            .ThenBy(record => record.Employee!.FirstName)
            .ToListAsync(cancellationToken);

        if (enrollments.Count == 0)
        {
            throw new BadRequestException("No active provident fund enrollments matched the selected payroll month and policy.");
        }

        var employeeIds = enrollments.Select(record => record.EmployeeId).Distinct().ToArray();
        var compensationProfiles = await _dbContext.CompensationProfiles
            .AsNoTracking()
            .Where(record =>
                employeeIds.Contains(record.EmployeeId) &&
                record.IsActive &&
                record.EffectiveStartDate <= periodEnd &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= periodStart))
            .OrderByDescending(record => record.EffectiveStartDate)
            .ToListAsync(cancellationToken);

        var batch = new ProvidentFundContributionBatch
        {
            BatchNumber = string.IsNullOrWhiteSpace(request.BatchNumber)
                ? await GenerateBatchNumberAsync(request.Month, request.Year, cancellationToken)
                : request.BatchNumber.Trim().ToUpperInvariant(),
            Month = request.Month,
            Year = request.Year,
            PolicyId = request.PolicyId,
            IsSupplemental = request.IsSupplemental,
            Status = ProvidentFundContributionBatchStatuses.Draft,
            CreatedByUserId = NormalizeUserId(actorUserId),
            Remarks = request.Remarks.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        foreach (var enrollment in enrollments)
        {
            var salary = compensationProfiles.FirstOrDefault(record => record.EmployeeId == enrollment.EmployeeId)?.BasicSalary ?? 0m;
            var hasManual = manualByEmployee.TryGetValue(enrollment.EmployeeId, out var manual);
            if (hasManual && manual!.BasicSalary.HasValue)
            {
                salary = manual.BasicSalary.Value;
            }

            var employeeContribution = hasManual && manual!.EmployeeContribution.HasValue
                ? manual.EmployeeContribution.Value
                : CalculateContribution(salary, ResolveContributionType(enrollment.EmployeeContributionOverrideType, enrollment.Policy!.EmployeeContributionType), enrollment.EmployeeContributionOverrideValue ?? enrollment.Policy!.EmployeeContributionValue);
            var employerContribution = hasManual && manual!.EmployerContribution.HasValue
                ? manual.EmployerContribution.Value
                : CalculateContribution(salary, ResolveContributionType(enrollment.EmployerContributionOverrideType, enrollment.Policy!.EmployerContributionType), enrollment.EmployerContributionOverrideValue ?? enrollment.Policy!.EmployerContributionValue);
            var voluntaryContribution = hasManual && manual!.VoluntaryContribution.HasValue && enrollment.Policy!.AllowVoluntaryContribution
                ? manual.VoluntaryContribution.Value
                : 0m;

            var hasSalary = salary > 0m;
            batch.Lines.Add(new ProvidentFundContributionBatchLine
            {
                EmployeeId = enrollment.EmployeeId,
                EnrollmentId = enrollment.Id,
                BasicSalary = RoundMoney(salary),
                EmployeeContribution = RoundMoney(employeeContribution),
                EmployerContribution = RoundMoney(employerContribution),
                VoluntaryContribution = RoundMoney(voluntaryContribution),
                TotalContribution = RoundMoney(employeeContribution + employerContribution + voluntaryContribution),
                Status = hasSalary ? ProvidentFundContributionLineStatuses.Draft : ProvidentFundContributionLineStatuses.Held,
                Remarks = hasSalary ? string.Empty : "Basic salary unavailable. Regenerate with a manual salary input before posting.",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        _dbContext.ProvidentFundContributionBatches.Add(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetContributionBatchByIdAsync(batch.Id, cancellationToken);
    }

    public async Task<ProvidentFundContributionBatchDetailDto> ReviewContributionBatchAsync(Guid batchId, ProvidentFundContributionBatchActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var batch = await LoadContributionBatchAsync(batchId, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Provident fund contribution batch '{batchId}' was not found.");

        if (batch.Status == ProvidentFundContributionBatchStatuses.Posted || batch.Status == ProvidentFundContributionBatchStatuses.Cancelled)
        {
            throw new ConflictException("Posted or cancelled contribution batches cannot be reviewed.");
        }

        if (batch.Lines.Any(line => line.Status == ProvidentFundContributionLineStatuses.Held))
        {
            throw new ConflictException("Resolve held contribution lines before review.");
        }

        batch.Status = ProvidentFundContributionBatchStatuses.Reviewed;
        batch.ReviewedByUserId = NormalizeUserId(actorUserId);
        batch.ReviewedAtUtc = DateTime.UtcNow;
        batch.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? batch.Remarks : request.Remarks.Trim();
        batch.UpdatedAtUtc = DateTime.UtcNow;
        foreach (var line in batch.Lines)
        {
            line.Status = ProvidentFundContributionLineStatuses.Reviewed;
            line.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetContributionBatchByIdAsync(batch.Id, cancellationToken);
    }

    public async Task<ProvidentFundContributionBatchDetailDto> PostContributionBatchAsync(Guid batchId, ProvidentFundContributionBatchActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var batch = await LoadContributionBatchAsync(batchId, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Provident fund contribution batch '{batchId}' was not found.");

        if (batch.Status == ProvidentFundContributionBatchStatuses.Posted)
        {
            return MapContributionBatchDetail(batch);
        }

        if (batch.Status == ProvidentFundContributionBatchStatuses.Cancelled)
        {
            throw new ConflictException("Cancelled contribution batches cannot be posted.");
        }

        if (batch.Status != ProvidentFundContributionBatchStatuses.Reviewed)
        {
            throw new ConflictException("Only reviewed contribution batches can be posted.");
        }

        if (batch.Lines.Count == 0)
        {
            throw new ConflictException("Contribution batches without lines cannot be posted.");
        }

        if (batch.Lines.Any(line => line.Status == ProvidentFundContributionLineStatuses.Held))
        {
            throw new ConflictException("Held contribution lines cannot be posted.");
        }

        await EnsureNoDuplicatePostedContributionBatchAsync(batch.Month, batch.Year, batch.PolicyId, batch.IsSupplemental, cancellationToken);

        var postingDate = new DateOnly(batch.Year, batch.Month, 1).AddMonths(1).AddDays(-1);
        foreach (var line in batch.Lines)
        {
            await PostContributionLineAsync(batch, line, postingDate, actorUserId, cancellationToken);
            line.Status = ProvidentFundContributionLineStatuses.Posted;
            line.UpdatedAtUtc = DateTime.UtcNow;
        }

        batch.Status = ProvidentFundContributionBatchStatuses.Posted;
        batch.PostedByUserId = NormalizeUserId(actorUserId);
        batch.PostingDate = DateTime.UtcNow;
        batch.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? batch.Remarks : request.Remarks.Trim();
        batch.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await NotifyBatchPostedAsync(batch, cancellationToken);
        _logger.LogInformation("Provident fund contribution batch {BatchNumber} posted.", batch.BatchNumber);
        return await GetContributionBatchByIdAsync(batch.Id, cancellationToken);
    }

    public async Task<ProvidentFundContributionBatchDetailDto> CancelContributionBatchAsync(Guid batchId, ProvidentFundContributionBatchActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var batch = await LoadContributionBatchAsync(batchId, tracking: true, cancellationToken)
            ?? throw new NotFoundException($"Provident fund contribution batch '{batchId}' was not found.");

        if (batch.Status == ProvidentFundContributionBatchStatuses.Posted)
        {
            throw new ConflictException("Posted contribution batches cannot be cancelled. Create a reversal or adjustment instead.");
        }

        batch.Status = ProvidentFundContributionBatchStatuses.Cancelled;
        batch.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? "Contribution batch cancelled." : request.Remarks.Trim();
        batch.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetContributionBatchByIdAsync(batch.Id, cancellationToken);
    }

    public async Task<PagedResultDto<ProvidentFundLedgerTransactionDto>> GetLedgerAsync(ProvidentFundLedgerListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = BuildLedgerQuery();
        source = ApplyLedgerFilters(source, query.EmployeeId, query.PolicyId, query.DepartmentId, query.TransactionType, query.DateFrom, query.DateTo, query.Search);

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("type", true) => source.OrderByDescending(record => record.TransactionType).ThenByDescending(record => record.TransactionDate),
            ("type", false) => source.OrderBy(record => record.TransactionType).ThenByDescending(record => record.TransactionDate),
            ("employee", true) => source.OrderByDescending(record => record.Employee!.LastName).ThenByDescending(record => record.TransactionDate),
            ("employee", false) => source.OrderBy(record => record.Employee!.LastName).ThenByDescending(record => record.TransactionDate),
            (_, false) => source.OrderBy(record => record.TransactionDate).ThenBy(record => record.CreatedAtUtc),
            _ => source.OrderByDescending(record => record.TransactionDate).ThenByDescending(record => record.CreatedAtUtc)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return ToPage(records.Select(MapLedgerTransaction).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<ProvidentFundLedgerTransactionDto> ReverseLedgerTransactionAsync(Guid ledgerTransactionId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var original = await _dbContext.ProvidentFundLedgerTransactions
            .SingleOrDefaultAsync(record => record.Id == ledgerTransactionId, cancellationToken)
            ?? throw new NotFoundException($"Ledger transaction '{ledgerTransactionId}' was not found.");

        if (original.IsReversed)
        {
            throw new ConflictException("This ledger transaction has already been reversed.");
        }

        var reversal = new ProvidentFundLedgerTransaction
        {
            TransactionNumber = await GenerateLedgerTransactionNumberAsync(cancellationToken),
            EmployeeId = original.EmployeeId,
            EnrollmentId = original.EnrollmentId,
            PolicyId = original.PolicyId,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TransactionType = ProvidentFundLedgerTransactionTypes.Reversal,
            SourceType = original.SourceType,
            SourceReferenceId = original.Id.ToString(),
            EmployeeShareAmount = -original.EmployeeShareAmount,
            EmployerShareAmount = -original.EmployerShareAmount,
            VoluntaryShareAmount = -original.VoluntaryShareAmount,
            InterestAmount = -original.InterestAmount,
            DebitAmount = original.CreditAmount,
            CreditAmount = original.DebitAmount,
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? $"Reversal of {original.TransactionNumber}." : request.Remarks.Trim(),
            CreatedByUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow,
            ReversalReferenceId = original.Id
        };

        original.IsReversed = true;
        _dbContext.ProvidentFundLedgerTransactions.Add(reversal);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var loaded = await BuildLedgerQuery().SingleAsync(record => record.Id == reversal.Id, cancellationToken);
        return MapLedgerTransaction(loaded);
    }

    public async Task<ProvidentFundBalanceDto> GetEmployeeBalanceAsync(Guid employeeId, DateOnly? asOfDate = null, CancellationToken cancellationToken = default)
    {
        return await ComputeBalanceAsync(employeeId, asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow), null, cancellationToken);
    }

    public async Task<ProvidentFundBalanceDto> GetMyBalanceAsync(string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        return await GetEmployeeBalanceAsync(actor.LinkedEmployeeId!.Value, null, cancellationToken);
    }

    public async Task<PagedResultDto<ProvidentFundWithdrawalRequestDto>> GetWithdrawalsAsync(ProvidentFundWithdrawalListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = IncludeWithdrawalGraph(_dbContext.ProvidentFundWithdrawalRequests.AsNoTracking());
        source = ApplyWithdrawalFilters(source, query);

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("request_date", true) => source.OrderByDescending(record => record.RequestDate),
            ("request_date", false) => source.OrderBy(record => record.RequestDate),
            ("status", true) => source.OrderByDescending(record => record.Status).ThenByDescending(record => record.CreatedAtUtc),
            ("status", false) => source.OrderBy(record => record.Status).ThenByDescending(record => record.CreatedAtUtc),
            (_, false) => source.OrderBy(record => record.CreatedAtUtc),
            _ => source.OrderByDescending(record => record.CreatedAtUtc)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return ToPage(records.Select(MapWithdrawal).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<ProvidentFundWithdrawalRequestDto> CreateWithdrawalAsync(SaveProvidentFundWithdrawalRequestDto request, string? actorUserId, bool ownOnly, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        var employeeId = ownOnly
            ? actor.LinkedEmployeeId ?? throw new ForbiddenApiException("This user account is not linked to an employee record yet.")
            : request.EmployeeId ?? throw new BadRequestException("Employee is required.");

        if (ownOnly && request.EmployeeId.HasValue && request.EmployeeId.Value != employeeId)
        {
            throw new ForbiddenApiException("Employees can create withdrawal requests only for their own provident fund.");
        }

        ValidateWithdrawalRequest(request);
        var enrollment = await ResolveEnrollmentAsync(employeeId, request.EnrollmentId, requireActive: true, cancellationToken);
        if (enrollment.Policy is null || !enrollment.Policy.AllowWithdrawal)
        {
            throw new ConflictException("Withdrawals are not enabled for this provident fund policy.");
        }

        var balance = await ComputeBalanceAsync(employeeId, request.RequestDate!.Value, enrollment.Id, cancellationToken);
        if (request.RequestedAmount > balance.WithdrawableBalance)
        {
            throw new ConflictException("Withdrawal amount cannot exceed the employee's withdrawable balance.");
        }

        var record = new ProvidentFundWithdrawalRequest
        {
            RequestNumber = await GenerateWithdrawalNumberAsync(cancellationToken),
            EmployeeId = employeeId,
            EnrollmentId = enrollment.Id,
            RequestDate = request.RequestDate.Value,
            WithdrawalType = NormalizeWithdrawalType(request.WithdrawalType),
            RequestedAmount = RoundMoney(request.RequestedAmount),
            EligibleWithdrawableAmount = balance.WithdrawableBalance,
            ApprovedAmount = 0m,
            Reason = request.Reason.Trim(),
            AttachmentPath = request.AttachmentPath.Trim(),
            Status = ProvidentFundWithdrawalStatuses.Draft,
            Remarks = request.Remarks.Trim(),
            CreatedByUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ProvidentFundWithdrawalRequests.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetWithdrawalByIdAsync(record.Id, cancellationToken);
    }

    public async Task<ProvidentFundWithdrawalRequestDto> SubmitWithdrawalAsync(Guid withdrawalId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ProvidentFundWithdrawalRequests.SingleOrDefaultAsync(item => item.Id == withdrawalId, cancellationToken)
            ?? throw new NotFoundException($"Withdrawal request '{withdrawalId}' was not found.");

        if (record.Status != ProvidentFundWithdrawalStatuses.Draft)
        {
            throw new ConflictException("Only draft withdrawal requests can be submitted.");
        }

        var balance = await ComputeBalanceAsync(record.EmployeeId, record.RequestDate, record.EnrollmentId, cancellationToken);
        if (record.RequestedAmount > balance.WithdrawableBalance)
        {
            throw new ConflictException("Withdrawal amount cannot exceed the employee's withdrawable balance.");
        }

        record.Status = ProvidentFundWithdrawalStatuses.Submitted;
        record.EligibleWithdrawableAmount = balance.WithdrawableBalance;
        record.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? record.Remarks : request.Remarks.Trim();
        record.UpdatedByUserId = NormalizeUserId(actorUserId);
        record.UpdatedAtUtc = DateTime.UtcNow;
        await AddWithdrawalApprovalAsync(record, "submission", "submitted", actorUserId, request.Remarks, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyEmployeeAsync(record.EmployeeId, "Withdrawal request submitted", $"Request {record.RequestNumber} was submitted for review.", ProvidentFundNotificationTypes.WithdrawalSubmitted, record.Id, "/me/provident-fund", cancellationToken);
        return await GetWithdrawalByIdAsync(record.Id, cancellationToken);
    }

    public async Task<ProvidentFundWithdrawalRequestDto> ApproveWithdrawalAsync(Guid withdrawalId, ProvidentFundWithdrawalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ProvidentFundWithdrawalRequests.SingleOrDefaultAsync(item => item.Id == withdrawalId, cancellationToken)
            ?? throw new NotFoundException($"Withdrawal request '{withdrawalId}' was not found.");

        if (record.Status is ProvidentFundWithdrawalStatuses.Paid or ProvidentFundWithdrawalStatuses.Rejected or ProvidentFundWithdrawalStatuses.Cancelled)
        {
            throw new ConflictException("This withdrawal request is already closed.");
        }

        var approvedAmount = RoundMoney(request.ApprovedAmount ?? record.RequestedAmount);
        var balance = await ComputeBalanceAsync(record.EmployeeId, DateOnly.FromDateTime(DateTime.UtcNow), record.EnrollmentId, cancellationToken);
        if (approvedAmount <= 0m || approvedAmount > balance.WithdrawableBalance)
        {
            throw new ConflictException("Approved amount must be greater than zero and cannot exceed withdrawable balance.");
        }

        record.Status = ProvidentFundWithdrawalStatuses.Approved;
        record.EligibleWithdrawableAmount = balance.WithdrawableBalance;
        record.ApprovedAmount = approvedAmount;
        record.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? record.Remarks : request.Remarks.Trim();
        record.UpdatedByUserId = NormalizeUserId(actorUserId);
        record.UpdatedAtUtc = DateTime.UtcNow;
        await AddWithdrawalApprovalAsync(record, "approval", "approved", actorUserId, request.Remarks, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyEmployeeAsync(record.EmployeeId, "Withdrawal approved", $"Request {record.RequestNumber} was approved.", ProvidentFundNotificationTypes.WithdrawalApproved, record.Id, "/me/provident-fund", cancellationToken);
        return await GetWithdrawalByIdAsync(record.Id, cancellationToken);
    }

    public async Task<ProvidentFundWithdrawalRequestDto> RejectWithdrawalAsync(Guid withdrawalId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ProvidentFundWithdrawalRequests.SingleOrDefaultAsync(item => item.Id == withdrawalId, cancellationToken)
            ?? throw new NotFoundException($"Withdrawal request '{withdrawalId}' was not found.");

        if (record.Status is ProvidentFundWithdrawalStatuses.Paid or ProvidentFundWithdrawalStatuses.Rejected or ProvidentFundWithdrawalStatuses.Cancelled)
        {
            throw new ConflictException("This withdrawal request is already closed.");
        }

        record.Status = ProvidentFundWithdrawalStatuses.Rejected;
        record.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? "Withdrawal rejected." : request.Remarks.Trim();
        record.UpdatedByUserId = NormalizeUserId(actorUserId);
        record.UpdatedAtUtc = DateTime.UtcNow;
        await AddWithdrawalApprovalAsync(record, "approval", "rejected", actorUserId, request.Remarks, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await NotifyEmployeeAsync(record.EmployeeId, "Withdrawal rejected", $"Request {record.RequestNumber} was rejected.", ProvidentFundNotificationTypes.WithdrawalRejected, record.Id, "/me/provident-fund", cancellationToken);
        return await GetWithdrawalByIdAsync(record.Id, cancellationToken);
    }

    public async Task<ProvidentFundWithdrawalRequestDto> MarkWithdrawalPaidAsync(Guid withdrawalId, ProvidentFundWithdrawalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await _dbContext.ProvidentFundWithdrawalRequests
            .Include(item => item.Enrollment)
                .ThenInclude(enrollment => enrollment!.Policy)
            .SingleOrDefaultAsync(item => item.Id == withdrawalId, cancellationToken)
            ?? throw new NotFoundException($"Withdrawal request '{withdrawalId}' was not found.");

        if (record.Status == ProvidentFundWithdrawalStatuses.Paid)
        {
            return await GetWithdrawalByIdAsync(record.Id, cancellationToken);
        }

        if (record.Status != ProvidentFundWithdrawalStatuses.Approved)
        {
            throw new ConflictException("Only approved withdrawal requests can be marked as paid.");
        }

        var amount = RoundMoney(record.ApprovedAmount > 0m ? record.ApprovedAmount : request.ApprovedAmount ?? record.RequestedAmount);
        var balance = await ComputeBalanceAsync(record.EmployeeId, DateOnly.FromDateTime(DateTime.UtcNow), record.EnrollmentId, cancellationToken);
        if (amount <= 0m || amount > balance.WithdrawableBalance)
        {
            throw new ConflictException("Paid amount must be greater than zero and cannot exceed withdrawable balance.");
        }

        _dbContext.ProvidentFundLedgerTransactions.Add(new ProvidentFundLedgerTransaction
        {
            TransactionNumber = await GenerateLedgerTransactionNumberAsync(cancellationToken),
            EmployeeId = record.EmployeeId,
            EnrollmentId = record.EnrollmentId,
            PolicyId = record.Enrollment?.PolicyId ?? Guid.Empty,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TransactionType = record.WithdrawalType == ProvidentFundWithdrawalTypes.Full
                ? ProvidentFundLedgerTransactionTypes.FinalSettlement
                : ProvidentFundLedgerTransactionTypes.Withdrawal,
            SourceType = ProvidentFundLedgerSourceTypes.Withdrawal,
            SourceReferenceId = record.Id.ToString(),
            DebitAmount = amount,
            CreditAmount = 0m,
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? $"Withdrawal {record.RequestNumber} paid." : request.Remarks.Trim(),
            CreatedByUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow
        });

        record.Status = ProvidentFundWithdrawalStatuses.Paid;
        record.PaymentDate = DateTime.UtcNow;
        record.UpdatedByUserId = NormalizeUserId(actorUserId);
        record.UpdatedAtUtc = DateTime.UtcNow;
        await AddWithdrawalApprovalAsync(record, "payment", "paid", actorUserId, request.Remarks, cancellationToken);

        if (record.WithdrawalType == ProvidentFundWithdrawalTypes.Full || request.CloseEnrollment)
        {
            record.Enrollment!.Status = ProvidentFundEnrollmentStatuses.Closed;
            record.Enrollment.ClosedAtUtc = DateTime.UtcNow;
            record.Enrollment.UpdatedAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await NotifyEmployeeAsync(record.EmployeeId, "Withdrawal paid", $"Request {record.RequestNumber} has been paid.", ProvidentFundNotificationTypes.WithdrawalPaid, record.Id, "/me/provident-fund", cancellationToken);
        return await GetWithdrawalByIdAsync(record.Id, cancellationToken);
    }

    public async Task<PagedResultDto<ProvidentFundAdjustmentDto>> GetAdjustmentsAsync(ProvidentFundAdjustmentListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = IncludeAdjustmentGraph(_dbContext.ProvidentFundAdjustments.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Reason.Contains(search) ||
                record.Employee!.EmployeeCode.Contains(search) ||
                record.Employee.FirstName.Contains(search) ||
                record.Employee.MiddleName.Contains(search) ||
                record.Employee.LastName.Contains(search));
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.DepartmentId == query.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == NormalizeAdjustmentStatus(query.Status));
        }

        if (!string.IsNullOrWhiteSpace(query.AdjustmentType))
        {
            source = source.Where(record => record.AdjustmentType == NormalizeAdjustmentType(query.AdjustmentType));
        }

        if (!string.IsNullOrWhiteSpace(query.ShareAffected))
        {
            source = source.Where(record => record.ShareAffected == NormalizeShareType(query.ShareAffected));
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(record => record.AdjustmentDate >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(record => record.AdjustmentDate <= query.DateTo.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("date", true) => source.OrderByDescending(record => record.AdjustmentDate),
            ("date", false) => source.OrderBy(record => record.AdjustmentDate),
            ("status", true) => source.OrderByDescending(record => record.Status).ThenByDescending(record => record.CreatedAtUtc),
            ("status", false) => source.OrderBy(record => record.Status).ThenByDescending(record => record.CreatedAtUtc),
            (_, false) => source.OrderBy(record => record.CreatedAtUtc),
            _ => source.OrderByDescending(record => record.CreatedAtUtc)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source.Skip((query.PageNumber - 1) * query.PageSize).Take(query.PageSize).ToListAsync(cancellationToken);
        return ToPage(records.Select(MapAdjustment).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<ProvidentFundAdjustmentDto> CreateAdjustmentAsync(SaveProvidentFundAdjustmentRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        ValidateAdjustment(request);
        var enrollment = await ResolveEnrollmentAsync(request.EmployeeId!.Value, request.EnrollmentId, requireActive: true, cancellationToken);

        var record = new ProvidentFundAdjustment
        {
            EmployeeId = request.EmployeeId.Value,
            EnrollmentId = enrollment.Id,
            AdjustmentType = NormalizeAdjustmentType(request.AdjustmentType),
            AdjustmentDate = request.AdjustmentDate!.Value,
            Amount = RoundMoney(request.Amount),
            ShareAffected = NormalizeShareType(request.ShareAffected),
            Reason = request.Reason.Trim(),
            AttachmentPath = request.AttachmentPath.Trim(),
            Status = ProvidentFundAdjustmentStatuses.Draft,
            RequestedByUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ProvidentFundAdjustments.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdjustmentByIdAsync(record.Id, cancellationToken);
    }

    public async Task<ProvidentFundAdjustmentDto> ApproveAdjustmentAsync(Guid adjustmentId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ProvidentFundAdjustments.SingleOrDefaultAsync(item => item.Id == adjustmentId, cancellationToken)
            ?? throw new NotFoundException($"Adjustment '{adjustmentId}' was not found.");

        if (record.Status != ProvidentFundAdjustmentStatuses.Draft)
        {
            throw new ConflictException("Only draft adjustments can be approved.");
        }

        record.Status = ProvidentFundAdjustmentStatuses.Approved;
        record.ApprovedByUserId = NormalizeUserId(actorUserId);
        record.ApprovedAtUtc = DateTime.UtcNow;
        record.DecisionRemarks = request.Remarks.Trim();
        record.UpdatedAtUtc = DateTime.UtcNow;
        await AddAdjustmentApprovalAsync(record, "approved", actorUserId, request.Remarks, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdjustmentByIdAsync(record.Id, cancellationToken);
    }

    public async Task<ProvidentFundAdjustmentDto> RejectAdjustmentAsync(Guid adjustmentId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ProvidentFundAdjustments.SingleOrDefaultAsync(item => item.Id == adjustmentId, cancellationToken)
            ?? throw new NotFoundException($"Adjustment '{adjustmentId}' was not found.");

        if (record.Status != ProvidentFundAdjustmentStatuses.Draft)
        {
            throw new ConflictException("Only draft adjustments can be rejected.");
        }

        record.Status = ProvidentFundAdjustmentStatuses.Rejected;
        record.ApprovedByUserId = NormalizeUserId(actorUserId);
        record.ApprovedAtUtc = DateTime.UtcNow;
        record.DecisionRemarks = string.IsNullOrWhiteSpace(request.Remarks) ? "Adjustment rejected." : request.Remarks.Trim();
        record.UpdatedAtUtc = DateTime.UtcNow;
        await AddAdjustmentApprovalAsync(record, "rejected", actorUserId, request.Remarks, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetAdjustmentByIdAsync(record.Id, cancellationToken);
    }

    public async Task<ProvidentFundAdjustmentDto> PostAdjustmentAsync(Guid adjustmentId, ProvidentFundActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var record = await _dbContext.ProvidentFundAdjustments
            .Include(item => item.Enrollment)
            .SingleOrDefaultAsync(item => item.Id == adjustmentId, cancellationToken)
            ?? throw new NotFoundException($"Adjustment '{adjustmentId}' was not found.");

        if (record.Status == ProvidentFundAdjustmentStatuses.Posted)
        {
            return await GetAdjustmentByIdAsync(record.Id, cancellationToken);
        }

        if (record.Status != ProvidentFundAdjustmentStatuses.Approved)
        {
            throw new ConflictException("Only approved adjustments can be posted.");
        }

        var isCredit = record.AdjustmentType == ProvidentFundAdjustmentTypes.Credit;
        var ledger = new ProvidentFundLedgerTransaction
        {
            TransactionNumber = await GenerateLedgerTransactionNumberAsync(cancellationToken),
            EmployeeId = record.EmployeeId,
            EnrollmentId = record.EnrollmentId,
            PolicyId = record.Enrollment?.PolicyId ?? Guid.Empty,
            TransactionDate = record.AdjustmentDate,
            TransactionType = ProvidentFundLedgerTransactionTypes.Adjustment,
            SourceType = ProvidentFundLedgerSourceTypes.Adjustment,
            SourceReferenceId = record.Id.ToString(),
            DebitAmount = isCredit ? 0m : record.Amount,
            CreditAmount = isCredit ? record.Amount : 0m,
            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? record.Reason : request.Remarks.Trim(),
            CreatedByUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow
        };
        ApplyShareAmount(ledger, record.ShareAffected, isCredit ? record.Amount : -record.Amount);
        _dbContext.ProvidentFundLedgerTransactions.Add(ledger);

        record.Status = ProvidentFundAdjustmentStatuses.Posted;
        record.PostedAtUtc = DateTime.UtcNow;
        record.UpdatedAtUtc = DateTime.UtcNow;
        await AddAdjustmentApprovalAsync(record, "posted", actorUserId, request.Remarks, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await NotifyEmployeeAsync(record.EmployeeId, "Provident fund adjustment posted", $"An adjustment of {record.Amount:N2} was posted to your provident fund.", ProvidentFundNotificationTypes.AdjustmentPosted, record.Id, "/me/provident-fund", cancellationToken);
        return await GetAdjustmentByIdAsync(record.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ProvidentFundContributionReportRowDto>> GetContributionReportAsync(ProvidentFundReportQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.ProvidentFundContributionBatchLines
            .AsNoTracking()
            .Include(line => line.Batch)
            .Include(line => line.Employee)
                .ThenInclude(employee => employee!.Department)
            .AsQueryable();

        if (query.Month is not null)
        {
            source = source.Where(line => line.Batch != null && line.Batch.Month == query.Month.Value);
        }

        if (query.Year is not null)
        {
            source = source.Where(line => line.Batch != null && line.Batch.Year == query.Year.Value);
        }

        if (query.PolicyId is not null)
        {
            source = source.Where(line => line.Batch != null && line.Batch.PolicyId == query.PolicyId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(line => line.Employee != null && line.Employee.DepartmentId == query.DepartmentId.Value);
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(line => line.EmployeeId == query.EmployeeId.Value);
        }

        return await source
            .OrderBy(line => line.Employee!.LastName)
            .ThenBy(line => line.Employee!.FirstName)
            .Select(line => new ProvidentFundContributionReportRowDto
            {
                EmployeeNumber = line.Employee != null ? line.Employee.EmployeeCode : string.Empty,
                EmployeeName = line.Employee != null ? BuildFullName(line.Employee.FirstName, line.Employee.MiddleName, line.Employee.LastName, line.Employee.Suffix) : string.Empty,
                Department = line.Employee != null && line.Employee.Department != null ? line.Employee.Department.Name : string.Empty,
                BasicSalary = line.BasicSalary,
                EmployeeContribution = line.EmployeeContribution,
                EmployerContribution = line.EmployerContribution,
                VoluntaryContribution = line.VoluntaryContribution,
                TotalContribution = line.TotalContribution,
                BatchStatus = line.Batch != null ? line.Batch.Status : string.Empty
            })
            .Take(2000)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProvidentFundBalanceReportRowDto>> GetBalanceReportAsync(ProvidentFundReportQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = IncludeEnrollmentGraph(_dbContext.ProvidentFundEnrollments.AsNoTracking());
        if (query.PolicyId is not null)
        {
            source = source.Where(record => record.PolicyId == query.PolicyId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.DepartmentId == query.DepartmentId.Value);
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.EmploymentStatus))
        {
            source = source.Where(record => record.Employee != null && record.Employee.EmploymentStatus != null && record.Employee.EmploymentStatus.Name == query.EmploymentStatus);
        }

        var enrollments = await source.OrderBy(record => record.Employee!.LastName).ThenBy(record => record.Employee!.FirstName).Take(2000).ToListAsync(cancellationToken);
        var rows = new List<ProvidentFundBalanceReportRowDto>();
        foreach (var enrollment in enrollments)
        {
            var balance = await ComputeBalanceAsync(enrollment.EmployeeId, query.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow), enrollment.Id, cancellationToken);
            rows.Add(new ProvidentFundBalanceReportRowDto
            {
                EmployeeNumber = balance.EmployeeCode,
                EmployeeName = balance.EmployeeFullName,
                TotalEmployeeShare = balance.TotalEmployeeContribution,
                TotalEmployerShare = balance.TotalEmployerContribution,
                VestedEmployerShare = balance.VestedEmployerBalance,
                NonVestedEmployerShare = balance.NonVestedEmployerBalance,
                Interest = balance.TotalInterest,
                Withdrawals = balance.TotalWithdrawals,
                CurrentBalance = balance.GrossFundBalance,
                WithdrawableBalance = balance.WithdrawableBalance
            });
        }

        return rows;
    }

    public async Task<IReadOnlyList<ProvidentFundWithdrawalReportRowDto>> GetWithdrawalReportAsync(ProvidentFundReportQueryDto query, CancellationToken cancellationToken = default)
    {
        var listQuery = new ProvidentFundWithdrawalListQueryDto
        {
            EmployeeId = query.EmployeeId,
            DepartmentId = query.DepartmentId,
            Status = query.Status,
            WithdrawalType = query.WithdrawalType,
            DateFrom = query.DateFrom,
            DateTo = query.DateTo,
            PageNumber = 1,
            PageSize = 100,
            SortBy = "request_date",
            Descending = true
        };

        var source = ApplyWithdrawalFilters(IncludeWithdrawalGraph(_dbContext.ProvidentFundWithdrawalRequests.AsNoTracking()), listQuery);
        return await source
            .OrderByDescending(record => record.RequestDate)
            .Take(2000)
            .Select(record => new ProvidentFundWithdrawalReportRowDto
            {
                RequestNumber = record.RequestNumber,
                Employee = record.Employee != null ? BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix) : string.Empty,
                RequestDate = record.RequestDate,
                WithdrawalType = record.WithdrawalType,
                RequestedAmount = record.RequestedAmount,
                ApprovedAmount = record.ApprovedAmount,
                Status = record.Status,
                PaymentDate = record.PaymentDate
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProvidentFundLedgerTransactionDto>> GetLedgerReportAsync(ProvidentFundReportQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = BuildLedgerQuery();
        source = ApplyLedgerFilters(source, query.EmployeeId, query.PolicyId, query.DepartmentId, query.TransactionType, query.DateFrom, query.DateTo, string.Empty);
        var records = await source
            .OrderByDescending(record => record.TransactionDate)
            .ThenByDescending(record => record.CreatedAtUtc)
            .Take(2000)
            .ToListAsync(cancellationToken);

        return records.Select(MapLedgerTransaction).ToList();
    }

    private async Task PostContributionLineAsync(ProvidentFundContributionBatch batch, ProvidentFundContributionBatchLine line, DateOnly postingDate, string? actorUserId, CancellationToken cancellationToken)
    {
        var enrollment = line.Enrollment ?? throw new ConflictException("Contribution line is missing enrollment information.");
        var policy = enrollment.Policy ?? throw new ConflictException("Contribution line is missing policy information.");

        if (line.EmployeeContribution > 0m)
        {
            _dbContext.ProvidentFundLedgerTransactions.Add(new ProvidentFundLedgerTransaction
            {
                TransactionNumber = await GenerateLedgerTransactionNumberAsync(cancellationToken),
                EmployeeId = line.EmployeeId,
                EnrollmentId = line.EnrollmentId,
                PolicyId = policy.Id,
                ContributionBatchId = batch.Id,
                ContributionBatchLineId = line.Id,
                TransactionDate = postingDate,
                TransactionType = ProvidentFundLedgerTransactionTypes.EmployeeContribution,
                SourceType = ProvidentFundLedgerSourceTypes.ContributionBatch,
                SourceReferenceId = batch.Id.ToString(),
                EmployeeShareAmount = line.EmployeeContribution,
                CreditAmount = line.EmployeeContribution,
                Remarks = $"Employee contribution from batch {batch.BatchNumber}.",
                CreatedByUserId = NormalizeUserId(actorUserId),
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (line.EmployerContribution > 0m)
        {
            _dbContext.ProvidentFundLedgerTransactions.Add(new ProvidentFundLedgerTransaction
            {
                TransactionNumber = await GenerateLedgerTransactionNumberAsync(cancellationToken),
                EmployeeId = line.EmployeeId,
                EnrollmentId = line.EnrollmentId,
                PolicyId = policy.Id,
                ContributionBatchId = batch.Id,
                ContributionBatchLineId = line.Id,
                TransactionDate = postingDate,
                TransactionType = ProvidentFundLedgerTransactionTypes.EmployerContribution,
                SourceType = ProvidentFundLedgerSourceTypes.ContributionBatch,
                SourceReferenceId = batch.Id.ToString(),
                EmployerShareAmount = line.EmployerContribution,
                CreditAmount = line.EmployerContribution,
                Remarks = $"Employer contribution from batch {batch.BatchNumber}.",
                CreatedByUserId = NormalizeUserId(actorUserId),
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        if (line.VoluntaryContribution > 0m)
        {
            _dbContext.ProvidentFundLedgerTransactions.Add(new ProvidentFundLedgerTransaction
            {
                TransactionNumber = await GenerateLedgerTransactionNumberAsync(cancellationToken),
                EmployeeId = line.EmployeeId,
                EnrollmentId = line.EnrollmentId,
                PolicyId = policy.Id,
                ContributionBatchId = batch.Id,
                ContributionBatchLineId = line.Id,
                TransactionDate = postingDate,
                TransactionType = ProvidentFundLedgerTransactionTypes.VoluntaryContribution,
                SourceType = ProvidentFundLedgerSourceTypes.ContributionBatch,
                SourceReferenceId = batch.Id.ToString(),
                VoluntaryShareAmount = line.VoluntaryContribution,
                CreditAmount = line.VoluntaryContribution,
                Remarks = $"Voluntary contribution from batch {batch.BatchNumber}.",
                CreatedByUserId = NormalizeUserId(actorUserId),
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private async Task<ProvidentFundBalanceDto> ComputeBalanceAsync(Guid employeeId, DateOnly asOfDate, Guid? enrollmentId, CancellationToken cancellationToken)
    {
        var enrollment = await IncludeEnrollmentGraph(_dbContext.ProvidentFundEnrollments.AsNoTracking())
            .Where(record => record.EmployeeId == employeeId && (enrollmentId == null || record.Id == enrollmentId.Value))
            .OrderByDescending(record => record.Status == ProvidentFundEnrollmentStatuses.Active)
            .ThenByDescending(record => record.EnrollmentDate)
            .FirstOrDefaultAsync(cancellationToken);

        var employee = enrollment?.Employee ?? await _dbContext.Employees
            .AsNoTracking()
            .Include(record => record.Department)
            .SingleOrDefaultAsync(record => record.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Employee '{employeeId}' was not found.");

        var ledger = await _dbContext.ProvidentFundLedgerTransactions
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId && record.TransactionDate <= asOfDate && (enrollmentId == null || record.EnrollmentId == enrollmentId.Value))
            .ToListAsync(cancellationToken);

        var employeeShare = ledger.Sum(record => record.EmployeeShareAmount);
        var employerShare = ledger.Sum(record => record.EmployerShareAmount);
        var voluntaryShare = ledger.Sum(record => record.VoluntaryShareAmount);
        var interest = ledger.Sum(record => record.InterestAmount);
        var withdrawals = ledger
            .Where(record => record.TransactionType == ProvidentFundLedgerTransactionTypes.Withdrawal || record.TransactionType == ProvidentFundLedgerTransactionTypes.FinalSettlement)
            .Sum(record => record.DebitAmount);
        var adjustments = ledger
            .Where(record =>
                record.TransactionType == ProvidentFundLedgerTransactionTypes.Adjustment ||
                record.TransactionType == ProvidentFundLedgerTransactionTypes.Reversal ||
                record.TransactionType == ProvidentFundLedgerTransactionTypes.Forfeiture)
            .Sum(record => record.CreditAmount - record.DebitAmount);
        var grossBalance = ledger.Sum(record => record.CreditAmount - record.DebitAmount);
        var vestingPercentage = enrollment is null ? 0m : await ResolveVestingPercentageAsync(enrollment.PolicyId, enrollment.VestingStartDate, asOfDate, cancellationToken);
        var vestedEmployer = Math.Max(0m, RoundMoney(employerShare * vestingPercentage / 100m));
        var nonVestedEmployer = Math.Max(0m, RoundMoney(employerShare - vestedEmployer));
        var withdrawable = Math.Max(0m, RoundMoney(employeeShare + voluntaryShare + interest + vestedEmployer - withdrawals));

        return new ProvidentFundBalanceDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            DepartmentName = employee.Department?.Name ?? string.Empty,
            EnrollmentId = enrollment?.Id,
            EnrollmentStatus = enrollment?.Status ?? string.Empty,
            PolicyId = enrollment?.PolicyId,
            PolicyName = enrollment?.Policy?.PolicyName ?? string.Empty,
            EnrollmentDate = enrollment?.EnrollmentDate,
            VestingStartDate = enrollment?.VestingStartDate,
            VestingPercentage = vestingPercentage,
            TotalEmployeeContribution = RoundMoney(employeeShare),
            TotalEmployerContribution = RoundMoney(employerShare),
            TotalVoluntaryContribution = RoundMoney(voluntaryShare),
            TotalInterest = RoundMoney(interest),
            TotalWithdrawals = RoundMoney(withdrawals),
            TotalAdjustments = RoundMoney(adjustments),
            GrossFundBalance = RoundMoney(grossBalance),
            VestedEmployerBalance = vestedEmployer,
            NonVestedEmployerBalance = nonVestedEmployer,
            WithdrawableBalance = withdrawable,
            LatestTransactionDate = ledger.Count == 0 ? null : ledger.Max(record => record.TransactionDate)
        };
    }

    private async Task<decimal> ResolveVestingPercentageAsync(Guid policyId, DateOnly vestingStartDate, DateOnly asOfDate, CancellationToken cancellationToken)
    {
        var years = CalculateWholeYears(vestingStartDate, asOfDate);
        return await _dbContext.ProvidentFundVestingRules
            .AsNoTracking()
            .Where(record => record.PolicyId == policyId && record.YearsOfService <= years)
            .OrderByDescending(record => record.YearsOfService)
            .Select(record => record.VestedPercentage)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<ProvidentFundEnrollment> ResolveEnrollmentAsync(Guid employeeId, Guid? enrollmentId, bool requireActive, CancellationToken cancellationToken)
    {
        var source = IncludeEnrollmentGraph(_dbContext.ProvidentFundEnrollments)
            .Where(record => record.EmployeeId == employeeId && (enrollmentId == null || record.Id == enrollmentId.Value));

        if (requireActive)
        {
            source = source.Where(record => record.Status == ProvidentFundEnrollmentStatuses.Active);
        }

        return await source
            .OrderByDescending(record => record.EnrollmentDate)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new BadRequestException("The employee does not have an active provident fund enrollment.");
    }

    private async Task<ProvidentFundContributionBatch?> LoadContributionBatchAsync(Guid batchId, bool tracking, CancellationToken cancellationToken)
    {
        var source = _dbContext.ProvidentFundContributionBatches
            .Include(record => record.Policy)
            .Include(record => record.CreatedByUser)
            .Include(record => record.ReviewedByUser)
            .Include(record => record.PostedByUser)
            .Include(record => record.Lines)
                .ThenInclude(line => line.Employee)
                    .ThenInclude(employee => employee!.Department)
            .Include(record => record.Lines)
                .ThenInclude(line => line.Enrollment)
                    .ThenInclude(enrollment => enrollment!.Policy)
            .AsQueryable();

        if (!tracking)
        {
            source = source.AsNoTracking();
        }

        return await source.SingleOrDefaultAsync(record => record.Id == batchId, cancellationToken);
    }

    private async Task<ProvidentFundWithdrawalRequestDto> GetWithdrawalByIdAsync(Guid withdrawalId, CancellationToken cancellationToken)
    {
        var record = await IncludeWithdrawalGraph(_dbContext.ProvidentFundWithdrawalRequests.AsNoTracking())
            .SingleOrDefaultAsync(item => item.Id == withdrawalId, cancellationToken)
            ?? throw new NotFoundException($"Withdrawal request '{withdrawalId}' was not found.");

        return MapWithdrawal(record);
    }

    private async Task<ProvidentFundAdjustmentDto> GetAdjustmentByIdAsync(Guid adjustmentId, CancellationToken cancellationToken)
    {
        var record = await IncludeAdjustmentGraph(_dbContext.ProvidentFundAdjustments.AsNoTracking())
            .SingleOrDefaultAsync(item => item.Id == adjustmentId, cancellationToken)
            ?? throw new NotFoundException($"Adjustment '{adjustmentId}' was not found.");

        return MapAdjustment(record);
    }

    private IQueryable<ProvidentFundLedgerTransaction> BuildLedgerQuery()
    {
        return _dbContext.ProvidentFundLedgerTransactions
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Policy)
            .Include(record => record.CreatedByUser)
            .AsQueryable();
    }

    private static IQueryable<ProvidentFundLedgerTransaction> ApplyLedgerFilters(
        IQueryable<ProvidentFundLedgerTransaction> source,
        Guid? employeeId,
        Guid? policyId,
        Guid? departmentId,
        string transactionType,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string search)
    {
        if (employeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == employeeId.Value);
        }

        if (policyId is not null)
        {
            source = source.Where(record => record.PolicyId == policyId.Value);
        }

        if (departmentId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(transactionType))
        {
            source = source.Where(record => record.TransactionType == transactionType.Trim().ToLowerInvariant());
        }

        if (dateFrom is not null)
        {
            source = source.Where(record => record.TransactionDate >= dateFrom.Value);
        }

        if (dateTo is not null)
        {
            source = source.Where(record => record.TransactionDate <= dateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var scopedSearch = search.Trim();
            source = source.Where(record =>
                record.TransactionNumber.Contains(scopedSearch) ||
                record.Remarks.Contains(scopedSearch) ||
                record.Employee!.EmployeeCode.Contains(scopedSearch) ||
                record.Employee.FirstName.Contains(scopedSearch) ||
                record.Employee.MiddleName.Contains(scopedSearch) ||
                record.Employee.LastName.Contains(scopedSearch));
        }

        return source;
    }

    private static IQueryable<ProvidentFundEnrollment> IncludeEnrollmentGraph(IQueryable<ProvidentFundEnrollment> source)
    {
        return source
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.EmploymentStatus)
            .Include(record => record.Policy);
    }

    private static IQueryable<ProvidentFundWithdrawalRequest> IncludeWithdrawalGraph(IQueryable<ProvidentFundWithdrawalRequest> source)
    {
        return source
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Enrollment)
            .Include(record => record.Approvals)
                .ThenInclude(approval => approval.ActorUser);
    }

    private static IQueryable<ProvidentFundAdjustment> IncludeAdjustmentGraph(IQueryable<ProvidentFundAdjustment> source)
    {
        return source
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Enrollment)
            .Include(record => record.RequestedByUser)
            .Include(record => record.ApprovedByUser)
            .Include(record => record.Approvals)
                .ThenInclude(approval => approval.ActorUser);
    }

    private static IQueryable<ProvidentFundWithdrawalRequest> ApplyWithdrawalFilters(IQueryable<ProvidentFundWithdrawalRequest> source, ProvidentFundWithdrawalListQueryDto query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.RequestNumber.Contains(search) ||
                record.Reason.Contains(search) ||
                record.Employee!.EmployeeCode.Contains(search) ||
                record.Employee.FirstName.Contains(search) ||
                record.Employee.MiddleName.Contains(search) ||
                record.Employee.LastName.Contains(search));
        }

        if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.Employee != null && record.Employee.DepartmentId == query.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim().ToLowerInvariant());
        }

        if (!string.IsNullOrWhiteSpace(query.WithdrawalType))
        {
            source = source.Where(record => record.WithdrawalType == query.WithdrawalType.Trim().ToLowerInvariant());
        }

        if (query.DateFrom is not null)
        {
            source = source.Where(record => record.RequestDate >= query.DateFrom.Value);
        }

        if (query.DateTo is not null)
        {
            source = source.Where(record => record.RequestDate <= query.DateTo.Value);
        }

        return source;
    }

    private async Task AddWithdrawalApprovalAsync(ProvidentFundWithdrawalRequest record, string stepName, string action, string? actorUserId, string remarks, CancellationToken cancellationToken)
    {
        var actorName = await ResolveActorNameAsync(actorUserId, cancellationToken);
        record.Approvals.Add(new ProvidentFundWithdrawalApproval
        {
            StepName = stepName,
            Action = action,
            ActorUserId = NormalizeUserId(actorUserId),
            ActorNameSnapshot = actorName,
            Remarks = remarks.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private async Task AddAdjustmentApprovalAsync(ProvidentFundAdjustment record, string action, string? actorUserId, string remarks, CancellationToken cancellationToken)
    {
        var actorName = await ResolveActorNameAsync(actorUserId, cancellationToken);
        record.Approvals.Add(new ProvidentFundAdjustmentApproval
        {
            Action = action,
            ActorUserId = NormalizeUserId(actorUserId),
            ActorNameSnapshot = actorName,
            Remarks = remarks.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private async Task NotifyEmployeeAsync(Guid employeeId, string title, string message, string notificationType, Guid referenceId, string actionUrl, CancellationToken cancellationToken)
    {
        var userId = await _notificationService.GetUserIdForEmployeeAsync(employeeId, cancellationToken);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        await _notificationService.CreateAsync(
            new NotificationDraft(userId, title, message, notificationType, "provident_fund", referenceId.ToString(), actionUrl),
            cancellationToken);
    }

    private async Task NotifyBatchPostedAsync(ProvidentFundContributionBatch batch, CancellationToken cancellationToken)
    {
        var employeeIds = batch.Lines.Select(line => line.EmployeeId).Distinct().ToArray();
        var userIds = await _notificationService.GetUserIdsForEmployeesAsync(employeeIds, cancellationToken);
        var notifications = batch.Lines
            .Where(line => userIds.ContainsKey(line.EmployeeId))
            .Select(line => new NotificationDraft(
                userIds[line.EmployeeId],
                "Provident fund contribution posted",
                $"Provident fund contributions for {batch.Month:00}/{batch.Year} have been posted.",
                ProvidentFundNotificationTypes.ContributionBatchPosted,
                "provident_fund_contribution_batch",
                batch.Id.ToString(),
                "/me/provident-fund"))
            .ToList();

        await _notificationService.CreateManyAsync(notifications, cancellationToken);
    }

    private async Task<string> ResolveActorNameAsync(string? actorUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return string.Empty;
        }

        return await _dbContext.Users
            .AsNoTracking()
            .Where(record => record.Id == actorUserId)
            .Select(record => !string.IsNullOrWhiteSpace(record.DisplayName) ? record.DisplayName : record.Email ?? string.Empty)
            .SingleOrDefaultAsync(cancellationToken) ?? string.Empty;
    }

    private async Task EnsurePolicyExistsAsync(Guid policyId, CancellationToken cancellationToken)
    {
        if (!await _dbContext.ProvidentFundPolicies.AnyAsync(record => record.Id == policyId, cancellationToken))
        {
            throw new BadRequestException("The selected provident fund policy does not exist.");
        }
    }

    private async Task EnsureUniquePolicyNameAsync(string policyName, Guid? currentPolicyId, CancellationToken cancellationToken)
    {
        var normalized = policyName.Trim();
        var exists = await _dbContext.ProvidentFundPolicies.AnyAsync(
            record => record.PolicyName == normalized && (currentPolicyId == null || record.Id != currentPolicyId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("A provident fund policy with the same name already exists.");
        }
    }

    private async Task EnsureUniqueVestingThresholdAsync(Guid policyId, int yearsOfService, Guid? currentRuleId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.ProvidentFundVestingRules.AnyAsync(
            record => record.PolicyId == policyId && record.YearsOfService == yearsOfService && (currentRuleId == null || record.Id != currentRuleId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("A vesting rule with the same year threshold already exists for this policy.");
        }
    }

    private async Task EnsureNoDuplicateActiveEnrollmentAsync(Guid employeeId, Guid? currentEnrollmentId, string status, CancellationToken cancellationToken)
    {
        if (status != ProvidentFundEnrollmentStatuses.Active)
        {
            return;
        }

        var exists = await _dbContext.ProvidentFundEnrollments.AnyAsync(
            record => record.EmployeeId == employeeId && record.Status == ProvidentFundEnrollmentStatuses.Active && (currentEnrollmentId == null || record.Id != currentEnrollmentId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("This employee already has an active provident fund enrollment.");
        }
    }

    private async Task EnsureNoDuplicatePostedContributionBatchAsync(int month, int year, Guid? policyId, bool isSupplemental, CancellationToken cancellationToken)
    {
        if (isSupplemental)
        {
            return;
        }

        var exists = await _dbContext.ProvidentFundContributionBatches.AnyAsync(
            record =>
                record.Month == month &&
                record.Year == year &&
                record.PolicyId == policyId &&
                !record.IsSupplemental &&
                record.Status == ProvidentFundContributionBatchStatuses.Posted,
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("A posted provident fund contribution batch already exists for this month and policy. Create a supplemental batch for corrections.");
        }
    }

    private async Task<string> GenerateBatchNumberAsync(int month, int year, CancellationToken cancellationToken)
    {
        var count = await _dbContext.ProvidentFundContributionBatches.CountAsync(record => record.Month == month && record.Year == year, cancellationToken) + 1;
        return $"PFB-{year}{month:00}-{count:000}";
    }

    private async Task<string> GenerateLedgerTransactionNumberAsync(CancellationToken cancellationToken)
    {
        var count = await _dbContext.ProvidentFundLedgerTransactions.CountAsync(cancellationToken) + 1;
        return $"PFL-{DateTime.UtcNow:yyyyMMdd}-{count:000000}";
    }

    private async Task<string> GenerateWithdrawalNumberAsync(CancellationToken cancellationToken)
    {
        var count = await _dbContext.ProvidentFundWithdrawalRequests.CountAsync(cancellationToken) + 1;
        return $"PFW-{DateTime.UtcNow:yyyyMMdd}-{count:0000}";
    }

    private static void ValidatePolicy(SaveProvidentFundPolicyRequestDto request)
    {
        _ = NormalizeContributionType(request.EmployeeContributionType);
        _ = NormalizeContributionType(request.EmployerContributionType);
        _ = NormalizePolicyStatus(request.Status);

        if (!string.Equals(request.ContributionFrequency.Trim(), "monthly", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Provident fund contribution frequency must be monthly.");
        }

        if (request.EffectiveDate is null)
        {
            throw new BadRequestException("Effective date is required.");
        }

        ValidateContributionValue(request.EmployeeContributionType, request.EmployeeContributionValue, "Employee contribution");
        ValidateContributionValue(request.EmployerContributionType, request.EmployerContributionValue, "Employer contribution");
    }

    private static void ValidateContributionValue(string contributionType, decimal value, string label)
    {
        if (value < 0m)
        {
            throw new BadRequestException($"{label} cannot be negative.");
        }

        if (NormalizeContributionType(contributionType) == ProvidentFundContributionTypes.Percentage && value > 100m)
        {
            throw new BadRequestException($"{label} percentage must be between 0 and 100.");
        }
    }

    private static void ValidateVestingRule(SaveProvidentFundVestingRuleRequestDto request)
    {
        if (request.PolicyId is null || request.PolicyId.Value == Guid.Empty)
        {
            throw new BadRequestException("Policy is required.");
        }

        if (request.VestedPercentage is < 0m or > 100m)
        {
            throw new BadRequestException("Vesting percentage must be between 0 and 100.");
        }
    }

    private static void ValidateEnrollment(SaveProvidentFundEnrollmentRequestDto request)
    {
        if (request.EmployeeId is null || request.EmployeeId.Value == Guid.Empty)
        {
            throw new BadRequestException("Employee is required.");
        }

        if (request.PolicyId is null || request.PolicyId.Value == Guid.Empty)
        {
            throw new BadRequestException("Provident fund policy is required.");
        }

        if (request.EnrollmentDate is null || request.VestingStartDate is null)
        {
            throw new BadRequestException("Enrollment date and vesting start date are required.");
        }

        _ = NormalizeEnrollmentStatus(request.Status);
        ValidateOptionalOverride(request.EmployeeContributionOverrideType, request.EmployeeContributionOverrideValue, "Employee override");
        ValidateOptionalOverride(request.EmployerContributionOverrideType, request.EmployerContributionOverrideValue, "Employer override");
    }

    private static void ValidateOptionalOverride(string contributionType, decimal? value, string label)
    {
        if (value is null && string.IsNullOrWhiteSpace(contributionType))
        {
            return;
        }

        if (value is null)
        {
            throw new BadRequestException($"{label} value is required when an override type is supplied.");
        }

        ValidateContributionValue(contributionType, value.Value, label);
    }

    private static void ValidateMonthYear(int month, int year)
    {
        if (month is < 1 or > 12)
        {
            throw new BadRequestException("Month must be between 1 and 12.");
        }

        if (year is < 2000 or > 2200)
        {
            throw new BadRequestException("Year must be between 2000 and 2200.");
        }
    }

    private static void ValidateWithdrawalRequest(SaveProvidentFundWithdrawalRequestDto request)
    {
        _ = NormalizeWithdrawalType(request.WithdrawalType);
        if (request.RequestDate is null)
        {
            throw new BadRequestException("Request date is required.");
        }

        if (request.RequestedAmount <= 0m)
        {
            throw new BadRequestException("Withdrawal amount must be greater than zero.");
        }
    }

    private static void ValidateAdjustment(SaveProvidentFundAdjustmentRequestDto request)
    {
        if (request.EmployeeId is null || request.EmployeeId.Value == Guid.Empty)
        {
            throw new BadRequestException("Employee is required.");
        }

        _ = NormalizeAdjustmentType(request.AdjustmentType);
        _ = NormalizeShareType(request.ShareAffected);

        if (request.AdjustmentDate is null)
        {
            throw new BadRequestException("Adjustment date is required.");
        }

        if (request.Amount <= 0m)
        {
            throw new BadRequestException("Adjustment amount must be greater than zero.");
        }
    }

    private static string NormalizeContributionType(string value)
    {
        var normalized = NormalizeStatus(value);
        return normalized switch
        {
            "percentage" => ProvidentFundContributionTypes.Percentage,
            "fixed" or "fixed_amount" => ProvidentFundContributionTypes.FixedAmount,
            _ => throw new BadRequestException("Contribution type must be percentage or fixed amount.")
        };
    }

    private static string NormalizeOptionalContributionType(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : NormalizeContributionType(value);
    }

    private static string NormalizePolicyStatus(string value)
    {
        var normalized = NormalizeStatus(value);
        if (!ProvidentFundPolicyStatuses.All.Contains(normalized))
        {
            throw new BadRequestException("Policy status must be active or inactive.");
        }

        return normalized;
    }

    private static string NormalizeEnrollmentStatus(string value)
    {
        var normalized = NormalizeStatus(value);
        if (!ProvidentFundEnrollmentStatuses.All.Contains(normalized))
        {
            throw new BadRequestException("Enrollment status must be active, suspended, or closed.");
        }

        return normalized;
    }

    private static string NormalizeBatchStatus(string value)
    {
        var normalized = NormalizeStatus(value);
        if (!ProvidentFundContributionBatchStatuses.All.Contains(normalized))
        {
            throw new BadRequestException("Contribution batch status is invalid.");
        }

        return normalized;
    }

    private static string NormalizeWithdrawalType(string value)
    {
        var normalized = NormalizeStatus(value);
        if (!ProvidentFundWithdrawalTypes.All.Contains(normalized))
        {
            throw new BadRequestException("Withdrawal type is invalid.");
        }

        return normalized;
    }

    private static string NormalizeAdjustmentType(string value)
    {
        var normalized = NormalizeStatus(value);
        if (!ProvidentFundAdjustmentTypes.All.Contains(normalized))
        {
            throw new BadRequestException("Adjustment type must be credit or debit.");
        }

        return normalized;
    }

    private static string NormalizeAdjustmentStatus(string value)
    {
        var normalized = NormalizeStatus(value);
        if (!ProvidentFundAdjustmentStatuses.All.Contains(normalized))
        {
            throw new BadRequestException("Adjustment status is invalid.");
        }

        return normalized;
    }

    private static string NormalizeShareType(string value)
    {
        var normalized = NormalizeStatus(value);
        if (!ProvidentFundShareTypes.All.Contains(normalized))
        {
            throw new BadRequestException("Share affected must be employee, employer, voluntary, or interest.");
        }

        return normalized;
    }

    private static string NormalizeStatus(string value)
    {
        return value.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
    }

    private static string ResolveContributionType(string overrideType, string policyType)
    {
        return string.IsNullOrWhiteSpace(overrideType) ? policyType : overrideType;
    }

    public static decimal CalculateContribution(decimal basicSalary, string contributionType, decimal contributionValue)
    {
        return ProvidentFundCalculator.CalculateContribution(basicSalary, contributionType, contributionValue);
    }

    private static void ApplyShareAmount(ProvidentFundLedgerTransaction ledger, string shareType, decimal amount)
    {
        switch (shareType)
        {
            case ProvidentFundShareTypes.Employee:
                ledger.EmployeeShareAmount = amount;
                break;
            case ProvidentFundShareTypes.Employer:
                ledger.EmployerShareAmount = amount;
                break;
            case ProvidentFundShareTypes.Voluntary:
                ledger.VoluntaryShareAmount = amount;
                break;
            case ProvidentFundShareTypes.Interest:
                ledger.InterestAmount = amount;
                break;
        }
    }

    private static int CalculateWholeYears(DateOnly start, DateOnly end)
    {
        return ProvidentFundCalculator.CalculateWholeYears(start, end);
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal RoundPercentage(decimal value)
    {
        return Math.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    private static string? NormalizeUserId(string? userId)
    {
        return string.IsNullOrWhiteSpace(userId) ? null : userId;
    }

    private static string BuildFullName(string firstName, string middleName, string lastName, string suffix)
    {
        return string.Join(" ", new[] { firstName.Trim(), middleName.Trim(), lastName.Trim(), suffix.Trim() }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string BuildUserDisplayName(ApplicationUser? user)
    {
        return user is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.Email ?? string.Empty
                : user.DisplayName;
    }

    private static ProvidentFundPolicyRecordDto MapPolicy(ProvidentFundPolicy record)
    {
        return new ProvidentFundPolicyRecordDto
        {
            Id = record.Id,
            PolicyName = record.PolicyName,
            Description = record.Description,
            EligibilityRules = record.EligibilityRules,
            EmployeeContributionType = record.EmployeeContributionType,
            EmployeeContributionValue = record.EmployeeContributionValue,
            EmployerContributionType = record.EmployerContributionType,
            EmployerContributionValue = record.EmployerContributionValue,
            ContributionFrequency = record.ContributionFrequency,
            EffectiveDate = record.EffectiveDate,
            Status = record.Status,
            AllowVoluntaryContribution = record.AllowVoluntaryContribution,
            AllowWithdrawal = record.AllowWithdrawal,
            AllowLoan = record.AllowLoan,
            Remarks = record.Remarks,
            VestingRuleCount = record.VestingRules.Count,
            ActiveEnrollmentCount = record.Enrollments.Count(item => item.Status == ProvidentFundEnrollmentStatuses.Active),
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static ProvidentFundVestingRuleDto MapVestingRule(ProvidentFundVestingRule record)
    {
        return new ProvidentFundVestingRuleDto
        {
            Id = record.Id,
            PolicyId = record.PolicyId,
            PolicyName = record.Policy?.PolicyName ?? string.Empty,
            YearsOfService = record.YearsOfService,
            VestedPercentage = record.VestedPercentage,
            Remarks = record.Remarks,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static ProvidentFundEnrollmentRecordDto MapEnrollment(ProvidentFundEnrollment record, ProvidentFundBalanceDto balance)
    {
        return new ProvidentFundEnrollmentRecordDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = record.Employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = record.Employee is null ? string.Empty : BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
            DepartmentName = record.Employee?.Department?.Name ?? string.Empty,
            PolicyId = record.PolicyId,
            PolicyName = record.Policy?.PolicyName ?? string.Empty,
            EnrollmentDate = record.EnrollmentDate,
            VestingStartDate = record.VestingStartDate,
            EmployeeContributionOverrideType = record.EmployeeContributionOverrideType,
            EmployeeContributionOverrideValue = record.EmployeeContributionOverrideValue,
            EmployerContributionOverrideType = record.EmployerContributionOverrideType,
            EmployerContributionOverrideValue = record.EmployerContributionOverrideValue,
            Status = record.Status,
            Remarks = record.Remarks,
            GrossBalance = balance.GrossFundBalance,
            WithdrawableBalance = balance.WithdrawableBalance,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static ProvidentFundContributionBatchSummaryDto MapContributionBatch(ProvidentFundContributionBatch record)
    {
        return new ProvidentFundContributionBatchSummaryDto
        {
            Id = record.Id,
            BatchNumber = record.BatchNumber,
            Month = record.Month,
            Year = record.Year,
            PolicyId = record.PolicyId,
            PolicyName = record.Policy?.PolicyName ?? string.Empty,
            IsSupplemental = record.IsSupplemental,
            Status = record.Status,
            LineCount = record.Lines.Count,
            TotalEmployeeContribution = record.Lines.Sum(item => item.EmployeeContribution),
            TotalEmployerContribution = record.Lines.Sum(item => item.EmployerContribution),
            TotalVoluntaryContribution = record.Lines.Sum(item => item.VoluntaryContribution),
            TotalContribution = record.Lines.Sum(item => item.TotalContribution),
            CreatedByDisplayName = BuildUserDisplayName(record.CreatedByUser),
            ReviewedByDisplayName = BuildUserDisplayName(record.ReviewedByUser),
            PostedByDisplayName = BuildUserDisplayName(record.PostedByUser),
            PostingDate = record.PostingDate,
            Remarks = record.Remarks,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static ProvidentFundContributionBatchDetailDto MapContributionBatchDetail(ProvidentFundContributionBatch record)
    {
        return new ProvidentFundContributionBatchDetailDto
        {
            Batch = MapContributionBatch(record),
            Lines = record.Lines
                .OrderBy(line => line.Employee?.LastName)
                .ThenBy(line => line.Employee?.FirstName)
                .Select(line => new ProvidentFundContributionBatchLineDto
                {
                    Id = line.Id,
                    BatchId = line.BatchId,
                    EmployeeId = line.EmployeeId,
                    EmployeeCode = line.Employee?.EmployeeCode ?? string.Empty,
                    EmployeeFullName = line.Employee is null ? string.Empty : BuildFullName(line.Employee.FirstName, line.Employee.MiddleName, line.Employee.LastName, line.Employee.Suffix),
                    DepartmentName = line.Employee?.Department?.Name ?? string.Empty,
                    EnrollmentId = line.EnrollmentId,
                    BasicSalary = line.BasicSalary,
                    EmployeeContribution = line.EmployeeContribution,
                    EmployerContribution = line.EmployerContribution,
                    VoluntaryContribution = line.VoluntaryContribution,
                    TotalContribution = line.TotalContribution,
                    Status = line.Status,
                    Remarks = line.Remarks
                })
                .ToList()
        };
    }

    private static ProvidentFundLedgerTransactionDto MapLedgerTransaction(ProvidentFundLedgerTransaction record)
    {
        return new ProvidentFundLedgerTransactionDto
        {
            Id = record.Id,
            TransactionNumber = record.TransactionNumber,
            EmployeeId = record.EmployeeId,
            EmployeeCode = record.Employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = record.Employee is null ? string.Empty : BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
            DepartmentName = record.Employee?.Department?.Name ?? string.Empty,
            EnrollmentId = record.EnrollmentId,
            PolicyId = record.PolicyId,
            PolicyName = record.Policy?.PolicyName ?? string.Empty,
            TransactionDate = record.TransactionDate,
            TransactionType = record.TransactionType,
            SourceType = record.SourceType,
            SourceReferenceId = record.SourceReferenceId,
            EmployeeShareAmount = record.EmployeeShareAmount,
            EmployerShareAmount = record.EmployerShareAmount,
            VoluntaryShareAmount = record.VoluntaryShareAmount,
            InterestAmount = record.InterestAmount,
            DebitAmount = record.DebitAmount,
            CreditAmount = record.CreditAmount,
            RunningBalance = record.RunningBalance,
            Remarks = record.Remarks,
            CreatedByDisplayName = BuildUserDisplayName(record.CreatedByUser),
            CreatedAtUtc = record.CreatedAtUtc,
            IsReversed = record.IsReversed,
            ReversalReferenceId = record.ReversalReferenceId
        };
    }

    private static ProvidentFundWithdrawalRequestDto MapWithdrawal(ProvidentFundWithdrawalRequest record)
    {
        return new ProvidentFundWithdrawalRequestDto
        {
            Id = record.Id,
            RequestNumber = record.RequestNumber,
            EmployeeId = record.EmployeeId,
            EmployeeCode = record.Employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = record.Employee is null ? string.Empty : BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
            DepartmentName = record.Employee?.Department?.Name ?? string.Empty,
            EnrollmentId = record.EnrollmentId,
            RequestDate = record.RequestDate,
            WithdrawalType = record.WithdrawalType,
            RequestedAmount = record.RequestedAmount,
            EligibleWithdrawableAmount = record.EligibleWithdrawableAmount,
            ApprovedAmount = record.ApprovedAmount,
            Reason = record.Reason,
            AttachmentPath = record.AttachmentPath,
            Status = record.Status,
            PaymentDate = record.PaymentDate,
            Remarks = record.Remarks,
            Approvals = record.Approvals.OrderBy(item => item.CreatedAtUtc).Select(item => new ProvidentFundApprovalHistoryDto
            {
                Id = item.Id,
                StepName = item.StepName,
                Action = item.Action,
                ActorName = !string.IsNullOrWhiteSpace(item.ActorNameSnapshot) ? item.ActorNameSnapshot : BuildUserDisplayName(item.ActorUser),
                Remarks = item.Remarks,
                CreatedAtUtc = item.CreatedAtUtc
            }).ToList(),
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static ProvidentFundAdjustmentDto MapAdjustment(ProvidentFundAdjustment record)
    {
        return new ProvidentFundAdjustmentDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = record.Employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = record.Employee is null ? string.Empty : BuildFullName(record.Employee.FirstName, record.Employee.MiddleName, record.Employee.LastName, record.Employee.Suffix),
            DepartmentName = record.Employee?.Department?.Name ?? string.Empty,
            EnrollmentId = record.EnrollmentId,
            AdjustmentType = record.AdjustmentType,
            AdjustmentDate = record.AdjustmentDate,
            Amount = record.Amount,
            ShareAffected = record.ShareAffected,
            Reason = record.Reason,
            AttachmentPath = record.AttachmentPath,
            Status = record.Status,
            RequestedByDisplayName = BuildUserDisplayName(record.RequestedByUser),
            ApprovedByDisplayName = BuildUserDisplayName(record.ApprovedByUser),
            ApprovedAtUtc = record.ApprovedAtUtc,
            PostedAtUtc = record.PostedAtUtc,
            DecisionRemarks = record.DecisionRemarks,
            Approvals = record.Approvals.OrderBy(item => item.CreatedAtUtc).Select(item => new ProvidentFundApprovalHistoryDto
            {
                Id = item.Id,
                StepName = "adjustment",
                Action = item.Action,
                ActorName = !string.IsNullOrWhiteSpace(item.ActorNameSnapshot) ? item.ActorNameSnapshot : BuildUserDisplayName(item.ActorUser),
                Remarks = item.Remarks,
                CreatedAtUtc = item.CreatedAtUtc
            }).ToList(),
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
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
}
