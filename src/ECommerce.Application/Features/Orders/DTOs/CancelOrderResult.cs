using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.DTOs;

/// <summary>
/// Represents the result of cancelling an order.
/// </summary>
/// <param name="Id">The order identifier.</param>
/// <param name="Status">The current order status.</param>
public sealed record CancelOrderResult(
    Guid Id,
    OrderStatus Status);
