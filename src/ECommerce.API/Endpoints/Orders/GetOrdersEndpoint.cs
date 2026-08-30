using ECommerce.API.Contracts.Orders;
using ECommerce.Application.Features.Orders.Queries.GetOrders;
using MediatR;

namespace ECommerce.API.Endpoints.Orders;

internal static class GetOrdersEndpoint
{
    internal static async Task<IResult> HandleAsync(
        ISender sender,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 10)
    {
        var result = await sender.Send(new GetOrdersQuery(page, pageSize), cancellationToken);

        var response = new GetOrdersResponse(
            result.Items
                .Select(item => new OrderListItemResponse(
                    item.Id,
                    item.CustomerId,
                    item.Status.ToString(),
                    item.CreatedAt,
                    item.TotalAmount))
                .ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);

        return Results.Ok(response);
    }
}
