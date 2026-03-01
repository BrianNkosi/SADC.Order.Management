using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace SADC.Order.Management.Api.Middleware;

/// <summary>
/// Global exception handler that converts exceptions to RFC 7807 Problem Details responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
        var problemDetails = exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Status = (int)HttpStatusCode.BadRequest,
                Title = "Validation Error",
                Detail = "One or more validation errors occurred.",
                Extensions =
                {
                    ["errors"] = validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                }
            },

            KeyNotFoundException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.NotFound,
                Title = "Not Found",
                Detail = exception.Message
            },

            InvalidOperationException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.UnprocessableEntity,
                Title = "Business Rule Violation",
                Detail = exception.Message
            },

            DbUpdateConcurrencyException => new ProblemDetails
            {
                Status = (int)HttpStatusCode.Conflict,
                Title = "Concurrency Conflict",
                Detail = "The resource was modified by another request. Please retry with the latest version."
            },

            _ => new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred."
            }
        };

        if (problemDetails.Status == (int)HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning("Handled exception ({StatusCode}): {Message}",
                problemDetails.Status, exception.Message);
        }

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = problemDetails.Status ?? 500;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
