using ECommerce.API.Contracts.Orders;
using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using MediatR;
using Microsoft.AspNetCore.Routing;

namespace ECommerce.API.Endpoints.Orders;

internal static class CreateOrderEndpoint
{
    internal static async Task<IResult> HandleAsync(
        CreateOrderRequest request,
        ISender sender,
        LinkGenerator linkGenerator,
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

        var location = linkGenerator.GetPathByName(
            GetOrderByIdEndpoint.RouteName,
            new { id = result.Id });

        return Results.Created(location, response);
    }
}
