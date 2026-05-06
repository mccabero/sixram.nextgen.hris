namespace Sixram.Api.Configuration;

public sealed class EmployeeDocumentOptions
{
    public const string SectionName = "EmployeeDocuments";

    public string StorageRootPath { get; set; } = "App_Data/employee-documents";

    public int MaxFileSizeMb { get; set; } = 10;

    public int ExpiringSoonDays { get; set; } = 30;

    public string[] AllowedExtensions { get; set; } = [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"];

    public long MaxFileSizeBytes => MaxFileSizeMb * 1024L * 1024L;
}
