using ECommerce.Application.Features.Orders.Queries.GetOrders;
using FluentAssertions;
using Xunit;

namespace ECommerce.Application.Tests.Features.Orders.Queries.GetOrders;

/// <summary>
/// Tests validation of the paginated order list input.
/// </summary>
public sealed class GetOrdersQueryValidatorTests
{
    private readonly GetOrdersQueryValidator _validator = new();

    /// <summary>Verifies that a non-positive page is rejected.</summary>
    /// <param name="page">The invalid page.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateAsync_ShouldRejectNonPositivePage(int page)
    {
        var query = new GetOrdersQuery(page, 10);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(GetOrdersQuery.Page));
    }

    /// <summary>Verifies that a non-positive page size is rejected.</summary>
    /// <param name="pageSize">The invalid page size.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateAsync_ShouldRejectNonPositivePageSize(int pageSize)
    {
        var query = new GetOrdersQuery(1, pageSize);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(GetOrdersQuery.PageSize));
    }

    /// <summary>Verifies that a valid payload is accepted.</summary>
    [Fact]
    public async Task ValidateAsync_ShouldAcceptValidPayload()
    {
        var query = new GetOrdersQuery(1, 10);

        var result = await _validator.ValidateAsync(query);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
