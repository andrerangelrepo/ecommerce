using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.API.Contracts.Auth;
using ECommerce.API.Contracts.Orders;
using ECommerce.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace ECommerce.IntegrationTests;

/// <summary>
/// Verifies <c>GET /api/orders</c> query-string binding failures are translated to
/// <c>400 Bad Request</c> by the <c>GlobalExceptionHandler</c>, rather than surfacing as
/// <c>500</c>. This is a regression test: a non-numeric <c>page</c>/<c>pageSize</c> throws
/// <c>BadHttpRequestException</c> during Minimal API model binding — before MediatR or
/// <c>ValidationBehavior</c> ever run — so no unit test of the Handler or Validator can
/// catch this; only an HTTP-level integration test exercises the real binding pipeline.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="GetOrdersIntegrationTests"/> class.</remarks>
/// <param name="factory">The API factory providing the in-process test server.</param>
public sealed class GetOrdersIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    /// <summary>A valid request returns 200 with the paginated shape, even with no orders yet.</summary>
    [Fact]
    public async Task GetOrders_ShouldReturnOk_WithValidPagination()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/api/orders?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOrdersResponse>();
        body!.Page.Should().Be(1);
        body.PageSize.Should().Be(10);
    }

    /// <summary>
    /// Unlike the pagination-shape test above, this creates a real order first — the only
    /// test that exercises the list endpoint's own item mapping (<c>Order</c> →
    /// <c>OrderListItemResponse</c>) with actual data. A handler-level unit test can't
    /// catch a bug in that specific mapping since it never reaches the API layer, and the
    /// empty-list test never has an item to map.
    /// </summary>
    [Fact]
    public async Task GetOrders_ShouldReturnCreatedOrder_WhenOneExists()
    {
        await AuthenticateAsync();

        var customerId = Guid.NewGuid();
        var createResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(
                customerId,
                [new CreateOrderItemRequest("Monitor", 2, 500m)]));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        var response = await _client.GetAsync("/api/orders?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOrdersResponse>();
        body!.Items.Should().Contain(item =>
            item.Id == created!.Id
            && item.CustomerId == customerId
            && item.Status == "Pending"
            && item.TotalAmount == 1000m);
    }

    /// <summary>A non-numeric <c>page</c> must return 400, not 500.</summary>
    [Fact]
    public async Task GetOrders_ShouldReturnBadRequest_WhenPageIsNotNumeric()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/api/orders?page=abc&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    /// <summary>A non-numeric <c>pageSize</c> must return 400, not 500.</summary>
    [Fact]
    public async Task GetOrders_ShouldReturnBadRequest_WhenPageSizeIsNotNumeric()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/api/orders?page=1&pageSize=abc");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be((int)HttpStatusCode.BadRequest);
    }

    private async Task AuthenticateAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("dev@martech.com", "Senha@123"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }
}
