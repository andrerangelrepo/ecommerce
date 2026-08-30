namespace ECommerce.API.Contracts.Orders;

/// <summary>
/// Represents the HTTP response returned after cancelling an order.
/// </summary>
/// <param name="Id">The order identifier.</param>
/// <param name="Status">The human-readable order status.</param>
public sealed record CancelOrderResponse(
    Guid Id,
    string Status);
