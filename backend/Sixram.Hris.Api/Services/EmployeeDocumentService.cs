using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sixram.Api.Configuration;
using Sixram.Api.Constants;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Documents;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IEmployeeDocumentService
{
    Task<DocumentComplianceSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);

    Task<EmployeeDocumentListOptionsDto> GetListOptionsAsync(CancellationToken cancellationToken = default);

    Task<PagedResultDto<EmployeeDocumentListItemDto>> GetDocumentsAsync(EmployeeDocumentListQueryDto query, CancellationToken cancellationToken = default);

    Task<EmployeeDocumentProfileDto> GetEmployeeDocumentProfileAsync(Guid employeeId, CancellationToken cancellationToken = default);

    Task<EmployeeDocumentListItemDto> CreateDocumentAsync(Guid employeeId, SaveEmployeeDocumentRequestDto request, string? uploadedByUserId, CancellationToken cancellationToken = default);

    Task<EmployeeDocumentListItemDto> UpdateDocumentMetadataAsync(Guid employeeId, Guid documentId, UpdateEmployeeDocumentMetadataRequestDto request, CancellationToken cancellationToken = default);

    Task<EmployeeDocumentListItemDto> ReplaceDocumentFileAsync(Guid employeeId, Guid documentId, ReplaceEmployeeDocumentFileRequestDto request, string? uploadedByUserId, CancellationToken cancellationToken = default);

    Task<EmployeeDocumentListItemDto> SetArchiveStateAsync(Guid employeeId, Guid documentId, SetEmployeeDocumentArchiveStateRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken = default);

    Task<StreamedEmployeeDocumentFile> GetDocumentContentAsync(Guid documentId, CancellationToken cancellationToken = default);
}

