using System.Reflection;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace ECommerce.Tests.Domain.Entities;

/// <summary>
/// Tests the cancellation behavior of <see cref="Order"/>.
/// </summary>
public sealed class OrderTests
{
    /// <summary>Verifies that a pending order transitions to cancelled.</summary>
    [Fact]
    public void Cancel_ShouldTransitionToCancelled_WhenOrderIsPending()
    {
        var order = CreateOrder();

        order.Cancel();

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    /// <summary>Verifies that an already cancelled order rejects a second cancellation.</summary>
    [Fact]
    public void Cancel_ShouldThrow_WhenOrderIsAlreadyCancelled()
    {
        var order = CreateOrder();
        order.Cancel();

        var act = () => order.Cancel();

        act.Should().Throw<OrderCannotBeCancelledException>();
    }

    /// <summary>Verifies that a confirmed order rejects cancellation.</summary>
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
