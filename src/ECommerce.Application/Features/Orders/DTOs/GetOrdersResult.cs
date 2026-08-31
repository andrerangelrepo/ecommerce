using ECommerce.Domain.Enums;

namespace ECommerce.Application.Features.Orders.DTOs;

/// <summary>
/// Represents the paginated result of fetching a list of orders.
/// </summary>
/// <param name="Items">The orders in the current page.</param>
/// <param name="Page">The page number.</param>
/// <param name="PageSize">The number of orders per page.</param>
/// <param name="TotalCount">The total number of orders across all pages.</param>
/// <param name="TotalPages">The total number of pages.</param>
public sealed record GetOrdersResult(
    IReadOnlyCollection<OrderListItemResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

/// <summary>
/// Represents an order in the paginated list result.
/// </summary>
/// <param name="Id">The order identifier.</param>
/// <param name="CustomerId">The customer identifier.</param>
/// <param name="Status">The current order status.</param>
/// <param name="CreatedAt">The date and time when the order was created.</param>
/// <param name="TotalAmount">The total amount calculated by the domain.</param>
public sealed record OrderListItemResult(
    Guid Id,
    Guid CustomerId,
    OrderStatus Status,
    DateTime CreatedAt,
    decimal TotalAmount);
