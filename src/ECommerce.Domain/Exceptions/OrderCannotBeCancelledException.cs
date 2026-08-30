using ECommerce.Domain.Enums;

namespace ECommerce.Domain.Exceptions;

/// <summary>
/// Thrown when an order cannot be cancelled because of its current status.
/// </summary>
/// <param name="currentStatus">The status of the order at the time cancellation was attempted.</param>
public sealed class OrderCannotBeCancelledException(OrderStatus currentStatus)
    : Exception($"Order with status '{currentStatus}' cannot be cancelled.");
