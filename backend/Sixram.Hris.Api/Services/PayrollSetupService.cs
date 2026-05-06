using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Attendance;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Payroll;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IPayrollSetupService
{
    Task<PayrollSetupSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<PayrollOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);

    Task<PayrollSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task<PayrollSettingsDto> UpdateSettingsAsync(PayrollSettingsDto request, CancellationToken cancellationToken = default);

    Task<PagedResultDto<PayPeriodTemplateRecordDto>> GetPayPeriodTemplatesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default);

    Task<PayPeriodTemplateRecordDto> CreatePayPeriodTemplateAsync(SavePayPeriodTemplateRequestDto request, CancellationToken cancellationToken = default);

    Task<PayPeriodTemplateRecordDto> UpdatePayPeriodTemplateAsync(Guid payPeriodTemplateId, SavePayPeriodTemplateRequestDto request, CancellationToken cancellationToken = default);

    Task DeletePayPeriodTemplateAsync(Guid payPeriodTemplateId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<EarningTypeRecordDto>> GetEarningTypesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default);

    Task<EarningTypeRecordDto> CreateEarningTypeAsync(SaveEarningTypeRequestDto request, CancellationToken cancellationToken = default);

    Task<EarningTypeRecordDto> UpdateEarningTypeAsync(Guid earningTypeId, SaveEarningTypeRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteEarningTypeAsync(Guid earningTypeId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<DeductionTypeRecordDto>> GetDeductionTypesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default);

    Task<DeductionTypeRecordDto> CreateDeductionTypeAsync(SaveDeductionTypeRequestDto request, CancellationToken cancellationToken = default);

    Task<DeductionTypeRecordDto> UpdateDeductionTypeAsync(Guid deductionTypeId, SaveDeductionTypeRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteDeductionTypeAsync(Guid deductionTypeId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<ContributionTypeRecordDto>> GetContributionTypesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default);

    Task<ContributionTypeRecordDto> CreateContributionTypeAsync(SaveContributionTypeRequestDto request, CancellationToken cancellationToken = default);

    Task<ContributionTypeRecordDto> UpdateContributionTypeAsync(Guid contributionTypeId, SaveContributionTypeRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteContributionTypeAsync(Guid contributionTypeId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<GovernmentContributionTableRecordDto>> GetGovernmentContributionTablesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default);

    Task<GovernmentContributionTableRecordDto> CreateGovernmentContributionTableAsync(SaveGovernmentContributionTableRequestDto request, CancellationToken cancellationToken = default);

    Task<GovernmentContributionTableRecordDto> UpdateGovernmentContributionTableAsync(Guid contributionTableId, SaveGovernmentContributionTableRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteGovernmentContributionTableAsync(Guid contributionTableId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<TaxTableRecordDto>> GetTaxTablesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default);

    Task<TaxTableRecordDto> CreateTaxTableAsync(SaveTaxTableRequestDto request, CancellationToken cancellationToken = default);

    Task<TaxTableRecordDto> UpdateTaxTableAsync(Guid taxTableId, SaveTaxTableRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteTaxTableAsync(Guid taxTableId, CancellationToken cancellationToken = default);
}

public class PayrollSetupService : IPayrollSetupService
{
    private readonly ApplicationDbContext _dbContext;

    public PayrollSetupService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PayrollSetupSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        return new PayrollSetupSummaryDto
        {
            PayPeriodTemplateCount = await _dbContext.PayPeriodTemplates.CountAsync(cancellationToken),
            ActivePayPeriodTemplateCount = await _dbContext.PayPeriodTemplates.CountAsync(record => record.IsActive, cancellationToken),
            EarningTypeCount = await _dbContext.EarningTypes.CountAsync(cancellationToken),
            ActiveEarningTypeCount = await _dbContext.EarningTypes.CountAsync(record => record.IsActive, cancellationToken),
            DeductionTypeCount = await _dbContext.DeductionTypes.CountAsync(cancellationToken),
            ActiveDeductionTypeCount = await _dbContext.DeductionTypes.CountAsync(record => record.IsActive, cancellationToken),
            ContributionTypeCount = await _dbContext.ContributionTypes.CountAsync(cancellationToken),
            ActiveContributionTypeCount = await _dbContext.ContributionTypes.CountAsync(record => record.IsActive, cancellationToken),
            GovernmentContributionTableCount = await _dbContext.GovernmentContributionTables.CountAsync(cancellationToken),
            ActiveGovernmentContributionTableCount = await _dbContext.GovernmentContributionTables.CountAsync(record => record.IsActive, cancellationToken),
            TaxTableCount = await _dbContext.TaxTables.CountAsync(cancellationToken),
            ActiveTaxTableCount = await _dbContext.TaxTables.CountAsync(record => record.IsActive, cancellationToken)
        };
    }

    public async Task<PayrollOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        return new PayrollOptionsDto
        {
            Employees = await _dbContext.Employees
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
                .ToListAsync(cancellationToken),
            Departments = await _dbContext.Departments
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new LookupOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            Branches = await _dbContext.Branches
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new LookupOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            EmploymentTypes = await _dbContext.EmploymentTypes
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new LookupOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            EmploymentStatuses = await _dbContext.EmploymentStatuses
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new LookupOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            PayPeriods = await _dbContext.PayPeriods
                .AsNoTracking()
                .Where(record => record.Status != PayPeriodStatuses.Cancelled)
                .OrderByDescending(record => record.PeriodStartDate)
                .ThenByDescending(record => record.PayrollDate)
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
                .ToListAsync(cancellationToken),
            PayPeriodTemplates = await _dbContext.PayPeriodTemplates
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new PayPeriodTemplateOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    PayFrequency = record.PayFrequency,
                    PeriodLengthDays = record.PeriodLengthDays,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            EarningTypes = await _dbContext.EarningTypes
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new EarningTypeOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    Category = record.Category,
                    Taxable = record.Taxable,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            DeductionTypes = await _dbContext.DeductionTypes
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new DeductionTypeOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    Category = record.Category,
                    PreTax = record.PreTax,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            ContributionTypes = await _dbContext.ContributionTypes
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new ContributionTypeOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            TaxTables = await _dbContext.TaxTables
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new TaxTableOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    PayFrequency = record.PayFrequency,
                    IsActive = record.IsActive
                })
                .ToListAsync(cancellationToken),
            PayTypes = PayrollPayTypes.All,
            PayFrequencies = PayrollPayFrequencies.All,
            RunStatuses = PayrollRunStatuses.All,
            AdjustmentStatuses = PayrollAdjustmentStatuses.All,
            AdjustmentTypes = PayrollAdjustmentTypes.All
        };
    }

    public async Task<PayrollSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.PayrollSettings
            .AsNoTracking()
            .ToDictionaryAsync(record => record.Key, record => record.Value, cancellationToken);

        return new PayrollSettingsDto
        {
            DefaultPayFrequency = GetSetting(settings, PayrollSettingKeys.DefaultPayFrequency, PayrollPayFrequencies.SemiMonthly),
            DefaultWorkingDaysPerMonth = ParseDecimalSetting(settings, PayrollSettingKeys.DefaultWorkingDaysPerMonth, 22m),
            DefaultWorkingHoursPerDay = ParseDecimalSetting(settings, PayrollSettingKeys.DefaultWorkingHoursPerDay, 8m),
            LateUndertimeDeductionPolicy = GetSetting(settings, PayrollSettingKeys.LateUndertimeDeductionPolicy, "minute_based"),
            AbsenceDeductionPolicy = GetSetting(settings, PayrollSettingKeys.AbsenceDeductionPolicy, "day_based"),
            OvertimeCalculationPolicy = GetSetting(settings, PayrollSettingKeys.OvertimeCalculationPolicy, "preliminary_only"),
            RoundingRule = GetSetting(settings, PayrollSettingKeys.RoundingRule, "round_2"),
            PayrollTimeZoneId = GetSetting(settings, PayrollSettingKeys.PayrollTimeZoneId, "Singapore Standard Time"),
            PayslipVisibilityRule = GetSetting(settings, PayrollSettingKeys.PayslipVisibilityRule, "approved_or_paid"),
            AllowNegativeNetPay = ParseBoolSetting(settings, PayrollSettingKeys.AllowNegativeNetPay, false),
            DefaultCurrency = GetSetting(settings, PayrollSettingKeys.DefaultCurrency, "PHP")
        };
    }

    public async Task<PayrollSettingsDto> UpdateSettingsAsync(PayrollSettingsDto request, CancellationToken cancellationToken = default)
    {
        ValidatePayFrequency(request.DefaultPayFrequency);

        await UpsertSettingAsync(PayrollSettingKeys.DefaultPayFrequency, request.DefaultPayFrequency.Trim().ToLowerInvariant(), "Default payroll frequency.", cancellationToken);
        await UpsertSettingAsync(PayrollSettingKeys.DefaultWorkingDaysPerMonth, request.DefaultWorkingDaysPerMonth.ToString("0.##"), "Default working days per month.", cancellationToken);
        await UpsertSettingAsync(PayrollSettingKeys.DefaultWorkingHoursPerDay, request.DefaultWorkingHoursPerDay.ToString("0.##"), "Default working hours per day.", cancellationToken);
        await UpsertSettingAsync(PayrollSettingKeys.LateUndertimeDeductionPolicy, request.LateUndertimeDeductionPolicy.Trim(), "Late and undertime deduction policy.", cancellationToken);
        await UpsertSettingAsync(PayrollSettingKeys.AbsenceDeductionPolicy, request.AbsenceDeductionPolicy.Trim(), "Absence deduction policy.", cancellationToken);
        await UpsertSettingAsync(PayrollSettingKeys.OvertimeCalculationPolicy, request.OvertimeCalculationPolicy.Trim(), "Overtime calculation policy.", cancellationToken);
        await UpsertSettingAsync(PayrollSettingKeys.RoundingRule, request.RoundingRule.Trim(), "Rounding rule for payroll values.", cancellationToken);
        await UpsertSettingAsync(PayrollSettingKeys.PayrollTimeZoneId, request.PayrollTimeZoneId.Trim(), "Payroll business timezone.", cancellationToken);
        await UpsertSettingAsync(PayrollSettingKeys.PayslipVisibilityRule, request.PayslipVisibilityRule.Trim(), "Payslip visibility rule.", cancellationToken);
        await UpsertSettingAsync(PayrollSettingKeys.AllowNegativeNetPay, request.AllowNegativeNetPay ? "true" : "false", "Whether negative net pay is allowed.", cancellationToken);
        await UpsertSettingAsync(PayrollSettingKeys.DefaultCurrency, request.DefaultCurrency.Trim().ToUpperInvariant(), "Default payroll currency.", cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetSettingsAsync(cancellationToken);
    }

    public async Task<PagedResultDto<PayPeriodTemplateRecordDto>> GetPayPeriodTemplatesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.PayPeriodTemplates.AsNoTracking().AsQueryable();
        source = ApplySetupFilters(source, query, nameof(PayPeriodTemplate.Code), nameof(PayPeriodTemplate.Name));
        source = ApplyTemplateSort(source, query.SortBy, query.Descending);

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(record => new PayPeriodTemplateRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                PayFrequency = record.PayFrequency,
                PeriodLengthDays = record.PeriodLengthDays,
                PayrollOffsetDays = record.PayrollOffsetDays,
                IsActive = record.IsActive,
                PayPeriodCount = record.PayPeriods.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return ToPage(records, query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<PayPeriodTemplateRecordDto> CreatePayPeriodTemplateAsync(SavePayPeriodTemplateRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatePayFrequency(request.PayFrequency);
        await EnsureUniqueTemplateCodeAsync(request.Code, null, cancellationToken);
        await EnsureUniqueTemplateNameAsync(request.Name, null, cancellationToken);

        var record = new PayPeriodTemplate
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            PayFrequency = request.PayFrequency.Trim().ToLowerInvariant(),
            PeriodLengthDays = request.PeriodLengthDays,
            PayrollOffsetDays = request.PayrollOffsetDays,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.PayPeriodTemplates.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetPayPeriodTemplateByIdAsync(record.Id, cancellationToken);
    }

    public async Task<PayPeriodTemplateRecordDto> UpdatePayPeriodTemplateAsync(Guid payPeriodTemplateId, SavePayPeriodTemplateRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatePayFrequency(request.PayFrequency);
        var record = await _dbContext.PayPeriodTemplates.SingleOrDefaultAsync(item => item.Id == payPeriodTemplateId, cancellationToken)
            ?? throw new NotFoundException($"Pay period template '{payPeriodTemplateId}' was not found.");

        await EnsureUniqueTemplateCodeAsync(request.Code, payPeriodTemplateId, cancellationToken);
        await EnsureUniqueTemplateNameAsync(request.Name, payPeriodTemplateId, cancellationToken);

        record.Code = request.Code.Trim().ToUpperInvariant();
        record.Name = request.Name.Trim();
        record.Description = request.Description.Trim();
        record.PayFrequency = request.PayFrequency.Trim().ToLowerInvariant();
        record.PeriodLengthDays = request.PeriodLengthDays;
        record.PayrollOffsetDays = request.PayrollOffsetDays;
        record.IsActive = request.IsActive;
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetPayPeriodTemplateByIdAsync(record.Id, cancellationToken);
    }

    public async Task DeletePayPeriodTemplateAsync(Guid payPeriodTemplateId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.PayPeriodTemplates
            .Include(item => item.PayPeriods)
            .SingleOrDefaultAsync(item => item.Id == payPeriodTemplateId, cancellationToken)
            ?? throw new NotFoundException($"Pay period template '{payPeriodTemplateId}' was not found.");

        if (record.PayPeriods.Count > 0)
        {
            throw new ConflictException("This pay period template is already used by existing pay periods. Deactivate it instead of deleting it.");
        }

        _dbContext.PayPeriodTemplates.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<EarningTypeRecordDto>> GetEarningTypesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.EarningTypes.AsNoTracking().AsQueryable();
        source = ApplySetupFilters(source, query, nameof(EarningType.Code), nameof(EarningType.Name));
        source = ApplySimpleNameSort(source, query.SortBy, query.Descending, record => record.Code, record => record.Name, record => record.CreatedAtUtc);

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(record => new EarningTypeRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                Category = record.Category,
                Taxable = record.Taxable,
                Recurring = record.Recurring,
                AffectsThirteenthMonth = record.AffectsThirteenthMonth,
                IsActive = record.IsActive,
                EmployeeRecurringCount = record.EmployeeRecurringEarnings.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return ToPage(records, query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<EarningTypeRecordDto> CreateEarningTypeAsync(SaveEarningTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateEarningCategory(request.Category);
        await EnsureUniqueEarningTypeCodeAsync(request.Code, null, cancellationToken);
        await EnsureUniqueEarningTypeNameAsync(request.Name, null, cancellationToken);

        var record = new EarningType
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim().ToLowerInvariant(),
            Taxable = request.Taxable,
            Recurring = request.Recurring,
            AffectsThirteenthMonth = request.AffectsThirteenthMonth,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.EarningTypes.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetEarningTypeByIdAsync(record.Id, cancellationToken);
    }

    public async Task<EarningTypeRecordDto> UpdateEarningTypeAsync(Guid earningTypeId, SaveEarningTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateEarningCategory(request.Category);
        var record = await _dbContext.EarningTypes.SingleOrDefaultAsync(item => item.Id == earningTypeId, cancellationToken)
            ?? throw new NotFoundException($"Earning type '{earningTypeId}' was not found.");

        await EnsureUniqueEarningTypeCodeAsync(request.Code, earningTypeId, cancellationToken);
        await EnsureUniqueEarningTypeNameAsync(request.Name, earningTypeId, cancellationToken);

        record.Code = request.Code.Trim().ToUpperInvariant();
        record.Name = request.Name.Trim();
        record.Description = request.Description.Trim();
        record.Category = request.Category.Trim().ToLowerInvariant();
        record.Taxable = request.Taxable;
        record.Recurring = request.Recurring;
        record.AffectsThirteenthMonth = request.AffectsThirteenthMonth;
        record.IsActive = request.IsActive;
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetEarningTypeByIdAsync(record.Id, cancellationToken);
    }

    public async Task DeleteEarningTypeAsync(Guid earningTypeId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.EarningTypes
            .Include(item => item.EmployeeRecurringEarnings)
            .Include(item => item.PayrollEarningLines)
            .Include(item => item.PayrollAdjustments)
            .SingleOrDefaultAsync(item => item.Id == earningTypeId, cancellationToken)
            ?? throw new NotFoundException($"Earning type '{earningTypeId}' was not found.");

        if (record.EmployeeRecurringEarnings.Count > 0 || record.PayrollEarningLines.Count > 0 || record.PayrollAdjustments.Count > 0)
        {
            throw new ConflictException("This earning type is already used. Deactivate it instead of deleting it.");
        }

        _dbContext.EarningTypes.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<DeductionTypeRecordDto>> GetDeductionTypesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.DeductionTypes.AsNoTracking().AsQueryable();
        source = ApplySetupFilters(source, query, nameof(DeductionType.Code), nameof(DeductionType.Name));
        source = ApplySimpleNameSort(source, query.SortBy, query.Descending, record => record.Code, record => record.Name, record => record.CreatedAtUtc);

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(record => new DeductionTypeRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                Category = record.Category,
                PreTax = record.PreTax,
                Recurring = record.Recurring,
                IsActive = record.IsActive,
                EmployeeRecurringCount = record.EmployeeRecurringDeductions.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return ToPage(records, query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<DeductionTypeRecordDto> CreateDeductionTypeAsync(SaveDeductionTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateDeductionCategory(request.Category);
        await EnsureUniqueDeductionTypeCodeAsync(request.Code, null, cancellationToken);
        await EnsureUniqueDeductionTypeNameAsync(request.Name, null, cancellationToken);

        var record = new DeductionType
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim().ToLowerInvariant(),
            PreTax = request.PreTax,
            Recurring = request.Recurring,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.DeductionTypes.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetDeductionTypeByIdAsync(record.Id, cancellationToken);
    }

    public async Task<DeductionTypeRecordDto> UpdateDeductionTypeAsync(Guid deductionTypeId, SaveDeductionTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateDeductionCategory(request.Category);
        var record = await _dbContext.DeductionTypes.SingleOrDefaultAsync(item => item.Id == deductionTypeId, cancellationToken)
            ?? throw new NotFoundException($"Deduction type '{deductionTypeId}' was not found.");

        await EnsureUniqueDeductionTypeCodeAsync(request.Code, deductionTypeId, cancellationToken);
        await EnsureUniqueDeductionTypeNameAsync(request.Name, deductionTypeId, cancellationToken);

        record.Code = request.Code.Trim().ToUpperInvariant();
        record.Name = request.Name.Trim();
        record.Description = request.Description.Trim();
        record.Category = request.Category.Trim().ToLowerInvariant();
        record.PreTax = request.PreTax;
        record.Recurring = request.Recurring;
        record.IsActive = request.IsActive;
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetDeductionTypeByIdAsync(record.Id, cancellationToken);
    }

    public async Task DeleteDeductionTypeAsync(Guid deductionTypeId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.DeductionTypes
            .Include(item => item.EmployeeRecurringDeductions)
            .Include(item => item.PayrollDeductionLines)
            .Include(item => item.PayrollAdjustments)
            .SingleOrDefaultAsync(item => item.Id == deductionTypeId, cancellationToken)
            ?? throw new NotFoundException($"Deduction type '{deductionTypeId}' was not found.");

        if (record.EmployeeRecurringDeductions.Count > 0 || record.PayrollDeductionLines.Count > 0 || record.PayrollAdjustments.Count > 0)
        {
            throw new ConflictException("This deduction type is already used. Deactivate it instead of deleting it.");
        }

        _dbContext.DeductionTypes.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<ContributionTypeRecordDto>> GetContributionTypesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.ContributionTypes.AsNoTracking().AsQueryable();
        source = ApplySetupFilters(source, query, nameof(ContributionType.Code), nameof(ContributionType.Name));
        source = ApplySimpleNameSort(source, query.SortBy, query.Descending, record => record.Code, record => record.Name, record => record.CreatedAtUtc);

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(record => new ContributionTypeRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                EmployeeShareApplicable = record.EmployeeShareApplicable,
                EmployerShareApplicable = record.EmployerShareApplicable,
                IsActive = record.IsActive,
                TableCount = record.GovernmentContributionTables.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return ToPage(records, query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<ContributionTypeRecordDto> CreateContributionTypeAsync(SaveContributionTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        await EnsureUniqueContributionTypeCodeAsync(request.Code, null, cancellationToken);
        await EnsureUniqueContributionTypeNameAsync(request.Name, null, cancellationToken);

        var record = new ContributionType
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            EmployeeShareApplicable = request.EmployeeShareApplicable,
            EmployerShareApplicable = request.EmployerShareApplicable,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ContributionTypes.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetContributionTypeByIdAsync(record.Id, cancellationToken);
    }

    public async Task<ContributionTypeRecordDto> UpdateContributionTypeAsync(Guid contributionTypeId, SaveContributionTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ContributionTypes.SingleOrDefaultAsync(item => item.Id == contributionTypeId, cancellationToken)
            ?? throw new NotFoundException($"Contribution type '{contributionTypeId}' was not found.");

        await EnsureUniqueContributionTypeCodeAsync(request.Code, contributionTypeId, cancellationToken);
        await EnsureUniqueContributionTypeNameAsync(request.Name, contributionTypeId, cancellationToken);

        record.Code = request.Code.Trim().ToUpperInvariant();
        record.Name = request.Name.Trim();
        record.Description = request.Description.Trim();
        record.EmployeeShareApplicable = request.EmployeeShareApplicable;
        record.EmployerShareApplicable = request.EmployerShareApplicable;
        record.IsActive = request.IsActive;
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetContributionTypeByIdAsync(record.Id, cancellationToken);
    }

    public async Task DeleteContributionTypeAsync(Guid contributionTypeId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ContributionTypes
            .Include(item => item.GovernmentContributionTables)
            .SingleOrDefaultAsync(item => item.Id == contributionTypeId, cancellationToken)
            ?? throw new NotFoundException($"Contribution type '{contributionTypeId}' was not found.");

        if (record.GovernmentContributionTables.Count > 0)
        {
            throw new ConflictException("This contribution type is already used. Deactivate it instead of deleting it.");
        }

        _dbContext.ContributionTypes.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<GovernmentContributionTableRecordDto>> GetGovernmentContributionTablesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.GovernmentContributionTables
            .AsNoTracking()
            .Include(record => record.ContributionType)
            .Include(record => record.Brackets)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Name.Contains(search) ||
                (record.ContributionType != null && record.ContributionType.Name.Contains(search)) ||
                (record.ContributionType != null && record.ContributionType.Code.Contains(search)));
        }

        if (query.IsActive is not null)
        {
            source = source.Where(record => record.IsActive == query.IsActive.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("effective_start", true) => source.OrderByDescending(record => record.EffectiveStartDate).ThenBy(record => record.Name),
            ("effective_start", false) => source.OrderBy(record => record.EffectiveStartDate).ThenBy(record => record.Name),
            ("type", true) => source.OrderByDescending(record => record.ContributionType != null ? record.ContributionType.Name : string.Empty).ThenBy(record => record.Name),
            ("type", false) => source.OrderBy(record => record.ContributionType != null ? record.ContributionType.Name : string.Empty).ThenBy(record => record.Name),
            (_, true) => source.OrderByDescending(record => record.Name),
            _ => source.OrderBy(record => record.Name)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ToPage(records.Select(MapGovernmentContributionTable).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<GovernmentContributionTableRecordDto> CreateGovernmentContributionTableAsync(SaveGovernmentContributionTableRequestDto request, CancellationToken cancellationToken = default)
    {
        var contributionType = await _dbContext.ContributionTypes
            .SingleOrDefaultAsync(record => record.Id == request.ContributionTypeId!.Value, cancellationToken)
            ?? throw new BadRequestException("The selected contribution type does not exist.");

        var record = new GovernmentContributionTable
        {
            ContributionTypeId = contributionType.Id,
            Name = request.Name.Trim(),
            EffectiveStartDate = request.EffectiveStartDate!.Value,
            EffectiveEndDate = request.EffectiveEndDate,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        ApplyGovernmentContributionBrackets(record, request.Brackets);
        _dbContext.GovernmentContributionTables.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetGovernmentContributionTableByIdAsync(record.Id, cancellationToken);
    }

    public async Task<GovernmentContributionTableRecordDto> UpdateGovernmentContributionTableAsync(Guid contributionTableId, SaveGovernmentContributionTableRequestDto request, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.GovernmentContributionTables
            .Include(item => item.Brackets)
            .SingleOrDefaultAsync(item => item.Id == contributionTableId, cancellationToken)
            ?? throw new NotFoundException($"Government contribution table '{contributionTableId}' was not found.");

        var contributionType = await _dbContext.ContributionTypes
            .SingleOrDefaultAsync(item => item.Id == request.ContributionTypeId!.Value, cancellationToken)
            ?? throw new BadRequestException("The selected contribution type does not exist.");

        record.ContributionTypeId = contributionType.Id;
        record.Name = request.Name.Trim();
        record.EffectiveStartDate = request.EffectiveStartDate!.Value;
        record.EffectiveEndDate = request.EffectiveEndDate;
        record.IsActive = request.IsActive;
        record.UpdatedAtUtc = DateTime.UtcNow;

        record.Brackets.Clear();
        ApplyGovernmentContributionBrackets(record, request.Brackets);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetGovernmentContributionTableByIdAsync(record.Id, cancellationToken);
    }

    public async Task DeleteGovernmentContributionTableAsync(Guid contributionTableId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.GovernmentContributionTables
            .Include(item => item.Brackets)
            .SingleOrDefaultAsync(item => item.Id == contributionTableId, cancellationToken)
            ?? throw new NotFoundException($"Government contribution table '{contributionTableId}' was not found.");

        _dbContext.GovernmentContributionTables.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<TaxTableRecordDto>> GetTaxTablesAsync(PayrollSetupListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.TaxTables
            .AsNoTracking()
            .Include(record => record.Brackets)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Code.Contains(search) ||
                record.Name.Contains(search));
        }

        if (query.IsActive is not null)
        {
            source = source.Where(record => record.IsActive == query.IsActive.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("effective_start", true) => source.OrderByDescending(record => record.EffectiveStartDate).ThenBy(record => record.Name),
            ("effective_start", false) => source.OrderBy(record => record.EffectiveStartDate).ThenBy(record => record.Name),
            ("frequency", true) => source.OrderByDescending(record => record.PayFrequency).ThenBy(record => record.Name),
            ("frequency", false) => source.OrderBy(record => record.PayFrequency).ThenBy(record => record.Name),
            (_, true) => source.OrderByDescending(record => record.Name),
            _ => source.OrderBy(record => record.Name)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ToPage(records.Select(MapTaxTable).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<TaxTableRecordDto> CreateTaxTableAsync(SaveTaxTableRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatePayFrequency(request.PayFrequency);
        await EnsureUniqueTaxTableCodeAsync(request.Code, null, cancellationToken);

        var record = new TaxTable
        {
            Code = request.Code.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            PayFrequency = request.PayFrequency.Trim().ToLowerInvariant(),
            EffectiveStartDate = request.EffectiveStartDate!.Value,
            EffectiveEndDate = request.EffectiveEndDate,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        ApplyTaxBrackets(record, request.Brackets);
        _dbContext.TaxTables.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetTaxTableByIdAsync(record.Id, cancellationToken);
    }

    public async Task<TaxTableRecordDto> UpdateTaxTableAsync(Guid taxTableId, SaveTaxTableRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatePayFrequency(request.PayFrequency);
        var record = await _dbContext.TaxTables
            .Include(item => item.Brackets)
            .SingleOrDefaultAsync(item => item.Id == taxTableId, cancellationToken)
            ?? throw new NotFoundException($"Tax table '{taxTableId}' was not found.");

        await EnsureUniqueTaxTableCodeAsync(request.Code, taxTableId, cancellationToken);

        record.Code = request.Code.Trim().ToUpperInvariant();
        record.Name = request.Name.Trim();
        record.PayFrequency = request.PayFrequency.Trim().ToLowerInvariant();
        record.EffectiveStartDate = request.EffectiveStartDate!.Value;
        record.EffectiveEndDate = request.EffectiveEndDate;
        record.IsActive = request.IsActive;
        record.UpdatedAtUtc = DateTime.UtcNow;

        record.Brackets.Clear();
        ApplyTaxBrackets(record, request.Brackets);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetTaxTableByIdAsync(record.Id, cancellationToken);
    }

    public async Task DeleteTaxTableAsync(Guid taxTableId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.TaxTables
            .Include(item => item.Brackets)
            .SingleOrDefaultAsync(item => item.Id == taxTableId, cancellationToken)
            ?? throw new NotFoundException($"Tax table '{taxTableId}' was not found.");

        _dbContext.TaxTables.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertSettingAsync(string key, string value, string description, CancellationToken cancellationToken)
    {
        var record = await _dbContext.PayrollSettings.SingleOrDefaultAsync(item => item.Key == key, cancellationToken);
        if (record is null)
        {
            _dbContext.PayrollSettings.Add(new PayrollSetting
            {
                Key = key,
                Value = value,
                Description = description,
                CreatedAtUtc = DateTime.UtcNow
            });
            return;
        }

        record.Value = value;
        record.Description = description;
        record.UpdatedAtUtc = DateTime.UtcNow;
    }

    private static IQueryable<TEntity> ApplySetupFilters<TEntity>(
        IQueryable<TEntity> source,
        PayrollSetupListQueryDto query,
        string codePropertyName,
        string namePropertyName)
        where TEntity : class
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                EF.Property<string>(record, codePropertyName).Contains(search) ||
                EF.Property<string>(record, namePropertyName).Contains(search));
        }

        if (query.IsActive is not null)
        {
            source = source.Where(record => EF.Property<bool>(record, "IsActive") == query.IsActive.Value);
        }

        return source;
    }

    private static IQueryable<PayPeriodTemplate> ApplyTemplateSort(IQueryable<PayPeriodTemplate> source, string sortBy, bool descending)
    {
        return (sortBy.Trim().ToLowerInvariant(), descending) switch
        {
            ("frequency", true) => source.OrderByDescending(record => record.PayFrequency).ThenBy(record => record.Name),
            ("frequency", false) => source.OrderBy(record => record.PayFrequency).ThenBy(record => record.Name),
            ("days", true) => source.OrderByDescending(record => record.PeriodLengthDays).ThenBy(record => record.Name),
            ("days", false) => source.OrderBy(record => record.PeriodLengthDays).ThenBy(record => record.Name),
            (_, true) => source.OrderByDescending(record => record.Name),
            _ => source.OrderBy(record => record.Name)
        };
    }

    private static IQueryable<TEntity> ApplySimpleNameSort<TEntity>(
        IQueryable<TEntity> source,
        string sortBy,
        bool descending,
        Expression<Func<TEntity, string>> codeSelector,
        Expression<Func<TEntity, string>> nameSelector,
        Expression<Func<TEntity, DateTime>> createdSelector)
        where TEntity : class
    {
        return (sortBy.Trim().ToLowerInvariant(), descending) switch
        {
            ("code", true) => source.OrderByDescending(codeSelector).ThenBy(nameSelector),
            ("code", false) => source.OrderBy(codeSelector).ThenBy(nameSelector),
            ("created", true) => source.OrderByDescending(createdSelector).ThenBy(nameSelector),
            ("created", false) => source.OrderBy(createdSelector).ThenBy(nameSelector),
            (_, true) => source.OrderByDescending(nameSelector),
            _ => source.OrderBy(nameSelector)
        };
    }

    private async Task<PayPeriodTemplateRecordDto> GetPayPeriodTemplateByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.PayPeriodTemplates
            .AsNoTracking()
            .Where(record => record.Id == id)
            .Select(record => new PayPeriodTemplateRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                PayFrequency = record.PayFrequency,
                PeriodLengthDays = record.PeriodLengthDays,
                PayrollOffsetDays = record.PayrollOffsetDays,
                IsActive = record.IsActive,
                PayPeriodCount = record.PayPeriods.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Pay period template '{id}' was not found.");
    }

    private async Task<EarningTypeRecordDto> GetEarningTypeByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.EarningTypes
            .AsNoTracking()
            .Where(record => record.Id == id)
            .Select(record => new EarningTypeRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                Category = record.Category,
                Taxable = record.Taxable,
                Recurring = record.Recurring,
                AffectsThirteenthMonth = record.AffectsThirteenthMonth,
                IsActive = record.IsActive,
                EmployeeRecurringCount = record.EmployeeRecurringEarnings.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Earning type '{id}' was not found.");
    }

    private async Task<DeductionTypeRecordDto> GetDeductionTypeByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.DeductionTypes
            .AsNoTracking()
            .Where(record => record.Id == id)
            .Select(record => new DeductionTypeRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                Category = record.Category,
                PreTax = record.PreTax,
                Recurring = record.Recurring,
                IsActive = record.IsActive,
                EmployeeRecurringCount = record.EmployeeRecurringDeductions.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Deduction type '{id}' was not found.");
    }

    private async Task<ContributionTypeRecordDto> GetContributionTypeByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.ContributionTypes
            .AsNoTracking()
            .Where(record => record.Id == id)
            .Select(record => new ContributionTypeRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                EmployeeShareApplicable = record.EmployeeShareApplicable,
                EmployerShareApplicable = record.EmployerShareApplicable,
                IsActive = record.IsActive,
                TableCount = record.GovernmentContributionTables.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Contribution type '{id}' was not found.");
    }

    private async Task<GovernmentContributionTableRecordDto> GetGovernmentContributionTableByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await _dbContext.GovernmentContributionTables
            .AsNoTracking()
            .Include(item => item.ContributionType)
            .Include(item => item.Brackets)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Government contribution table '{id}' was not found.");

        return MapGovernmentContributionTable(record);
    }

    private async Task<TaxTableRecordDto> GetTaxTableByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var record = await _dbContext.TaxTables
            .AsNoTracking()
            .Include(item => item.Brackets)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Tax table '{id}' was not found.");

        return MapTaxTable(record);
    }

    private static GovernmentContributionTableRecordDto MapGovernmentContributionTable(GovernmentContributionTable record)
    {
        return new GovernmentContributionTableRecordDto
        {
            Id = record.Id,
            ContributionTypeId = record.ContributionTypeId,
            ContributionTypeCode = record.ContributionType?.Code ?? string.Empty,
            ContributionTypeName = record.ContributionType?.Name ?? string.Empty,
            Name = record.Name,
            EffectiveStartDate = record.EffectiveStartDate,
            EffectiveEndDate = record.EffectiveEndDate,
            IsActive = record.IsActive,
            BracketCount = record.Brackets.Count,
            Brackets = record.Brackets
                .OrderBy(item => item.MinCompensation)
                .Select(item => new GovernmentContributionBracketDto
                {
                    Id = item.Id,
                    MinCompensation = item.MinCompensation,
                    MaxCompensation = item.MaxCompensation,
                    EmployeeShareAmount = item.EmployeeShareAmount,
                    EmployeeShareRate = item.EmployeeShareRate,
                    EmployerShareAmount = item.EmployerShareAmount,
                    EmployerShareRate = item.EmployerShareRate,
                    Remarks = item.Remarks
                })
                .ToList(),
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static TaxTableRecordDto MapTaxTable(TaxTable record)
    {
        return new TaxTableRecordDto
        {
            Id = record.Id,
            Code = record.Code,
            Name = record.Name,
            PayFrequency = record.PayFrequency,
            EffectiveStartDate = record.EffectiveStartDate,
            EffectiveEndDate = record.EffectiveEndDate,
            IsActive = record.IsActive,
            Brackets = record.Brackets
                .OrderBy(item => item.MinTaxableIncome)
                .Select(item => new TaxBracketDto
                {
                    Id = item.Id,
                    MinTaxableIncome = item.MinTaxableIncome,
                    MaxTaxableIncome = item.MaxTaxableIncome,
                    BaseTax = item.BaseTax,
                    TaxRate = item.TaxRate,
                    ExcessOver = item.ExcessOver
                })
                .ToList(),
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static void ApplyGovernmentContributionBrackets(
        GovernmentContributionTable record,
        IReadOnlyList<SaveGovernmentContributionBracketRequestDto> brackets)
    {
        foreach (var bracket in brackets.OrderBy(item => item.MinCompensation))
        {
            record.Brackets.Add(new GovernmentContributionBracket
            {
                MinCompensation = bracket.MinCompensation,
                MaxCompensation = bracket.MaxCompensation,
                EmployeeShareAmount = bracket.EmployeeShareAmount,
                EmployeeShareRate = bracket.EmployeeShareRate,
                EmployerShareAmount = bracket.EmployerShareAmount,
                EmployerShareRate = bracket.EmployerShareRate,
                Remarks = bracket.Remarks.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private static void ApplyTaxBrackets(
        TaxTable record,
        IReadOnlyList<SaveTaxBracketRequestDto> brackets)
    {
        foreach (var bracket in brackets.OrderBy(item => item.MinTaxableIncome))
        {
            record.Brackets.Add(new TaxBracket
            {
                MinTaxableIncome = bracket.MinTaxableIncome,
                MaxTaxableIncome = bracket.MaxTaxableIncome,
                BaseTax = bracket.BaseTax,
                TaxRate = bracket.TaxRate,
                ExcessOver = bracket.ExcessOver,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private async Task EnsureUniqueTemplateCodeAsync(string code, Guid? existingId, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var exists = await _dbContext.PayPeriodTemplates.AnyAsync(
            record => record.Code == normalized && (!existingId.HasValue || record.Id != existingId.Value),
            cancellationToken);

        if (exists)
        {
            throw BuildValidationException("The pay period template code is already in use.", nameof(SavePayPeriodTemplateRequestDto.Code));
        }
    }

    private async Task EnsureUniqueTemplateNameAsync(string name, Guid? existingId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        var exists = await _dbContext.PayPeriodTemplates.AnyAsync(
            record => record.Name == normalized && (!existingId.HasValue || record.Id != existingId.Value),
            cancellationToken);

        if (exists)
        {
            throw BuildValidationException("The pay period template name is already in use.", nameof(SavePayPeriodTemplateRequestDto.Name));
        }
    }

    private async Task EnsureUniqueEarningTypeCodeAsync(string code, Guid? existingId, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var exists = await _dbContext.EarningTypes.AnyAsync(
            record => record.Code == normalized && (!existingId.HasValue || record.Id != existingId.Value),
            cancellationToken);

        if (exists)
        {
            throw BuildValidationException("The earning type code is already in use.", nameof(SaveEarningTypeRequestDto.Code));
        }
    }

    private async Task EnsureUniqueEarningTypeNameAsync(string name, Guid? existingId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        var exists = await _dbContext.EarningTypes.AnyAsync(
            record => record.Name == normalized && (!existingId.HasValue || record.Id != existingId.Value),
            cancellationToken);

        if (exists)
        {
            throw BuildValidationException("The earning type name is already in use.", nameof(SaveEarningTypeRequestDto.Name));
        }
    }

    private async Task EnsureUniqueDeductionTypeCodeAsync(string code, Guid? existingId, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var exists = await _dbContext.DeductionTypes.AnyAsync(
            record => record.Code == normalized && (!existingId.HasValue || record.Id != existingId.Value),
            cancellationToken);

        if (exists)
        {
            throw BuildValidationException("The deduction type code is already in use.", nameof(SaveDeductionTypeRequestDto.Code));
        }
    }

    private async Task EnsureUniqueDeductionTypeNameAsync(string name, Guid? existingId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        var exists = await _dbContext.DeductionTypes.AnyAsync(
            record => record.Name == normalized && (!existingId.HasValue || record.Id != existingId.Value),
            cancellationToken);

        if (exists)
        {
            throw BuildValidationException("The deduction type name is already in use.", nameof(SaveDeductionTypeRequestDto.Name));
        }
    }

    private async Task EnsureUniqueContributionTypeCodeAsync(string code, Guid? existingId, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var exists = await _dbContext.ContributionTypes.AnyAsync(
            record => record.Code == normalized && (!existingId.HasValue || record.Id != existingId.Value),
            cancellationToken);

        if (exists)
        {
            throw BuildValidationException("The contribution type code is already in use.", nameof(SaveContributionTypeRequestDto.Code));
        }
    }

    private async Task EnsureUniqueContributionTypeNameAsync(string name, Guid? existingId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        var exists = await _dbContext.ContributionTypes.AnyAsync(
            record => record.Name == normalized && (!existingId.HasValue || record.Id != existingId.Value),
            cancellationToken);

        if (exists)
        {
            throw BuildValidationException("The contribution type name is already in use.", nameof(SaveContributionTypeRequestDto.Name));
        }
    }

    private async Task EnsureUniqueTaxTableCodeAsync(string code, Guid? existingId, CancellationToken cancellationToken)
    {
        var normalized = code.Trim().ToUpperInvariant();
        var exists = await _dbContext.TaxTables.AnyAsync(
            record => record.Code == normalized && (!existingId.HasValue || record.Id != existingId.Value),
            cancellationToken);

        if (exists)
        {
            throw BuildValidationException("The tax table code is already in use.", nameof(SaveTaxTableRequestDto.Code));
        }
    }

    private static void ValidatePayFrequency(string payFrequency)
    {
        if (!PayrollPayFrequencies.Standard.Contains(payFrequency.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Pay frequency must be weekly, semi_monthly, monthly, or custom.");
        }
    }

    private static void ValidateEarningCategory(string category)
    {
        if (!EarningTypeCategories.All.Contains(category.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Invalid earning type category.");
        }
    }

    private static void ValidateDeductionCategory(string category)
    {
        if (!DeductionTypeCategories.All.Contains(category.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Invalid deduction type category.");
        }
    }

    private static string GetSetting(IReadOnlyDictionary<string, string> settings, string key, string fallback)
    {
        return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }

    private static decimal ParseDecimalSetting(IReadOnlyDictionary<string, string> settings, string key, decimal fallback)
    {
        return settings.TryGetValue(key, out var value) && decimal.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    private static bool ParseBoolSetting(IReadOnlyDictionary<string, string> settings, string key, bool fallback)
    {
        return settings.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
            ? parsed
            : fallback;
    }

    private static LookupOptionDto MapLookup(Department record)
    {
        return new LookupOptionDto
        {
            Id = record.Id,
            Code = record.Code,
            Name = record.Name,
            IsActive = record.IsActive
        };
    }

    private static LookupOptionDto MapLookup(Branch record)
    {
        return new LookupOptionDto
        {
            Id = record.Id,
            Code = record.Code,
            Name = record.Name,
            IsActive = record.IsActive
        };
    }

    private static LookupOptionDto MapLookup(EmploymentType record)
    {
        return new LookupOptionDto
        {
            Id = record.Id,
            Code = record.Code,
            Name = record.Name,
            IsActive = record.IsActive
        };
    }

    private static LookupOptionDto MapLookup(EmploymentStatus record)
    {
        return new LookupOptionDto
        {
            Id = record.Id,
            Code = record.Code,
            Name = record.Name,
            IsActive = record.IsActive
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

    private static string BuildFullName(string firstName, string middleName, string lastName, string suffix)
    {
        return string.Join(
            " ",
            new[] { firstName.Trim(), middleName.Trim(), lastName.Trim(), suffix.Trim() }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static BadRequestException BuildValidationException(string message, string fieldName)
    {
        return new BadRequestException(message, new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }
}
