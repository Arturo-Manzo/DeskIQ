using DeskIQ.TicketSystem.Core.Entities;
using DeskIQ.TicketSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DeskIQ.TicketSystem.Application.Services;

public class AuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogRoleChangeAsync(Guid userId, UserRole oldRole, UserRole newRole, Guid performedByUserId, string? ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActionType = "RoleChanged",
            EntityName = "User",
            EntityId = userId,
            OldValue = oldRole.ToString(),
            NewValue = newRole.ToString(),
            Description = $"User role changed from {oldRole} to {newRole}",
            PerformedByUserId = performedByUserId,
            PerformedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task LogUserCreationAsync(Guid userId, UserRole role, Guid departmentId, Guid performedByUserId, string? ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActionType = "UserCreated",
            EntityName = "User",
            EntityId = userId,
            OldValue = null,
            NewValue = $"Role: {role}, Department: {departmentId}",
            Description = $"User created with role {role} in department {departmentId}",
            PerformedByUserId = performedByUserId,
            PerformedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task LogUserUpdateAsync(Guid userId, Guid performedByUserId, string? ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActionType = "UserUpdated",
            EntityName = "User",
            EntityId = userId,
            OldValue = null,
            NewValue = null,
            Description = $"User {userId} updated",
            PerformedByUserId = performedByUserId,
            PerformedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task LogDepartmentAssignmentAsync(Guid userId, Guid oldDepartmentId, Guid newDepartmentId, Guid performedByUserId, string? ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActionType = "DepartmentAssigned",
            EntityName = "User",
            EntityId = userId,
            OldValue = oldDepartmentId.ToString(),
            NewValue = newDepartmentId.ToString(),
            Description = $"User department changed from {oldDepartmentId} to {newDepartmentId}",
            PerformedByUserId = performedByUserId,
            PerformedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task LogMultiDepartmentAssignmentAsync(Guid userId, Guid departmentId, Guid performedByUserId, string? ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActionType = "MultiDepartmentAssigned",
            EntityName = "UserDepartment",
            EntityId = userId,
            OldValue = null,
            NewValue = departmentId.ToString(),
            Description = $"Department {departmentId} assigned to user {userId}",
            PerformedByUserId = performedByUserId,
            PerformedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetUserAuditLogsAsync(Guid userId, int page = 1, int pageSize = 50)
    {
        return await _context.AuditLogs
            .Include(a => a.PerformedBy)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 50, Guid? userId = null)
    {
        var query = _context.AuditLogs
            .Include(a => a.User)
            .Include(a => a.PerformedBy)
            .AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        return await query
            .OrderByDescending(a => a.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task LogTicketDepartmentChangeAsync(
        Guid ticketId,
        Guid oldDepartmentId,
        string oldDepartmentName,
        Guid newDepartmentId,
        string newDepartmentName,
        Guid performedByUserId,
        string? ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = performedByUserId,
            ActionType = "TicketDepartmentChanged",
            EntityName = "Ticket",
            EntityId = ticketId,
            OldValue = $"DepartmentId: {oldDepartmentId}, DepartmentName: {oldDepartmentName}",
            NewValue = $"DepartmentId: {newDepartmentId}, DepartmentName: {newDepartmentName}",
            Description = $"Ticket department changed from {oldDepartmentName} to {newDepartmentName}",
            PerformedByUserId = performedByUserId,
            PerformedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }

    public async Task LogTicketDepartmentChangeAttemptAsync(
        Guid ticketId,
        Guid attemptedDepartmentId,
        string attemptedDepartmentName,
        Guid performedByUserId,
        string rejectionReason,
        string? ipAddress = null)
    {
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = performedByUserId,
            ActionType = "TicketDepartmentChangeAttempt",
            EntityName = "Ticket",
            EntityId = ticketId,
            OldValue = null,
            NewValue = $"AttemptedDepartmentId: {attemptedDepartmentId}, AttemptedDepartmentName: {attemptedDepartmentName}",
            Description = $"Ticket department change attempt rejected: {rejectionReason}",
            PerformedByUserId = performedByUserId,
            PerformedAt = DateTime.UtcNow,
            IpAddress = ipAddress
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
    }
}
