namespace DeskIQ.TicketSystem.API.Exceptions;

public abstract class ApiException : Exception
{
    public string ErrorCode { get; }
    public object? Details { get; }

    protected ApiException(string errorCode, string message, object? details = null) : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string errorCode, string message, object? details = null) 
        : base(errorCode, message, details) { }
}

public class BadRequestException : ApiException
{
    public BadRequestException(string errorCode, string message, object? details = null) 
        : base(errorCode, message, details) { }
}

public class ConflictException : ApiException
{
    public ConflictException(string errorCode, string message, object? details = null) 
        : base(errorCode, message, details) { }
}

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string errorCode, string message, object? details = null) 
        : base(errorCode, message, details) { }
}

public class ForbiddenException : ApiException
{
    public ForbiddenException(string errorCode, string message, object? details = null) 
        : base(errorCode, message, details) { }
}
