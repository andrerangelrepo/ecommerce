using ECommerce.API.Contracts.Orders;
using ECommerce.Application.Features.Orders.Commands.CancelOrder;
using MediatR;

namespace ECommerce.API.Endpoints.Orders;

internal static class CancelOrderEndpoint
{
    internal static async Task<IResult> HandleAsync(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelOrderCommand(id), cancellationToken);

        if (result is null)
        {
            return Results.NotFound();
        }

        var response = new CancelOrderResponse(
            result.Id,
            result.Status.ToString());

        return Results.Ok(response);
    }
}
