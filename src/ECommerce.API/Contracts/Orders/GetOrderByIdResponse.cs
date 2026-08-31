namespace ECommerce.API.Contracts.Orders;

/// <summary>
/// Represents the HTTP response returned when fetching an order by its identifier.
/// </summary>
/// <param name="Id">The order identifier.</param>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="Status">The human-readable order status.</param>
/// <param name="CreatedAt">The date and time when the order was created.</param>
/// <param name="TotalAmount">The total amount calculated by the domain.</param>
/// <param name="Items">The items in the order.</param>
public sealed record GetOrderByIdResponse(
    Guid Id,
    Guid CustomerId,
    string Status,
    DateTime CreatedAt,
    decimal TotalAmount,
    IReadOnlyCollection<GetOrderItemResponse> Items);

/// <summary>
/// Represents an item in the HTTP response returned when fetching an order.
/// </summary>
/// <param name="Id">The item identifier.</param>
/// <param name="ProductName">The product name.</param>
/// <param name="Quantity">The quantity ordered.</param>
/// <param name="UnitPrice">The price of one unit.</param>
/// <param name="TotalPrice">The total price calculated by the domain.</param>
public sealed record GetOrderItemResponse(
    Guid Id,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);