public class EmployeeDocumentService : IEmployeeDocumentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEmployeeDocumentStorageService _storageService;
    private readonly IAuditLogService _auditLogService;
    private readonly EmployeeDocumentOptions _options;
    private readonly ILogger<EmployeeDocumentService> _logger;

    public EmployeeDocumentService(
        ApplicationDbContext dbContext,
        IEmployeeDocumentStorageService storageService,
        IAuditLogService auditLogService,
        IOptions<EmployeeDocumentOptions> options,
        ILogger<EmployeeDocumentService> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _auditLogService = auditLogService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<DocumentComplianceSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = GetToday();
        var expiringSoonCutoff = today.AddDays(_options.ExpiringSoonDays);
        var activeEmployeeIds = await _dbContext.Employees
            .AsNoTracking()
            .Where(record => record.IsActive)
            .Select(record => record.Id)
            .ToListAsync(cancellationToken);

        var requiredTypes = await _dbContext.DocumentTypes
            .AsNoTracking()
            .Where(record => record.IsActive && record.IsRequired)
            .Select(record => new MissingRequiredDocumentDto
            {
                DocumentTypeId = record.Id,
                Code = record.Code,
                Name = record.Name,
                RequiresExpiryDate = record.RequiresExpiryDate
            })
            .ToListAsync(cancellationToken);

        var employeeDocuments = await _dbContext.EmployeeDocuments
            .AsNoTracking()
            .Where(record => activeEmployeeIds.Contains(record.EmployeeId))
            .Select(record => new EmployeeDocumentComplianceProjection
            {
                EmployeeId = record.EmployeeId,
                DocumentTypeId = record.DocumentTypeId,
                IsArchived = record.IsArchived,
                ExpiryDate = record.ExpiryDate
            })
            .ToListAsync(cancellationToken);

        var activeDocuments = employeeDocuments.Where(record => !record.IsArchived).ToList();
        var documentsByEmployee = activeDocuments
            .GroupBy(record => record.EmployeeId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var missingRequiredDocuments = 0;
        var employeesWithIncompleteDocuments = 0;
        var employeesWithExpiringDocuments = 0;

        foreach (var employeeId in activeEmployeeIds)
        {
            var employeeRecords = documentsByEmployee.GetValueOrDefault(employeeId, []);
            var submittedTypeIds = employeeRecords
                .Select(record => record.DocumentTypeId)
                .ToHashSet();

            var missingForEmployee = requiredTypes.Count(requiredType => !submittedTypeIds.Contains(requiredType.DocumentTypeId));
            var hasExpired = employeeRecords.Any(record => IsExpired(record.ExpiryDate, today));
            var hasExpiringSoon = employeeRecords.Any(record => IsExpiringSoon(record.ExpiryDate, today, expiringSoonCutoff));

            missingRequiredDocuments += missingForEmployee;

            if (missingForEmployee > 0 || hasExpired)
            {
                employeesWithIncompleteDocuments++;
            }

            if (hasExpiringSoon)
            {
                employeesWithExpiringDocuments++;
            }
        }

        return new DocumentComplianceSummaryDto
        {
            TotalDocuments = await _dbContext.EmployeeDocuments.CountAsync(cancellationToken),
            ArchivedDocuments = await _dbContext.EmployeeDocuments.CountAsync(record => record.IsArchived, cancellationToken),
            ExpiredDocuments = activeDocuments.Count(record => IsExpired(record.ExpiryDate, today)),
            ExpiringSoonDocuments = activeDocuments.Count(record => IsExpiringSoon(record.ExpiryDate, today, expiringSoonCutoff)),
            MissingRequiredDocuments = missingRequiredDocuments,
            EmployeesWithIncompleteDocuments = employeesWithIncompleteDocuments,
            EmployeesWithExpiringDocuments = employeesWithExpiringDocuments,
            RequiredDocumentTypes = requiredTypes.Count
        };
    }

    public async Task<EmployeeDocumentListOptionsDto> GetListOptionsAsync(CancellationToken cancellationToken = default)
    {
        return new EmployeeDocumentListOptionsDto
        {
            DocumentTypes = await _dbContext.DocumentTypes
                .AsNoTracking()
                .Where(record => record.IsActive)
                .OrderBy(record => record.Name)
                .Select(record => new DocumentTypeOptionDto
                {
                    Id = record.Id,
                    Code = record.Code,
                    Name = record.Name,
                    RequiresExpiryDate = record.RequiresExpiryDate,
                    IsRequired = record.IsRequired,
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
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<PagedResultDto<EmployeeDocumentListItemDto>> GetDocumentsAsync(EmployeeDocumentListQueryDto query, CancellationToken cancellationToken = default)
    {
        var today = GetToday();
        var expiringSoonCutoff = today.AddDays(_options.ExpiringSoonDays);
        var source = _dbContext.EmployeeDocuments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(record =>
                record.Title.Contains(search) ||
                record.OriginalFileName.Contains(search) ||
                record.DocumentType!.Name.Contains(search) ||
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
            source = source.Where(record => record.Employee!.DepartmentId == query.DepartmentId.Value);
        }

        if (query.BranchId is not null)
        {
            source = source.Where(record => record.Employee!.BranchId == query.BranchId.Value);
        }

        if (query.DocumentTypeId is not null)
        {
            source = source.Where(record => record.DocumentTypeId == query.DocumentTypeId.Value);
        }

        source = ApplyStatusFilter(source, query.Status, today, expiringSoonCutoff);

        var projected = source.Select(record => new EmployeeDocumentProjection
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = record.Employee!.EmployeeCode,
            EmployeeFirstName = record.Employee.FirstName,
            EmployeeMiddleName = record.Employee.MiddleName,
            EmployeeLastName = record.Employee.LastName,
            EmployeeSuffix = record.Employee.Suffix,
            DepartmentName = record.Employee.Department != null ? record.Employee.Department.Name : string.Empty,
            BranchName = record.Employee.Branch != null ? record.Employee.Branch.Name : string.Empty,
            DocumentTypeId = record.DocumentTypeId,
            DocumentTypeCode = record.DocumentType!.Code,
            DocumentTypeName = record.DocumentType.Name,
            DocumentTypeIsActive = record.DocumentType.IsActive,
            DocumentTypeRequiresExpiryDate = record.DocumentType.RequiresExpiryDate,
            DocumentTypeIsRequired = record.DocumentType.IsRequired,
            Title = record.Title,
            OriginalFileName = record.OriginalFileName,
            FileSize = record.FileSize,
            MimeType = record.MimeType,
            IssueDate = record.IssueDate,
            ExpiryDate = record.ExpiryDate,
            Remarks = record.Remarks,
            UploadedByDisplayName = record.UploadedByUser != null
                ? (!string.IsNullOrWhiteSpace(record.UploadedByUser.DisplayName) ? record.UploadedByUser.DisplayName : record.UploadedByUser.Email ?? string.Empty)
                : string.Empty,
            UploadedByEmail = record.UploadedByUser != null ? record.UploadedByUser.Email ?? string.Empty : string.Empty,
            IsArchived = record.IsArchived,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        });

        projected = (query.SortBy.Trim().ToLowerInvariant(), query.Descending) switch
        {
            ("employee", true) => projected.OrderByDescending(record => record.EmployeeLastName).ThenByDescending(record => record.EmployeeFirstName).ThenByDescending(record => record.CreatedAtUtc),
            ("employee", false) => projected.OrderBy(record => record.EmployeeLastName).ThenBy(record => record.EmployeeFirstName).ThenByDescending(record => record.CreatedAtUtc),
            ("title", true) => projected.OrderByDescending(record => record.Title).ThenByDescending(record => record.CreatedAtUtc),
            ("title", false) => projected.OrderBy(record => record.Title).ThenByDescending(record => record.CreatedAtUtc),
            ("expiry", true) => projected.OrderByDescending(record => record.ExpiryDate.HasValue).ThenByDescending(record => record.ExpiryDate).ThenByDescending(record => record.CreatedAtUtc),
            ("expiry", false) => projected.OrderBy(record => record.ExpiryDate.HasValue ? 0 : 1).ThenBy(record => record.ExpiryDate).ThenByDescending(record => record.CreatedAtUtc),
            ("uploaded", false) => projected.OrderBy(record => record.CreatedAtUtc).ThenBy(record => record.Title),
            (_, true) => projected.OrderByDescending(record => record.CreatedAtUtc).ThenByDescending(record => record.Title),
            _ => projected.OrderByDescending(record => record.CreatedAtUtc).ThenByDescending(record => record.Title)
        };

        var totalCount = await projected.CountAsync(cancellationToken);
        var items = await projected
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<EmployeeDocumentListItemDto>
        {
            Items = items.Select(record => MapListItem(record, today, expiringSoonCutoff)).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<EmployeeDocumentProfileDto> GetEmployeeDocumentProfileAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var today = GetToday();
        var expiringSoonCutoff = today.AddDays(_options.ExpiringSoonDays);
        var employee = await _dbContext.Employees
            .AsNoTracking()
            .Where(record => record.Id == employeeId)
            .Select(record => new
            {
                record.Id,
                record.EmployeeCode,
                record.FirstName,
                record.MiddleName,
                record.LastName,
                record.Suffix
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Employee '{employeeId}' was not found.");

        var activeTypes = await _dbContext.DocumentTypes
            .AsNoTracking()
            .Where(record => record.IsActive)
            .OrderBy(record => record.Name)
            .Select(record => new DocumentTypeOptionDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                RequiresExpiryDate = record.RequiresExpiryDate,
                IsRequired = record.IsRequired,
                IsActive = record.IsActive
            })
            .ToListAsync(cancellationToken);

        var requiredTypes = activeTypes
            .Where(record => record.IsRequired)
            .Select(record => new MissingRequiredDocumentDto
            {
                DocumentTypeId = record.Id,
                Code = record.Code,
                Name = record.Name,
                RequiresExpiryDate = record.RequiresExpiryDate
            })
            .ToList();

        var documentRows = await _dbContext.EmployeeDocuments
            .AsNoTracking()
            .Where(record => record.EmployeeId == employeeId)
            .OrderByDescending(record => record.CreatedAtUtc)
            .Select(record => new EmployeeDocumentProjection
            {
                Id = record.Id,
                EmployeeId = record.EmployeeId,
                EmployeeCode = record.Employee!.EmployeeCode,
                EmployeeFirstName = record.Employee.FirstName,
                EmployeeMiddleName = record.Employee.MiddleName,
                EmployeeLastName = record.Employee.LastName,
                EmployeeSuffix = record.Employee.Suffix,
                DepartmentName = record.Employee.Department != null ? record.Employee.Department.Name : string.Empty,
                BranchName = record.Employee.Branch != null ? record.Employee.Branch.Name : string.Empty,
                DocumentTypeId = record.DocumentTypeId,
                DocumentTypeCode = record.DocumentType!.Code,
                DocumentTypeName = record.DocumentType.Name,
                DocumentTypeIsActive = record.DocumentType.IsActive,
                DocumentTypeRequiresExpiryDate = record.DocumentType.RequiresExpiryDate,
                DocumentTypeIsRequired = record.DocumentType.IsRequired,
                Title = record.Title,
                OriginalFileName = record.OriginalFileName,
                FileSize = record.FileSize,
                MimeType = record.MimeType,
                IssueDate = record.IssueDate,
                ExpiryDate = record.ExpiryDate,
                Remarks = record.Remarks,
                UploadedByDisplayName = record.UploadedByUser != null
                    ? (!string.IsNullOrWhiteSpace(record.UploadedByUser.DisplayName) ? record.UploadedByUser.DisplayName : record.UploadedByUser.Email ?? string.Empty)
                    : string.Empty,
                UploadedByEmail = record.UploadedByUser != null ? record.UploadedByUser.Email ?? string.Empty : string.Empty,
                IsArchived = record.IsArchived,
                CreatedAtUtc = record.CreatedAtUtc,
                UpdatedAtUtc = record.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var documents = documentRows
            .Select(record => MapListItem(record, today, expiringSoonCutoff))
            .ToList();

        var activeDocumentTypeIds = documents
            .Where(record => !record.IsArchived)
            .Select(record => record.DocumentTypeId)
            .ToHashSet();

        var missingRequiredDocuments = requiredTypes
            .Where(record => !activeDocumentTypeIds.Contains(record.DocumentTypeId))
            .ToList();

        var activeDocuments = documents.Where(record => !record.IsArchived).ToList();
        var submittedRequiredDocumentTypes = requiredTypes.Count(record => activeDocumentTypeIds.Contains(record.DocumentTypeId));

        return new EmployeeDocumentProfileDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.EmployeeCode,
            EmployeeFullName = BuildFullName(employee.FirstName, employee.MiddleName, employee.LastName, employee.Suffix),
            Summary = new EmployeeDocumentComplianceSummaryDto
            {
                TotalDocuments = documents.Count,
                ActiveDocuments = activeDocuments.Count,
                ArchivedDocuments = documents.Count(record => record.IsArchived),
                MissingRequiredDocuments = missingRequiredDocuments.Count,
                ExpiredDocuments = activeDocuments.Count(record => record.StatusCode == "expired"),
                ExpiringSoonDocuments = activeDocuments.Count(record => record.StatusCode == "expiring-soon"),
                RequiredDocumentTypes = requiredTypes.Count,
                SubmittedRequiredDocumentTypes = submittedRequiredDocumentTypes,
                HasIssues = missingRequiredDocuments.Count > 0 || activeDocuments.Any(record => record.StatusCode == "expired")
            },
            AvailableDocumentTypes = activeTypes,
            MissingRequiredDocuments = missingRequiredDocuments,
            Documents = documents
        };
    }

    public async Task<EmployeeDocumentListItemDto> CreateDocumentAsync(Guid employeeId, SaveEmployeeDocumentRequestDto request, string? uploadedByUserId, CancellationToken cancellationToken = default)
    {
        await EnsureEmployeeExistsAsync(employeeId, cancellationToken);

        var documentType = await GetDocumentTypeForAssignmentAsync(request.DocumentTypeId, null, cancellationToken);
        EnsureExpiryRequirement(documentType, request.ExpiryDate);

        var title = request.Title.Trim();
        await EnsureDuplicateTitleAsync(employeeId, documentType.Id, title, null, cancellationToken);

        var document = new EmployeeDocument
        {
            EmployeeId = employeeId,
            DocumentTypeId = documentType.Id,
            Title = title,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            Remarks = request.Remarks.Trim(),
            UploadedByUserId = NormalizeUserId(uploadedByUserId),
            CreatedAtUtc = DateTime.UtcNow
        };

        var storedFile = await _storageService.SaveAsync(employeeId, document.Id, request.File!, cancellationToken);
        document.OriginalFileName = storedFile.OriginalFileName;
        document.FilePath = storedFile.StoragePath;
        document.FileSize = storedFile.FileSize;
        document.MimeType = storedFile.MimeType;

        _dbContext.EmployeeDocuments.Add(document);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storageService.DeleteAsync(storedFile.StoragePath, cancellationToken);
            throw;
        }

        _logger.LogInformation("Employee document {DocumentId} created for employee {EmployeeId}.", document.Id, employeeId);
        return await GetDocumentByIdAsync(document.Id, cancellationToken);
    }

    public async Task<EmployeeDocumentListItemDto> UpdateDocumentMetadataAsync(Guid employeeId, Guid documentId, UpdateEmployeeDocumentMetadataRequestDto request, CancellationToken cancellationToken = default)
    {
        var document = await GetEmployeeDocumentEntityAsync(employeeId, documentId, cancellationToken);
        var documentType = await GetDocumentTypeForAssignmentAsync(request.DocumentTypeId, document.DocumentTypeId, cancellationToken);
        EnsureExpiryRequirement(documentType, request.ExpiryDate);

        var title = request.Title.Trim();
        await EnsureDuplicateTitleAsync(employeeId, documentType.Id, title, documentId, cancellationToken);

        document.DocumentTypeId = documentType.Id;
        document.Title = title;
        document.IssueDate = request.IssueDate;
        document.ExpiryDate = request.ExpiryDate;
        document.Remarks = request.Remarks.Trim();
        document.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Employee document {DocumentId} metadata updated.", documentId);
        return await GetDocumentByIdAsync(documentId, cancellationToken);
    }

    public async Task<EmployeeDocumentListItemDto> ReplaceDocumentFileAsync(Guid employeeId, Guid documentId, ReplaceEmployeeDocumentFileRequestDto request, string? uploadedByUserId, CancellationToken cancellationToken = default)
    {
        var document = await GetEmployeeDocumentEntityAsync(employeeId, documentId, cancellationToken);
        var previousStoragePath = document.FilePath;
        var storedFile = await _storageService.SaveAsync(employeeId, documentId, request.File!, cancellationToken);

        document.OriginalFileName = storedFile.OriginalFileName;
        document.FilePath = storedFile.StoragePath;
        document.FileSize = storedFile.FileSize;
        document.MimeType = storedFile.MimeType;
        document.UploadedByUserId = NormalizeUserId(uploadedByUserId);
        document.UpdatedAtUtc = DateTime.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storageService.DeleteAsync(storedFile.StoragePath, cancellationToken);
            throw;
        }

        if (!string.Equals(previousStoragePath, storedFile.StoragePath, StringComparison.OrdinalIgnoreCase))
        {
            await _storageService.DeleteAsync(previousStoragePath, cancellationToken);
        }

        _logger.LogInformation("Employee document {DocumentId} file replaced.", documentId);
        return await GetDocumentByIdAsync(documentId, cancellationToken);
    }

    public async Task<EmployeeDocumentListItemDto> SetArchiveStateAsync(Guid employeeId, Guid documentId, SetEmployeeDocumentArchiveStateRequestDto request, CancellationToken cancellationToken = default)
    {
        var document = await GetEmployeeDocumentEntityAsync(employeeId, documentId, cancellationToken);
        document.IsArchived = request.IsArchived;
        document.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Employee document {DocumentId} archive state changed to {IsArchived}.", documentId, request.IsArchived);
        return await GetDocumentByIdAsync(documentId, cancellationToken);
    }

    public async Task DeleteDocumentAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await GetEmployeeDocumentEntityAsync(employeeId, documentId, cancellationToken);
        var storagePath = document.FilePath;

        _dbContext.EmployeeDocuments.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _storageService.DeleteAsync(storagePath, cancellationToken);

        _logger.LogInformation("Employee document {DocumentId} deleted.", documentId);
    }

    public async Task<StreamedEmployeeDocumentFile> GetDocumentContentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.EmployeeDocuments
            .AsNoTracking()
            .Where(record => record.Id == documentId)
            .Select(record => new
            {
                record.Id,
                record.EmployeeId,
                record.Title,
                record.OriginalFileName,
                record.FilePath,
                record.MimeType
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Employee document '{documentId}' was not found.");

        await _auditLogService.WriteAsync(
            new AuditLogEntry
            {
                Action = "download",
                EntityType = AuditEntityTypes.EmployeeDocument,
                EntityId = document.Id.ToString(),
                EmployeeId = document.EmployeeId,
                Remarks = $"Downloaded employee document '{document.Title}'."
            },
            cancellationToken);

        return await _storageService.OpenReadAsync(
            document.FilePath,
            document.OriginalFileName,
            document.MimeType,
            cancellationToken);
    }

    private IQueryable<EmployeeDocument> ApplyStatusFilter(
        IQueryable<EmployeeDocument> source,
        string status,
        DateOnly today,
        DateOnly expiringSoonCutoff)
    {
        return status.Trim().ToLowerInvariant() switch
        {
            "archived" => source.Where(record => record.IsArchived),
            "expired" => source.Where(record => !record.IsArchived && record.ExpiryDate != null && record.ExpiryDate < today),
            "expiring-soon" => source.Where(record => !record.IsArchived && record.ExpiryDate != null && record.ExpiryDate >= today && record.ExpiryDate <= expiringSoonCutoff),
            "valid" => source.Where(record => !record.IsArchived && record.ExpiryDate != null && record.ExpiryDate > expiringSoonCutoff),
            "no-expiry" => source.Where(record => !record.IsArchived && record.ExpiryDate == null),
            _ => source
        };
    }

    private async Task EnsureEmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Employees.AnyAsync(record => record.Id == employeeId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Employee '{employeeId}' was not found.");
        }
    }

    private async Task<EmployeeDocument> GetEmployeeDocumentEntityAsync(Guid employeeId, Guid documentId, CancellationToken cancellationToken)
    {
        return await _dbContext.EmployeeDocuments
            .SingleOrDefaultAsync(record => record.Id == documentId && record.EmployeeId == employeeId, cancellationToken)
            ?? throw new NotFoundException($"Employee document '{documentId}' was not found.");
    }

    private async Task<DocumentType> GetDocumentTypeForAssignmentAsync(Guid? documentTypeId, Guid? currentDocumentTypeId, CancellationToken cancellationToken)
    {
        if (documentTypeId is null)
        {
            throw BuildValidationException("A document type is required.", nameof(SaveEmployeeDocumentRequestDto.DocumentTypeId));
        }

        var documentType = await _dbContext.DocumentTypes
            .AsNoTracking()
            .SingleOrDefaultAsync(record => record.Id == documentTypeId.Value, cancellationToken)
            ?? throw BuildValidationException("The selected document type does not exist.", nameof(SaveEmployeeDocumentRequestDto.DocumentTypeId));

        if (!documentType.IsActive && documentType.Id != currentDocumentTypeId)
        {
            throw BuildValidationException("The selected document type is inactive.", nameof(SaveEmployeeDocumentRequestDto.DocumentTypeId));
        }

        return documentType;
    }

    private void EnsureExpiryRequirement(DocumentType documentType, DateOnly? expiryDate)
    {
        if (documentType.RequiresExpiryDate && expiryDate is null)
        {
            throw BuildValidationException("This document type requires an expiry date.", nameof(SaveEmployeeDocumentRequestDto.ExpiryDate));
        }
    }

    private async Task EnsureDuplicateTitleAsync(
        Guid employeeId,
        Guid documentTypeId,
        string title,
        Guid? existingDocumentId,
        CancellationToken cancellationToken)
    {
        var duplicateExists = await _dbContext.EmployeeDocuments.AnyAsync(
            record => record.EmployeeId == employeeId &&
                      record.DocumentTypeId == documentTypeId &&
                      record.Title == title &&
                      record.Id != existingDocumentId,
            cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException("A document with the same title already exists for this employee and document type.");
        }
    }

    private async Task<EmployeeDocumentListItemDto> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var today = GetToday();
        var expiringSoonCutoff = today.AddDays(_options.ExpiringSoonDays);
        var record = await _dbContext.EmployeeDocuments
            .AsNoTracking()
            .Where(item => item.Id == documentId)
            .Select(item => new EmployeeDocumentProjection
            {
                Id = item.Id,
                EmployeeId = item.EmployeeId,
                EmployeeCode = item.Employee!.EmployeeCode,
                EmployeeFirstName = item.Employee.FirstName,
                EmployeeMiddleName = item.Employee.MiddleName,
                EmployeeLastName = item.Employee.LastName,
                EmployeeSuffix = item.Employee.Suffix,
                DepartmentName = item.Employee.Department != null ? item.Employee.Department.Name : string.Empty,
                BranchName = item.Employee.Branch != null ? item.Employee.Branch.Name : string.Empty,
                DocumentTypeId = item.DocumentTypeId,
                DocumentTypeCode = item.DocumentType!.Code,
                DocumentTypeName = item.DocumentType.Name,
                DocumentTypeIsActive = item.DocumentType.IsActive,
                DocumentTypeRequiresExpiryDate = item.DocumentType.RequiresExpiryDate,
                DocumentTypeIsRequired = item.DocumentType.IsRequired,
                Title = item.Title,
                OriginalFileName = item.OriginalFileName,
                FileSize = item.FileSize,
                MimeType = item.MimeType,
                IssueDate = item.IssueDate,
                ExpiryDate = item.ExpiryDate,
                Remarks = item.Remarks,
                UploadedByDisplayName = item.UploadedByUser != null
                    ? (!string.IsNullOrWhiteSpace(item.UploadedByUser.DisplayName) ? item.UploadedByUser.DisplayName : item.UploadedByUser.Email ?? string.Empty)
                    : string.Empty,
                UploadedByEmail = item.UploadedByUser != null ? item.UploadedByUser.Email ?? string.Empty : string.Empty,
                IsArchived = item.IsArchived,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Employee document '{documentId}' was not found.");

        return MapListItem(record, today, expiringSoonCutoff);
    }

    private static EmployeeDocumentListItemDto MapListItem(EmployeeDocumentProjection record, DateOnly today, DateOnly expiringSoonCutoff)
    {
        var (statusCode, statusLabel) = GetStatus(record.IsArchived, record.ExpiryDate, today, expiringSoonCutoff);

        return new EmployeeDocumentListItemDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            EmployeeCode = record.EmployeeCode,
            EmployeeFullName = BuildFullName(record.EmployeeFirstName, record.EmployeeMiddleName, record.EmployeeLastName, record.EmployeeSuffix),
            DepartmentName = record.DepartmentName,
            BranchName = record.BranchName,
            DocumentTypeId = record.DocumentTypeId,
            DocumentTypeCode = record.DocumentTypeCode,
            DocumentTypeName = record.DocumentTypeName,
            DocumentTypeIsActive = record.DocumentTypeIsActive,
            DocumentTypeRequiresExpiryDate = record.DocumentTypeRequiresExpiryDate,
            DocumentTypeIsRequired = record.DocumentTypeIsRequired,
            Title = record.Title,
            OriginalFileName = record.OriginalFileName,
            FileSize = record.FileSize,
            MimeType = record.MimeType,
            IssueDate = record.IssueDate,
            ExpiryDate = record.ExpiryDate,
            Remarks = record.Remarks,
            UploadedByDisplayName = record.UploadedByDisplayName,
            UploadedByEmail = record.UploadedByEmail,
            IsArchived = record.IsArchived,
            StatusCode = statusCode,
            StatusLabel = statusLabel,
            CreatedAtUtc = record.CreatedAtUtc,
            UpdatedAtUtc = record.UpdatedAtUtc
        };
    }

    private static (string StatusCode, string StatusLabel) GetStatus(bool isArchived, DateOnly? expiryDate, DateOnly today, DateOnly expiringSoonCutoff)
    {
        if (isArchived)
        {
            return ("archived", "Archived");
        }

        if (expiryDate is null)
        {
            return ("no-expiry", "No expiry");
        }

        if (expiryDate < today)
        {
            return ("expired", "Expired");
        }

        if (expiryDate <= expiringSoonCutoff)
        {
            return ("expiring-soon", "Expiring soon");
        }

        return ("valid", "Valid");
    }

    private static bool IsExpired(DateOnly? expiryDate, DateOnly today)
    {
        return expiryDate is not null && expiryDate < today;
    }

    private static bool IsExpiringSoon(DateOnly? expiryDate, DateOnly today, DateOnly expiringSoonCutoff)
    {
        return expiryDate is not null && expiryDate >= today && expiryDate <= expiringSoonCutoff;
    }

    private static DateOnly GetToday()
    {
        return DateOnly.FromDateTime(DateTime.Today);
    }

    private static string? NormalizeUserId(string? uploadedByUserId)
    {
        return string.IsNullOrWhiteSpace(uploadedByUserId) ? null : uploadedByUserId.Trim();
    }

    private static string BuildFullName(string firstName, string middleName, string lastName, string suffix)
    {
        return string.Join(
            " ",
            new[]
            {
                firstName.Trim(),
                middleName.Trim(),
                lastName.Trim(),
                suffix.Trim()
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static BadRequestException BuildValidationException(string message, string fieldName)
    {
        return new BadRequestException(message, new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }

    private sealed class EmployeeDocumentProjection
    {
        public Guid Id { get; init; }

        public Guid EmployeeId { get; init; }

        public string EmployeeCode { get; init; } = string.Empty;

        public string EmployeeFirstName { get; init; } = string.Empty;

        public string EmployeeMiddleName { get; init; } = string.Empty;

        public string EmployeeLastName { get; init; } = string.Empty;

        public string EmployeeSuffix { get; init; } = string.Empty;

        public string DepartmentName { get; init; } = string.Empty;

        public string BranchName { get; init; } = string.Empty;

        public Guid DocumentTypeId { get; init; }

        public string DocumentTypeCode { get; init; } = string.Empty;

        public string DocumentTypeName { get; init; } = string.Empty;

        public bool DocumentTypeIsActive { get; init; }

        public bool DocumentTypeRequiresExpiryDate { get; init; }

        public bool DocumentTypeIsRequired { get; init; }

        public string Title { get; init; } = string.Empty;

        public string OriginalFileName { get; init; } = string.Empty;

        public long FileSize { get; init; }

        public string MimeType { get; init; } = string.Empty;

        public DateOnly? IssueDate { get; init; }

        public DateOnly? ExpiryDate { get; init; }

        public string Remarks { get; init; } = string.Empty;

        public string UploadedByDisplayName { get; init; } = string.Empty;

        public string UploadedByEmail { get; init; } = string.Empty;

        public bool IsArchived { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime? UpdatedAtUtc { get; init; }
    }

    private sealed class EmployeeDocumentComplianceProjection
    {
        public Guid EmployeeId { get; init; }

        public Guid DocumentTypeId { get; init; }

        public bool IsArchived { get; init; }

        public DateOnly? ExpiryDate { get; init; }
    }
}
