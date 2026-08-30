using ECommerce.API.Contracts.Orders;
using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using MediatR;

namespace ECommerce.API.Endpoints.Orders;

internal static class CreateOrderEndpoint
{
    internal static async Task<IResult> HandleAsync(
        CreateOrderRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var items = request.Items?
            .Select(item => new CreateOrderItemCommand(
                item.ProductName,
                item.Quantity,
                item.UnitPrice))
            .ToArray() ?? [];

        var command = new CreateOrderCommand(
            request.CustomerId,
            items);

        var result = await sender.Send(command, cancellationToken);

        var response = new CreateOrderResponse(
            result.Id,
            result.CustomerId,
            result.Status.ToString(),
            result.CreatedAt,
            result.TotalAmount);

        return Results.Created(
            $"/api/orders/{result.Id}",
            response);
    }
}
