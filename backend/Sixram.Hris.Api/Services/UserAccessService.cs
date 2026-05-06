using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public sealed class PortalActorContext
{
    public string UserId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public Guid? LinkedEmployeeId { get; init; }

    public string LinkedEmployeeCode { get; init; } = string.Empty;

    public IReadOnlyList<Guid> ManagedEmployeeIds { get; init; } = Array.Empty<Guid>();

    public bool IsAdministrator => Roles.Contains(SystemRoles.Administrator, StringComparer.OrdinalIgnoreCase);

    public bool IsHumanResources => Roles.Contains(SystemRoles.HumanResources, StringComparer.OrdinalIgnoreCase);

    public bool IsPayrollOfficer => Roles.Contains(SystemRoles.PayrollOfficer, StringComparer.OrdinalIgnoreCase);

    public bool HasLinkedEmployee => LinkedEmployeeId.HasValue;

    public bool IsManager => ManagedEmployeeIds.Count > 0;
}

public interface IUserAccessService
{
    Task<PortalActorContext> GetActorContextAsync(string? userId, CancellationToken cancellationToken = default);

    Task<PortalActorContext> GetLinkedEmployeeContextAsync(string? userId, CancellationToken cancellationToken = default);

    void EnsureCanAccessEmployee(PortalActorContext context, Guid employeeId, bool allowSelf = true, bool allowManagedEmployees = false);

    void EnsureCanManageEmployee(PortalActorContext context, Guid employeeId, bool allowSelf = false);

    void EnsureCanReviewProfileChanges(PortalActorContext context);

    void EnsureCanReviewPayrollAdjustments(PortalActorContext context);

    void EnsureCanAccessReports(PortalActorContext context);

    void EnsureCanAccessCompliance(PortalActorContext context);

    void EnsureCanAccessPayrollReports(PortalActorContext context);

    void EnsureCanAccessAuditLogs(PortalActorContext context);
}

public class UserAccessService : IUserAccessService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAccessService(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<PortalActorContext> GetActorContextAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedApiException("The current access token is invalid.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !user.IsEnabled)
        {
            throw new ForbiddenApiException("The user account is disabled or unavailable.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var linkedEmployee = await _dbContext.Employees
            .AsNoTracking()
            .Where(record => record.UserId == userId)
            .Select(record => new
            {
                record.Id,
                record.EmployeeCode
            })
            .SingleOrDefaultAsync(cancellationToken);

        IReadOnlyList<Guid> managedEmployeeIds = Array.Empty<Guid>();
        if (linkedEmployee is not null)
        {
            managedEmployeeIds = await _dbContext.Employees
                .AsNoTracking()
                .Where(record => record.ManagerId == linkedEmployee.Id && record.IsActive)
                .OrderBy(record => record.LastName)
                .ThenBy(record => record.FirstName)
                .Select(record => record.Id)
                .ToListAsync(cancellationToken);
        }

        return new PortalActorContext
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            DisplayName = !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.Email ?? string.Empty,
            Roles = roles.OrderBy(role => role).ToArray(),
            LinkedEmployeeId = linkedEmployee?.Id,
            LinkedEmployeeCode = linkedEmployee?.EmployeeCode ?? string.Empty,
            ManagedEmployeeIds = managedEmployeeIds
        };
    }

    public async Task<PortalActorContext> GetLinkedEmployeeContextAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var context = await GetActorContextAsync(userId, cancellationToken);
        if (!context.HasLinkedEmployee)
        {
            throw new ForbiddenApiException("This user account is not linked to an employee record yet.");
        }

        return context;
    }

    public void EnsureCanAccessEmployee(PortalActorContext context, Guid employeeId, bool allowSelf = true, bool allowManagedEmployees = false)
    {
        if (context.IsAdministrator || context.IsHumanResources)
        {
            return;
        }

        if (allowSelf && context.LinkedEmployeeId == employeeId)
        {
            return;
        }

        if (allowManagedEmployees && context.ManagedEmployeeIds.Contains(employeeId))
        {
            return;
        }

        throw new ForbiddenApiException("You do not have permission to access this employee record.");
    }

    public void EnsureCanManageEmployee(PortalActorContext context, Guid employeeId, bool allowSelf = false)
    {
        if (context.IsAdministrator || context.IsHumanResources)
        {
            return;
        }

        if (allowSelf && context.LinkedEmployeeId == employeeId)
        {
            return;
        }

        if (context.ManagedEmployeeIds.Contains(employeeId))
        {
            return;
        }

        throw new ForbiddenApiException("You do not have permission to manage requests for this employee.");
    }

    public void EnsureCanReviewProfileChanges(PortalActorContext context)
    {
        if (context.IsAdministrator || context.IsHumanResources)
        {
            return;
        }

        throw new ForbiddenApiException("You do not have permission to review profile change requests.");
    }

    public void EnsureCanReviewPayrollAdjustments(PortalActorContext context)
    {
        if (context.IsAdministrator || context.IsPayrollOfficer)
        {
            return;
        }

        throw new ForbiddenApiException("You do not have permission to review payroll adjustments.");
    }

    public void EnsureCanAccessReports(PortalActorContext context)
    {
        if (context.IsAdministrator || context.IsHumanResources || context.IsPayrollOfficer || context.IsManager)
        {
            return;
        }

        throw new ForbiddenApiException("You do not have permission to access the reporting center.");
    }

    public void EnsureCanAccessCompliance(PortalActorContext context)
    {
        if (context.IsAdministrator || context.IsHumanResources || context.IsManager)
        {
            return;
        }

        throw new ForbiddenApiException("You do not have permission to access the compliance center.");
    }

    public void EnsureCanAccessPayrollReports(PortalActorContext context)
    {
        if (context.IsAdministrator || context.IsPayrollOfficer)
        {
            return;
        }

        throw new ForbiddenApiException("You do not have permission to access payroll reports.");
    }

    public void EnsureCanAccessAuditLogs(PortalActorContext context)
    {
        if (context.IsAdministrator || context.IsHumanResources || context.IsPayrollOfficer)
        {
            return;
        }

        throw new ForbiddenApiException("You do not have permission to access audit logs.");
    }
}
