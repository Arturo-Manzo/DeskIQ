namespace DeskIQ.TicketSystem.Core.Entities;

public class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AutoAssignRules { get; set; } = string.Empty; // JSON rules for auto-assignment
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<TicketSequence> TicketSequences { get; set; } = new List<TicketSequence>();
    public ICollection<EmailAccount> EmailAccounts { get; set; } = new List<EmailAccount>();
    public ICollection<WhatsAppConfig> WhatsAppConfigs { get; set; } = new List<WhatsAppConfig>();
}
