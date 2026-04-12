namespace DeskIQ.TicketSystem.Core.Entities;

public class TicketAttachment
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid UploadedById { get; set; }
    public DateTime UploadedAt { get; set; }

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
    public User UploadedBy { get; set; } = null!;
}
