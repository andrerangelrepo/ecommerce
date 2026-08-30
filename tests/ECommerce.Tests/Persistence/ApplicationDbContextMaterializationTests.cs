using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ECommerce.Tests.Persistence;

/// <summary>
/// Tests persistence materialization of domain entities.
/// </summary>
public sealed class ApplicationDbContextMaterializationTests
{
    /// <summary>Verifies that an order and its items can be persisted and materialized.</summary>
    [Fact]
    public async Task Context_ShouldMaterializeOrderWithItems()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var order = new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [("Keyboard", 2, 150m)]);

        await using (var writeContext = new ApplicationDbContext(options))
        {
            await writeContext.Database.MigrateAsync();
            writeContext.Orders.Add(order);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new ApplicationDbContext(options);
        var persistedOrder = await readContext.Orders
            .Include(currentOrder => currentOrder.Items)
            .SingleAsync();

        persistedOrder.Should().BeEquivalentTo(order, options => options
            .Including(currentOrder => currentOrder.Id)
            .Including(currentOrder => currentOrder.CustomerId)
            .Including(currentOrder => currentOrder.Status)
            .Including(currentOrder => currentOrder.CreatedAt)
            .Including(currentOrder => currentOrder.TotalAmount));
        persistedOrder.Items.Should().ContainSingle();
        persistedOrder.Items.Single().OrderId.Should().Be(persistedOrder.Id);
    }
}
