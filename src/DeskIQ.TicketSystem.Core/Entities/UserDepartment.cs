namespace DeskIQ.TicketSystem.Core.Entities;

public class UserDepartment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public DateTime AssignedAt { get; set; }
    public Guid? AssignedByUserId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Department Department { get; set; } = null!;
    public User? AssignedBy { get; set; }
}
