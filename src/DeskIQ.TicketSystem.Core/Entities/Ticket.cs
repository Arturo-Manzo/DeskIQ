namespace DeskIQ.TicketSystem.Core.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public string TicketId { get; set; } = string.Empty; // Human-readable ID like "RH-AA-0001"
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public Guid CreatedById { get; set; }
    public Guid? AssignedToId { get; set; }
    public Guid DepartmentId { get; set; }
    public TicketSource Source { get; set; }
    public string? ExternalId { get; set; } // For tracking external source IDs
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ParentTicketId { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockedReason { get; set; }

    // Navigation properties
    public User CreatedBy { get; set; } = null!;
    public User? AssignedTo { get; set; }
    public Department Department { get; set; } = null!;
    public Ticket? ParentTicket { get; set; }
    public ICollection<Ticket> Subtickets { get; set; } = new List<Ticket>();
    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
}

public enum TicketStatus
{
    Open = 1,
    InProgress = 2,
    PendingCustomer = 3,
    Resolved = 4,
    Closed = 5,
    Reopened = 6
}

public enum TicketPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum TicketSource
{
    Web = 1,
    Email = 2,
    WhatsApp = 3,
    API = 4
}
