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
/// Verifies <c>POST /api/orders</c> end to end. This single endpoint is chosen as the
/// primary integration scenario because it exercises the whole stack in one request:
/// API routing, JWT authentication, MediatR dispatch, <c>ValidationBehavior</c>, the
/// handler, the domain's own invariants and calculations, the repository, EF Core, and
/// a real SQLite database.
/// </summary>
public sealed class CreateOrderIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of the <see cref="CreateOrderIntegrationTests"/> class.</summary>
    /// <param name="factory">The API factory providing the in-process test server.</param>
    public CreateOrderIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>Proves the API/JWT layer: the endpoint rejects requests without a token.</summary>
    [Fact]
    public async Task CreateOrder_ShouldReturnUnauthorized_WithoutToken()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(
                Guid.NewGuid(),
                [new CreateOrderItemRequest("Keyboard", 1, 100m)]));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Proves the full validation pipeline — API → MediatR → <c>ValidationBehavior</c> →
    /// FluentValidation → <c>ProblemDetails</c> — with an authenticated but invalid payload
    /// (empty <c>customerId</c> and no items), rejected before ever reaching the handler.
    /// </summary>
    [Fact]
    public async Task CreateOrder_ShouldReturnValidationError_WhenPayloadIsInvalid()
    {
        await AuthenticateAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(Guid.Empty, []));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("CustomerId");
        problem.Errors.Should().ContainKey("Items");
    }

    /// <summary>
    /// Proves the full stack — Handler, Domain, Repository, EF Core, SQLite — by creating
    /// a multi-item order and then reading it back through a separate request. The
    /// re-read only succeeds if the order was genuinely persisted, and its values only
    /// match if the domain (not the API or the test) computed <c>TotalAmount</c> and each
    /// item's <c>TotalPrice</c>.
    /// </summary>
    [Fact]
    public async Task CreateOrder_ShouldPersistOrder_ComputedByTheDomain()
    {
        await AuthenticateAsync();

        var customerId = Guid.Parse("fa36c046-b6ba-4913-8904-d6dd524a0abb");
        var createResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(
                customerId,
                [
                    new CreateOrderItemRequest("Keyboard", 2, 150m),
                    new CreateOrderItemRequest("Mouse", 1, 100m)
                ]));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();
        created!.Id.Should().NotBe(Guid.Empty);
        created.CustomerId.Should().Be(customerId);
        created.Status.Should().Be("Pending");
        created.TotalAmount.Should().Be(400m);
        createResponse.Headers.Location!.ToString().Should().Be($"/api/orders/{created.Id}");

        var getResponse = await _client.GetAsync($"/api/orders/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var persisted = await getResponse.Content.ReadFromJsonAsync<GetOrderByIdResponse>();

        persisted.Should().NotBeNull();
        persisted!.Id.Should().Be(created.Id);
        persisted.CustomerId.Should().Be(customerId);
        persisted.Status.Should().Be("Pending");
        persisted.TotalAmount.Should().Be(400m);
        persisted.Items.Should().HaveCount(2);
        persisted.Items.Should().Contain(item =>
            item.ProductName == "Keyboard" && item.Quantity == 2 && item.TotalPrice == 300m);
        persisted.Items.Should().Contain(item =>
            item.ProductName == "Mouse" && item.Quantity == 1 && item.TotalPrice == 100m);
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
