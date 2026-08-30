using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Domain.Entities;

namespace ECommerce.Infrastructure.Persistence.Repositories;

/// <summary>
/// Persists orders using Entity Framework Core.
/// </summary>
/// <param name="context">The application database context.</param>
public sealed class OrderRepository(ApplicationDbContext context)
    : IOrderRepository
{
    /// <inheritdoc />
    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        await context.Orders.AddAsync(order, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
