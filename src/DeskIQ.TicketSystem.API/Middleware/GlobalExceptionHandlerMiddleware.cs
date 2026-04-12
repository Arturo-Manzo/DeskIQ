using System.Net;
using System.Text.Json;
using DeskIQ.TicketSystem.API.Exceptions;
using DeskIQ.TicketSystem.API.Models;

namespace DeskIQ.TicketSystem.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);

        var (statusCode, errorResponse) = exception switch
        {
            NotFoundException notFound => ((int)HttpStatusCode.NotFound, new ErrorResponse
            {
                Code = notFound.ErrorCode,
                Message = notFound.Message,
                Details = notFound.Details
            }),
            BadRequestException badRequest => ((int)HttpStatusCode.BadRequest, new ErrorResponse
            {
                Code = badRequest.ErrorCode,
                Message = badRequest.Message,
                Details = badRequest.Details
            }),
            ConflictException conflict => ((int)HttpStatusCode.Conflict, new ErrorResponse
            {
                Code = conflict.ErrorCode,
                Message = conflict.Message,
                Details = conflict.Details
            }),
            UnauthorizedException unauthorized => ((int)HttpStatusCode.Unauthorized, new ErrorResponse
            {
                Code = unauthorized.ErrorCode,
                Message = unauthorized.Message,
                Details = unauthorized.Details
            }),
            ForbiddenException forbidden => ((int)HttpStatusCode.Forbidden, new ErrorResponse
            {
                Code = forbidden.ErrorCode,
                Message = forbidden.Message,
                Details = forbidden.Details
            }),
            _ => ((int)HttpStatusCode.InternalServerError, new ErrorResponse
            {
                Code = "INTERNAL_SERVER_ERROR",
                Message = "An unexpected error occurred. Please try again later.",
                Details = _environment.IsDevelopment() ? exception.Message : null
            })
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}
