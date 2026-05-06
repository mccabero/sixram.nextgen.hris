using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Portal;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IProfileChangeRequestService
{
    Task<PagedResultDto<EmployeeProfileChangeRequestDto>> GetRequestsAsync(
        EmployeeProfileChangeRequestListQueryDto query,
        string? actorUserId,
        bool ownOnly,
        CancellationToken cancellationToken = default);

    Task<EmployeeProfileChangeRequestDto> GetByIdAsync(Guid requestId, string? actorUserId, bool ownOnly, CancellationToken cancellationToken = default);

    Task<EmployeeProfileChangeRequestDto> CreateOwnRequestAsync(SaveProfileChangeRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<EmployeeProfileChangeRequestDto> ApproveAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<EmployeeProfileChangeRequestDto> RejectAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);

    Task<EmployeeProfileChangeRequestDto> CancelAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default);
}

public class ProfileChangeRequestService : IProfileChangeRequestService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _dbContext;
    private readonly IUserAccessService _userAccessService;
    private readonly INotificationService _notificationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProfileChangeRequestService> _logger;

    public ProfileChangeRequestService(
        ApplicationDbContext dbContext,
        IUserAccessService userAccessService,
        INotificationService notificationService,
        UserManager<ApplicationUser> userManager,
        ILogger<ProfileChangeRequestService> logger)
    {
        _dbContext = dbContext;
        _userAccessService = userAccessService;
        _notificationService = notificationService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<PagedResultDto<EmployeeProfileChangeRequestDto>> GetRequestsAsync(
        EmployeeProfileChangeRequestListQueryDto query,
        string? actorUserId,
        bool ownOnly,
        CancellationToken cancellationToken = default)
    {
        var actor = ownOnly
            ? await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken)
            : await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);

        if (!ownOnly)
        {
            _userAccessService.EnsureCanReviewProfileChanges(actor);
        }

        var source = _dbContext.EmployeeProfileChangeRequests
            .AsNoTracking()
            .Include(record => record.Employee)
            .Include(record => record.RequestedByUser)
            .Include(record => record.ReviewedByUser)
            .AsQueryable();

        if (ownOnly)
        {
            source = source.Where(record => record.EmployeeId == actor.LinkedEmployeeId);
        }
        else if (query.EmployeeId is not null)
        {
            source = source.Where(record => record.EmployeeId == query.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Employee!.EmployeeCode.Contains(search) ||
                record.Employee.FirstName.Contains(search) ||
                record.Employee.MiddleName.Contains(search) ||
                record.Employee.LastName.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(record => record.Status == query.Status.Trim().ToLowerInvariant());
        }

        if (query.DateFrom is not null)
        {
            var fromUtc = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            source = source.Where(record => record.CreatedAtUtc >= fromUtc);
        }

        if (query.DateTo is not null)
        {
            var toExclusiveUtc = query.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            source = source.Where(record => record.CreatedAtUtc < toExclusiveUtc);
        }

        source = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("status", true) => source.OrderByDescending(record => record.Status).ThenByDescending(record => record.CreatedAtUtc),
            ("status", false) => source.OrderBy(record => record.Status).ThenByDescending(record => record.CreatedAtUtc),
            (_, false) => source.OrderBy(record => record.CreatedAtUtc),
            _ => source.OrderByDescending(record => record.CreatedAtUtc)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var records = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<EmployeeProfileChangeRequestDto>
        {
            Items = records.Select(Map).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<EmployeeProfileChangeRequestDto> GetByIdAsync(Guid requestId, string? actorUserId, bool ownOnly, CancellationToken cancellationToken = default)
    {
        var actor = ownOnly
            ? await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken)
            : await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);

        var record = await LoadRequestAsync(requestId, cancellationToken);
        if (ownOnly)
        {
            _userAccessService.EnsureCanAccessEmployee(actor, record.EmployeeId, allowSelf: true, allowManagedEmployees: false);
        }
        else
        {
            _userAccessService.EnsureCanReviewProfileChanges(actor);
        }

        return Map(record);
    }

    public async Task<EmployeeProfileChangeRequestDto> CreateOwnRequestAsync(SaveProfileChangeRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        var employee = await _dbContext.Employees
            .SingleOrDefaultAsync(record => record.Id == actor.LinkedEmployeeId, cancellationToken)
            ?? throw new NotFoundException("The linked employee record could not be found.");

        var changes = BuildFieldChanges(employee, request);
        if (changes.Count == 0)
        {
            throw BuildValidationException("No profile changes were detected to submit.", nameof(SaveProfileChangeRequestDto));
        }

        await EnsureNoPendingConflictAsync(employee.Id, changes.Select(change => change.FieldKey).ToHashSet(StringComparer.OrdinalIgnoreCase), cancellationToken);

        var record = new EmployeeProfileChangeRequest
        {
            EmployeeId = employee.Id,
            RequestedByUserId = actor.UserId,
            RequestType = ProfileChangeRequestTypes.PersonalProfileUpdate,
            FieldChangesJson = JsonSerializer.Serialize(changes, SerializerOptions),
            Status = RequestStatuses.Pending,
            Reason = request.Reason.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.EmployeeProfileChangeRequests.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var reviewers = await GetReviewerUserIdsAsync(cancellationToken);
        await _notificationService.CreateManyAsync(
            reviewers.Select(userId => new NotificationDraft(
                userId,
                "Profile change request submitted",
                $"{BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix)} submitted a profile update request.",
                NotificationTypes.ProfileChangeSubmitted,
                ApprovableTypes.ProfileChangeRequest,
                record.Id.ToString(),
                "/approvals")),
            cancellationToken);

        _logger.LogInformation("Profile change request {RequestId} created for employee {EmployeeId}.", record.Id, employee.Id);
        return await GetByIdAsync(record.Id, actorUserId, ownOnly: true, cancellationToken);
    }

    public async Task<EmployeeProfileChangeRequestDto> ApproveAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanReviewProfileChanges(actor);

        var record = await LoadRequestForUpdateAsync(requestId, cancellationToken);
        EnsurePending(record.Status);

        var employee = record.Employee ?? throw new NotFoundException("The employee linked to this request could not be found.");
        var changes = DeserializeChanges(record.FieldChangesJson);
        ApplyChanges(employee, changes);

        record.Status = RequestStatuses.Approved;
        record.ReviewedByUserId = actor.UserId;
        record.ReviewedAtUtc = DateTime.UtcNow;
        record.ReviewerRemarks = request.Remarks.Trim();
        record.AppliedAtUtc = DateTime.UtcNow;
        record.UpdatedAtUtc = DateTime.UtcNow;
        employee.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await NotifyRequesterAsync(
            record.RequestedByUserId,
            "Profile change request approved",
            "Your profile update request has been approved and applied to your employee record.",
            NotificationTypes.ProfileChangeApproved,
            record.Id,
            cancellationToken);

        return await GetByIdAsync(requestId, actorUserId, ownOnly: false, cancellationToken);
    }

    public async Task<EmployeeProfileChangeRequestDto> RejectAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetActorContextAsync(actorUserId, cancellationToken);
        _userAccessService.EnsureCanReviewProfileChanges(actor);

        var record = await LoadRequestForUpdateAsync(requestId, cancellationToken);
        EnsurePending(record.Status);

        record.Status = RequestStatuses.Rejected;
        record.ReviewedByUserId = actor.UserId;
        record.ReviewedAtUtc = DateTime.UtcNow;
        record.ReviewerRemarks = request.Remarks.Trim();
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await NotifyRequesterAsync(
            record.RequestedByUserId,
            "Profile change request rejected",
            string.IsNullOrWhiteSpace(request.Remarks)
                ? "Your profile update request was reviewed but not approved."
                : $"Your profile update request was rejected: {request.Remarks.Trim()}",
            NotificationTypes.ProfileChangeRejected,
            record.Id,
            cancellationToken);

        return await GetByIdAsync(requestId, actorUserId, ownOnly: false, cancellationToken);
    }

    public async Task<EmployeeProfileChangeRequestDto> CancelAsync(Guid requestId, ApprovalActionRequestDto request, string? actorUserId, CancellationToken cancellationToken = default)
    {
        var actor = await _userAccessService.GetLinkedEmployeeContextAsync(actorUserId, cancellationToken);
        var record = await LoadRequestForUpdateAsync(requestId, cancellationToken);
        _userAccessService.EnsureCanAccessEmployee(actor, record.EmployeeId, allowSelf: true, allowManagedEmployees: false);
        EnsurePending(record.Status);

        record.Status = RequestStatuses.Cancelled;
        record.ReviewerRemarks = request.Remarks.Trim();
        record.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await NotifyReviewerGroupAsync(
            "Profile change request cancelled",
            $"{record.Employee?.EmployeeCode ?? "Employee"} cancelled a pending profile change request.",
            NotificationTypes.ProfileChangeCancelled,
            record.Id,
            cancellationToken);

        return await GetByIdAsync(requestId, actorUserId, ownOnly: true, cancellationToken);
    }

    private async Task<EmployeeProfileChangeRequest> LoadRequestAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return await _dbContext.EmployeeProfileChangeRequests
            .AsNoTracking()
            .Include(record => record.Employee)
            .Include(record => record.RequestedByUser)
            .Include(record => record.ReviewedByUser)
            .SingleOrDefaultAsync(record => record.Id == requestId, cancellationToken)
            ?? throw new NotFoundException($"Profile change request '{requestId}' was not found.");
    }

    private async Task<EmployeeProfileChangeRequest> LoadRequestForUpdateAsync(Guid requestId, CancellationToken cancellationToken)
    {
        return await _dbContext.EmployeeProfileChangeRequests
            .Include(record => record.Employee)
            .Include(record => record.RequestedByUser)
            .Include(record => record.ReviewedByUser)
            .SingleOrDefaultAsync(record => record.Id == requestId, cancellationToken)
            ?? throw new NotFoundException($"Profile change request '{requestId}' was not found.");
    }

    private static List<ProfileFieldChangeDto> BuildFieldChanges(Employee employee, SaveProfileChangeRequestDto request)
    {
        var changes = new List<ProfileFieldChangeDto>();
        AddChange(changes, "mobileNumber", "Mobile number", employee.MobileNumber, request.MobileNumber);
        AddChange(changes, "email", "Email", employee.Email, request.Email);
        AddChange(changes, "address", "Address", employee.Address, request.Address);
        AddChange(changes, "cityProvince", "City / Province", employee.CityProvince, request.CityProvince);
        AddChange(changes, "postalCode", "ZIP / Postal code", employee.PostalCode, request.PostalCode);
        AddChange(changes, "civilStatus", "Civil status", employee.CivilStatus, request.CivilStatus);
        AddChange(changes, "nationality", "Nationality", employee.Nationality, request.Nationality);
        AddChange(changes, "emergencyContactName", "Emergency contact name", employee.EmergencyContactName, request.EmergencyContactName);
        AddChange(changes, "emergencyContactRelationship", "Emergency contact relationship", employee.EmergencyContactRelationship, request.EmergencyContactRelationship);
        AddChange(changes, "emergencyContactPhone", "Emergency contact phone", employee.EmergencyContactPhone, request.EmergencyContactPhone);
        return changes;
    }

    private static void AddChange(ICollection<ProfileFieldChangeDto> changes, string fieldKey, string label, string currentValue, string newValue)
    {
        var normalizedCurrent = (currentValue ?? string.Empty).Trim();
        var normalizedNew = (newValue ?? string.Empty).Trim();
        if (string.Equals(normalizedCurrent, normalizedNew, StringComparison.Ordinal))
        {
            return;
        }

        changes.Add(new ProfileFieldChangeDto
        {
            FieldKey = fieldKey,
            Label = label,
            OldValue = normalizedCurrent,
            NewValue = normalizedNew
        });
    }

    private async Task EnsureNoPendingConflictAsync(Guid employeeId, ISet<string> fieldKeys, CancellationToken cancellationToken)
    {
        var pendingRequests = await _dbContext.EmployeeProfileChangeRequests
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId && record.Status == RequestStatuses.Pending)
            .Select(record => record.FieldChangesJson)
            .ToListAsync(cancellationToken);

        foreach (var pendingRequest in pendingRequests)
        {
            var pendingChanges = DeserializeChanges(pendingRequest);
            if (pendingChanges.Any(change => fieldKeys.Contains(change.FieldKey)))
            {
                throw new ConflictException("There is already a pending profile change request covering one or more of these fields.");
            }
        }
    }

    private static void ApplyChanges(Employee employee, IReadOnlyList<ProfileFieldChangeDto> changes)
    {
        foreach (var change in changes)
        {
            switch (change.FieldKey)
            {
                case "mobileNumber":
                    employee.MobileNumber = change.NewValue;
                    break;
                case "email":
                    employee.Email = change.NewValue;
                    break;
                case "address":
                    employee.Address = change.NewValue;
                    break;
                case "cityProvince":
                    employee.CityProvince = change.NewValue;
                    break;
                case "postalCode":
                    employee.PostalCode = change.NewValue;
                    break;
                case "civilStatus":
                    employee.CivilStatus = change.NewValue;
                    break;
                case "nationality":
                    employee.Nationality = change.NewValue;
                    break;
                case "emergencyContactName":
                    employee.EmergencyContactName = change.NewValue;
                    break;
                case "emergencyContactRelationship":
                    employee.EmergencyContactRelationship = change.NewValue;
                    break;
                case "emergencyContactPhone":
                    employee.EmergencyContactPhone = change.NewValue;
                    break;
            }
        }
    }

    private async Task NotifyRequesterAsync(string requesterUserId, string title, string message, string type, Guid requestId, CancellationToken cancellationToken)
    {
        await _notificationService.CreateAsync(
            new NotificationDraft(
                requesterUserId,
                title,
                message,
                type,
                ApprovableTypes.ProfileChangeRequest,
                requestId.ToString(),
                "/me/requests"),
            cancellationToken);
    }

    private async Task NotifyReviewerGroupAsync(string title, string message, string type, Guid requestId, CancellationToken cancellationToken)
    {
        var reviewers = await GetReviewerUserIdsAsync(cancellationToken);
        await _notificationService.CreateManyAsync(
            reviewers.Select(userId => new NotificationDraft(
                userId,
                title,
                message,
                type,
                ApprovableTypes.ProfileChangeRequest,
                requestId.ToString(),
                "/approvals")),
            cancellationToken);
    }

    private async Task<IReadOnlyList<string>> GetReviewerUserIdsAsync(CancellationToken cancellationToken)
    {
        var roleNames = new[] { SystemRoles.Administrator, SystemRoles.HumanResources };
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .Where(role => roleNames.Contains(role.Name!))
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);

        if (roles.Count == 0)
        {
            return Array.Empty<string>();
        }

        return await _dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => roles.Contains(userRole.RoleId))
            .Select(userRole => userRole.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private static void EnsurePending(string status)
    {
        if (!string.Equals(status, RequestStatuses.Pending, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Only pending requests can be reviewed or cancelled.");
        }
    }

    private static List<ProfileFieldChangeDto> DeserializeChanges(string json)
    {
        return JsonSerializer.Deserialize<List<ProfileFieldChangeDto>>(json, SerializerOptions) ?? [];
    }

    private static EmployeeProfileChangeRequestDto Map(EmployeeProfileChangeRequest record)
    {
        var employee = record.Employee;
        return new EmployeeProfileChangeRequestDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = employee?.EmployeeCode ?? string.Empty,
            EmployeeFullName = employee is null ? string.Empty : BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            RequestType = record.RequestType,
            FieldChanges = DeserializeChanges(record.FieldChangesJson),
            Reason = record.Reason,
            Status = record.Status,
            RequestedByDisplayName = ResolveUserDisplayName(record.RequestedByUser),
            ReviewedByDisplayName = ResolveUserDisplayName(record.ReviewedByUser),
            ReviewerRemarks = record.ReviewerRemarks,
            ReviewedAtUtc = record.ReviewedAtUtc,
            AppliedAtUtc = record.AppliedAtUtc,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static string ResolveUserDisplayName(ApplicationUser? user)
    {
        return user is null
            ? string.Empty
            : !string.IsNullOrWhiteSpace(user.DisplayName)
                ? user.DisplayName
                : user.Email ?? string.Empty;
    }

    private static string BuildFullName(string firstName, string middleName, string lastName, string suffix)
    {
        var parts = new[] { firstName, middleName, lastName, suffix }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part.Trim());

        return string.Join(" ", parts);
    }

    private static BadRequestException BuildValidationException(string message, string fieldName)
    {
        return new BadRequestException(message, new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }
}
