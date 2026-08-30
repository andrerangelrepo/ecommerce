using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Queries.GetOrderById;

/// <summary>
/// Handles requests to fetch an order by its identifier.
/// </summary>
/// <param name="orderRepository">The order persistence abstraction.</param>
public sealed class GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetOrderByIdQuery, GetOrderByIdResult?>
{
    /// <inheritdoc />
    public async Task<GetOrderByIdResult?> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (order is null)
        {
            return null;
        }

        return new GetOrderByIdResult(
            order.Id,
            order.CustomerId,
            order.Status,
            order.CreatedAt,
            order.TotalAmount,
            order.Items
                .Select(item => new GetOrderItemResult(
                    item.Id,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    item.TotalPrice))
                .ToArray());
    }
}
