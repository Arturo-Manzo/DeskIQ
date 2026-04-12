namespace DeskIQ.TicketSystem.Core.Entities;

public class TicketActivity
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public ActivityType Type { get; set; }
    public string? Description { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid PerformedById { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
    public User PerformedBy { get; set; } = null!;
}

public enum ActivityType
{
    TicketCreated = 1,
    StatusChanged = 2,
    PriorityChanged = 3,
    Assigned = 4,
    Reassigned = 5,
    SubticketCreated = 6,
    TicketBlocked = 7,
    TicketUnblocked = 8,
    TicketResolved = 9,
    TicketClosed = 10,
    TicketReopened = 11,
    CommentAdded = 12
}
