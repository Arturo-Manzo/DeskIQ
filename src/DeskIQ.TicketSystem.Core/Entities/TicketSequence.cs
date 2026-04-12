namespace DeskIQ.TicketSystem.Core.Entities;

public class TicketSequence
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public int Year { get; set; }
    public int LastValue { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Department Department { get; set; } = null!;
}