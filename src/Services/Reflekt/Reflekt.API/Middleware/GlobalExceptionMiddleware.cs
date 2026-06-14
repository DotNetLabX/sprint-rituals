using System.Net;
using System.Text.Json;
using Blocks.Domain.Exceptions;
using Blocks.Exceptions;
using FluentValidation;

namespace Reflekt.API.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IWebHostEnvironment env)
{
    private static HttpStatusCode MapStatusCode(Exception ex) => ex switch
    {
        ValidationException => HttpStatusCode.BadRequest,
        ArgumentException => HttpStatusCode.BadRequest,
        BadRequestException => HttpStatusCode.BadRequest,
        NotFoundException => HttpStatusCode.NotFound,
        DomainException => HttpStatusCode.BadRequest,
        UnauthorizedException => HttpStatusCode.Unauthorized,
        _ => HttpStatusCode.InternalServerError
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (OperationCanceledException)
        {
            if (!context.Response.HasStarted)
                context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = MapStatusCode(exception);
        if (statusCode >= HttpStatusCode.InternalServerError)
            logger.LogError(exception, "Unhandled exception. TraceId={TraceId}", context.TraceIdentifier);

        var response = new
        {
            StatusCode = (int)statusCode,
            exception.Message,
            TraceId = context.TraceIdentifier,
            Details = env.IsDevelopment() ? exception.StackTrace : null
        };

        return WriteResponseAsync(context, statusCode, response);
    }

    private Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var response = new
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = "One or more validation errors occurred.",
            TraceId = context.TraceIdentifier,
            Errors = exception.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }),
            Details = env.IsDevelopment() ? exception.StackTrace : null
        };

        return WriteResponseAsync(context, HttpStatusCode.BadRequest, response);
    }

    private static Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, object response)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
