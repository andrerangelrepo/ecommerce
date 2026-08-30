using ECommerce.API.Contracts.Orders;
using ECommerce.Application.Features.Orders.Queries.GetOrderById;
using MediatR;

namespace ECommerce.API.Endpoints.Orders;

internal static class GetOrderByIdEndpoint
{
    internal static async Task<IResult> HandleAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);

        if (result is null)
        {
            return Results.NotFound();
        }

        var response = new GetOrderByIdResponse(
            result.Id,
            result.CustomerId,
            result.Status.ToString(),
            result.CreatedAt,
            result.TotalAmount,
            result.Items
                .Select(item => new GetOrderItemResponse(
                    item.Id,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    item.TotalPrice))
                .ToArray());

        return Results.Ok(response);
    }
}
