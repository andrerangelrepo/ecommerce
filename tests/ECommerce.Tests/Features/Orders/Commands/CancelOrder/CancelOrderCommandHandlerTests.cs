using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Features.Orders.Commands.CancelOrder;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace ECommerce.Tests.Features.Orders.Commands.CancelOrder;

/// <summary>
/// Tests the handler that cancels an order.
/// </summary>
public sealed class CancelOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();

    /// <summary>CT-H01: verifies a pending order is cancelled, persisted, and the token is forwarded.</summary>
    [Fact]
    public async Task Handle_ShouldCancelAndPersist_WhenOrderIsPending()
    {
        var order = CreateOrder();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _orderRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(order.Id, cancellationToken))
            .ReturnsAsync(order);

        var handler = new CancelOrderCommandHandler(_orderRepository.Object);

        var result = await handler.Handle(new CancelOrderCommand(order.Id), cancellationToken);

        order.Status.Should().Be(OrderStatus.Cancelled);
        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.Status.Should().Be(OrderStatus.Cancelled);

        _orderRepository.Verify(
            repository => repository.GetByIdForUpdateAsync(order.Id, cancellationToken),
            Times.Once);
        _orderRepository.Verify(
            repository => repository.UpdateAsync(order, cancellationToken),
            Times.Once);
    }

    /// <summary>CT-H02: verifies a missing order returns null without persisting anything.</summary>
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        _orderRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = new CancelOrderCommandHandler(_orderRepository.Object);

        var result = await handler.Handle(new CancelOrderCommand(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();

        _orderRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>CT-H03: verifies an already cancelled order is rejected without persisting anything.</summary>
    [Fact]
    public async Task Handle_ShouldThrowAndNotPersist_WhenOrderIsAlreadyCancelled()
    {
        var order = CreateOrder();
        order.Cancel();

        _orderRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new CancelOrderCommandHandler(_orderRepository.Object);

        var act = () => handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<OrderCannotBeCancelledException>();

        _orderRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>CT-H03: verifies a confirmed order is rejected without persisting anything.</summary>
    [Fact]
    public async Task Handle_ShouldThrowAndNotPersist_WhenOrderIsConfirmed()
    {
        var order = CreateOrder();
        SetStatus(order, OrderStatus.Confirmed);

        _orderRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new CancelOrderCommandHandler(_orderRepository.Object);

        var act = () => handler.Handle(new CancelOrderCommand(order.Id), CancellationToken.None);

        await act.Should().ThrowAsync<OrderCannotBeCancelledException>();

        _orderRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Order CreateOrder()
    {
        return new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [("Keyboard", 1, 100m)]);
    }

    // No public API transitions an order to Confirmed yet, so the state is set
    // via reflection to exercise the rejection path against a status it must reject.
    private static void SetStatus(Order order, OrderStatus status)
    {
        typeof(Order)
            .GetProperty(nameof(Order.Status))!
            .SetValue(order, status);
    }
}
