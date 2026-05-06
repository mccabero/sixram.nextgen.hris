using Microsoft.EntityFrameworkCore;
using Sixram.Api.Data;
using Sixram.Api.DTOs.Common;
using Sixram.Api.DTOs.Documents;
using Sixram.Api.Entities;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IDocumentTypeService
{
    Task<PagedResultDto<DocumentTypeRecordDto>> GetDocumentTypesAsync(DocumentTypeListQueryDto query, CancellationToken cancellationToken = default);

    Task<DocumentTypeRecordDto> GetDocumentTypeByIdAsync(Guid documentTypeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentTypeOptionDto>> GetActiveOptionsAsync(CancellationToken cancellationToken = default);

    Task<DocumentTypeRecordDto> CreateDocumentTypeAsync(SaveDocumentTypeRequestDto request, CancellationToken cancellationToken = default);

    Task<DocumentTypeRecordDto> UpdateDocumentTypeAsync(Guid documentTypeId, SaveDocumentTypeRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default);
}

public class DocumentTypeService : IDocumentTypeService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DocumentTypeService> _logger;

    public DocumentTypeService(ApplicationDbContext dbContext, ILogger<DocumentTypeService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedResultDto<DocumentTypeRecordDto>> GetDocumentTypesAsync(DocumentTypeListQueryDto query, CancellationToken cancellationToken = default)
    {
        var source = _dbContext.DocumentTypes
            .AsNoTracking()
            .Select(record => new DocumentTypeRecordDto
            {
                Id = record.Id,
                Code = record.Code,
                Name = record.Name,
                Description = record.Description,
                RequiresExpiryDate = record.RequiresExpiryDate,
                IsRequired = record.IsRequired,
                IsActive = record.IsActive,
                DocumentCount = record.EmployeeDocuments.Count,
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
            ("required", true) => source.OrderByDescending(record => record.IsRequired).ThenByDescending(record => record.Name),
            ("required", false) => source.OrderBy(record => record.IsRequired).ThenBy(record => record.Name),
            ("created", true) => source.OrderByDescending(record => record.CreatedAtUtc).ThenByDescending(record => record.Name),
            ("created", false) => source.OrderBy(record => record.CreatedAtUtc).ThenBy(record => record.Name),
            (_, true) => source.OrderByDescending(record => record.Name).ThenByDescending(record => record.Code),
            _ => source.OrderBy(record => record.Name).ThenBy(record => record.Code)
        };

        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<DocumentTypeRecordDto>
        {
            Items = items,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<DocumentTypeRecordDto> GetDocumentTypeByIdAsync(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.DocumentTypes
            .AsNoTracking()
            .Where(item => item.Id == documentTypeId)
            .Select(item => new DocumentTypeRecordDto
            {
                Id = item.Id,
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                RequiresExpiryDate = item.RequiresExpiryDate,
                IsRequired = item.IsRequired,
                IsActive = item.IsActive,
                DocumentCount = item.EmployeeDocuments.Count,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedAtUtc = item.UpdatedAtUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        return record ?? throw new NotFoundException($"Document type '{documentTypeId}' was not found.");
    }

    public async Task<IReadOnlyList<DocumentTypeOptionDto>> GetActiveOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.DocumentTypes
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
    }

    public async Task<DocumentTypeRecordDto> CreateDocumentTypeAsync(SaveDocumentTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        var record = new DocumentType
        {
            CreatedAtUtc = DateTime.UtcNow
        };

        Apply(record, request);
        await EnsureUniquenessAsync(record.Code, record.Name, null, cancellationToken);

        _dbContext.DocumentTypes.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document type {DocumentTypeCode} created.", record.Code);
        return await GetDocumentTypeByIdAsync(record.Id, cancellationToken);
    }

    public async Task<DocumentTypeRecordDto> UpdateDocumentTypeAsync(Guid documentTypeId, SaveDocumentTypeRequestDto request, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.DocumentTypes.SingleOrDefaultAsync(item => item.Id == documentTypeId, cancellationToken)
            ?? throw new NotFoundException($"Document type '{documentTypeId}' was not found.");

        Apply(record, request);
        record.UpdatedAtUtc = DateTime.UtcNow;
        await EnsureUniquenessAsync(record.Code, record.Name, documentTypeId, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document type {DocumentTypeId} updated.", documentTypeId);
        return await GetDocumentTypeByIdAsync(documentTypeId, cancellationToken);
    }

    public async Task DeleteDocumentTypeAsync(Guid documentTypeId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.DocumentTypes
            .Include(item => item.EmployeeDocuments)
            .SingleOrDefaultAsync(item => item.Id == documentTypeId, cancellationToken)
            ?? throw new NotFoundException($"Document type '{documentTypeId}' was not found.");

        if (record.EmployeeDocuments.Count > 0)
        {
            throw new BadRequestException("This document type is already referenced by one or more employee documents. Deactivate it instead of deleting it.");
        }

        _dbContext.DocumentTypes.Remove(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Document type {DocumentTypeId} deleted.", documentTypeId);
    }

    private async Task EnsureUniquenessAsync(string code, string name, Guid? existingId, CancellationToken cancellationToken)
    {
        if (await _dbContext.DocumentTypes.AnyAsync(record => record.Code == code && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A document type with code '{code}' already exists.");
        }

        if (await _dbContext.DocumentTypes.AnyAsync(record => record.Name == name && record.Id != existingId, cancellationToken))
        {
            throw new ConflictException($"A document type named '{name}' already exists.");
        }
    }

    private static void Apply(DocumentType record, SaveDocumentTypeRequestDto request)
    {
        record.Code = request.Code.Trim().ToUpperInvariant();
        record.Name = request.Name.Trim();
        record.Description = request.Description.Trim();
        record.RequiresExpiryDate = request.RequiresExpiryDate;
        record.IsRequired = request.IsRequired;
        record.IsActive = request.IsActive;
    }
}
