namespace DeskIQ.TicketSystem.API.Configuration;

public class FileStorageSettings
{
    public const string SectionName = "FileStorage";

    public string BasePath { get; set; } = "./uploads";
    public string TicketsPath { get; set; } = "tickets";
    public long MaxFileSize { get; set; } = 10485760; // 10MB default
}
