using System.Diagnostics;
using ECommerce.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Application.Behaviors;

/// <summary>
/// Logs the start, duration, and outcome of every request that flows through the
/// MediatR pipeline, without inspecting request/response contents.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();

            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (ValidationException exception)
        {
            stopwatch.Stop();

            // No exception object passed here: a validation failure is an expected
            // outcome, not an operational error, so it should not carry a stack trace.
            logger.LogWarning(
                "Request {RequestName} failed validation after {ElapsedMilliseconds} ms: {ValidationMessage}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                exception.Message);

            throw;
        }
        catch (OrderCannotBeCancelledException exception)
        {
            stopwatch.Stop();

            logger.LogWarning(
                "Request {RequestName} was rejected after {ElapsedMilliseconds} ms: {RejectionReason}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                exception.Message);

            throw;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            logger.LogError(
                exception,
                "Request {RequestName} failed after {ElapsedMilliseconds} ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
