namespace DeskIQ.TicketSystem.API.Models;

public class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
}

public static class ErrorCodes
{
    // Authentication & Authorization
    public const string UNAUTHORIZED = "UNAUTHORIZED";
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
    public const string INVALID_TOKEN = "INVALID_TOKEN";
    public const string FORBIDDEN = "FORBIDDEN";
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";
    public const string USER_INACTIVE = "USER_INACTIVE";

    // Resource Not Found
    public const string NOT_FOUND = "NOT_FOUND";
    public const string DEPARTMENT_NOT_FOUND = "DEPARTMENT_NOT_FOUND";
    public const string TICKET_NOT_FOUND = "TICKET_NOT_FOUND";
    public const string ATTACHMENT_NOT_FOUND = "ATTACHMENT_NOT_FOUND";
    public const string MESSAGE_NOT_FOUND = "MESSAGE_NOT_FOUND";
    public const string FILE_NOT_FOUND = "FILE_NOT_FOUND";

    // Validation Errors
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string INVALID_REQUEST = "INVALID_REQUEST";
    public const string EMAIL_ALREADY_EXISTS = "EMAIL_ALREADY_EXISTS";
    public const string INVALID_DEPARTMENT = "INVALID_DEPARTMENT";
    public const string DEPARTMENT_CODE_REQUIRED = "DEPARTMENT_CODE_REQUIRED";
    public const string DEPARTMENT_CODE_INVALID = "DEPARTMENT_CODE_INVALID";
    public const string DEPARTMENT_CODE_EXISTS = "DEPARTMENT_CODE_EXISTS";
    public const string DEPARTMENT_CODE_IMMUTABLE = "DEPARTMENT_CODE_IMMUTABLE";
    public const string BLOCKED_REASON_REQUIRED = "BLOCKED_REASON_REQUIRED";
    public const string NO_FILE_PROVIDED = "NO_FILE_PROVIDED";
    public const string FILE_SIZE_EXCEEDED = "FILE_SIZE_EXCEEDED";
    public const string FILE_EXTENSION_NOT_ALLOWED = "FILE_EXTENSION_NOT_ALLOWED";
    public const string PARENT_MESSAGE_NOT_FOUND = "PARENT_MESSAGE_NOT_FOUND";
    public const string PARENT_MESSAGE_MISMATCH = "PARENT_MESSAGE_MISMATCH";

    // Business Logic Conflicts
    public const string CONFLICT = "CONFLICT";
    public const string CANNOT_CLOSE_PARENT_WITH_OPEN_SUBTICKETS = "CANNOT_CLOSE_PARENT_WITH_OPEN_SUBTICKETS";
    public const string CANNOT_REASSIGN_RESOLVED_TICKET = "CANNOT_REASSIGN_RESOLVED_TICKET";
}
