using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions.Persistence;

/// <summary>
/// Represents a page of orders together with the total number of matching records.
/// </summary>
/// <param name="Items">The orders in the current page.</param>
/// <param name="TotalCount">The total number of orders across all pages.</param>
public sealed record OrderPage(
    IReadOnlyCollection<Order> Items,
    int TotalCount);
