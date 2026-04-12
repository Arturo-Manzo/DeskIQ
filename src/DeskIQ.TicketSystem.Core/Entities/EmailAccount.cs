namespace DeskIQ.TicketSystem.Core.Entities;

public class EmailAccount
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // This should be encrypted
    public string ImapServer { get; set; } = string.Empty;
    public int ImapPort { get; set; } = 993;
    public bool UseSsl { get; set; } = true;
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public Guid DepartmentId { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastSyncAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public Department Department { get; set; } = null!;
}
