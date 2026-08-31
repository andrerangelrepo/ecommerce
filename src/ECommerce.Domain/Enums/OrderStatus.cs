namespace ECommerce.Domain.Enums;

/// <summary>
/// Represents the current state of an order.
/// </summary>
public enum OrderStatus
{
    /// <summary>The order is awaiting processing.</summary>
    Pending,

    /// <summary>The order has been confirmed.</summary>
    Confirmed,

    /// <summary>The order has been cancelled.</summary>
    Cancelled
}
