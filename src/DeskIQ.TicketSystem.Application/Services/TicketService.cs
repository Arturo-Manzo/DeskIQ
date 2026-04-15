using DeskIQ.TicketSystem.Core.Entities;
using DeskIQ.TicketSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskIQ.TicketSystem.Application.Services;

public class TicketService
{
    private readonly AppDbContext _context;
    private readonly PermissionService _permissionService;

    public TicketService(AppDbContext context, PermissionService permissionService)
    {
        _context = context;
        _permissionService = permissionService;
    }

    public async Task<Department?> GetDepartmentAsync(Guid departmentId)
    {
        return await _context.Departments.FindAsync(departmentId);
    }

    public async Task<Ticket?> GetTicketAsync(Guid ticketId)
    {
        return await _context.Tickets.FindAsync(ticketId);
    }

    public async Task<Ticket?> GetTicketWithDetailsAsync(Guid ticketId)
    {
        return await _context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Department)
            .Include(t => t.ParentTicket)
                .ThenInclude(p => p.AssignedTo)
            .Include(t => t.ParentTicket)
                .ThenInclude(p => p.Department)
            .Include(t => t.Subtickets.OrderBy(s => s.CreatedAt))
                .ThenInclude(s => s.AssignedTo)
            .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
                .ThenInclude(m => m.Sender)
            .Include(t => t.Attachments)
                .ThenInclude(a => a.UploadedBy)
            .FirstOrDefaultAsync(t => t.Id == ticketId);
    }

    public async Task<bool> ValidateTicketCreationAsync(Guid departmentId, User currentUser)
    {
        var department = await _context.Departments.FindAsync(departmentId);
        if (department == null)
            return false;

        return _permissionService.CanCreateTicket(currentUser, departmentId);
    }

    public async Task<bool> ValidateSubticketCreationAsync(Guid parentTicketId, bool isBlocked, string? blockedReason)
    {
        if (isBlocked && string.IsNullOrWhiteSpace(blockedReason))
            return false;

        var parentTicket = await _context.Tickets
            .Include(t => t.Department)
            .FirstOrDefaultAsync(t => t.Id == parentTicketId);

        return parentTicket != null;
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateTicketUpdateAsync(
        Ticket ticket,
        UpdateTicketValidationRequest request)
    {
        if (request.IsBlocked == true && string.IsNullOrWhiteSpace(request.BlockedReason) && string.IsNullOrWhiteSpace(ticket.BlockedReason))
        {
            return (false, "Blocked reason is required when ticket is blocked");
        }

        // Validate title/description editing for resolved/closed tickets
        var isEditingTitleOrDescription = !string.IsNullOrWhiteSpace(request.Title) || !string.IsNullOrWhiteSpace(request.Description);
        if (isEditingTitleOrDescription && (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed))
        {
            return (false, "No se puede editar título/descripción de un ticket resuelto o cerrado.");
        }

        var wantsToCloseParent = ticket.ParentTicketId == null &&
            (request.Status == TicketStatus.Resolved || request.Status == TicketStatus.Closed);

        if (wantsToCloseParent)
        {
            var openSubtickets = await _context.Tickets
                .Where(t => t.ParentTicketId == ticket.Id &&
                            t.Status != TicketStatus.Resolved &&
                            t.Status != TicketStatus.Closed)
                .ToListAsync();

            if (openSubtickets.Count > 0 && !request.CloseOpenSubticketsWithParent)
            {
                return (false, "No se puede cerrar/resolver el ticket padre mientras existan subtickets abiertos.");
            }
        }

        var assignmentChanged = request.AssignedToId.HasValue && request.AssignedToId.Value != ticket.AssignedToId;
        if (assignmentChanged && ticket.ParentTicketId == null &&
            (ticket.Status == TicketStatus.Resolved || ticket.Status == TicketStatus.Closed))
        {
            return (false, "No se puede reasignar un ticket principal resuelto o cerrado.");
        }

        return (true, null);
    }

    public async Task<bool> ValidateAttachmentUploadAsync(long fileSize, string fileName, string allowedExtensionsConfig, long maxFileSize)
    {
        if (fileSize == 0)
            return false;

        if (fileSize > maxFileSize)
            return false;

        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensionsList = allowedExtensionsConfig.Split(',').Select(e => e.Trim().ToLowerInvariant()).ToArray();
        if (!allowedExtensionsList.Contains(fileExtension))
            return false;

        return true;
    }

    public async Task<bool> ValidateMessageAdditionAsync(Guid? parentMessageId, Guid ticketId)
    {
        if (parentMessageId.HasValue)
        {
            var parentMessage = await _context.TicketMessages
                .FirstOrDefaultAsync(m => m.Id == parentMessageId.Value);

            if (parentMessage == null)
                return false;

            if (parentMessage.TicketId != ticketId)
                return false;
        }

        return true;
    }

    public bool CanViewTicket(User user, Ticket ticket)
    {
        return _permissionService.CanViewTicket(user, ticket);
    }

    public bool CanCommentTicket(User user, Ticket ticket)
    {
        return _permissionService.CanCommentTicket(user, ticket);
    }

    public bool CanUpdateTicket(User user, Ticket ticket, UpdateTicketValidationRequest request)
    {
        return _permissionService.CanEditTicket(user, ticket);
    }

    public bool CanAssignTicket(User user, Ticket ticket, Guid? newAssigneeId)
    {
        return _permissionService.CanAssignTicket(user, ticket, newAssigneeId);
    }

    public async Task<List<Ticket>> GetOpenSubticketsAsync(Guid parentTicketId)
    {
        return await _context.Tickets
            .Where(t => t.ParentTicketId == parentTicketId &&
                        t.Status != TicketStatus.Resolved &&
                        t.Status != TicketStatus.Closed)
            .ToListAsync();
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateDepartmentChangeAsync(
        Ticket ticket,
        Guid newDepartmentId)
    {
        var newDepartment = await _context.Departments.FindAsync(newDepartmentId);
        if (newDepartment == null)
            return (false, "El departamento seleccionado no existe.");

        if (ticket.DepartmentId == newDepartmentId)
            return (false, "El ticket ya pertenece al departamento seleccionado.");

        // Check if ticket has assigned user
        if (ticket.AssignedToId.HasValue)
        {
            var assignedUser = await _context.Users
                .Include(u => u.UserDepartments)
                .FirstOrDefaultAsync(u => u.Id == ticket.AssignedToId.Value);

            if (assignedUser != null)
            {
                // Check if user belongs to new department (primary or multi-department)
                bool belongsToNewDepartment = assignedUser.DepartmentId == newDepartmentId ||
                    assignedUser.UserDepartments.Any(ud => ud.DepartmentId == newDepartmentId);

                if (!belongsToNewDepartment)
                {
                    return (false, "El usuario asignado al ticket no pertenece al departamento destino. Para cambiar el departamento, primero desasigna el ticket o asigna a un usuario que pertenezca al departamento seleccionado.");
                }
            }
        }

        return (true, null);
    }
}

public class UpdateTicketValidationRequest
{
    public TicketStatus? Status { get; set; }
    public Guid? AssignedToId { get; set; }
    public bool? IsBlocked { get; set; }
    public string? BlockedReason { get; set; }
    public bool CloseOpenSubticketsWithParent { get; set; }
}
