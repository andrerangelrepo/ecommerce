using ECommerce.API.Contracts.Orders;
using ECommerce.API.ExceptionHandling;
using ECommerce.Application.Features.Orders.Commands.CancelOrder;
using MediatR;

namespace ECommerce.API.Endpoints.Orders;

internal static class CancelOrderEndpoint
{
    internal static async Task<IResult> HandleAsync(
        Guid id,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CancelOrderCommand(id), cancellationToken);

        if (result is null)
        {
            return OrderNotFoundProblem.Result(httpContext);
        }

        var response = new CancelOrderResponse(
            result.Id,
            result.Status.ToString());

        return Results.Ok(response);
    }
}
