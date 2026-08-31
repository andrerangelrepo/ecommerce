using System.Diagnostics;
using ECommerce.Application.Observability;
using MediatR;

namespace ECommerce.Application.Behaviors;

/// <summary>
/// Wraps every request in an <see cref="Activity"/>, nesting it under the HTTP span produced
/// by ASP.NET Core's own instrumentation so a trace shows which Command/Query ran inside
/// which request.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class TracingBehavior<TRequest, TResponse>
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

        using var activity = ApplicationTelemetry.ActivitySource.StartActivity(
            requestName,
            ActivityKind.Internal);

        activity?.SetTag("application.request.name", requestName);

        try
        {
            return await next();
        }
        catch (Exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error);

            throw;
        }
    }
}
