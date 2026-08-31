using ECommerce.API.Contracts.Orders;
using ECommerce.API.ExceptionHandling;
using ECommerce.Application.Features.Orders.Queries.GetOrderById;
using MediatR;

namespace ECommerce.API.Endpoints.Orders;

internal static class GetOrderByIdEndpoint
{
    /// <summary>
    /// The route name used by <see cref="CreateOrderEndpoint"/> to build the
    /// <c>Location</c> header without hardcoding the route template a second time.
    /// </summary>
    internal const string RouteName = "GetOrderById";

    internal static async Task<IResult> HandleAsync(
        Guid id,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);

        if (result is null)
        {
            return OrderNotFoundProblem.Result(httpContext);
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
