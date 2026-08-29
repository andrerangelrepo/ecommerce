using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.DTOs;

/// <summary>
/// Represents the result of creating an order.
/// </summary>
/// <param name="Id">The order identifier.</param>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="Status">The current order status.</param>
/// <param name="CreatedAt">The date and time when the order was created.</param>
/// <param name="TotalAmount">The total amount calculated by the domain.</param>
public sealed record CreateOrderResult(
    Guid Id,
    Guid CustomerId,
    OrderStatus Status,
    DateTime CreatedAt,
    decimal TotalAmount);
