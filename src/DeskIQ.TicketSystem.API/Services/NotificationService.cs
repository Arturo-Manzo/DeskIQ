using Microsoft.AspNetCore.SignalR;
using DeskIQ.TicketSystem.API.Hubs;
using DeskIQ.TicketSystem.Core.Entities;

namespace DeskIQ.TicketSystem.API.Services;

public interface INotificationService
{
    Task NotifyTicketCreatedAsync(Ticket ticket, string performedBy);
    Task NotifyTicketUpdatedAsync(Ticket ticket, string performedBy);
    Task NotifyTicketAssignedAsync(Ticket ticket, string performedBy);
    Task NotifyMessageAddedAsync(TicketMessage message, string senderName);
    Task NotifyUserConnectedAsync(UserConnectionInfo userInfo);
    Task NotifyUserDisconnectedAsync(UserConnectionInfo userInfo);
}

public class NotificationService : INotificationService
{
    private readonly IHubContext<TicketHub, ITicketHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IHubContext<TicketHub, ITicketHub> hubContext, ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyTicketCreatedAsync(Ticket ticket, string performedBy)
    {
        var notification = new TicketNotification
        {
            TicketId = ticket.Id,
            Title = ticket.Title,
            Status = ticket.Status,
            Priority = ticket.Priority,
            DepartmentId = ticket.DepartmentId,
            AssignedToId = ticket.AssignedToId,
            AssignedToName = ticket.AssignedTo?.Name ?? "",
            CreatedAt = ticket.CreatedAt,
            Action = "Created",
            PerformedBy = performedBy
        };

        try
        {
            // Notify department group
            await _hubContext.Clients.Group($"department_{ticket.DepartmentId}")
                .TicketCreated(notification);

            // If assigned, notify the specific user
            if (ticket.AssignedToId.HasValue)
            {
                await _hubContext.Clients.Group($"user_{ticket.AssignedToId}")
                    .TicketCreated(notification);
            }

            _logger.LogInformation("Ticket created notification sent for ticket {TicketId}", ticket.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ticket created notification for ticket {TicketId}", ticket.Id);
        }
    }

    public async Task NotifyTicketUpdatedAsync(Ticket ticket, string performedBy)
    {
        var notification = new TicketNotification
        {
            TicketId = ticket.Id,
            Title = ticket.Title,
            Status = ticket.Status,
            Priority = ticket.Priority,
            DepartmentId = ticket.DepartmentId,
            AssignedToId = ticket.AssignedToId,
            AssignedToName = ticket.AssignedTo?.Name ?? "",
            CreatedAt = ticket.UpdatedAt,
            Action = "Updated",
            PerformedBy = performedBy
        };

        try
        {
            // Notify ticket group (people viewing the ticket)
            await _hubContext.Clients.Group($"ticket_{ticket.Id}")
                .TicketUpdated(notification);

            // Notify department group
            await _hubContext.Clients.Group($"department_{ticket.DepartmentId}")
                .TicketUpdated(notification);

            // If assigned, notify the specific user
            if (ticket.AssignedToId.HasValue)
            {
                await _hubContext.Clients.Group($"user_{ticket.AssignedToId}")
                    .TicketUpdated(notification);
            }

            _logger.LogInformation("Ticket updated notification sent for ticket {TicketId}", ticket.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ticket updated notification for ticket {TicketId}", ticket.Id);
        }
    }

    public async Task NotifyTicketAssignedAsync(Ticket ticket, string performedBy)
    {
        var notification = new TicketNotification
        {
            TicketId = ticket.Id,
            Title = ticket.Title,
            Status = ticket.Status,
            Priority = ticket.Priority,
            DepartmentId = ticket.DepartmentId,
            AssignedToId = ticket.AssignedToId,
            AssignedToName = ticket.AssignedTo?.Name ?? "",
            CreatedAt = ticket.UpdatedAt,
            Action = "Assigned",
            PerformedBy = performedBy
        };

        try
        {
            // Notify ticket group
            await _hubContext.Clients.Group($"ticket_{ticket.Id}")
                .TicketAssigned(notification);

            // Notify department group
            await _hubContext.Clients.Group($"department_{ticket.DepartmentId}")
                .TicketAssigned(notification);

            // Notify the assigned user specifically
            if (ticket.AssignedToId.HasValue)
            {
                await _hubContext.Clients.Group($"user_{ticket.AssignedToId}")
                    .TicketAssigned(notification);
            }

            _logger.LogInformation("Ticket assigned notification sent for ticket {TicketId}", ticket.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending ticket assigned notification for ticket {TicketId}", ticket.Id);
        }
    }

    public async Task NotifyMessageAddedAsync(TicketMessage message, string senderName)
    {
        var notification = new MessageNotification
        {
            TicketId = message.TicketId,
            MessageId = message.Id,
            Content = message.Content.Length > 100 ? message.Content.Substring(0, 100) + "..." : message.Content,
            SenderId = message.SenderId,
            SenderName = senderName,
            IsInternal = message.IsInternal,
            CreatedAt = message.CreatedAt
        };

        try
        {
            // Notify ticket group
            await _hubContext.Clients.Group($"ticket_{message.TicketId}")
                .MessageAdded(notification);

            // If it's an internal message, only notify department members
            if (message.IsInternal)
            {
                // We would need to get the ticket's department to notify the right group
                // For now, we'll notify the ticket group which includes department members
                await _hubContext.Clients.Group($"ticket_{message.TicketId}")
                    .MessageAdded(notification);
            }

            _logger.LogInformation("Message added notification sent for message {MessageId} in ticket {TicketId}", 
                message.Id, message.TicketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message added notification for message {MessageId}", message.Id);
        }
    }

    public async Task NotifyUserConnectedAsync(UserConnectionInfo userInfo)
    {
        try
        {
            // Add user to their personal group for targeted notifications
            await _hubContext.Groups.AddToGroupAsync(userInfo.ConnectionId, $"user_{userInfo.UserId}");

            _logger.LogInformation("User {UserId} connected and added to notification group", userInfo.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying user connection for user {UserId}", userInfo.UserId);
        }
    }

    public async Task NotifyUserDisconnectedAsync(UserConnectionInfo userInfo)
    {
        try
        {
            // Remove user from their personal group
            await _hubContext.Groups.RemoveFromGroupAsync(userInfo.ConnectionId, $"user_{userInfo.UserId}");

            _logger.LogInformation("User {UserId} disconnected and removed from notification group", userInfo.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying user disconnection for user {UserId}", userInfo.UserId);
        }
    }
}
