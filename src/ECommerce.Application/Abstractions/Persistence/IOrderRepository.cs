using ECommerce.Domain.Entities;

namespace ECommerce.Application.Abstractions.Persistence;

/// <summary>
/// Defines the persistence operations required for orders.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Adds an order to the persistence store.
    /// </summary>
    /// <param name="order">The order to add.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);
}
