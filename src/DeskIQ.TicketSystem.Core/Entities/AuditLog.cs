namespace DeskIQ.TicketSystem.Core.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ActionType { get; set; } = string.Empty; // e.g., "RoleChanged", "UserCreated", "DepartmentAssigned"
    public string EntityName { get; set; } = string.Empty; // e.g., "User", "Department", "Ticket"
    public Guid? EntityId { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string Description { get; set; } = string.Empty;
    public Guid PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? IpAddress { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public User PerformedBy { get; set; } = null!;
}
