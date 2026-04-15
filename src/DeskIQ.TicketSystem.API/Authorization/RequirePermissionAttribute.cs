using Microsoft.AspNetCore.Authorization;

namespace DeskIQ.TicketSystem.API.Authorization;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = permission;
    }
}
