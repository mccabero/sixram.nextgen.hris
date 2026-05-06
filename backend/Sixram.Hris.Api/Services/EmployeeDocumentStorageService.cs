using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Sixram.Api.Configuration;
using Sixram.Api.Exceptions;

namespace Sixram.Api.Services;

public interface IEmployeeDocumentStorageService
{
    Task<StoredEmployeeDocumentFile> SaveAsync(Guid employeeId, Guid documentId, IFormFile file, CancellationToken cancellationToken = default);

    Task<StreamedEmployeeDocumentFile> OpenReadAsync(string storagePath, string downloadFileName, string mimeType, CancellationToken cancellationToken = default);

    Task DeleteAsync(string? storagePath, CancellationToken cancellationToken = default);
}

public sealed record StoredEmployeeDocumentFile(
    string OriginalFileName,
    string StoragePath,
    long FileSize,
    string MimeType);

public sealed record StreamedEmployeeDocumentFile(
    Stream Content,
    string ContentType,
    string DownloadFileName);

public class EmployeeDocumentStorageService : IEmployeeDocumentStorageService
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedMimeTypes = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = ["application/pdf"],
        [".doc"] = ["application/msword", "application/octet-stream"],
        [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/zip", "application/octet-stream"],
        [".jpg"] = ["image/jpeg", "image/pjpeg", "application/octet-stream"],
        [".jpeg"] = ["image/jpeg", "image/pjpeg", "application/octet-stream"],
        [".png"] = ["image/png", "application/octet-stream"]
    };

    private readonly EmployeeDocumentOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EmployeeDocumentStorageService> _logger;

    public EmployeeDocumentStorageService(
        IOptions<EmployeeDocumentOptions> options,
        IWebHostEnvironment environment,
        ILogger<EmployeeDocumentStorageService> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<StoredEmployeeDocumentFile> SaveAsync(Guid employeeId, Guid documentId, IFormFile file, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var originalFileName = SanitizeFileName(file.FileName);
        var mimeType = NormalizeMimeType(extension, file.ContentType);
        var storageDirectory = Path.Combine(DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"), employeeId.ToString("N"));
        var storageFileName = $"{documentId:N}-{Guid.NewGuid():N}{extension}";
        var storagePath = Path.Combine(storageDirectory, storageFileName).Replace('\\', '/');
        var absolutePath = ResolveAbsolutePath(storagePath);
        var absoluteDirectory = Path.GetDirectoryName(absolutePath)
            ?? throw new InvalidOperationException("The employee document directory could not be resolved.");

        Directory.CreateDirectory(absoluteDirectory);

        await using var output = new FileStream(
            absolutePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await file.CopyToAsync(output, cancellationToken);

        _logger.LogInformation("Stored employee document {DocumentId} at {StoragePath}.", documentId, storagePath);

        return new StoredEmployeeDocumentFile(
            OriginalFileName: originalFileName,
            StoragePath: storagePath,
            FileSize: file.Length,
            MimeType: mimeType);
    }

    public Task<StreamedEmployeeDocumentFile> OpenReadAsync(string storagePath, string downloadFileName, string mimeType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath = ResolveAbsolutePath(storagePath);
        if (!File.Exists(absolutePath))
        {
            throw new NotFoundException("The requested document file could not be found in storage.");
        }

        Stream stream = new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult(new StreamedEmployeeDocumentFile(
            Content: stream,
            ContentType: string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType,
            DownloadFileName: downloadFileName));
    }

    public Task DeleteAsync(string? storagePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return Task.CompletedTask;
        }

        var absolutePath = ResolveAbsolutePath(storagePath);
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
            _logger.LogInformation("Deleted employee document file {StoragePath}.", storagePath);
        }

        return Task.CompletedTask;
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length <= 0)
        {
            throw BuildValidationException("The uploaded file is empty.", nameof(file));
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw BuildValidationException(
                $"The uploaded file exceeds the {_options.MaxFileSizeMb} MB limit.",
                nameof(file));
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) ||
            !_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) ||
            !AllowedMimeTypes.ContainsKey(extension))
        {
            throw BuildValidationException("Unsupported file type. Allowed types are PDF, DOC, DOCX, JPG, JPEG, and PNG.", nameof(file));
        }
    }

    private string NormalizeMimeType(string extension, string? contentType)
    {
        var allowedMimeTypes = AllowedMimeTypes[extension];
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return allowedMimeTypes[0];
        }

        var normalized = contentType.Trim();
        if (!allowedMimeTypes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            throw BuildValidationException("The uploaded file type does not match the file extension.", "file");
        }

        return normalized.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase)
            ? allowedMimeTypes[0]
            : normalized;
    }

    private string SanitizeFileName(string fileName)
    {
        var trimmed = Path.GetFileName(fileName).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "document";
        }

        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitizedCharacters = trimmed
            .Select(character => invalidCharacters.Contains(character) ? '_' : character)
            .ToArray();

        var sanitized = new string(sanitizedCharacters);
        return sanitized.Length <= 200 ? sanitized : sanitized[..200];
    }

    private string ResolveAbsolutePath(string storagePath)
    {
        var rootPath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, _options.StorageRootPath));
        var combinedPath = Path.GetFullPath(Path.Combine(rootPath, storagePath.Replace('/', Path.DirectorySeparatorChar)));

        if (!combinedPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("The document storage path is invalid.");
        }

        return combinedPath;
    }

    private static BadRequestException BuildValidationException(string message, string fieldName)
    {
        return new BadRequestException(message, new Dictionary<string, string[]>
        {
            [fieldName] = [message]
        });
    }
}
