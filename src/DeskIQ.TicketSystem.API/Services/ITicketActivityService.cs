using DeskIQ.TicketSystem.Core.Entities;

namespace DeskIQ.TicketSystem.API.Services;

public interface ITicketActivityService
{
    Task LogActivityAsync(Guid ticketId, ActivityType type, Guid performedBy, string? description = null, string? oldValue = null, string? newValue = null);
    Task<List<TicketActivity>> GetActivitiesAsync(Guid ticketId, int page = 1, int pageSize = 50);
}
