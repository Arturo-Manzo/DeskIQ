using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DeskIQ.TicketSystem.API.Services;
using DeskIQ.TicketSystem.Infrastructure.Data;
using DeskIQ.TicketSystem.Core.Entities;
using Microsoft.Extensions.Options;
using DeskIQ.TicketSystem.API.Configuration;
using DeskIQ.TicketSystem.API.Exceptions;
using DeskIQ.TicketSystem.API.Models;
using DeskIQ.TicketSystem.Application.Services;

namespace DeskIQ.TicketSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITicketIdGenerator _ticketIdGenerator;
    private readonly IFileStorageService _fileStorageService;
    private readonly FileStorageSettings _fileStorageSettings;
    private readonly IConfiguration _configuration;
    private readonly ITicketActivityService _activityService;
    private readonly TicketService _ticketService;

    public TicketsController(
        AppDbContext context,
        ITicketIdGenerator ticketIdGenerator,
        IFileStorageService fileStorageService,
        IOptions<FileStorageSettings> fileStorageSettings,
        IConfiguration configuration,
        ITicketActivityService activityService,
        TicketService ticketService)
    {
        _context = context;
        _ticketIdGenerator = ticketIdGenerator;
        _fileStorageService = fileStorageService;
        _fileStorageSettings = fileStorageSettings.Value;
        _configuration = configuration;
        _activityService = activityService;
        _ticketService = ticketService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Ticket>>> GetTickets(
        [FromQuery] Guid? departmentId = null,
        [FromQuery] TicketStatus? status = null,
        [FromQuery] Guid? assignedToId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var query = _context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Department)
            .AsQueryable();

        if (currentUser.Role != UserRole.Admin)
        {
            query = query.Where(t => t.DepartmentId == currentUser.DepartmentId);
        }

        if (departmentId.HasValue)
            query = query.Where(t => t.DepartmentId == departmentId.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (assignedToId.HasValue)
            query = query.Where(t => t.AssignedToId == assignedToId.Value);

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(tickets);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Ticket>> GetTicket(Guid id)
    {
        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var ticket = await _ticketService.GetTicketWithDetailsAsync(id);
        if (ticket == null)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Ticket not found");

        if (!TicketService.CanViewTicket(currentUser, ticket))
            throw new ForbiddenException(ErrorCodes.FORBIDDEN, "Access denied");

        return Ok(ticket);
    }

    [HttpPost]
    public async Task<ActionResult<Ticket>> CreateTicket([FromBody] CreateTicketRequest request)
    {
        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var isValid = await _ticketService.ValidateTicketCreationAsync(request.DepartmentId, currentUser);
        if (!isValid)
            throw new BadRequestException(ErrorCodes.INVALID_DEPARTMENT, "Department not found or access denied");

        var department = await _ticketService.GetDepartmentAsync(request.DepartmentId);
        if (department == null)
            throw new NotFoundException(ErrorCodes.DEPARTMENT_NOT_FOUND, "Department not found");

        var createdAt = DateTime.UtcNow;
        var ticketId = await _ticketIdGenerator.GenerateNextAsync(
            request.DepartmentId,
            department.Code,
            createdAt);

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Title = request.Title,
            Description = request.Description,
            Status = TicketStatus.Open,
            Priority = request.Priority,
            CreatedById = currentUser.Id,
            DepartmentId = request.DepartmentId,
            Source = request.Source,
            ExternalId = request.ExternalId,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        // Auto-assignment logic would go here
        // For now, we'll leave it unassigned

        _context.Tickets.Add(ticket);
        await _context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(ticket.Id, ActivityType.TicketCreated, currentUser.Id, "Ticket creado");

        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
    }

    [HttpGet("{id}/subtickets")]
    public async Task<ActionResult<IEnumerable<Ticket>>> GetSubtickets(Guid id)
    {
        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var parentTicket = await _ticketService.GetTicketAsync(id);
        if (parentTicket == null)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Ticket not found");

        if (!TicketService.CanViewTicket(currentUser, parentTicket))
            throw new ForbiddenException(ErrorCodes.FORBIDDEN, "Access denied");

        var subtickets = await _context.Tickets
            .Include(t => t.AssignedTo)
            .Where(t => t.ParentTicketId == id)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync();

        return Ok(subtickets);
    }

    [HttpPost("{id}/subtickets")]
    public async Task<ActionResult<Ticket>> CreateSubticket(Guid id, [FromBody] CreateSubticketRequest request)
    {
        if (request.IsBlocked && string.IsNullOrWhiteSpace(request.BlockedReason))
            throw new BadRequestException(ErrorCodes.BLOCKED_REASON_REQUIRED, "Blocked reason is required when subticket is blocked");

        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var isValid = await _ticketService.ValidateSubticketCreationAsync(id, request.IsBlocked, request.BlockedReason);
        if (!isValid)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Parent ticket not found");

        var parentTicket = await _context.Tickets
            .Include(t => t.Department)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (parentTicket == null)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Parent ticket not found");

        if (!TicketService.CanViewTicket(currentUser, parentTicket))
            throw new ForbiddenException(ErrorCodes.FORBIDDEN, "Access denied");

        var department = await _ticketService.GetDepartmentAsync(parentTicket.DepartmentId);
        if (department == null)
            throw new NotFoundException(ErrorCodes.DEPARTMENT_NOT_FOUND, "Department not found");

        var createdAt = DateTime.UtcNow;
        var ticketId = await _ticketIdGenerator.GenerateNextAsync(
            parentTicket.DepartmentId,
            department.Code,
            createdAt);

        var subticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Title = request.Title,
            Description = request.Description,
            Status = TicketStatus.Open,
            Priority = request.Priority,
            CreatedById = currentUser.Id,
            AssignedToId = request.AssignedToId,
            DepartmentId = parentTicket.DepartmentId,
            Source = parentTicket.Source,
            ParentTicketId = parentTicket.Id,
            IsBlocked = request.IsBlocked,
            BlockedReason = request.IsBlocked ? request.BlockedReason : null,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        _context.Tickets.Add(subticket);

        if (subticket.IsBlocked && !string.IsNullOrWhiteSpace(subticket.BlockedReason))
        {
            _context.TicketMessages.Add(new TicketMessage
            {
                Id = Guid.NewGuid(),
                TicketId = subticket.Id,
                Content = $"Subticket bloqueado: {subticket.BlockedReason}",
                SenderId = currentUser.Id,
                IsInternal = false,
                Type = MessageType.System,
                CreatedAt = createdAt
            });
        }

        await _context.SaveChangesAsync();

        // Log activity on parent ticket
        await _activityService.LogActivityAsync(parentTicket.Id, ActivityType.SubticketCreated, currentUser.Id, $"Subticket creado: {subticket.TicketId}");

        return CreatedAtAction(nameof(GetTicket), new { id = subticket.Id }, subticket);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTicket(Guid id, [FromBody] UpdateTicketRequest request)
    {
        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var ticket = await _ticketService.GetTicketAsync(id);
        if (ticket == null)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Ticket not found");

        var validationRequest = new UpdateTicketValidationRequest
        {
            Status = request.Status,
            AssignedToId = request.AssignedToId,
            IsBlocked = request.IsBlocked,
            BlockedReason = request.BlockedReason,
            CloseOpenSubticketsWithParent = request.CloseOpenSubticketsWithParent
        };

        if (!TicketService.CanUpdateTicket(currentUser, ticket, validationRequest))
            throw new ForbiddenException(ErrorCodes.FORBIDDEN, "Access denied");

        var (isValid, errorMessage) = await _ticketService.ValidateTicketUpdateAsync(ticket, validationRequest);
        if (!isValid)
            throw new BadRequestException(ErrorCodes.VALIDATION_ERROR, errorMessage ?? "Validation failed");

        var wantsToCloseParent = ticket.ParentTicketId == null &&
            (request.Status == TicketStatus.Resolved || request.Status == TicketStatus.Closed);

        if (wantsToCloseParent)
        {
            var openSubtickets = await _ticketService.GetOpenSubticketsAsync(ticket.Id);

            if (openSubtickets.Count > 0 && request.CloseOpenSubticketsWithParent)
            {
                var closeStatus = request.Status ?? TicketStatus.Closed;
                var subticketsUpdatedAt = DateTime.UtcNow;

                foreach (var subticket in openSubtickets)
                {
                    subticket.Status = closeStatus;
                    subticket.UpdatedAt = subticketsUpdatedAt;
                    subticket.ResolvedAt = subticket.ResolvedAt ?? subticketsUpdatedAt;
                }
            }
        }

        ticket.Title = request.Title ?? ticket.Title;
        ticket.Description = request.Description ?? ticket.Description;
        ticket.Status = request.Status ?? ticket.Status;
        ticket.Priority = request.Priority ?? ticket.Priority;
        ticket.AssignedToId = request.AssignedToId ?? ticket.AssignedToId;
        ticket.IsBlocked = request.IsBlocked ?? ticket.IsBlocked;
        ticket.BlockedReason = request.IsBlocked == true
            ? request.BlockedReason
            : request.IsBlocked == false
                ? null
                : request.BlockedReason ?? ticket.BlockedReason;
        ticket.UpdatedAt = DateTime.UtcNow;

        // Log status change
        if (request.Status.HasValue && request.Status.Value != ticket.Status)
        {
            await _activityService.LogActivityAsync(
                ticket.Id,
                ActivityType.StatusChanged,
                currentUser.Id,
                "Estado cambiado",
                ticket.Status.ToString(),
                request.Status.Value.ToString());
        }

        // Log priority change
        if (request.Priority.HasValue && request.Priority.Value != ticket.Priority)
        {
            await _activityService.LogActivityAsync(
                ticket.Id,
                ActivityType.PriorityChanged,
                currentUser.Id,
                "Prioridad cambiada",
                ticket.Priority.ToString(),
                request.Priority.Value.ToString());
        }

        // Log assignment change
        if (request.AssignedToId.HasValue && request.AssignedToId.Value != ticket.AssignedToId)
        {
            var activityType = ticket.AssignedToId == null ? ActivityType.Assigned : ActivityType.Reassigned;
            await _activityService.LogActivityAsync(
                ticket.Id,
                activityType,
                currentUser.Id,
                ticket.AssignedToId == null ? "Ticket asignado" : "Ticket reasignado",
                ticket.AssignedToId?.ToString(),
                request.AssignedToId.Value.ToString());
        }

        if (request.Status == TicketStatus.Resolved && ticket.ResolvedAt == null)
            ticket.ResolvedAt = DateTime.UtcNow;

        if (request.Status != TicketStatus.Resolved && request.Status != TicketStatus.Closed)
            ticket.ResolvedAt = null;

        await _context.SaveChangesAsync();

        // Log resolved/closed activities
        if (request.Status == TicketStatus.Resolved)
        {
            await _activityService.LogActivityAsync(ticket.Id, ActivityType.TicketResolved, currentUser.Id, "Ticket resuelto");
        }
        else if (request.Status == TicketStatus.Closed)
        {
            await _activityService.LogActivityAsync(ticket.Id, ActivityType.TicketClosed, currentUser.Id, "Ticket cerrado");
        }
        else if (request.Status == TicketStatus.Reopened)
        {
            await _activityService.LogActivityAsync(ticket.Id, ActivityType.TicketReopened, currentUser.Id, "Ticket reabierto");
        }

        return NoContent();
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> BlockTicket(Guid id, [FromBody] BlockTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new BadRequestException(ErrorCodes.VALIDATION_ERROR, "Reason is required");

        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var ticket = await _ticketService.GetTicketAsync(id);
        if (ticket == null)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Ticket not found");

        var validationRequest = new UpdateTicketValidationRequest();
        if (!TicketService.CanUpdateTicket(currentUser, ticket, validationRequest))
            throw new ForbiddenException(ErrorCodes.FORBIDDEN, "Access denied");

        ticket.IsBlocked = true;
        ticket.BlockedReason = request.Reason.Trim();
        ticket.UpdatedAt = DateTime.UtcNow;

        _context.TicketMessages.Add(new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Content = $"Ticket bloqueado: {ticket.BlockedReason}",
            SenderId = currentUser.Id,
            IsInternal = false,
            Type = MessageType.System,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(ticket.Id, ActivityType.TicketBlocked, currentUser.Id, $"Ticket bloqueado: {ticket.BlockedReason}");

        return NoContent();
    }

    [HttpPost("{id}/unblock")]
    public async Task<IActionResult> UnblockTicket(Guid id, [FromBody] UnblockTicketRequest request)
    {
        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var ticket = await _ticketService.GetTicketAsync(id);
        if (ticket == null)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Ticket not found");

        var validationRequest = new UpdateTicketValidationRequest();
        if (!TicketService.CanUpdateTicket(currentUser, ticket, validationRequest))
            throw new ForbiddenException(ErrorCodes.FORBIDDEN, "Access denied");

        ticket.IsBlocked = false;
        ticket.BlockedReason = null;
        ticket.UpdatedAt = DateTime.UtcNow;

        var notesSuffix = string.IsNullOrWhiteSpace(request.ResolutionNotes)
            ? string.Empty
            : $" ({request.ResolutionNotes.Trim()})";

        _context.TicketMessages.Add(new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            Content = $"Ticket desbloqueado{notesSuffix}",
            SenderId = currentUser.Id,
            IsInternal = false,
            Type = MessageType.System,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(ticket.Id, ActivityType.TicketUnblocked, currentUser.Id, "Ticket desbloqueado");

        return NoContent();
    }

    [HttpPost("{id}/attachments")]
    public async Task<ActionResult<TicketAttachment>> UploadAttachment(Guid id, IFormFile file)
    {
        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var ticket = await _ticketService.GetTicketAsync(id);
        if (ticket == null)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Ticket not found");

        if (!TicketService.CanViewTicket(currentUser, ticket))
            throw new ForbiddenException(ErrorCodes.FORBIDDEN, "Access denied");

        if (file == null || file.Length == 0)
            throw new BadRequestException(ErrorCodes.NO_FILE_PROVIDED, "No file provided");

        var allowedExtensionsConfig = _configuration["FileUpload:AllowedExtensions"] ?? ".jpg,.jpeg,.png,.gif,.pdf,.doc,.docx,.txt,.csv";
        var isValid = await _ticketService.ValidateAttachmentUploadAsync(file.Length, file.FileName, allowedExtensionsConfig, _fileStorageSettings.MaxFileSize);
        if (!isValid)
        {
            if (file.Length > _fileStorageSettings.MaxFileSize)
                throw new BadRequestException(ErrorCodes.FILE_SIZE_EXCEEDED, $"File size exceeds maximum allowed size of {_fileStorageSettings.MaxFileSize / (1024 * 1024)}MB");
            
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            throw new BadRequestException(ErrorCodes.FILE_EXTENSION_NOT_ALLOWED, $"File extension {fileExtension} is not allowed");
        }

        // Save file to disk
        var relativePath = await _fileStorageService.SaveAttachmentAsync(id, file, currentUser.Id);

        // Create attachment record
        var attachment = new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketId = id,
            FileName = file.FileName,
            FilePath = relativePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedById = currentUser.Id,
            UploadedAt = DateTime.UtcNow
        };

        _context.TicketAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        // Update ticket timestamp
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, attachment);
    }

    [HttpGet("{id}/attachments/{attachmentId}")]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId)
    {
        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var ticket = await _ticketService.GetTicketAsync(id);
        if (ticket == null)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Ticket not found");

        if (!TicketService.CanViewTicket(currentUser, ticket))
            throw new ForbiddenException(ErrorCodes.FORBIDDEN, "Access denied");

        var attachment = await _context.TicketAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == id);

        if (attachment == null)
            throw new NotFoundException(ErrorCodes.ATTACHMENT_NOT_FOUND, "Attachment not found");

        var filePath = _fileStorageService.GetFullFilePath(attachment.FilePath);

        if (!System.IO.File.Exists(filePath))
        {
            // Log detailed information for debugging
            var expectedPath = System.IO.Path.GetDirectoryName(filePath);
            var pathExists = System.IO.Directory.Exists(expectedPath);
            var basePath = _fileStorageSettings.BasePath;
            
            throw new NotFoundException(
                ErrorCodes.FILE_NOT_FOUND, 
                $"File not found on disk. Expected path: {filePath}, Directory exists: {pathExists}, BasePath: {basePath}");
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, attachment.ContentType, attachment.FileName);
    }

    [HttpDelete("{id}/attachments/{attachmentId}")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
    {
        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var ticket = await _ticketService.GetTicketAsync(id);
        if (ticket == null)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Ticket not found");

        var validationRequest = new UpdateTicketValidationRequest();
        if (!TicketService.CanUpdateTicket(currentUser, ticket, validationRequest))
            throw new ForbiddenException(ErrorCodes.FORBIDDEN, "Access denied");

        var attachment = await _context.TicketAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == id);

        if (attachment == null)
            throw new NotFoundException(ErrorCodes.ATTACHMENT_NOT_FOUND, "Attachment not found");

        // Delete file from disk
        await _fileStorageService.DeleteAttachmentAsync(attachmentId);

        // Delete from database
        _context.TicketAttachments.Remove(attachment);
        await _context.SaveChangesAsync();

        // Update ticket timestamp
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{id}/messages")]
    public async Task<ActionResult<TicketMessage>> AddMessage(Guid id, [FromBody] AddMessageRequest request)
    {
        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var ticket = await _ticketService.GetTicketAsync(id);
        if (ticket == null)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Ticket not found");

        if (!TicketService.CanCommentTicket(currentUser, ticket))
            throw new ForbiddenException(ErrorCodes.FORBIDDEN, "Access denied");

        var isValid = await _ticketService.ValidateMessageAdditionAsync(request.ParentMessageId, id);
        if (!isValid)
        {
            if (request.ParentMessageId.HasValue)
            {
                var parentMessage = await _context.TicketMessages
                    .FirstOrDefaultAsync(m => m.Id == request.ParentMessageId.Value);
                if (parentMessage == null)
                    throw new BadRequestException(ErrorCodes.PARENT_MESSAGE_NOT_FOUND, "Parent message not found");
                else
                    throw new BadRequestException(ErrorCodes.PARENT_MESSAGE_MISMATCH, "Parent message does not belong to this ticket");
            }
        }

        var message = new TicketMessage
        {
            Id = Guid.NewGuid(),
            TicketId = id,
            ParentMessageId = request.ParentMessageId,
            Content = request.Content,
            SenderId = currentUser.Id,
            IsInternal = request.IsInternal,
            Type = MessageType.Text,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketMessages.Add(message);

        // Update ticket timestamp
        ticket.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Log activity
        await _activityService.LogActivityAsync(ticket.Id, ActivityType.CommentAdded, currentUser.Id, "Comentario agregado");

        return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, message);
    }

    [HttpGet("{id}/activities")]
    public async Task<ActionResult<IEnumerable<TicketActivity>>> GetActivities(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var currentUser = await GetCurrentActiveUserAsync();
        if (currentUser == null)
            throw new UnauthorizedException(ErrorCodes.UNAUTHORIZED, "Authenticated user not found");

        var ticket = await _ticketService.GetTicketAsync(id);
        if (ticket == null)
            throw new NotFoundException(ErrorCodes.TICKET_NOT_FOUND, "Ticket not found");

        if (!TicketService.CanViewTicket(currentUser, ticket))
            throw new ForbiddenException(ErrorCodes.FORBIDDEN, "Access denied");

        var activities = await _activityService.GetActivitiesAsync(id, page, pageSize);
        return Ok(activities);
    }

    private async Task<User?> GetCurrentActiveUserAsync()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return null;

        return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value && u.IsActive);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        return null;
    }
}

public class CreateTicketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public Guid CreatedById { get; set; }
    public Guid DepartmentId { get; set; }
    public TicketSource Source { get; set; }
    public string? ExternalId { get; set; }
}

public class UpdateTicketRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public Guid? AssignedToId { get; set; }
    public bool? IsBlocked { get; set; }
    public string? BlockedReason { get; set; }
    public bool CloseOpenSubticketsWithParent { get; set; }
}

public class AddMessageRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public bool IsInternal { get; set; }
    public Guid? ParentMessageId { get; set; }
}

public class CreateSubticketRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public Guid? AssignedToId { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockedReason { get; set; }
}

public class BlockTicketRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class UnblockTicketRequest
{
    public string? ResolutionNotes { get; set; }
}
