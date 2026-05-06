using Microsoft.EntityFrameworkCore;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Organization;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IOrganizationSetupService
{
    Task<OrganizationSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<OrganizationOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default);

    Task<PagedResultDto<OrganizationRecordDto>> GetDepartmentsAsync(OrganizationListQueryDto query, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> GetDepartmentByIdAsync(Guid departmentId, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> CreateDepartmentAsync(SaveDepartmentRequestDto request, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> UpdateDepartmentAsync(Guid departmentId, SaveDepartmentRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<OrganizationRecordDto>> GetPositionsAsync(OrganizationListQueryDto query, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> GetPositionByIdAsync(Guid positionId, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> CreatePositionAsync(SavePositionRequestDto request, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> UpdatePositionAsync(Guid positionId, SavePositionRequestDto request, CancellationToken cancellationToken = default);

    Task DeletePositionAsync(Guid positionId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<OrganizationRecordDto>> GetBranchesAsync(OrganizationListQueryDto query, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> GetBranchByIdAsync(Guid branchId, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> CreateBranchAsync(SaveBranchRequestDto request, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> UpdateBranchAsync(Guid branchId, SaveBranchRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteBranchAsync(Guid branchId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<OrganizationRecordDto>> GetEmploymentTypesAsync(OrganizationListQueryDto query, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> GetEmploymentTypeByIdAsync(Guid employmentTypeId, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> CreateEmploymentTypeAsync(SaveEmploymentTypeRequestDto request, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> UpdateEmploymentTypeAsync(Guid employmentTypeId, SaveEmploymentTypeRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteEmploymentTypeAsync(Guid employmentTypeId, CancellationToken cancellationToken = default);

    Task<PagedResultDto<OrganizationRecordDto>> GetEmploymentStatusesAsync(OrganizationListQueryDto query, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> GetEmploymentStatusByIdAsync(Guid employmentStatusId, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> CreateEmploymentStatusAsync(SaveEmploymentStatusRequestDto request, CancellationToken cancellationToken = default);

    Task<OrganizationRecordDto> UpdateEmploymentStatusAsync(Guid employmentStatusId, SaveEmploymentStatusRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteEmploymentStatusAsync(Guid employmentStatusId, CancellationToken cancellationToken = default);
}

public class OrganizationSetupService : IOrganizationSetupService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<OrganizationSetupService> _logger;

    public OrganizationSetupService(ApplicationDbContext dbContext, ILogger<OrganizationSetupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<OrganizationSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        return new OrganizationSummaryDto
        {
            DepartmentCount = await _dbContext.Departments.CountAsync(cancellationToken),
            ActiveDepartmentCount = await _dbContext.Departments.CountAsync(record => record.IsActive, cancellationToken),
            PositionCount = await _dbContext.Positions.CountAsync(cancellationToken),
            ActivePositionCount = await _dbContext.Positions.CountAsync(record => record.IsActive, cancellationToken),
            BranchCount = await _dbContext.Branches.CountAsync(cancellationToken),
            ActiveBranchCount = await _dbContext.Branches.CountAsync(record => record.IsActive, cancellationToken),
            EmploymentTypeCount = await _dbContext.EmploymentTypes.CountAsync(cancellationToken),
            ActiveEmploymentTypeCount = await _dbContext.EmploymentTypes.CountAsync(record => record.IsActive, cancellationToken),
            EmploymentStatusCount = await _dbContext.EmploymentStatuses.CountAsync(cancellationToken),
            ActiveEmploymentStatusCount = await _dbContext.EmploymentStatuses.CountAsync(record => record.IsActive, cancellationToken),
            EmployeeCount = await _dbContext.Employees.CountAsync(cancellationToken),
            ActiveEmployeeCount = await _dbContext.Employees.CountAsync(record => record.IsActive, cancellationToken)
        };
    }

    public async Task<OrganizationOptionsDto> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        return new OrganizationOptionsDto
        {
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
            Positions = await _dbContext.Positions
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new LookupOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    ParentId = record.DepartmentId,
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
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<PagedResultDto<OrganizationRecordDto>> GetDepartmentsAsync(OrganizationListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.Departments
            .AsNoTracking()
            .Select(record => new OrganizationRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                IsActive = record.IsActive,
                EmployeeCount = record.Employees.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            });

        return await ToPageAsync(ApplyOrganizationQuery(source, query), query, cancellationToken);
    }

    public async Task<OrganizationRecordDto> GetDepartmentByIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments
            .AsNoTracking()
            .Where(record => record.Id == departmentId)
            .Select(record => new OrganizationRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                IsActive = record.IsActive,
                EmployeeCount = record.Employees.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return department ?? throw new NotFoundException($"Department '{departmentId}' was not found.");
    }

    public async Task<OrganizationRecordDto> CreateDepartmentAsync(SaveDepartmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var department = new Department();
        ApplyDepartment(department, request);
        department.CreatedAtUtc = DateTime.UtcNow;

        await EnsureDepartmentUniquenessAsync(department.Code, department.Name, null, cancellationToken);

        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Department {DepartmentCode} created.", department.Code);
        return await GetDepartmentByIdAsync(department.Id, cancellationToken);
    }

    public async Task<OrganizationRecordDto> UpdateDepartmentAsync(Guid departmentId, SaveDepartmentRequestDto request, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments.SingleOrDefaultAsync(record => record.Id == departmentId, cancellationToken)
            ?? throw new NotFoundException($"Department '{departmentId}' was not found.");

        ApplyDepartment(department, request);
        department.UpdatedAtUtc = DateTime.UtcNow;

        await EnsureDepartmentUniquenessAsync(department.Code, department.Name, departmentId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Department {DepartmentId} updated.", departmentId);
        return await GetDepartmentByIdAsync(departmentId, cancellationToken);
    }

    public async Task DeleteDepartmentAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var department = await _dbContext.Departments
            .Include(record => record.Positions)
            .Include(record => record.Employees)
            .SingleOrDefaultAsync(record => record.Id == departmentId, cancellationToken)
            ?? throw new NotFoundException($"Department '{departmentId}' was not found.");

        if (department.Positions.Count > 0 || department.Employees.Count > 0)
        {
            throw new BadRequestException("This department is already referenced. Deactivate it instead of deleting it.");
        }

        _dbContext.Departments.Remove(department);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Department {DepartmentId} deleted.", departmentId);
    }

    public async Task<PagedResultDto<OrganizationRecordDto>> GetPositionsAsync(OrganizationListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.Positions
            .AsNoTracking()
            .Select(record => new OrganizationRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                DepartmentId = record.DepartmentId,
                DepartmentName = record.Department != null ? record.Department.Name : string.Empty,
                IsActive = record.IsActive,
                EmployeeCount = record.Employees.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            });

        return await ToPageAsync(ApplyOrganizationQuery(source, query), query, cancellationToken);
    }

    public async Task<OrganizationRecordDto> GetPositionByIdAsync(Guid positionId, CancellationToken cancellationToken = default)
    {
        var position = await _dbContext.Positions
            .AsNoTracking()
            .Where(record => record.Id == positionId)
            .Select(record => new OrganizationRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                DepartmentId = record.DepartmentId,
                DepartmentName = record.Department != null ? record.Department.Name : string.Empty,
                IsActive = record.IsActive,
                EmployeeCount = record.Employees.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return position ?? throw new NotFoundException($"Position '{positionId}' was not found.");
    }

    public async Task<OrganizationRecordDto> CreatePositionAsync(SavePositionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.DepartmentId is not null)
        {
            await EnsureDepartmentExistsAsync(request.DepartmentId.Value, cancellationToken);
        }

        var position = new Position();
        ApplyPosition(position, request);
        position.CreatedAtUtc = DateTime.UtcNow;

        await EnsurePositionUniquenessAsync(position.Code, position.Name, null, cancellationToken);

        _dbContext.Positions.Add(position);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Position {PositionCode} created.", position.Code);
        return await GetPositionByIdAsync(position.Id, cancellationToken);
    }

    public async Task<OrganizationRecordDto> UpdatePositionAsync(Guid positionId, SavePositionRequestDto request, CancellationToken cancellationToken = default)
    {
        var position = await _dbContext.Positions.SingleOrDefaultAsync(record => record.Id == positionId, cancellationToken)
            ?? throw new NotFoundException($"Position '{positionId}' was not found.");

        if (request.DepartmentId is not null)
        {
            await EnsureDepartmentExistsAsync(request.DepartmentId.Value, cancellationToken);
        }

        ApplyPosition(position, request);
        position.UpdatedAtUtc = DateTime.UtcNow;

        await EnsurePositionUniquenessAsync(position.Code, position.Name, positionId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Position {PositionId} updated.", positionId);
        return await GetPositionByIdAsync(positionId, cancellationToken);
    }

    public async Task DeletePositionAsync(Guid positionId, CancellationToken cancellationToken = default)
    {
        var position = await _dbContext.Positions
            .Include(record => record.Employees)
            .SingleOrDefaultAsync(record => record.Id == positionId, cancellationToken)
            ?? throw new NotFoundException($"Position '{positionId}' was not found.");

        if (position.Employees.Count > 0)
        {
            throw new BadRequestException("This position is already referenced by one or more employees. Deactivate it instead of deleting it.");
        }

        _dbContext.Positions.Remove(position);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Position {PositionId} deleted.", positionId);
    }

    public async Task<PagedResultDto<OrganizationRecordDto>> GetBranchesAsync(OrganizationListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.Branches
            .AsNoTracking()
            .Select(record => new OrganizationRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                Address = record.Address,
                IsActive = record.IsActive,
                EmployeeCount = record.Employees.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            });

        return await ToPageAsync(ApplyOrganizationQuery(source, query), query, cancellationToken);
    }

    public async Task<OrganizationRecordDto> GetBranchByIdAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var branch = await _dbContext.Branches
            .AsNoTracking()
            .Where(record => record.Id == branchId)
            .Select(record => new OrganizationRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                Address = record.Address,
                IsActive = record.IsActive,
                EmployeeCount = record.Employees.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return branch ?? throw new NotFoundException($"Branch '{branchId}' was not found.");
    }

    public async Task<OrganizationRecordDto> CreateBranchAsync(SaveBranchRequestDto request, CancellationToken cancellationToken = default)
    {
        var branch = new Branch();
        ApplyBranch(branch, request);
        branch.CreatedAtUtc = DateTime.UtcNow;

        await EnsureBranchUniquenessAsync(branch.Code, branch.Name, null, cancellationToken);

        _dbContext.Branches.Add(branch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Branch {BranchCode} created.", branch.Code);
        return await GetBranchByIdAsync(branch.Id, cancellationToken);
    }

    public async Task<OrganizationRecordDto> UpdateBranchAsync(Guid branchId, SaveBranchRequestDto request, CancellationToken cancellationToken = default)
    {
        var branch = await _dbContext.Branches.SingleOrDefaultAsync(record => record.Id == branchId, cancellationToken)
            ?? throw new NotFoundException($"Branch '{branchId}' was not found.");

        ApplyBranch(branch, request);
        branch.UpdatedAtUtc = DateTime.UtcNow;

        await EnsureBranchUniquenessAsync(branch.Code, branch.Name, branchId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Branch {BranchId} updated.", branchId);
        return await GetBranchByIdAsync(branchId, cancellationToken);
    }

    public async Task DeleteBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var branch = await _dbContext.Branches
            .Include(record => record.Employees)
            .SingleOrDefaultAsync(record => record.Id == branchId, cancellationToken)
            ?? throw new NotFoundException($"Branch '{branchId}' was not found.");

        if (branch.Employees.Count > 0)
        {
            throw new BadRequestException("This branch is already referenced by one or more employees. Deactivate it instead of deleting it.");
        }

        _dbContext.Branches.Remove(branch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Branch {BranchId} deleted.", branchId);
    }

    public async Task<PagedResultDto<OrganizationRecordDto>> GetEmploymentTypesAsync(OrganizationListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.EmploymentTypes
            .AsNoTracking()
            .Select(record => new OrganizationRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                IsActive = record.IsActive,
                EmployeeCount = record.Employees.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            });

        return await ToPageAsync(ApplyOrganizationQuery(source, query), query, cancellationToken);
    }

    public async Task<OrganizationRecordDto> GetEmploymentTypeByIdAsync(Guid employmentTypeId, CancellationToken cancellationToken = default)
    {
        var employmentType = await _dbContext.EmploymentTypes
            .AsNoTracking()
            .Where(record => record.Id == employmentTypeId)
            .Select(record => new OrganizationRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                IsActive = record.IsActive,
                EmployeeCount = record.Employees.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return employmentType ?? throw new NotFoundException($"Employment type '{employmentTypeId}' was not found.");
    }

    public async Task<OrganizationRecordDto> CreateEmploymentTypeAsync(SaveEmploymentTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        var employmentType = new EmploymentType();
        ApplyEmploymentType(employmentType, request);
        employmentType.CreatedAtUtc = DateTime.UtcNow;

        await EnsureEmploymentTypeUniquenessAsync(employmentType.Code, employmentType.Name, null, cancellationToken);

        _dbContext.EmploymentTypes.Add(employmentType);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Employment type {EmploymentTypeCode} created.", employmentType.Code);
        return await GetEmploymentTypeByIdAsync(employmentType.Id, cancellationToken);
    }

    public async Task<OrganizationRecordDto> UpdateEmploymentTypeAsync(Guid employmentTypeId, SaveEmploymentTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        var employmentType = await _dbContext.EmploymentTypes.SingleOrDefaultAsync(record => record.Id == employmentTypeId, cancellationToken)
            ?? throw new NotFoundException($"Employment type '{employmentTypeId}' was not found.");

        ApplyEmploymentType(employmentType, request);
        employmentType.UpdatedAtUtc = DateTime.UtcNow;

        await EnsureEmploymentTypeUniquenessAsync(employmentType.Code, employmentType.Name, employmentTypeId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Employment type {EmploymentTypeId} updated.", employmentTypeId);
        return await GetEmploymentTypeByIdAsync(employmentTypeId, cancellationToken);
    }

    public async Task DeleteEmploymentTypeAsync(Guid employmentTypeId, CancellationToken cancellationToken = default)
    {
        var employmentType = await _dbContext.EmploymentTypes
            .Include(record => record.Employees)
            .SingleOrDefaultAsync(record => record.Id == employmentTypeId, cancellationToken)
            ?? throw new NotFoundException($"Employment type '{employmentTypeId}' was not found.");

        if (employmentType.Employees.Count > 0)
        {
            throw new BadRequestException("This employment type is already referenced by one or more employees. Deactivate it instead of deleting it.");
        }

        _dbContext.EmploymentTypes.Remove(employmentType);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Employment type {EmploymentTypeId} deleted.", employmentTypeId);
    }

    public async Task<PagedResultDto<OrganizationRecordDto>> GetEmploymentStatusesAsync(OrganizationListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.EmploymentStatuses
            .AsNoTracking()
            .Select(record => new OrganizationRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                IsActive = record.IsActive,
                EmployeeCount = record.Employees.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            });

        return await ToPageAsync(ApplyOrganizationQuery(source, query), query, cancellationToken);
    }

    public async Task<OrganizationRecordDto> GetEmploymentStatusByIdAsync(Guid employmentStatusId, CancellationToken cancellationToken = default)
    {
        var employmentStatus = await _dbContext.EmploymentStatuses
            .AsNoTracking()
            .Where(record => record.Id == employmentStatusId)
            .Select(record => new OrganizationRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                IsActive = record.IsActive,
                EmployeeCount = record.Employees.Count,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return employmentStatus ?? throw new NotFoundException($"Employment status '{employmentStatusId}' was not found.");
    }

    public async Task<OrganizationRecordDto> CreateEmploymentStatusAsync(SaveEmploymentStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var employmentStatus = new EmploymentStatus();
        ApplyEmploymentStatus(employmentStatus, request);
        employmentStatus.CreatedAtUtc = DateTime.UtcNow;

        await EnsureEmploymentStatusUniquenessAsync(employmentStatus.Code, employmentStatus.Name, null, cancellationToken);

        _dbContext.EmploymentStatuses.Add(employmentStatus);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Employment status {EmploymentStatusCode} created.", employmentStatus.Code);
        return await GetEmploymentStatusByIdAsync(employmentStatus.Id, cancellationToken);
    }

    public async Task<OrganizationRecordDto> UpdateEmploymentStatusAsync(Guid employmentStatusId, SaveEmploymentStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var employmentStatus = await _dbContext.EmploymentStatuses.SingleOrDefaultAsync(record => record.Id == employmentStatusId, cancellationToken)
            ?? throw new NotFoundException($"Employment status '{employmentStatusId}' was not found.");

        ApplyEmploymentStatus(employmentStatus, request);
        employmentStatus.UpdatedAtUtc = DateTime.UtcNow;

        await EnsureEmploymentStatusUniquenessAsync(employmentStatus.Code, employmentStatus.Name, employmentStatusId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Employment status {EmploymentStatusId} updated.", employmentStatusId);
        return await GetEmploymentStatusByIdAsync(employmentStatusId, cancellationToken);
    }

    public async Task DeleteEmploymentStatusAsync(Guid employmentStatusId, CancellationToken cancellationToken = default)
    {
        var employmentStatus = await _dbContext.EmploymentStatuses
            .Include(record => record.Employees)
            .SingleOrDefaultAsync(record => record.Id == employmentStatusId, cancellationToken)
            ?? throw new NotFoundException($"Employment status '{employmentStatusId}' was not found.");

        if (employmentStatus.Employees.Count > 0)
        {
            throw new BadRequestException("This employment status is already referenced by one or more employees. Deactivate it instead of deleting it.");
        }

        _dbContext.EmploymentStatuses.Remove(employmentStatus);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Employment status {EmploymentStatusId} deleted.", employmentStatusId);
    }

    private static IQueryable<OrganizationRecordDto> ApplyOrganizationQuery(IQueryable<OrganizationRecordDto> source, OrganizationListQueryDto query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Code.Contains(search) ||
                record.Name.Contains(search) ||
                record.Description.Contains(search) ||
                record.Address.Contains(search) ||
                record.DepartmentName.Contains(search));
        }

        if (query.IsActive is not null)
        {
            source = source.Where(record => record.IsActive == query.IsActive.Value);
        }

        return (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("code", true) => source.OrderByDescending(record => record.Code).ThenBy(record => record.Name),
            ("code", false) => source.OrderBy(record => record.Code).ThenBy(record => record.Name),
            ("employees", true) => source.OrderByDescending(record => record.EmployeeCount).ThenBy(record => record.Name),
            ("employees", false) => source.OrderBy(record => record.EmployeeCount).ThenBy(record => record.Name),
            ("created", true) => source.OrderByDescending(record => record.CreatedAtUtc).ThenBy(record => record.Name),
            ("created", false) => source.OrderBy(record => record.CreatedAtUtc).ThenBy(record => record.Name),
            (_, true) => source.OrderByDescending(record => record.Name).ThenBy(record => record.Code),
            _ => source.OrderBy(record => record.Name).ThenBy(record => record.Code)
        };
    }

    private static async Task<PagedResultDto<OrganizationRecordDto>> ToPageAsync(
        IQueryable<OrganizationRecordDto> source,
        OrganizationListQueryDto query,
        CancellationToken cancellationToken)
    {
        var totalCount = await source.CountAsync(cancellationToken);
        var pageSize = query.PageSize;
        var pageNumber = query.PageNumber;
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<OrganizationRecordDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private async Task EnsureDepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Departments.AnyAsync(record => record.Id == departmentId, cancellationToken);
        if (!exists)
        {
            throw new BadRequestException($"Department '{departmentId}' does not exist.");
        }
    }

    private async Task EnsureDepartmentUniquenessAsync(string code, string name, Guid? existingId, CancellationToken cancellationToken)
    {
        if (await _dbContext.Departments.AnyAsync(record => record.Code == code && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A department with code '{code}' already exists.");
        }

        if (await _dbContext.Departments.AnyAsync(record => record.Name == name && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A department named '{name}' already exists.");
        }
    }

    private async Task EnsurePositionUniquenessAsync(string code, string name, Guid? existingId, CancellationToken cancellationToken)
    {
        if (await _dbContext.Positions.AnyAsync(record => record.Code == code && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A position with code '{code}' already exists.");
        }

        if (await _dbContext.Positions.AnyAsync(record => record.Name == name && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A position named '{name}' already exists.");
        }
    }

    private async Task EnsureBranchUniquenessAsync(string code, string name, Guid? existingId, CancellationToken cancellationToken)
    {
        if (await _dbContext.Branches.AnyAsync(record => record.Code == code && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A branch with code '{code}' already exists.");
        }

        if (await _dbContext.Branches.AnyAsync(record => record.Name == name && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A branch named '{name}' already exists.");
        }
    }

    private async Task EnsureEmploymentTypeUniquenessAsync(string code, string name, Guid? existingId, CancellationToken cancellationToken)
    {
        if (await _dbContext.EmploymentTypes.AnyAsync(record => record.Code == code && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"An employment type with code '{code}' already exists.");
        }

        if (await _dbContext.EmploymentTypes.AnyAsync(record => record.Name == name && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"An employment type named '{name}' already exists.");
        }
    }

    private async Task EnsureEmploymentStatusUniquenessAsync(string code, string name, Guid? existingId, CancellationToken cancellationToken)
    {
        if (await _dbContext.EmploymentStatuses.AnyAsync(record => record.Code == code && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"An employment status with code '{code}' already exists.");
        }

        if (await _dbContext.EmploymentStatuses.AnyAsync(record => record.Name == name && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"An employment status named '{name}' already exists.");
        }
    }

    private static void ApplyDepartment(Department department, SaveDepartmentRequestDto request)
    {
        department.Code = NormalizeCode(request.Code);
        department.Name = request.Name.Trim();
        department.Description = request.Description.Trim();
        department.IsActive = request.IsActive;
    }

    private static void ApplyPosition(Position position, SavePositionRequestDto request)
    {
        position.Code = NormalizeCode(request.Code);
        position.Name = request.Name.Trim();
        position.Description = request.Description.Trim();
        position.DepartmentId = request.DepartmentId;
        position.IsActive = request.IsActive;
    }

    private static void ApplyBranch(Branch branch, SaveBranchRequestDto request)
    {
        branch.Code = NormalizeCode(request.Code);
        branch.Name = request.Name.Trim();
        branch.Description = request.Description.Trim();
        branch.Address = request.Address.Trim();
        branch.IsActive = request.IsActive;
    }

    private static void ApplyEmploymentType(EmploymentType employmentType, SaveEmploymentTypeRequestDto request)
    {
        employmentType.Code = NormalizeCode(request.Code);
        employmentType.Name = request.Name.Trim();
        employmentType.Description = request.Description.Trim();
        employmentType.IsActive = request.IsActive;
    }

    private static void ApplyEmploymentStatus(EmploymentStatus employmentStatus, SaveEmploymentStatusRequestDto request)
    {
        employmentStatus.Code = NormalizeCode(request.Code);
        employmentStatus.Name = request.Name.Trim();
        employmentStatus.Description = request.Description.Trim();
        employmentStatus.IsActive = request.IsActive;
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().ToUpperInvariant();
    }
}
