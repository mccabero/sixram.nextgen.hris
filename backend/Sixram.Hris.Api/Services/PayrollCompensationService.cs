using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Payroll;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IPayrollCompensationService
{
    Task<PagedResultDto<CompensationProfileRecordDto>> GetCompensationProfilesAsync(CompensationProfileListQueryDto query, CancellationToken cancellationToken = default);

    Task<CompensationProfileRecordDto> CreateCompensationProfileAsync(SaveCompensationProfileRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<CompensationProfileRecordDto> UpdateCompensationProfileAsync(Guid compensationProfileId, SaveCompensationProfileRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task DeleteCompensationProfileAsync(Guid compensationProfileId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<EmployeeRecurringEarningRecordDto>> GetRecurringEarningsAsync(RecurringPayrollComponentListQueryDto query, CancellationToken cancellationToken = default);

    Task<EmployeeRecurringEarningRecordDto> CreateRecurringEarningAsync(SaveEmployeeRecurringEarningRequestDto request, CancellationToken cancellationToken = default);

    Task<EmployeeRecurringEarningRecordDto> UpdateRecurringEarningAsync(Guid recurringEarningId, SaveEmployeeRecurringEarningRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteRecurringEarningAsync(Guid recurringEarningId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<EmployeeRecurringDeductionRecordDto>> GetRecurringDeductionsAsync(RecurringPayrollComponentListQueryDto query, CancellationToken cancellationToken = default);

    Task<EmployeeRecurringDeductionRecordDto> CreateRecurringDeductionAsync(SaveEmployeeRecurringDeductionRequestDto request, CancellationToken cancellationToken = default);

    Task<EmployeeRecurringDeductionRecordDto> UpdateRecurringDeductionAsync(Guid recurringDeductionId, SaveEmployeeRecurringDeductionRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteRecurringDeductionAsync(Guid recurringDeductionId, CancellationToken cancellationToken = default);

    Task<EmployeePayrollProfileDto> GetEmployeePayrollProfileAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

public class PayrollCompensationService : IPayrollCompensationService
{
    private readonly ApplicationDbContext _dbContext;

    public PayrollCompensationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResultDto<CompensationProfileRecordDto>> GetCompensationProfilesAsync(CompensationProfileListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.CompensationProfiles
            .AsNoTracking()
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(record => record.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(record => record.CreatedByUser)
            .Include(record => record.UpdatedByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Employee != null &&
                (record.Employee.EmployeeCode.Contains(search) ||
                 record.Employee.FirstName.Contains(search) ||
                 record.Employee.MiddleName.Contains(search) ||
                 record.Employee.LastName.Contains(search)));
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

        if (query.IsActive is not null)
        {
            source = source.Where(record => record.IsActive == query.IsActive.Value);
        }

        if (query.EffectiveDate is not null)
        {
            var effectiveDate = query.EffectiveDate.Value;
            source = source.Where(record =>
                record.EffectiveStartDate <= effectiveDate &&
                (!record.EffectiveEndDate.HasValue || record.EffectiveEndDate.Value >= effectiveDate));
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("employee", true) => source.OrderByDescending(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenByDescending(record => record.Employee != null ? record.Employee.FirstName : string.Empty).ThenByDescending(record => record.EffectiveStartDate),
            ("employee", false) => source.OrderBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.FirstName : string.Empty).ThenByDescending(record => record.EffectiveStartDate),
            ("effective_end", true) => source.OrderByDescending(record => record.EffectiveEndDate).ThenByDescending(record => record.EffectiveStartDate),
            ("effective_end", false) => source.OrderBy(record => record.EffectiveEndDate).ThenByDescending(record => record.EffectiveStartDate),
            (_, true) => source.OrderByDescending(record => record.EffectiveStartDate).ThenBy(record => record.Employee != null ? record.Employee.LastName : string.Empty),
            _ => source.OrderBy(record => record.EffectiveStartDate).ThenBy(record => record.Employee != null ? record.Employee.LastName : string.Empty)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ToPage(records.Select(MapCompensationProfile).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<CompensationProfileRecordDto> CreateCompensationProfileAsync(SaveCompensationProfileRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        ValidatePayType(request.PayType);
        ValidatePayFrequency(request.PayFrequency);
        ValidateCompensationAmounts(request);

        var employee = await EnsureEmployeeAsync(request.EmployeeId!.Value, cancellationToken);
        await EnsureCompensationProfileDoesNotOverlapAsync(
            employee.Id,
            request.EffectiveStartDate!.Value,
            request.EffectiveEndDate,
            request.IsActive,
            null,
            cancellationToken);

        var record = new CompensationProfile
        {
            EmployeeId = employee.Id,
            PayType = request.PayType.Trim().ToLowerInvariant(),
            PayFrequency = request.PayFrequency.Trim().ToLowerInvariant(),
            BasicSalary = request.BasicSalary,
            DailyRate = request.DailyRate,
            HourlyRate = request.HourlyRate,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            EffectiveStartDate = request.EffectiveStartDate.Value,
            EffectiveEndDate = request.EffectiveEndDate,
            IsActive = request.IsActive,
            Remarks = request.Remarks.Trim(),
            CreatedByUserId = NormalizeUserId(actorUserId),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.CompensationProfiles.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetCompensationProfileByIdAsync(record.Id, cancellationToken);
    }

    public async Task<CompensationProfileRecordDto> UpdateCompensationProfileAsync(Guid compensationProfileId, SaveCompensationProfileRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        ValidatePayType(request.PayType);
        ValidatePayFrequency(request.PayFrequency);
        ValidateCompensationAmounts(request);

        var record = await _dbContext.CompensationProfiles.SingleOrDefaultAsync(item => item.Id == compensationProfileId, cancellationToken)
            ?? throw new NotFoundException($"Compensation profile '{compensationProfileId}' was not found.");

        var employee = await EnsureEmployeeAsync(request.EmployeeId!.Value, cancellationToken);
        await EnsureCompensationProfileDoesNotOverlapAsync(
            employee.Id,
            request.EffectiveStartDate!.Value,
            request.EffectiveEndDate,
            request.IsActive,
            compensationProfileId,
            cancellationToken);

        record.EmployeeId = employee.Id;
        record.PayType = request.PayType.Trim().ToLowerInvariant();
        record.PayFrequency = request.PayFrequency.Trim().ToLowerInvariant();
        record.BasicSalary = request.BasicSalary;
        record.DailyRate = request.DailyRate;
        record.HourlyRate = request.HourlyRate;
        record.Currency = request.Currency.Trim().ToUpperInvariant();
        record.EffectiveStartDate = request.EffectiveStartDate.Value;
        record.EffectiveEndDate = request.EffectiveEndDate;
        record.IsActive = request.IsActive;
        record.Remarks = request.Remarks.Trim();
        record.UpdatedByUserId = NormalizeUserId(actorUserId);
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetCompensationProfileByIdAsync(record.Id, cancellationToken);
    }

    public async Task DeleteCompensationProfileAsync(Guid compensationProfileId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.CompensationProfiles
            .Include(item => item.PayrollRunItems)
            .SingleOrDefaultAsync(item => item.Id == compensationProfileId, cancellationToken)
            ?? throw new NotFoundException($"Compensation profile '{compensationProfileId}' was not found.");

        if (record.PayrollRunItems.Count > 0)
        {
            throw new ConflictException("This compensation profile has already been used by payroll runs. Deactivate it instead of deleting it.");
        }

        _dbContext.CompensationProfiles.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<EmployeeRecurringEarningRecordDto>> GetRecurringEarningsAsync(RecurringPayrollComponentListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.EmployeeRecurringEarnings
            .AsNoTracking()
            .Include(record => record.Employee)
            .Include(record => record.EarningType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Employee != null &&
                record.EarningType != null &&
                (record.Employee.EmployeeCode.Contains(search) ||
                 record.Employee.FirstName.Contains(search) ||
                 record.Employee.MiddleName.Contains(search) ||
                 record.Employee.LastName.Contains(search) ||
                 record.EarningType.Name.Contains(search)));
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

        if (query.IsActive is not null)
        {
            source = source.Where(record => record.IsActive == query.IsActive.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("employee", true) => source.OrderByDescending(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenByDescending(record => record.Employee != null ? record.Employee.FirstName : string.Empty).ThenByDescending(record => record.EffectiveStartDate),
            ("employee", false) => source.OrderBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.FirstName : string.Empty).ThenByDescending(record => record.EffectiveStartDate),
            ("amount", true) => source.OrderByDescending(record => record.Amount).ThenByDescending(record => record.EffectiveStartDate),
            ("amount", false) => source.OrderBy(record => record.Amount).ThenByDescending(record => record.EffectiveStartDate),
            ("effective_end", true) => source.OrderByDescending(record => record.EffectiveEndDate).ThenByDescending(record => record.EffectiveStartDate),
            ("effective_end", false) => source.OrderBy(record => record.EffectiveEndDate).ThenByDescending(record => record.EffectiveStartDate),
            (_, true) => source.OrderByDescending(record => record.EffectiveStartDate),
            _ => source.OrderBy(record => record.EffectiveStartDate)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ToPage(records.Select(MapRecurringEarning).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<EmployeeRecurringEarningRecordDto> CreateRecurringEarningAsync(SaveEmployeeRecurringEarningRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatePayFrequency(request.Frequency);
        _ = await EnsureEmployeeAsync(request.EmployeeId!.Value, cancellationToken);
        _ = await EnsureEarningTypeAsync(request.EarningTypeId!.Value, cancellationToken);

        var record = new EmployeeRecurringEarning
        {
            EmployeeId = request.EmployeeId.Value,
            EarningTypeId = request.EarningTypeId.Value,
            Amount = request.Amount,
            Frequency = request.Frequency.Trim().ToLowerInvariant(),
            EffectiveStartDate = request.EffectiveStartDate!.Value,
            EffectiveEndDate = request.EffectiveEndDate,
            IsActive = request.IsActive,
            Remarks = request.Remarks.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.EmployeeRecurringEarnings.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetRecurringEarningByIdAsync(record.Id, cancellationToken);
    }

    public async Task<EmployeeRecurringEarningRecordDto> UpdateRecurringEarningAsync(Guid recurringEarningId, SaveEmployeeRecurringEarningRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatePayFrequency(request.Frequency);
        var record = await _dbContext.EmployeeRecurringEarnings.SingleOrDefaultAsync(item => item.Id == recurringEarningId, cancellationToken)
            ?? throw new NotFoundException($"Recurring earning '{recurringEarningId}' was not found.");

        _ = await EnsureEmployeeAsync(request.EmployeeId!.Value, cancellationToken);
        _ = await EnsureEarningTypeAsync(request.EarningTypeId!.Value, cancellationToken);

        record.EmployeeId = request.EmployeeId.Value;
        record.EarningTypeId = request.EarningTypeId.Value;
        record.Amount = request.Amount;
        record.Frequency = request.Frequency.Trim().ToLowerInvariant();
        record.EffectiveStartDate = request.EffectiveStartDate!.Value;
        record.EffectiveEndDate = request.EffectiveEndDate;
        record.IsActive = request.IsActive;
        record.Remarks = request.Remarks.Trim();
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetRecurringEarningByIdAsync(record.Id, cancellationToken);
    }

    public async Task DeleteRecurringEarningAsync(Guid recurringEarningId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.EmployeeRecurringEarnings
            .SingleOrDefaultAsync(item => item.Id == recurringEarningId, cancellationToken)
            ?? throw new NotFoundException($"Recurring earning '{recurringEarningId}' was not found.");

        _dbContext.EmployeeRecurringEarnings.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResultDto<EmployeeRecurringDeductionRecordDto>> GetRecurringDeductionsAsync(RecurringPayrollComponentListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.EmployeeRecurringDeductions
            .AsNoTracking()
            .Include(record => record.Employee)
            .Include(record => record.DeductionType)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Employee != null &&
                record.DeductionType != null &&
                (record.Employee.EmployeeCode.Contains(search) ||
                 record.Employee.FirstName.Contains(search) ||
                 record.Employee.MiddleName.Contains(search) ||
                 record.Employee.LastName.Contains(search) ||
                 record.DeductionType.Name.Contains(search)));
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

        if (query.IsActive is not null)
        {
            source = source.Where(record => record.IsActive == query.IsActive.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("employee", true) => source.OrderByDescending(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenByDescending(record => record.Employee != null ? record.Employee.FirstName : string.Empty).ThenByDescending(record => record.EffectiveStartDate),
            ("employee", false) => source.OrderBy(record => record.Employee != null ? record.Employee.LastName : string.Empty).ThenBy(record => record.Employee != null ? record.Employee.FirstName : string.Empty).ThenByDescending(record => record.EffectiveStartDate),
            ("amount", true) => source.OrderByDescending(record => record.Amount).ThenByDescending(record => record.EffectiveStartDate),
            ("amount", false) => source.OrderBy(record => record.Amount).ThenByDescending(record => record.EffectiveStartDate),
            ("effective_end", true) => source.OrderByDescending(record => record.EffectiveEndDate).ThenByDescending(record => record.EffectiveStartDate),
            ("effective_end", false) => source.OrderBy(record => record.EffectiveEndDate).ThenByDescending(record => record.EffectiveStartDate),
            (_, true) => source.OrderByDescending(record => record.EffectiveStartDate),
            _ => source.OrderBy(record => record.EffectiveStartDate)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return ToPage(records.Select(MapRecurringDeduction).ToList(), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<EmployeeRecurringDeductionRecordDto> CreateRecurringDeductionAsync(SaveEmployeeRecurringDeductionRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatePayFrequency(request.Frequency);
        _ = await EnsureEmployeeAsync(request.EmployeeId!.Value, cancellationToken);
        _ = await EnsureDeductionTypeAsync(request.DeductionTypeId!.Value, cancellationToken);

        var record = new EmployeeRecurringDeduction
        {
            EmployeeId = request.EmployeeId.Value,
            DeductionTypeId = request.DeductionTypeId.Value,
            Amount = request.Amount,
            Frequency = request.Frequency.Trim().ToLowerInvariant(),
            Balance = request.Balance,
            EffectiveStartDate = request.EffectiveStartDate!.Value,
            EffectiveEndDate = request.EffectiveEndDate,
            IsActive = request.IsActive,
            Remarks = request.Remarks.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.EmployeeRecurringDeductions.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetRecurringDeductionByIdAsync(record.Id, cancellationToken);
    }

    public async Task<EmployeeRecurringDeductionRecordDto> UpdateRecurringDeductionAsync(Guid recurringDeductionId, SaveEmployeeRecurringDeductionRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidatePayFrequency(request.Frequency);
        var record = await _dbContext.EmployeeRecurringDeductions.SingleOrDefaultAsync(item => item.Id == recurringDeductionId, cancellationToken)
            ?? throw new NotFoundException($"Recurring deduction '{recurringDeductionId}' was not found.");

        _ = await EnsureEmployeeAsync(request.EmployeeId!.Value, cancellationToken);
        _ = await EnsureDeductionTypeAsync(request.DeductionTypeId!.Value, cancellationToken);

        record.EmployeeId = request.EmployeeId.Value;
        record.DeductionTypeId = request.DeductionTypeId.Value;
        record.Amount = request.Amount;
        record.Frequency = request.Frequency.Trim().ToLowerInvariant();
        record.Balance = request.Balance;
        record.EffectiveStartDate = request.EffectiveStartDate!.Value;
        record.EffectiveEndDate = request.EffectiveEndDate;
        record.IsActive = request.IsActive;
        record.Remarks = request.Remarks.Trim();
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetRecurringDeductionByIdAsync(record.Id, cancellationToken);
    }

    public async Task DeleteRecurringDeductionAsync(Guid recurringDeductionId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.EmployeeRecurringDeductions
            .SingleOrDefaultAsync(item => item.Id == recurringDeductionId, cancellationToken)
            ?? throw new NotFoundException($"Recurring deduction '{recurringDeductionId}' was not found.");

        _dbContext.EmployeeRecurringDeductions.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmployeePayrollProfileDto> GetEmployeePayrollProfileAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Employee '{employeeId}' was not found.");

        var compensationProfiles = await _dbContext.CompensationProfiles
            .AsNoTracking()
            .Include(record => record.Employee)
            .Include(record => record.CreatedByUser)
            .Include(record => record.UpdatedByUser)
            .Where(record => record.EmployeeId == employeeId)
            .OrderByDescending(record => record.EffectiveStartDate)
            .ToListAsync(cancellationToken);

        var recurringEarnings = await _dbContext.EmployeeRecurringEarnings
            .AsNoTracking()
            .Include(record => record.Employee)
            .Include(record => record.EarningType)
            .Where(record => record.EmployeeId == employeeId)
            .OrderByDescending(record => record.EffectiveStartDate)
            .ToListAsync(cancellationToken);

        var recurringDeductions = await _dbContext.EmployeeRecurringDeductions
            .AsNoTracking()
            .Include(record => record.Employee)
            .Include(record => record.DeductionType)
            .Where(record => record.EmployeeId == employeeId)
            .OrderByDescending(record => record.EffectiveStartDate)
            .ToListAsync(cancellationToken);

        var payrollHistory = await _dbContext.PayrollRunItems
            .AsNoTracking()
            .Include(record => record.PayrollRun)
            .Where(record => record.EmployeeId == employeeId)
            .OrderByDescending(record => record.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new EmployeePayrollProfileDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            CompensationProfiles = compensationProfiles.Select(MapCompensationProfile).ToList(),
            RecurringEarnings = recurringEarnings.Select(MapRecurringEarning).ToList(),
            RecurringDeductions = recurringDeductions.Select(MapRecurringDeduction).ToList(),
            PayrollHistory = payrollHistory.Select(MapPayrollRunItemSummary).ToList()
        };
    }

    private async Task EnsureCompensationProfileDoesNotOverlapAsync(
        Guid employeeId,
        DateOnly effectiveStartDate,
        DateOnly? effectiveEndDate,
        bool isActive,
        Guid? existingId,
        CancellationToken cancellationToken)
    {
        if (!isActive)
        {
            return;
        }

        var overlaps = await _dbContext.CompensationProfiles.AnyAsync(
            record =>
                record.EmployeeId == employeeId &&
                record.IsActive &&
                (!existingId.HasValue || record.Id != existingId.Value) &&
                record.EffectiveStartDate <= (effectiveEndDate ?? DateOnly.MaxValue) &&
                (record.EffectiveEndDate ?? DateOnly.MaxValue) >= effectiveStartDate,
            cancellationToken);

        if (overlaps)
        {
            throw BuildValidationException("This employee already has an overlapping active compensation profile for the selected date range.", nameof(SaveCompensationProfileRequestDto.EffectiveStartDate));
        }
    }

    private async Task<CompensationProfileRecordDto> GetCompensationProfileByIdAsync(Guid compensationProfileId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.CompensationProfiles
            .AsNoTracking()
            .Include(item => item.Employee)
                .ThenInclude(employee => employee!.Department)
            .Include(item => item.Employee)
                .ThenInclude(employee => employee!.Branch)
            .Include(item => item.CreatedByUser)
            .Include(item => item.UpdatedByUser)
            .SingleOrDefaultAsync(item => item.Id == compensationProfileId, cancellationToken)
            ?? throw new NotFoundException($"Compensation profile '{compensationProfileId}' was not found.");

        return MapCompensationProfile(record);
    }

    private async Task<EmployeeRecurringEarningRecordDto> GetRecurringEarningByIdAsync(Guid recurringEarningId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.EmployeeRecurringEarnings
            .AsNoTracking()
            .Include(item => item.Employee)
            .Include(item => item.EarningType)
            .SingleOrDefaultAsync(item => item.Id == recurringEarningId, cancellationToken)
            ?? throw new NotFoundException($"Recurring earning '{recurringEarningId}' was not found.");

        return MapRecurringEarning(record);
    }

    private async Task<EmployeeRecurringDeductionRecordDto> GetRecurringDeductionByIdAsync(Guid recurringDeductionId, CancellationToken cancellationToken)
    {
        var record = await _dbContext.EmployeeRecurringDeductions
            .AsNoTracking()
            .Include(item => item.Employee)
            .Include(item => item.DeductionType)
            .SingleOrDefaultAsync(item => item.Id == recurringDeductionId, cancellationToken)
            ?? throw new NotFoundException($"Recurring deduction '{recurringDeductionId}' was not found.");

        return MapRecurringDeduction(record);
    }

    private async Task<Employee> EnsureEmployeeAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        return await _dbContext.Employees
            .SingleOrDefaultAsync(record => record.Id == employeeId, cancellationToken)
            ?? throw new BadRequestException("The selected employee does not exist.");
    }

    private async Task<EarningType> EnsureEarningTypeAsync(Guid earningTypeId, CancellationToken cancellationToken)
    {
        return await _dbContext.EarningTypes
            .SingleOrDefaultAsync(record => record.Id == earningTypeId, cancellationToken)
            ?? throw new BadRequestException("The selected earning type does not exist.");
    }

    private async Task<DeductionType> EnsureDeductionTypeAsync(Guid deductionTypeId, CancellationToken cancellationToken)
    {
        return await _dbContext.DeductionTypes
            .SingleOrDefaultAsync(record => record.Id == deductionTypeId, cancellationToken)
            ?? throw new BadRequestException("The selected deduction type does not exist.");
    }

    private static CompensationProfileRecordDto MapCompensationProfile(CompensationProfile record)
    {
        var employee = record.Employee ?? throw new NotFoundException("The employee linked to this compensation profile could not be found.");
        return new CompensationProfileRecordDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            DepartmentName = employee.Department?.Name ?? string.Empty,
            BranchName = employee.Branch?.Name ?? string.Empty,
            PayType = record.PayType,
            PayFrequency = record.PayFrequency,
            BasicSalary = record.BasicSalary,
            DailyRate = record.DailyRate,
            HourlyRate = record.HourlyRate,
            Currency = record.Currency,
            EffectiveStartDate = record.EffectiveStartDate,
            EffectiveEndDate = record.EffectiveEndDate,
            IsActive = record.IsActive,
            Remarks = record.Remarks,
            CreatedByDisplayName = BuildUserDisplayName(record.CreatedByUser),
            UpdatedByDisplayName = BuildUserDisplayName(record.UpdatedByUser),
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static EmployeeRecurringEarningRecordDto MapRecurringEarning(EmployeeRecurringEarning record)
    {
        var employee = record.Employee ?? throw new NotFoundException("The employee linked to this recurring earning could not be found.");
        var type = record.EarningType ?? throw new NotFoundException("The earning type linked to this recurring earning could not be found.");

        return new EmployeeRecurringEarningRecordDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            EarningTypeId = record.EarningTypeId,
            EarningTypeCode = type.Code,
            EarningTypeName = type.Name,
            Amount = record.Amount,
            Frequency = record.Frequency,
            EffectiveStartDate = record.EffectiveStartDate,
            EffectiveEndDate = record.EffectiveEndDate,
            IsActive = record.IsActive,
            Remarks = record.Remarks,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static EmployeeRecurringDeductionRecordDto MapRecurringDeduction(EmployeeRecurringDeduction record)
    {
        var employee = record.Employee ?? throw new NotFoundException("The employee linked to this recurring deduction could not be found.");
        var type = record.DeductionType ?? throw new NotFoundException("The deduction type linked to this recurring deduction could not be found.");

        return new EmployeeRecurringDeductionRecordDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            DeductionTypeId = record.DeductionTypeId,
            DeductionTypeCode = type.Code,
            DeductionTypeName = type.Name,
            Amount = record.Amount,
            Frequency = record.Frequency,
            Balance = record.Balance,
            EffectiveStartDate = record.EffectiveStartDate,
            EffectiveEndDate = record.EffectiveEndDate,
            IsActive = record.IsActive,
            Remarks = record.Remarks,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static PayrollRunItemDto MapPayrollRunItemSummary(PayrollRunItem record)
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
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static void ValidatePayType(string payType)
    {
        if (!PayrollPayTypes.All.Contains(payType.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Pay type must be monthly, daily, hourly, project, commission, or other.");
        }
    }

    private static void ValidatePayFrequency(string payFrequency)
    {
        if (!PayrollPayFrequencies.All.Contains(payFrequency.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Invalid pay frequency.");
        }
    }

    private static void ValidateCompensationAmounts(SaveCompensationProfileRequestDto request)
    {
        if (request.BasicSalary <= 0m && (request.DailyRate ?? 0m) <= 0m && (request.HourlyRate ?? 0m) <= 0m)
        {
            throw BuildValidationException("At least one salary or rate value must be greater than zero.", nameof(SaveCompensationProfileRequestDto.BasicSalary));
        }
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
}
