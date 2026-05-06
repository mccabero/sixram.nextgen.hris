using Microsoft.EntityFrameworkCore;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Leave;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface ILeaveTypeService
{
    Task<PagedResultDto<LeaveTypeRecordDto>> GetLeaveTypesAsync(LeaveTypeListQueryDto query, CancellationToken cancellationToken = default);

    Task<LeaveTypeRecordDto> GetLeaveTypeByIdAsync(Guid leaveTypeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeaveTypeOptionDto>> GetActiveOptionsAsync(CancellationToken cancellationToken = default);

    Task<LeaveTypeRecordDto> CreateLeaveTypeAsync(SaveLeaveTypeRequestDto request, CancellationToken cancellationToken = default);

    Task<LeaveTypeRecordDto> UpdateLeaveTypeAsync(Guid leaveTypeId, SaveLeaveTypeRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteLeaveTypeAsync(Guid leaveTypeId, CancellationToken cancellationToken = default);
}

public class LeaveTypeService : ILeaveTypeService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<LeaveTypeService> _logger;

    public LeaveTypeService(ApplicationDbContext dbContext, ILogger<LeaveTypeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResultDto<LeaveTypeRecordDto>> GetLeaveTypesAsync(LeaveTypeListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.LeaveTypes
            .AsNoTracking()
            .Select(record => new LeaveTypeRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                IsPaid = record.IsPaid,
                RequiresAttachment = record.RequiresAttachment,
                RequiresReason = record.RequiresReason,
                AllowHalfDay = record.AllowHalfDay,
                AllowNegativeBalance = record.AllowNegativeBalance,
                DefaultAnnualCredits = record.DefaultAnnualCredits,
                MaxDaysPerRequest = record.MaxDaysPerRequest,
                MinDaysBeforeFiling = record.MinDaysBeforeFiling,
                GenderRestriction = record.GenderRestriction,
                EmploymentTypeRestrictionIds = ParseEmploymentTypeRestrictions(record.EmploymentTypeRestrictions),
                CountsRestDays = record.CountsRestDays,
                CountsHolidays = record.CountsHolidays,
                AllowDuringProbationaryPeriod = record.AllowDuringProbationaryPeriod,
                IsActive = record.IsActive,
                EmployeeCount = record.EmployeeLeaveBalances.Select(item => item.EmployeeId).Distinct().Count(),
                PendingRequestCount = record.LeaveRequests.Count(item => item.Status == Constants.LeaveRequestStatuses.Pending),
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            });

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Code.Contains(search) ||
                record.Name.Contains(search) ||
                record.Description.Contains(search));
        }

        if (query.IsActive is not null)
        {
            source = source.Where(record => record.IsActive == query.IsActive.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("code", true) => source.OrderByDescending(record => record.Code).ThenBy(record => record.Name),
            ("code", false) => source.OrderBy(record => record.Code).ThenBy(record => record.Name),
            ("created", true) => source.OrderByDescending(record => record.CreatedAtUtc).ThenByDescending(record => record.Name),
            ("created", false) => source.OrderBy(record => record.CreatedAtUtc).ThenBy(record => record.Name),
            ("pending", true) => source.OrderByDescending(record => record.PendingRequestCount).ThenBy(record => record.Name),
            ("pending", false) => source.OrderBy(record => record.PendingRequestCount).ThenBy(record => record.Name),
            (_, true) => source.OrderByDescending(record => record.Name).ThenByDescending(record => record.Code),
            _ => source.OrderBy(record => record.Name).ThenBy(record => record.Code)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<LeaveTypeRecordDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<LeaveTypeRecordDto> GetLeaveTypeByIdAsync(Guid leaveTypeId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.LeaveTypes
            .AsNoTracking()
            .Where(item => item.Id == leaveTypeId)
            .Select(item => new LeaveTypeRecordDto
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                IsPaid = item.IsPaid,
                RequiresAttachment = item.RequiresAttachment,
                RequiresReason = item.RequiresReason,
                AllowHalfDay = item.AllowHalfDay,
                AllowNegativeBalance = item.AllowNegativeBalance,
                DefaultAnnualCredits = item.DefaultAnnualCredits,
                MaxDaysPerRequest = item.MaxDaysPerRequest,
                MinDaysBeforeFiling = item.MinDaysBeforeFiling,
                GenderRestriction = item.GenderRestriction,
                EmploymentTypeRestrictionIds = ParseEmploymentTypeRestrictions(item.EmploymentTypeRestrictions),
                CountsRestDays = item.CountsRestDays,
                CountsHolidays = item.CountsHolidays,
                AllowDuringProbationaryPeriod = item.AllowDuringProbationaryPeriod,
                IsActive = item.IsActive,
                EmployeeCount = item.EmployeeLeaveBalances.Select(balance => balance.EmployeeId).Distinct().Count(),
                PendingRequestCount = item.LeaveRequests.Count(request => request.Status == Constants.LeaveRequestStatuses.Pending),
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return record ?? throw new NotFoundException($"Leave type '{leaveTypeId}' was not found.");
    }

    public async Task<IReadOnlyList<LeaveTypeOptionDto>> GetActiveOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.LeaveTypes
            .AsNoTracking()
            .Where(record => record.IsActive)
            .OrderBy(record => record.Name)
            .Select(record => new LeaveTypeOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                AllowHalfDay = record.AllowHalfDay,
                RequiresAttachment = record.RequiresAttachment,
                RequiresReason = record.RequiresReason,
                AllowNegativeBalance = record.AllowNegativeBalance,
                DefaultAnnualCredits = record.DefaultAnnualCredits,
                IsActive = record.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveTypeRecordDto> CreateLeaveTypeAsync(SaveLeaveTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateEmploymentTypeRestrictionsAsync(request.EmploymentTypeRestrictionIds, cancellationToken);

        var record = new LeaveType
        {
            CreatedAtUtc = DateTime.UtcNow
        };

        Apply(record, request);
        await EnsureUniquenessAsync(record.Code, record.Name, null, cancellationToken);

        _dbContext.LeaveTypes.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Leave type {LeaveTypeCode} created.", record.Code);
        return await GetLeaveTypeByIdAsync(record.Id, cancellationToken);
    }

    public async Task<LeaveTypeRecordDto> UpdateLeaveTypeAsync(Guid leaveTypeId, SaveLeaveTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateEmploymentTypeRestrictionsAsync(request.EmploymentTypeRestrictionIds, cancellationToken);

        var record = await _dbContext.LeaveTypes.SingleOrDefaultAsync(item => item.Id == leaveTypeId, cancellationToken)
            ?? throw new NotFoundException($"Leave type '{leaveTypeId}' was not found.");

        Apply(record, request);
        record.UpdatedAtUtc = DateTime.UtcNow;
        await EnsureUniquenessAsync(record.Code, record.Name, leaveTypeId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Leave type {LeaveTypeId} updated.", leaveTypeId);
        return await GetLeaveTypeByIdAsync(leaveTypeId, cancellationToken);
    }

    public async Task DeleteLeaveTypeAsync(Guid leaveTypeId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.LeaveTypes
            .Include(item => item.LeaveRequests)
            .Include(item => item.EmployeeLeaveBalances)
            .SingleOrDefaultAsync(item => item.Id == leaveTypeId, cancellationToken)
            ?? throw new NotFoundException($"Leave type '{leaveTypeId}' was not found.");

        if (record.LeaveRequests.Count > 0 || record.EmployeeLeaveBalances.Count > 0)
        {
            throw new BadRequestException("This leave type is already referenced by leave balances or requests. Deactivate it instead of deleting it.");
        }

        _dbContext.LeaveTypes.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Leave type {LeaveTypeId} deleted.", leaveTypeId);
    }

    private async Task EnsureUniquenessAsync(string code, string name, Guid? existingId, CancellationToken cancellationToken)
    {
        if (await _dbContext.LeaveTypes.AnyAsync(record => record.Code == code && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A leave type with code '{code}' already exists.");
        }

        if (await _dbContext.LeaveTypes.AnyAsync(record => record.Name == name && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A leave type named '{name}' already exists.");
        }
    }

    private async Task ValidateEmploymentTypeRestrictionsAsync(IReadOnlyList<Guid> employmentTypeRestrictionIds, CancellationToken cancellationToken)
    {
        if (employmentTypeRestrictionIds.Count == 0)
        {
            return;
        }

        var existingIds = await _dbContext.EmploymentTypes
            .AsNoTracking()
            .Where(record => employmentTypeRestrictionIds.Contains(record.Id))
            .Select(record => record.Id)
            .ToListAsync(cancellationToken);

        if (existingIds.Count != employmentTypeRestrictionIds.Distinct().Count())
        {
            throw new BadRequestException("One or more employment type restrictions are invalid.");
        }
    }

    private static void Apply(LeaveType record, SaveLeaveTypeRequestDto request)
    {
        record.Code = request.Code.Trim().ToUpperInvariant();
        record.Name = request.Name.Trim();
        record.Description = request.Description.Trim();
        record.IsPaid = request.IsPaid;
        record.RequiresAttachment = request.RequiresAttachment;
        record.RequiresReason = request.RequiresReason;
        record.AllowHalfDay = request.AllowHalfDay;
        record.AllowNegativeBalance = request.AllowNegativeBalance;
        record.DefaultAnnualCredits = request.DefaultAnnualCredits;
        record.MaxDaysPerRequest = request.MaxDaysPerRequest;
        record.MinDaysBeforeFiling = request.MinDaysBeforeFiling;
        record.GenderRestriction = request.GenderRestriction.Trim();
        record.EmploymentTypeRestrictions = SerializeEmploymentTypeRestrictions(request.EmploymentTypeRestrictionIds);
        record.CountsRestDays = request.CountsRestDays;
        record.CountsHolidays = request.CountsHolidays;
        record.AllowDuringProbationaryPeriod = request.AllowDuringProbationaryPeriod;
        record.IsActive = request.IsActive;
    }

    public static IReadOnlyList<Guid> ParseEmploymentTypeRestrictions(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return Array.Empty<Guid>();
        }

        return serialized
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => Guid.TryParse(value, out var parsedValue) ? parsedValue : Guid.Empty)
            .Where(value => value != Guid.Empty)
            .Distinct()
            .ToArray();
    }

    public static string SerializeEmploymentTypeRestrictions(IEnumerable<Guid> employmentTypeRestrictionIds)
    {
        return string.Join(
            ",",
            employmentTypeRestrictionIds
                .Where(value => value != Guid.Empty)
                .Distinct()
                .OrderBy(value => value)
                .Select(value => value.ToString("D")));
    }
}
