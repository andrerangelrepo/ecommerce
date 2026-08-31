using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Features.Orders.DTOs;
using MediatR;

namespace ECommerce.Application.Features.Orders.Queries.GetOrders;

/// <summary>
/// Handles requests to fetch a paginated list of orders.
/// </summary>
/// <param name="orderRepository">The order persistence abstraction.</param>
public sealed class GetOrdersQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetOrdersQuery, GetOrdersResult>
{
    /// <inheritdoc />
    public async Task<GetOrdersResult> Handle(
        GetOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var result = await orderRepository.GetPageAsync(
            request.Page,
            request.PageSize,
            cancellationToken);

        var items = result.Items
            .Select(order => new OrderListItemResult(
                order.Id,
                order.CustomerId,
                order.Status,
                order.CreatedAt,
                order.TotalAmount))
            .ToArray();

        var totalPages = (int)Math.Ceiling(
            result.TotalCount / (double)request.PageSize);

        return new GetOrdersResult(
            items,
            request.Page,
            request.PageSize,
            result.TotalCount,
            totalPages);
    }
}
