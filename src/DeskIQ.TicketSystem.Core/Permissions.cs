namespace DeskIQ.TicketSystem.Core;

public static class Permissions
{
    // Ticket permissions
    public const string ViewTicket = "Ticket.View";
    public const string CreateTicket = "Ticket.Create";
    public const string EditTicket = "Ticket.Edit";
    public const string DeleteTicket = "Ticket.Delete";
    public const string AssignTicket = "Ticket.Assign";
    public const string CommentTicket = "Ticket.Comment";
    public const string ViewTicketMetrics = "Ticket.ViewMetrics";

    // User management permissions
    public const string ViewUsers = "User.View";
    public const string CreateUser = "User.Create";
    public const string EditUser = "User.Edit";
    public const string DeleteUser = "User.Delete";
    public const string ChangeUserRole = "User.ChangeRole";
    public const string AssignUserDepartment = "User.AssignDepartment";

    // Department management permissions
    public const string ViewDepartments = "Department.View";
    public const string CreateDepartment = "Department.Create";
    public const string EditDepartment = "Department.Edit";
    public const string DeleteDepartment = "Department.Delete";

    // System administration permissions
    public const string ViewAuditLogs = "Audit.View";
    public const string ManageSystemSettings = "System.ManageSettings";
    public const string ManageSSO = "System.ManageSSO";
}
