namespace ECommerce.API.Contracts.Orders;

/// <summary>
/// Represents the HTTP request to create an order.
/// </summary>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="Items">The items to include in the order.</param>
public sealed record CreateOrderRequest(
    Guid CustomerId,
    IReadOnlyCollection<CreateOrderItemRequest> Items);

/// <summary>
/// Represents an item in the HTTP request to create an order.
/// </summary>
/// <param name="ProductName">The product name.</param>
/// <param name="Quantity">The quantity ordered.</param>
/// <param name="UnitPrice">The price of one unit.</param>
public sealed record CreateOrderItemRequest(
    string ProductName,
    int Quantity,
    decimal UnitPrice);
