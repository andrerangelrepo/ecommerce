using System.Reflection;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace ECommerce.Application.Tests.Domain.Entities;

/// <summary>
/// Tests the invariants and cancellation behavior of <see cref="Order"/>.
/// </summary>
public sealed class OrderTests
{
    /// <summary>CT-DOMAIN-01: verifies an order cannot be created without items.</summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenNoItemsProvided()
    {
        var act = () => new Order(Guid.NewGuid(), Guid.NewGuid(), []);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>CT-DOMAIN-02: verifies an item with zero quantity is rejected.</summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenItemQuantityIsZero()
    {
        var act = () => new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [("Keyboard", 0, 100m)]);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>CT-DOMAIN-03: verifies an item with negative quantity is rejected.</summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenItemQuantityIsNegative()
    {
        var act = () => new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [("Keyboard", -1, 100m)]);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>CT-DOMAIN-04: verifies an item with zero unit price is rejected.</summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenItemUnitPriceIsZero()
    {
        var act = () => new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [("Keyboard", 1, 0m)]);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>CT-DOMAIN-05: verifies an item with negative unit price is rejected.</summary>
    [Fact]
    public void Constructor_ShouldThrow_WhenItemUnitPriceIsNegative()
    {
        var act = () => new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [("Keyboard", 1, -10m)]);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>CT-DOMAIN-06: verifies TotalAmount sums UnitPrice * Quantity across all items.</summary>
    [Fact]
    public void TotalAmount_ShouldSumUnitPriceTimesQuantity_AcrossItems()
    {
        var order = new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [("Item A", 2, 10m), ("Item B", 3, 5m)]);

        order.TotalAmount.Should().Be(35m);
    }

    /// <summary>CT-DOMAIN-07: verifies a newly created order starts as Pending.</summary>
    [Fact]
    public void Constructor_ShouldSetStatusToPending()
    {
        var order = CreateOrder();

        order.Status.Should().Be(OrderStatus.Pending);
    }

    /// <summary>CT-DOMAIN-08: verifies that a pending order transitions to cancelled.</summary>
    [Fact]
    public void Cancel_ShouldTransitionToCancelled_WhenOrderIsPending()
    {
        var order = CreateOrder();

        order.Cancel();

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    /// <summary>CT-DOMAIN-09: verifies that an already cancelled order rejects a second cancellation.</summary>
    [Fact]
    public void Cancel_ShouldThrow_WhenOrderIsAlreadyCancelled()
    {
        var order = CreateOrder();
        order.Cancel();

        var act = () => order.Cancel();

        act.Should().Throw<OrderCannotBeCancelledException>();
    }

    /// <summary>CT-DOMAIN-10: verifies that a confirmed order rejects cancellation.</summary>
    [Fact]
    public void Cancel_ShouldThrow_WhenOrderIsConfirmed()
    {
        var order = CreateOrder();
        SetStatus(order, OrderStatus.Confirmed);

        var act = () => order.Cancel();

        act.Should().Throw<OrderCannotBeCancelledException>();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    private static Order CreateOrder()
    {
        return new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [("Keyboard", 1, 100m)]);
    }

    // No public API transitions an order to Confirmed yet, so the state is set
    // via reflection to exercise the guard clause against a status it must reject.
    private static void SetStatus(Order order, OrderStatus status)
    {
        typeof(Order)
            .GetProperty(nameof(Order.Status))!
            .SetValue(order, status);
    }
}
