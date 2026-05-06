using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Employees;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IEmployeeService
{
    Task<PagedResultDto<EmployeeSummaryDto>> GetEmployeesAsync(EmployeeListQueryDto query, CancellationToken cancellationToken = default);

    Task<EmployeeDetailDto> GetEmployeeByIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeEditorOptionsDto> GetEditorOptionsAsync(Guid? employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeDetailDto> CreateEmployeeAsync(SaveEmployeeRequestDto request, CancellationToken cancellationToken = default);

    Task<EmployeeDetailDto> UpdateEmployeeAsync(Guid employeeId, SaveEmployeeRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default);
}

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmployeeDocumentStorageService _documentStorageService;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IEmployeeDocumentStorageService documentStorageService,
        ILogger<EmployeeService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _documentStorageService = documentStorageService;
        _logger = logger;
    }

    public async Task<PagedResultDto<EmployeeSummaryDto>> GetEmployeesAsync(EmployeeListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.Employees
            .AsNoTracking()
            .Include(record => record.Department)
            .Include(record => record.Position)
            .Include(record => record.Branch)
            .Include(record => record.EmploymentType)
            .Include(record => record.EmploymentStatus)
            .Include(record => record.Manager)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.EmployeeCode.Contains(search) ||
                record.FirstName.Contains(search) ||
                record.MiddleName.Contains(search) ||
                record.LastName.Contains(search) ||
                record.Email.Contains(search) ||
                (record.Department != null && record.Department.Name.Contains(search)) ||
                (record.Position != null && record.Position.Name.Contains(search)) ||
                (record.Branch != null && record.Branch.Name.Contains(search)));
        }

        if (query.DepartmentId is not null)
        {
            source = source.Where(record => record.DepartmentId == query.DepartmentId);
        }

        if (query.PositionId is not null)
        {
            source = source.Where(record => record.PositionId == query.PositionId);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.BranchId == query.BranchId);
        }

        if (query.EmploymentTypeId is not null)
        {
            source = source.Where(record => record.EmploymentTypeId == query.EmploymentTypeId);
        }

        if (query.EmploymentStatusId is not null)
        {
            source = source.Where(record => record.EmploymentStatusId == query.EmploymentStatusId);
        }

        if (query.IsActive is not null)
        {
            source = source.Where(record => record.IsActive == query.IsActive.Value);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("code", true) => source.OrderByDescending(record => record.EmployeeCode).ThenByDescending(record => record.LastName).ThenByDescending(record => record.FirstName),
            ("code", false) => source.OrderBy(record => record.EmployeeCode).ThenBy(record => record.LastName).ThenBy(record => record.FirstName),
            ("hired", true) => source.OrderByDescending(record => record.DateHired).ThenByDescending(record => record.LastName).ThenByDescending(record => record.FirstName),
            ("hired", false) => source.OrderBy(record => record.DateHired).ThenBy(record => record.LastName).ThenBy(record => record.FirstName),
            ("status", true) => source.OrderByDescending(record => record.EmploymentStatus != null ? record.EmploymentStatus.Name : string.Empty).ThenByDescending(record => record.LastName).ThenByDescending(record => record.FirstName),
            ("status", false) => source.OrderBy(record => record.EmploymentStatus != null ? record.EmploymentStatus.Name : string.Empty).ThenBy(record => record.LastName).ThenBy(record => record.FirstName),
            (_, true) => source.OrderByDescending(record => record.LastName).ThenByDescending(record => record.FirstName),
            _ => source.OrderBy(record => record.LastName).ThenBy(record => record.FirstName)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var pageSize = query.PageSize;
        var pageNumber = query.PageNumber;

        var records = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = records
            .Select(MapSummary)
            .ToList();

        return new PagedResultDto<EmployeeSummaryDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<EmployeeDetailDto> GetEmployeeByIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _dbContext.Employees
            .AsNoTracking()
            .Include(record => record.Department)
            .Include(record => record.Position)
            .Include(record => record.Branch)
            .Include(record => record.EmploymentType)
            .Include(record => record.EmploymentStatus)
            .Include(record => record.Manager)
            .Include(record => record.User)
            .SingleOrDefaultAsync(record => record.Id == employeeId, cancellationToken);

        return employee is null
            ? throw new NotFoundException($"Employee '{employeeId}' was not found.")
            : MapDetail(employee);
    }

    public async Task<EmployeeEditorOptionsDto> GetEditorOptionsAsync(Guid? employeeId, CancellationToken cancellationToken = default)
    {
        var employee = employeeId is null
            ? null
            : await _dbContext.Employees.AsNoTracking().SingleOrDefaultAsync(record => record.Id == employeeId.Value, cancellationToken);

        var linkedUserIds = await _dbContext.Employees
            .AsNoTracking()
            .Where(record => record.UserId != null && record.Id != employeeId)
            .Select(record => record.UserId!)
            .ToListAsync(cancellationToken);

        var options = new EmployeeEditorOptionsDto
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
                .ToListAsync(cancellationToken),
            Managers = Array.Empty<EmployeeManagerOptionDto>(),
            UserAccounts = await _userManager.Users
                .AsNoTracking()
                .Where(record => !linkedUserIds.Contains(record.Id))
                .OrderBy(record => record.Email)
                .Select(record => new UserOptionDto
                {
                    Id = record.Id,
                    Email = record.Email ?? string.Empty,
                    DisplayName = record.DisplayName,
                    IsEnabled = record.IsEnabled
                })
                .ToListAsync(cancellationToken)
        };

        var managerRecords = await _dbContext.Employees
            .AsNoTracking()
            .Where(record => record.IsActive && record.Id != employeeId)
            .OrderBy(record => record.LastName)
            .ThenBy(record => record.FirstName)
            .Select(record => new
            {
                record.Id,
                record.EmployeeCode,
                record.FirstName,
                record.MiddleName,
                record.LastName,
                record.Suffix,
                record.IsActive
            })
            .ToListAsync(cancellationToken);

        options.Managers = managerRecords
            .Select(record => new EmployeeManagerOptionDto
            {
                Id = record.Id,
                EmployeeCode = record.EmployeeCode,
                FullName = BuildFullName(record.FirstName, record.MiddleName, record.LastName, record.Suffix),
                IsActive = record.IsActive
            })
            .ToList();

        if (employee is not null)
        {
            await AppendInactiveSelectionsAsync(employee, options, cancellationToken);
        }

        return options;
    }

    public async Task<EmployeeDetailDto> CreateEmployeeAsync(SaveEmployeeRequestDto request, CancellationToken cancellationToken = default)
    {
        var employee = new Employee
        {
            CreatedAtUtc = DateTime.UtcNow
        };

        await ApplyAndValidateAsync(employee, request, null, cancellationToken);

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Employee {EmployeeCode} created.", employee.EmployeeCode);
        return await GetEmployeeByIdAsync(employee.Id, cancellationToken);
    }

    public async Task<EmployeeDetailDto> UpdateEmployeeAsync(Guid employeeId, SaveEmployeeRequestDto request, CancellationToken cancellationToken = default)
    {
        var employee = await _dbContext.Employees.SingleOrDefaultAsync(record => record.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Employee '{employeeId}' was not found.");

        await ApplyAndValidateAsync(employee, request, employeeId, cancellationToken);
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Employee {EmployeeId} updated.", employeeId);
        return await GetEmployeeByIdAsync(employeeId, cancellationToken);
    }

    public async Task DeleteEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _dbContext.Employees
            .Include(record => record.Documents)
            .Include(record => record.ScheduleAssignments)
            .Include(record => record.AttendanceRecords)
            .Include(record => record.LeaveBalances)
            .Include(record => record.LeaveBalanceTransactions)
            .Include(record => record.LeaveRequests)
            .SingleOrDefaultAsync(record => record.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Employee '{employeeId}' was not found.");

        if (employee.AttendanceRecords.Count > 0 ||
            employee.ScheduleAssignments.Count > 0 ||
            employee.LeaveBalances.Count > 0 ||
            employee.LeaveBalanceTransactions.Count > 0 ||
            employee.LeaveRequests.Count > 0)
        {
            throw new BadRequestException("This employee has attendance, leave, or schedule history. Deactivate the profile instead of deleting it.");
        }

        var directReports = await _dbContext.Employees
            .Where(record => record.ManagerId == employeeId)
            .ToListAsync(cancellationToken);

        foreach (var directReport in directReports)
        {
            directReport.ManagerId = null;
            directReport.UpdatedAtUtc = DateTime.UtcNow;
        }

        var documentStoragePaths = employee.Documents
            .Select(record => record.FilePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToList();

        if (employee.Documents.Count > 0)
        {
            _dbContext.EmployeeDocuments.RemoveRange(employee.Documents);
        }

        _dbContext.Employees.Remove(employee);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var storagePath in documentStoragePaths)
        {
            await _documentStorageService.DeleteAsync(storagePath, cancellationToken);
        }

        _logger.LogInformation("Employee {EmployeeId} deleted.", employeeId);
    }

    private async Task ApplyAndValidateAsync(
        Employee employee,
        SaveEmployeeRequestDto request,
        Guid? existingEmployeeId,
        CancellationToken cancellationToken)
    {
        var employeeCode = request.EmployeeCode.Trim().ToUpperInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _dbContext.Employees.AnyAsync(record => record.EmployeeCode == employeeCode && record.Id != existingEmployeeId, cancellationToken))
        {
            throw new ConflictException($"An employee with code '{employeeCode}' already exists.");
        }

        var department = await _dbContext.Departments.SingleOrDefaultAsync(record => record.Id == request.DepartmentId, cancellationToken)
            ?? throw new BadRequestException("The selected department does not exist.");

        var position = await _dbContext.Positions.SingleOrDefaultAsync(record => record.Id == request.PositionId, cancellationToken)
            ?? throw new BadRequestException("The selected position does not exist.");

        var branch = await _dbContext.Branches.SingleOrDefaultAsync(record => record.Id == request.BranchId, cancellationToken)
            ?? throw new BadRequestException("The selected branch does not exist.");

        var employmentType = await _dbContext.EmploymentTypes.SingleOrDefaultAsync(record => record.Id == request.EmploymentTypeId, cancellationToken)
            ?? throw new BadRequestException("The selected employment type does not exist.");

        var employmentStatus = await _dbContext.EmploymentStatuses.SingleOrDefaultAsync(record => record.Id == request.EmploymentStatusId, cancellationToken)
            ?? throw new BadRequestException("The selected employment status does not exist.");

        if (position.DepartmentId is not null && position.DepartmentId != request.DepartmentId)
        {
            throw new BadRequestException("The selected position belongs to a different department.");
        }

        if (request.ManagerId is not null)
        {
            if (existingEmployeeId is not null && request.ManagerId == existingEmployeeId)
            {
                throw new BadRequestException("An employee cannot report to themselves.");
            }

            var managerExists = await _dbContext.Employees.AnyAsync(record => record.Id == request.ManagerId, cancellationToken);
            if (!managerExists)
            {
                throw new BadRequestException("The selected reporting manager does not exist.");
            }
        }

        ApplicationUser? linkedUser = null;
        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            linkedUser = await _userManager.FindByIdAsync(request.UserId.Trim());
            if (linkedUser is null)
            {
                throw new BadRequestException("The selected linked user account does not exist.");
            }

            var userAlreadyLinked = await _dbContext.Employees.AnyAsync(
                record => record.UserId == linkedUser.Id && record.Id != existingEmployeeId,
                cancellationToken);

            if (userAlreadyLinked)
            {
                throw new ConflictException("That user account is already linked to another employee profile.");
            }
        }

        employee.EmployeeCode = employeeCode;
        employee.FirstName = request.FirstName.Trim();
        employee.MiddleName = request.MiddleName.Trim();
        employee.LastName = request.LastName.Trim();
        employee.Suffix = request.Suffix.Trim();
        employee.Gender = request.Gender.Trim();
        employee.BirthDate = request.BirthDate;
        employee.CivilStatus = request.CivilStatus.Trim();
        employee.Nationality = request.Nationality.Trim();
        employee.MobileNumber = request.MobileNumber.Trim();
        employee.Email = email;
        employee.Address = request.Address.Trim();
        employee.CityProvince = request.CityProvince.Trim();
        employee.PostalCode = request.PostalCode.Trim();
        employee.EmergencyContactName = request.EmergencyContactName.Trim();
        employee.EmergencyContactRelationship = request.EmergencyContactRelationship.Trim();
        employee.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
        employee.DepartmentId = department.Id;
        employee.PositionId = position.Id;
        employee.BranchId = branch.Id;
        employee.EmploymentTypeId = employmentType.Id;
        employee.EmploymentStatusId = employmentStatus.Id;
        employee.ManagerId = request.ManagerId;
        employee.WorkSchedule = request.WorkSchedule.Trim();
        employee.DateHired = request.DateHired;
        employee.DateRegularized = request.DateRegularized;
        employee.DateSeparated = request.DateSeparated;
        employee.SssNumber = request.SssNumber.Trim();
        employee.PhilHealthNumber = request.PhilHealthNumber.Trim();
        employee.PagIbigNumber = request.PagIbigNumber.Trim();
        employee.TinNumber = request.TinNumber.Trim();
        employee.OtherGovernmentId = request.OtherGovernmentId.Trim();
        employee.UserId = linkedUser?.Id;
        employee.IsActive = request.IsActive;
    }

    private async Task AppendInactiveSelectionsAsync(Employee employee, EmployeeEditorOptionsDto options, CancellationToken cancellationToken)
    {
        if (employee.DepartmentId is not null && options.Departments.All(option => option.Id != employee.DepartmentId.Value))
        {
            var department = await _dbContext.Departments.AsNoTracking().SingleAsync(record => record.Id == employee.DepartmentId.Value, cancellationToken);
            options.Departments = [.. options.Departments, new LookupOptionDto { Id = department.Id, Code = department.Code, Name = department.Name, IsActive = department.IsActive }];
        }

        if (employee.PositionId is not null && options.Positions.All(option => option.Id != employee.PositionId.Value))
        {
            var position = await _dbContext.Positions.AsNoTracking().SingleAsync(record => record.Id == employee.PositionId.Value, cancellationToken);
            options.Positions = [.. options.Positions, new LookupOptionDto { Id = position.Id, Code = position.Code, Name = position.Name, ParentId = position.DepartmentId, IsActive = position.IsActive }];
        }

        if (employee.BranchId is not null && options.Branches.All(option => option.Id != employee.BranchId.Value))
        {
            var branch = await _dbContext.Branches.AsNoTracking().SingleAsync(record => record.Id == employee.BranchId.Value, cancellationToken);
            options.Branches = [.. options.Branches, new LookupOptionDto { Id = branch.Id, Code = branch.Code, Name = branch.Name, IsActive = branch.IsActive }];
        }

        if (employee.EmploymentTypeId is not null && options.EmploymentTypes.All(option => option.Id != employee.EmploymentTypeId.Value))
        {
            var employmentType = await _dbContext.EmploymentTypes.AsNoTracking().SingleAsync(record => record.Id == employee.EmploymentTypeId.Value, cancellationToken);
            options.EmploymentTypes = [.. options.EmploymentTypes, new LookupOptionDto { Id = employmentType.Id, Code = employmentType.Code, Name = employmentType.Name, IsActive = employmentType.IsActive }];
        }

        if (employee.EmploymentStatusId is not null && options.EmploymentStatuses.All(option => option.Id != employee.EmploymentStatusId.Value))
        {
            var employmentStatus = await _dbContext.EmploymentStatuses.AsNoTracking().SingleAsync(record => record.Id == employee.EmploymentStatusId.Value, cancellationToken);
            options.EmploymentStatuses = [.. options.EmploymentStatuses, new LookupOptionDto { Id = employmentStatus.Id, Code = employmentStatus.Code, Name = employmentStatus.Name, IsActive = employmentStatus.IsActive }];
        }

        if (employee.ManagerId is not null && options.Managers.All(option => option.Id != employee.ManagerId.Value))
        {
            var manager = await _dbContext.Employees.AsNoTracking().SingleAsync(record => record.Id == employee.ManagerId.Value, cancellationToken);
            options.Managers = [.. options.Managers, new EmployeeManagerOptionDto
            {
                Id = manager.Id,
                EmployeeCode = manager.EmployeeCode,
                FullName = BuildFullName(manager.FirstName, manager.MiddleName, manager.LastName, manager.Suffix),
                IsActive = manager.IsActive
            }];
        }

        if (!string.IsNullOrWhiteSpace(employee.UserId) && options.UserAccounts.All(option => option.Id != employee.UserId))
        {
            var user = await _userManager.Users.AsNoTracking().SingleAsync(record => record.Id == employee.UserId, cancellationToken);
            options.UserAccounts = [.. options.UserAccounts, new UserOptionDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                DisplayName = user.DisplayName,
                IsEnabled = user.IsEnabled
            }];
        }
    }

    private static EmployeeSummaryDto MapSummary(Employee employee)
    {
        return new EmployeeSummaryDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            Email = employee.Email,
            MobileNumber = employee.MobileNumber,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            PositionName = employee.Position?.Name ?? string.Empty,
            BranchName = employee.Branch?.Name ?? string.Empty,
            EmploymentTypeName = employee.EmploymentType?.Name ?? string.Empty,
            EmploymentStatusName = employee.EmploymentStatus?.Name ?? string.Empty,
            ManagerName = employee.Manager is null
                ? string.Empty
                : BuildFullName(employee.Manager.FirstName, employee.Manager.MiddleName, employee.Manager.LastName, employee.Manager.Suffix),
            DateHired = employee.DateHired,
            IsActive = employee.IsActive
        };
    }

    private static EmployeeDetailDto MapDetail(Employee employee)
    {
        return new EmployeeDetailDto
        {
            Id = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName,
            MiddleName = employee.MiddleName,
            LastName = employee.LastName,
            Suffix = employee.Suffix,
            FullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            Gender = employee.Gender,
            BirthDate = employee.BirthDate,
            CivilStatus = employee.CivilStatus,
            Nationality = employee.Nationality,
            MobileNumber = employee.MobileNumber,
            Email = employee.Email,
            Address = employee.Address,
            CityProvince = employee.CityProvince,
            PostalCode = employee.PostalCode,
            EmergencyContactName = employee.EmergencyContactName,
            EmergencyContactRelationship = employee.EmergencyContactRelationship,
            EmergencyContactPhone = employee.EmergencyContactPhone,
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            DepartmentIsActive = employee.Department?.IsActive ?? false,
            PositionId = employee.PositionId,
            PositionName = employee.Position?.Name ?? string.Empty,
            PositionIsActive = employee.Position?.IsActive ?? false,
            BranchId = employee.BranchId,
            BranchName = employee.Branch?.Name ?? string.Empty,
            BranchIsActive = employee.Branch?.IsActive ?? false,
            EmploymentTypeId = employee.EmploymentTypeId,
            EmploymentTypeName = employee.EmploymentType?.Name ?? string.Empty,
            EmploymentTypeIsActive = employee.EmploymentType?.IsActive ?? false,
            EmploymentStatusId = employee.EmploymentStatusId,
            EmploymentStatusName = employee.EmploymentStatus?.Name ?? string.Empty,
            EmploymentStatusIsActive = employee.EmploymentStatus?.IsActive ?? false,
            ManagerId = employee.ManagerId,
            ManagerName = employee.Manager is null
                ? string.Empty
                : BuildFullName(employee.Manager.FirstName, employee.Manager.MiddleName, employee.Manager.LastName, employee.Manager.Suffix),
            WorkSchedule = employee.WorkSchedule,
            DateHired = employee.DateHired,
            DateRegularized = employee.DateRegularized,
            DateSeparated = employee.DateSeparated,
            SssNumber = employee.SssNumber,
            PhilHealthNumber = employee.PhilHealthNumber,
            PagIbigNumber = employee.PagIbigNumber,
            TinNumber = employee.TinNumber,
            OtherGovernmentId = employee.OtherGovernmentId,
            UserId = employee.UserId ?? string.Empty,
            LinkedUserEmail = employee.User?.Email ?? string.Empty,
            LinkedUserDisplayName = employee.User?.DisplayName ?? string.Empty,
            IsActive = employee.IsActive,
            CreatedAtUtc = employee.CreatedAtUtc,
            UpdatedAtUtc = employee.UpdatedAtUtc
        };
    }

    private static string BuildFullName(string firstName, string middleName, string lastName, string suffix)
    {
        var parts = new[]
        {
            firstName.Trim(),
            middleName.Trim(),
            lastName.Trim(),
            suffix.Trim()
        };

        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
