namespace ECommerce.API.Endpoints.Orders;

/// <summary>
/// Maps HTTP endpoints for orders.
/// </summary>
public static class OrderEndpoints
{
    /// <summary>
    /// Maps all order endpoints under the <c>/api/orders</c> route group.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/orders")
            .RequireAuthorization();

        group.MapPost(string.Empty, CreateOrderEndpoint.HandleAsync);

        return endpoints;
    }
}
