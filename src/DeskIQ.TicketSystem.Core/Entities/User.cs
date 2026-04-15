namespace DeskIQ.TicketSystem.Core.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // SSO fields for identity provider integration
    public string? ExtId { get; set; } // External ID from SSO provider
    public bool DepartmentPendingAssign { get; set; } // Flag for SSO users without department assignment

    // Navigation properties
    public Department Department { get; set; } = null!;
    public ICollection<UserDepartment> UserDepartments { get; set; } = new List<UserDepartment>();
    public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();
    public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
    public ICollection<TicketMessage> Messages { get; set; } = new List<TicketMessage>();
}

public enum UserRole
{
    Cliente = 1,
    ClienteSupervisor = 2,
    Operador = 3,
    OperadorSupervisor = 4,
    SupervisorGeneral = 5,
    Auditor = 6,
    Administrador = 7
}
