using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Application.Features.Orders.Queries.GetOrderById;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace ECommerce.Application.Tests.Features.Orders.Queries.GetOrderById;

/// <summary>
/// Tests the handler that fetches an order by its identifier.
/// </summary>
public sealed class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();

    /// <summary>CT-GET-ID-01: verifies the mapped result when the order exists.</summary>
    [Fact]
    public async Task Handle_ShouldReturnMappedResult_WhenOrderExists()
    {
        var order = CreateOrder();

        _orderRepository
            .Setup(repository => repository.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new GetOrderByIdQueryHandler(_orderRepository.Object);

        var result = await handler.Handle(
            new GetOrderByIdQuery(order.Id),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.CustomerId.Should().Be(order.CustomerId);
        result.Status.Should().Be(order.Status);
        result.CreatedAt.Should().Be(order.CreatedAt);
        result.TotalAmount.Should().Be(order.TotalAmount);
        result.Items.Should().BeEquivalentTo(order.Items.Select(item =>
            new GetOrderItemResult(
                item.Id,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.TotalPrice)));
    }

    /// <summary>
    /// CT-GET-ID-02: verifies <see langword="null"/> is returned when the order does not exist,
    /// without throwing.
    /// </summary>
    [Fact]
    public async Task Handle_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        _orderRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = new GetOrderByIdQueryHandler(_orderRepository.Object);

        var act = () => handler.Handle(
            new GetOrderByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        var result = await act.Should().NotThrowAsync();
        result.Subject.Should().BeNull();
    }

    /// <summary>CT-GET-ID-03: verifies the received cancellation token is forwarded to the repository.</summary>
    [Fact]
    public async Task Handle_ShouldForwardCancellationToken_ToRepository()
    {
        var order = CreateOrder();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _orderRepository
            .Setup(repository => repository.GetByIdAsync(order.Id, cancellationToken))
            .ReturnsAsync(order);

        var handler = new GetOrderByIdQueryHandler(_orderRepository.Object);

        await handler.Handle(new GetOrderByIdQuery(order.Id), cancellationToken);

        _orderRepository.Verify(
            repository => repository.GetByIdAsync(order.Id, cancellationToken),
            Times.Once);
    }

    private static Order CreateOrder()
    {
        return new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [("Keyboard", 2, 100m), ("Mouse", 1, 50m)]);
    }
}
