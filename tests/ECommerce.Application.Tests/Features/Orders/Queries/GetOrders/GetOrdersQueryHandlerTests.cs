using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Features.Orders.Queries.GetOrders;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace ECommerce.Tests.Features.Orders.Queries.GetOrders;

/// <summary>
/// Tests the handler that fetches a paginated list of orders.
/// </summary>
public sealed class GetOrdersQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();

    /// <summary>CT-H04: verifies pagination fields when the page has records.</summary>
    [Fact]
    public async Task Handle_ShouldReturnPageWithRecords_WhenTotalCountExceedsPageSize()
    {
        var orders = Enumerable.Range(1, 10)
            .Select(_ => CreateOrder())
            .ToArray();

        _orderRepository
            .Setup(repository => repository.GetPageAsync(2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPage(orders, TotalCount: 21));

        var handler = new GetOrdersQueryHandler(_orderRepository.Object);

        var result = await handler.Handle(
            new GetOrdersQuery(Page: 2, PageSize: 10),
            CancellationToken.None);

        result.TotalCount.Should().Be(21);
        result.TotalPages.Should().Be(3);
        result.Items.Should().HaveCount(10);
    }

    /// <summary>CT-H05: verifies an empty page (within bounds) is returned without error.</summary>
    [Fact]
    public async Task Handle_ShouldReturnEmptyItems_WhenPageHasNoRecords()
    {
        _orderRepository
            .Setup(repository => repository.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPage([], TotalCount: 5));

        var handler = new GetOrdersQueryHandler(_orderRepository.Object);

        var result = await handler.Handle(
            new GetOrdersQuery(Page: 5, PageSize: 10),
            CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(5);
    }

    /// <summary>CT-H06: verifies a zero total count produces zero pages, not an error.</summary>
    [Fact]
    public async Task Handle_ShouldReturnZeroPages_WhenTotalCountIsZero()
    {
        _orderRepository
            .Setup(repository => repository.GetPageAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPage([], TotalCount: 0));

        var handler = new GetOrdersQueryHandler(_orderRepository.Object);

        var result = await handler.Handle(
            new GetOrdersQuery(Page: 1, PageSize: 10),
            CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    private static Order CreateOrder()
    {
        return new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [("Keyboard", 1, 100m)]);
    }
}
