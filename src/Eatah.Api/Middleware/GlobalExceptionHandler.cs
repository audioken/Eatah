using Eatah.Api.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Eatah.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Optimistic concurrency conflict: another workspace member modified the resource
        // between read and write. Return 409 so the client can refetch and retry.
        if (exception is DbUpdateConcurrencyException)
        {
            _logger.LogInformation(exception, "Concurrency conflict on {Path}.", httpContext.Request.Path);
            var conflict = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7807",
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency conflict.",
                Detail = "The resource was modified by another user. Please refresh and try again.",
                Instance = httpContext.Request.Path,
                Extensions = { ["errorCode"] = ErrorCodes.ConcurrencyConflict }
            };
            httpContext.Response.StatusCode = conflict.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(conflict, cancellationToken);
            return true;
        }

        _logger.LogError(exception, "Unhandled exception occurred.");

        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = _environment.IsDevelopment() ? exception.Message : "Please try again later.",
            Instance = httpContext.Request.Path,
            Extensions = { ["errorCode"] = ErrorCodes.Unexpected }
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
