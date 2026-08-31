using FluentValidation;
using MediatR;

namespace ECommerce.Application.Behaviors;

/// <summary>
/// Validates a request before passing it to the next pipeline component.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
/// </remarks>
/// <param name="validators">The validators associated with the request.</param>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validationContext = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(validator =>
                validator.ValidateAsync(validationContext, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
