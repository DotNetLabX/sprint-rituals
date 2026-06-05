using System.Text.Json;
using Blocks.Domain.Exceptions;
using Blocks.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Blocks.AspNetCore.Middlewares;

public class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException)
        {
            context.Response.StatusCode = 499;
        }
        catch (Exception ex) when (IsCausedByCancellation(ex))
        {
            context.Response.StatusCode = 499;
        }
        catch (ValidationException ex)
        {
            await WriteResponseAsync(context, 400, "One or more validation errors occurred.", errors: ex.Errors
                .Select(e => new ValidationError(e.PropertyName, e.ErrorMessage))
                .ToList());
        }
        catch (Exception ex)
        {
            var (statusCode, message) = MapStatusCode(ex);
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            var details = environment.IsDevelopment() ? ex.ToString() : null;
            await WriteResponseAsync(context, statusCode, message, details: details);
        }
    }

    private static bool IsCausedByCancellation(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
            if (e is OperationCanceledException) return true;
        return false;
    }

    private static (int StatusCode, string Message) MapStatusCode(Exception ex) => ex switch
    {
        BadRequestException e => (400, e.Message),
        ForbiddenException e => (403, e.Message),
        NotFoundException e => (404, e.Message),
        ConflictException e => (409, e.Message),
        UnauthorizedException e => (401, e.Message),
        BadGatewayException e => (502, e.Message),
        DomainException e => (400, e.Message),
        ArgumentException e => (400, e.Message),
        _ => (500, "An unexpected error occurred.")
    };

    private static async Task WriteResponseAsync(
        HttpContext context,
        int statusCode,
        string message,
        string? details = null,
        List<ValidationError>? errors = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var traceId = context.TraceIdentifier;

        object response = errors is not null
            ? new { StatusCode = statusCode, Message = message, TraceId = traceId, Errors = errors, Details = details }
            : new { StatusCode = statusCode, Message = message, TraceId = traceId, Details = details };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private record ValidationError(string PropertyName, string ErrorMessage);
}
