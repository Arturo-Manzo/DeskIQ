using DeskIQ.TicketSystem.Core.Entities;
using DeskIQ.TicketSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskIQ.TicketSystem.Application.Services;

public class PermissionService
{
    private readonly AppDbContext _context;

    public PermissionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> GetUserAccessibleDepartmentIdsAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.UserDepartments)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return new List<Guid>();

        // Multi-department roles: SupervisorGeneral, Auditor, Administrador
        if (user.Role == UserRole.SupervisorGeneral || 
            user.Role == UserRole.Auditor || 
            user.Role == UserRole.Administrador)
        {
            // If user has explicit multi-department assignments, use those
            if (user.UserDepartments.Any())
            {
                return user.UserDepartments.Select(ud => ud.DepartmentId).ToList();
            }

            // Otherwise, return all departments
            return await _context.Departments
                .Where(d => d.IsActive)
                .Select(d => d.Id)
                .ToListAsync();
        }

        // Single-department roles: Cliente, ClienteSupervisor, Operador, OperadorSupervisor
        return new List<Guid> { user.DepartmentId };
    }

    public bool CanViewTicket(User user, Ticket ticket)
    {
        // Administrador can view all tickets
        if (user.Role == UserRole.Administrador)
            return true;

        // Cliente can only view their own tickets
        if (user.Role == UserRole.Cliente)
            return ticket.CreatedById == user.Id;

        // ClienteSupervisor can view all tickets in their department
        if (user.Role == UserRole.ClienteSupervisor)
            return user.DepartmentId == ticket.DepartmentId;

        // Operador and OperadorSupervisor can view tickets in their department
        if (user.Role == UserRole.Operador || user.Role == UserRole.OperadorSupervisor)
            return user.DepartmentId == ticket.DepartmentId;

        // SupervisorGeneral and Auditor can view tickets in their assigned departments
        if (user.Role == UserRole.SupervisorGeneral || user.Role == UserRole.Auditor)
        {
            // For now, check if user has access to the ticket's department
            // In a real implementation, this would check UserDepartments
            return true; // Placeholder - will be implemented with UserDepartment logic
        }

        return false;
    }

    public bool CanCreateTicket(User user, Guid departmentId)
    {
        // ClienteSupervisor cannot create tickets (read-only)
        if (user.Role == UserRole.ClienteSupervisor)
            return false;

        // Auditor cannot create tickets (read-only)
        if (user.Role == UserRole.Auditor)
            return false;

        // Operador, OperadorSupervisor, SupervisorGeneral, Administrador can create in their accessible departments
        if (user.Role == UserRole.Operador || 
            user.Role == UserRole.OperadorSupervisor || 
            user.Role == UserRole.SupervisorGeneral || 
            user.Role == UserRole.Administrador || 
            user.Role == UserRole.Cliente)
        {
            // Multi-department roles can create in any accessible department
            return true; // Placeholder - will be implemented with UserDepartment logic
        }

        return false;
    }

    public bool CanEditTicket(User user, Ticket ticket)
    {
        // Cliente cannot edit tickets
        if (user.Role == UserRole.Cliente)
            return false;

        // ClienteSupervisor cannot edit tickets (read-only)
        if (user.Role == UserRole.ClienteSupervisor)
            return false;

        // Auditor cannot edit tickets (read-only, only comments)
        if (user.Role == UserRole.Auditor)
            return false;

        // Operador can edit tickets in their department that are assigned to them
        if (user.Role == UserRole.Operador)
        {
            return user.DepartmentId == ticket.DepartmentId && 
                   (ticket.AssignedToId == user.Id || ticket.AssignedToId == null);
        }

        // OperadorSupervisor can edit any ticket in their department
        if (user.Role == UserRole.OperadorSupervisor)
            return user.DepartmentId == ticket.DepartmentId;

        // SupervisorGeneral can edit tickets in their assigned departments
        if (user.Role == UserRole.SupervisorGeneral)
            return true; // Placeholder - will be implemented with UserDepartment logic

        // Administrador can edit all tickets
        if (user.Role == UserRole.Administrador)
            return true;

        return false;
    }

    public bool CanAssignTicket(User user, Ticket ticket, Guid? newAssigneeId)
    {
        // Cliente cannot assign tickets
        if (user.Role == UserRole.Cliente)
            return false;

        // ClienteSupervisor cannot assign tickets (read-only)
        if (user.Role == UserRole.ClienteSupervisor)
            return false;

        // Auditor cannot assign tickets (read-only)
        if (user.Role == UserRole.Auditor)
            return false;

        // Operador can assign tickets within their department only
        if (user.Role == UserRole.Operador)
        {
            if (user.DepartmentId != ticket.DepartmentId)
                return false;

            // Can only assign to users in the same department
            if (newAssigneeId.HasValue)
            {
                var assignee = _context.Users.FirstOrDefault(u => u.Id == newAssigneeId.Value);
                return assignee != null && assignee.DepartmentId == user.DepartmentId;
            }

            return true;
        }

        // OperadorSupervisor can assign tickets within their department
        if (user.Role == UserRole.OperadorSupervisor)
        {
            if (user.DepartmentId != ticket.DepartmentId)
                return false;

            if (newAssigneeId.HasValue)
            {
                var assignee = _context.Users.FirstOrDefault(u => u.Id == newAssigneeId.Value);
                return assignee != null && assignee.DepartmentId == user.DepartmentId;
            }

            return true;
        }

        // SupervisorGeneral can assign tickets within their assigned departments
        if (user.Role == UserRole.SupervisorGeneral)
            return true; // Placeholder - will be implemented with UserDepartment logic

        // Administrador can assign any ticket
        if (user.Role == UserRole.Administrador)
            return true;

        return false;
    }

    public bool CanViewAssignees(User user, Guid departmentId)
    {
        // Cliente cannot view assignees
        if (user.Role == UserRole.Cliente)
            return false;

        // ClienteSupervisor cannot view assignees (read-only)
        if (user.Role == UserRole.ClienteSupervisor)
            return false;

        // Auditor cannot view assignees (read-only)
        if (user.Role == UserRole.Auditor)
            return false;

        // Operador can only view assignees in their own department
        if (user.Role == UserRole.Operador)
        {
            return user.DepartmentId != Guid.Empty && user.DepartmentId == departmentId;
        }

        // OperadorSupervisor can view assignees in their department
        if (user.Role == UserRole.OperadorSupervisor)
        {
            return user.DepartmentId != Guid.Empty && user.DepartmentId == departmentId;
        }

        // SupervisorGeneral, Administrador can view assignees in any department
        return true;
    }

    public bool CanCommentTicket(User user, Ticket ticket)
    {
        // All roles can comment on tickets they can view
        return CanViewTicket(user, ticket);
    }

    public bool CanViewMetrics(User user)
    {
        // Only supervisor roles and above can view metrics
        return user.Role == UserRole.OperadorSupervisor ||
               user.Role == UserRole.SupervisorGeneral ||
               user.Role == UserRole.Auditor ||
               user.Role == UserRole.Administrador;
    }

    public bool CanManageUsers(User user)
    {
        // Only Administrador can manage users
        return user.Role == UserRole.Administrador;
    }

    public bool CanManageDepartments(User user)
    {
        // Only Administrador can manage departments
        return user.Role == UserRole.Administrador;
    }

    public bool CanChangeUserRole(User user)
    {
        // Only Administrador can change user roles
        return user.Role == UserRole.Administrador;
    }

    public bool CanAssignUserDepartment(User user)
    {
        // Only Administrador can assign departments to users
        return user.Role == UserRole.Administrador;
    }

    public bool CanChangeTicketDepartment(User user, Ticket ticket)
    {
        // Cliente cannot change department
        if (user.Role == UserRole.Cliente)
            return false;

        // ClienteSupervisor cannot change department (read-only)
        if (user.Role == UserRole.ClienteSupervisor)
            return false;

        // Auditor cannot change department (read-only)
        if (user.Role == UserRole.Auditor)
            return false;

        // Operador cannot change department
        if (user.Role == UserRole.Operador)
            return false;

        // OperadorSupervisor can change within their department
        if (user.Role == UserRole.OperadorSupervisor)
            return user.DepartmentId == ticket.DepartmentId;

        // SupervisorGeneral can change within their assigned departments
        if (user.Role == UserRole.SupervisorGeneral)
            return true; // Will check UserDepartment logic in implementation

        // Administrador can change any ticket department
        if (user.Role == UserRole.Administrador)
            return true;

        return false;
    }

    public bool CanEditTicketTitleDescription(User user, Ticket ticket)
    {
        // Cannot edit if ticket is resolved or closed
        if (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed)
            return false;

        // Creator can always edit their own ticket
        if (user.Id == ticket.CreatedById)
            return true;

        // OperadorSupervisor can edit in their department
        if (user.Role == UserRole.OperadorSupervisor)
            return user.DepartmentId == ticket.DepartmentId;

        // ClienteSupervisor can edit in their department
        if (user.Role == UserRole.ClienteSupervisor)
            return user.DepartmentId == ticket.DepartmentId;

        // SupervisorGeneral can edit in their assigned departments
        if (user.Role == UserRole.SupervisorGeneral)
            return true; // Will check UserDepartment logic in implementation

        // Administrador can edit all tickets
        if (user.Role == UserRole.Administrador)
            return true;

        return false;
    }
}
