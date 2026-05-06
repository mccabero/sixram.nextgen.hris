namespace Sixram.Api.Configuration;

public sealed class LeaveOptions
{
    public const string SectionName = "Leave";

    public string StorageRootPath { get; set; } = "App_Data/leave-attachments";

    public int MaxAttachmentSizeMb { get; set; } = 10;

    public int ExpiringSoonDays { get; set; } = 30;

    public int UpcomingWindowDays { get; set; } = 14;

    public decimal LowBalanceThreshold { get; set; } = 1m;

    public string[] AllowedExtensions { get; set; } = [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"];

    public long MaxAttachmentSizeBytes => MaxAttachmentSizeMb * 1024L * 1024L;
}
