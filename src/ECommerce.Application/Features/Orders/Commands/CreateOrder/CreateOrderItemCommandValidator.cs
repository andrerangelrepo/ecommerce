using FluentValidation;

namespace ECommerce.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Validates an item supplied when creating an order.
/// </summary>
public sealed class CreateOrderItemCommandValidator
    : AbstractValidator<CreateOrderItemCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOrderItemCommandValidator"/> class.
    /// </summary>
    public CreateOrderItemCommandValidator()
    {
        RuleFor(item => item.ProductName)
            .NotEmpty()
            .WithMessage("ProductName is required.");

        RuleFor(item => item.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than zero.");

        RuleFor(item => item.UnitPrice)
            .GreaterThan(0)
            .WithMessage("UnitPrice must be greater than zero.");
    }
}
