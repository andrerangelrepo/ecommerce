using FluentValidation;

namespace ECommerce.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Validates input for creating an order.
/// </summary>
public sealed class CreateOrderCommandValidator
    : AbstractValidator<CreateOrderCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOrderCommandValidator"/> class.
    /// </summary>
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId is required.");

        RuleFor(command => command.Items)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .WithMessage("At least one item is required.")
            .NotEmpty()
            .WithMessage("At least one item is required.");

        RuleForEach(command => command.Items)
            .SetValidator(new CreateOrderItemCommandValidator());
    }
}
