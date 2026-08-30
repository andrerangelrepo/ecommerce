using ECommerce.Application.Abstractions.Persistence;
using ECommerce.Application.Features.Orders.Queries.GetOrders;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace ECommerce.Application.Tests.Features.Orders.Queries.GetOrders;

/// <summary>
/// Tests the handler that fetches a paginated list of orders.
/// </summary>
public sealed class GetOrdersQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepository = new();

    /// <summary>CT-LIST-01: verifies all pagination fields when the page has records.</summary>
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

        result.Items.Should().HaveCount(10);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(21);
        result.TotalPages.Should().Be(3);
    }

    /// <summary>CT-LIST-02: verifies no orders at all produces an empty, zero-page result.</summary>
    [Fact]
    public async Task Handle_ShouldReturnZeroPages_WhenNoOrdersExist()
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

    /// <summary>CT-LIST-03: verifies TotalPages rounds up correctly for a partial last page.</summary>
    [Fact]
    public async Task Handle_ShouldRoundUpTotalPages_ForPartialLastPage()
    {
        var orders = new[] { CreateOrder() };

        _orderRepository
            .Setup(repository => repository.GetPageAsync(3, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPage(orders, TotalCount: 21));

        var handler = new GetOrdersQueryHandler(_orderRepository.Object);

        var result = await handler.Handle(
            new GetOrdersQuery(Page: 3, PageSize: 10),
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalPages.Should().Be(3);
    }

    /// <summary>CT-LIST-04: verifies a page beyond the total returns empty results, not an error.</summary>
    [Fact]
    public async Task Handle_ShouldReturnEmptyItems_WhenPageIsBeyondTotal()
    {
        _orderRepository
            .Setup(repository => repository.GetPageAsync(5, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderPage([], TotalCount: 15));

        var handler = new GetOrdersQueryHandler(_orderRepository.Object);

        var result = await handler.Handle(
            new GetOrdersQuery(Page: 5, PageSize: 10),
            CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(2);
    }

    /// <summary>CT-LIST-05: verifies the received cancellation token is forwarded to the repository.</summary>
    [Fact]
    public async Task Handle_ShouldForwardCancellationToken_ToRepository()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        _orderRepository
            .Setup(repository => repository.GetPageAsync(1, 10, cancellationToken))
            .ReturnsAsync(new OrderPage([], TotalCount: 0));

        var handler = new GetOrdersQueryHandler(_orderRepository.Object);

        await handler.Handle(new GetOrdersQuery(Page: 1, PageSize: 10), cancellationToken);

        _orderRepository.Verify(
            repository => repository.GetPageAsync(1, 10, cancellationToken),
            Times.Once);
    }

    private static Order CreateOrder()
    {
        return new Order(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [("Keyboard", 1, 100m)]);
    }
}
