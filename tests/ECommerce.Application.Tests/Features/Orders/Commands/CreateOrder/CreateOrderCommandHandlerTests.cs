using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Features.Orders.Commands.CreateOrder;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace ECommerce.Application.Tests.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Tests the handler that creates an order.
/// </summary>
public sealed class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();

    /// <summary>CT-CREATE-01: verifies a valid command produces a persisted, correctly-shaped order.</summary>
    [Fact]
    public async Task Handle_ShouldCreateOrder_WhenCommandIsValid()
    {
        Order? persistedOrder = null;
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _orderRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Order>(), cancellationToken))
            .Callback<Order, CancellationToken>((order, _) => persistedOrder = order)
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [
                new CreateOrderItemCommand("Keyboard", 2, 100m),
                new CreateOrderItemCommand("Mouse", 1, 50m)
            ]);

        var handler = new CreateOrderCommandHandler(_orderRepository.Object);

        var result = await handler.Handle(command, cancellationToken);

        persistedOrder.Should().NotBeNull();
        persistedOrder!.Status.Should().Be(OrderStatus.Pending);
        persistedOrder.CustomerId.Should().Be(command.CustomerId);
        persistedOrder.Items.Should().HaveCount(command.Items.Count);
        persistedOrder.Items.Should().OnlyContain(item => item.OrderId == persistedOrder.Id);
        persistedOrder.TotalAmount.Should().Be(250m);

        _orderRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Order>(), cancellationToken),
            Times.Once);

        result.Id.Should().Be(persistedOrder.Id);
        result.Id.Should().NotBe(Guid.Empty);
    }

    /// <summary>
    /// CT-CREATE-02: verifies <c>TotalAmount</c> reflects the domain's own calculation
    /// (<c>Order.TotalAmount</c>), not a value the handler recomputes independently.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnTotalAmount_CalculatedByDomain()
    {
        Order? persistedOrder = null;
        _orderRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => persistedOrder = order)
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [
                new CreateOrderItemCommand("Item A", 2, 10m),
                new CreateOrderItemCommand("Item B", 3, 5m)
            ]);

        var handler = new CreateOrderCommandHandler(_orderRepository.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        persistedOrder!.TotalAmount.Should().Be(35m);
        result.TotalAmount.Should().Be(persistedOrder.TotalAmount);
    }

    /// <summary>
    /// CT-CREATE-03: captures the entity actually sent to <see cref="IOrderRepository.AddAsync"/>
    /// and inspects its shape, rather than only asserting that the call happened.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldSendFullyFormedEntity_ToRepository()
    {
        Order? capturedOrder = null;
        _orderRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [
                new CreateOrderItemCommand("Keyboard", 1, 100m),
                new CreateOrderItemCommand("Mouse", 1, 50m),
                new CreateOrderItemCommand("Headset", 1, 200m)
            ]);

        var handler = new CreateOrderCommandHandler(_orderRepository.Object);

        await handler.Handle(command, CancellationToken.None);

        capturedOrder.Should().NotBeNull();
        capturedOrder!.Id.Should().NotBe(Guid.Empty);
        capturedOrder.CustomerId.Should().Be(command.CustomerId);
        capturedOrder.Status.Should().Be(OrderStatus.Pending);
        capturedOrder.Items.Should().HaveCount(3);
    }

    /// <summary>CT-CREATE-04: verifies the received cancellation token is forwarded to the repository.</summary>
    [Fact]
    public async Task Handle_ShouldForwardCancellationToken_ToRepository()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _orderRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Order>(), cancellationToken))
            .Returns(Task.CompletedTask);

        var command = new CreateOrderCommand(
            Guid.NewGuid(),
            [new CreateOrderItemCommand("Keyboard", 1, 100m)]);

        var handler = new CreateOrderCommandHandler(_orderRepository.Object);

        await handler.Handle(command, cancellationToken);

        _orderRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Order>(), cancellationToken),
            Times.Once);
    }
}
