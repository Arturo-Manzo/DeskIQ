using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using DeskIQ.TicketSystem.Core.Entities;

namespace DeskIQ.TicketSystem.API.Hubs;

public interface ITicketHub
{
    Task TicketCreated(TicketNotification notification);
    Task TicketUpdated(TicketNotification notification);
    Task TicketAssigned(TicketNotification notification);
    Task MessageAdded(MessageNotification notification);
    Task UserConnected(UserConnectionInfo userInfo);
    Task UserDisconnected(UserConnectionInfo userInfo);
}

public class TicketHub : Hub<ITicketHub>
{
    private static readonly ConcurrentDictionary<string, UserConnectionInfo> _connectedUsers = new();

    public async Task JoinDepartmentGroup(string departmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"department_{departmentId}");
    }

    public async Task LeaveDepartmentGroup(string departmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"department_{departmentId}");
    }

    public async Task JoinTicketGroup(string ticketId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket_{ticketId}");
    }

    public async Task LeaveTicketGroup(string ticketId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ticket_{ticketId}");
    }

    public async Task SetUserStatus(UserStatusInfo statusInfo)
    {
        var connectionId = Context.ConnectionId;
        var userInfo = new UserConnectionInfo
        {
            ConnectionId = connectionId,
            UserId = statusInfo.UserId,
            UserName = statusInfo.UserName,
            DepartmentId = statusInfo.DepartmentId,
            Role = statusInfo.Role,
            IsOnline = true,
            ConnectedAt = DateTime.UtcNow
        };

        _connectedUsers.TryAdd(connectionId, userInfo);

        // Notify others in the same department
        await Clients.GroupExcept($"department_{statusInfo.DepartmentId}", connectionId)
                    .UserConnected(userInfo);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_connectedUsers.TryRemove(Context.ConnectionId, out var userInfo))
        {
            userInfo.IsOnline = false;
            userInfo.DisconnectedAt = DateTime.UtcNow;

            // Notify others in the same department
            await Clients.GroupExcept($"department_{userInfo.DepartmentId}", Context.ConnectionId)
                        .UserDisconnected(userInfo);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static IEnumerable<UserConnectionInfo> GetConnectedUsers()
    {
        return _connectedUsers.Values;
    }

    public static UserConnectionInfo? GetUser(string connectionId)
    {
        return _connectedUsers.TryGetValue(connectionId, out var user) ? user : null;
    }
}

public class TicketNotification
{
    public Guid TicketId { get; set; }
    public string Title { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? AssignedToId { get; set; }
    public string AssignedToName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Updated, Assigned, etc.
    public string PerformedBy { get; set; } = string.Empty;
}

public class MessageNotification
{
    public Guid TicketId { get; set; }
    public Guid MessageId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserConnectionInfo
{
    public string ConnectionId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime? DisconnectedAt { get; set; }
}

public class UserStatusInfo
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string Role { get; set; } = string.Empty;
}
