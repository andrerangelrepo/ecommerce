using FluentValidation;

namespace ECommerce.Application.Features.Orders.Queries.GetOrders;

/// <summary>
/// Validates input for fetching a paginated list of orders.
/// </summary>
public sealed class GetOrdersQueryValidator
    : AbstractValidator<GetOrdersQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetOrdersQueryValidator"/> class.
    /// </summary>
    public GetOrdersQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than zero.");

        RuleFor(query => query.PageSize)
            .GreaterThan(0)
            .WithMessage("PageSize must be greater than zero.");
    }
}
