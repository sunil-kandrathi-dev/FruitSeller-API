using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FruitSeller.API.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            logger.LogInformation(exception, "Request was cancelled while processing {Path}", httpContext.Request.Path);
            return false;
        }

        logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);

        var problem = exception is InvalidOperationException invalidOperationException
            ? new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Request validation failed",
                Detail = invalidOperationException.Message
            }
            : new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Detail = "The request could not be processed. Please try again or contact support."
            };

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
