namespace DeskIQ.TicketSystem.Core.Entities;

public class TicketMessage
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid? ParentMessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public bool IsInternal { get; set; }
    public MessageType Type { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Ticket Ticket { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public TicketMessage? ParentMessage { get; set; }
    public ICollection<TicketMessage> Replies { get; set; } = new List<TicketMessage>();
}

public enum MessageType
{
    Text = 1,
    Attachment = 2,
    System = 3
}
