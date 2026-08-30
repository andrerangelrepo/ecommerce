using ECommerce.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.ExceptionHandling;

/// <summary>
/// Converts unhandled application exceptions into standardized HTTP error responses.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService)
    : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = CreateProblemDetails(httpContext, exception);

        if (exception is ValidationException or OrderCannotBeCancelledException or BadHttpRequestException)
        {
            logger.LogWarning(
                "Request rejected for {Method} {Path}: {Message}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                exception.Message);
        }
        else
        {
            logger.LogError(
                exception,
                "An unhandled exception occurred while processing {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problemDetails.Status
            ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        Exception exception)
    {
        ProblemDetails problemDetails = exception switch
        {
            ValidationException validationException =>
                new ValidationProblemDetails(
                    validationException.Errors
                        .GroupBy(failure => failure.PropertyName)
                        .ToDictionary(
                            group => group.Key,
                            group => group
                                .Select(failure => failure.ErrorMessage)
                                .Distinct()
                                .ToArray()))
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation error",
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                },
            OrderCannotBeCancelledException orderCannotBeCancelledException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Order cannot be cancelled",
                    Detail = orderCannotBeCancelledException.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10"
                },
            BadHttpRequestException badHttpRequestException =>
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid request",
                    Detail = badHttpRequestException.Message,
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
                },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1"
            }
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        return problemDetails;
    }
}
