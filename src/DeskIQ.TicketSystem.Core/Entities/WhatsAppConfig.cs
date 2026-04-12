namespace DeskIQ.TicketSystem.Core.Entities;

public class WhatsAppConfig
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty; // This should be encrypted
    public string WebhookSecret { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Department Department { get; set; } = null!;
}
