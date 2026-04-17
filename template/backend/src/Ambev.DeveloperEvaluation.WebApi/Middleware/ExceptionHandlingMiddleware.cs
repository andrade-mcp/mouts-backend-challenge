using Ambev.DeveloperEvaluation.Common.Validation;
using Ambev.DeveloperEvaluation.WebApi.Common;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace Ambev.DeveloperEvaluation.WebApi.Middleware;

/// <summary>
/// Translates domain and infrastructure exceptions into the standard
/// <see cref="ApiResponse"/> envelope. Sits outside
/// <see cref="ValidationExceptionMiddleware"/> so validation keeps its dedicated
/// 400 response; everything else is mapped here or falls through to 500.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
        catch (KeyNotFoundException ex)
        {
            await Write(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (DomainException ex)
        {
            await Write(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // Handlers use this for duplicate-key / state-violation cases that
            // aren't raised from the aggregate itself (e.g. duplicate SaleNumber).
            await Write(context, HttpStatusCode.Conflict, ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            await Write(context, HttpStatusCode.Conflict,
                "The resource was modified by another request. Reload and try again.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await Write(context, HttpStatusCode.InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static Task Write(HttpContext context, HttpStatusCode status, string message)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;
        var body = new ApiResponse { Success = false, Message = message };
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }
}
