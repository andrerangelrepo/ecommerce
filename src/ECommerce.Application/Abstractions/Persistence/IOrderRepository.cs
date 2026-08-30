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

    /// <summary>
    /// Retrieves an order by its identifier.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The order, or <see langword="null"/> when not found.</returns>
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a page of orders together with the total number of matching records.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The number of orders per page.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The requested page of orders.</returns>
    Task<OrderPage> GetPageAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
