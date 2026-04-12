using Microsoft.EntityFrameworkCore;
using DeskIQ.TicketSystem.Infrastructure.Data;
using DeskIQ.TicketSystem.Core.Entities;

namespace DeskIQ.TicketSystem.API.Services;

public class TicketActivityService : ITicketActivityService
{
    private readonly AppDbContext _context;

    public TicketActivityService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogActivityAsync(Guid ticketId, ActivityType type, Guid performedBy, string? description = null, string? oldValue = null, string? newValue = null)
    {
        var activity = new TicketActivity
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            Type = type,
            Description = description,
            OldValue = oldValue,
            NewValue = newValue,
            PerformedById = performedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.TicketActivities.Add(activity);
        await _context.SaveChangesAsync();
    }

    public async Task<List<TicketActivity>> GetActivitiesAsync(Guid ticketId, int page = 1, int pageSize = 50)
    {
        return await _context.TicketActivities
            .Include(a => a.PerformedBy)
            .Where(a => a.TicketId == ticketId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
