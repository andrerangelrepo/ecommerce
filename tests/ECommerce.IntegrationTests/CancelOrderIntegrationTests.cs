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
/// Verifies that cancelling an order through the HTTP API persists the change,
/// not just the in-memory result of a single request.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="CancelOrderIntegrationTests"/> class.</remarks>
/// <param name="factory">The API factory providing the in-process test server.</param>
public sealed class CancelOrderIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    /// <summary>
    /// Creates an order, confirms it starts <c>Pending</c>, cancels it, and confirms a
    /// subsequent read reflects <c>Cancelled</c> — proving the change was persisted.
    /// </summary>
    [Fact]
    public async Task CancelOrder_ShouldPersistCancellation_AcrossSeparateRequests()
    {
        await AuthenticateAsync();
        var orderId = await CreateOrderAsync();

        var beforeCancel = await _client.GetFromJsonAsync<GetOrderByIdResponse>(
            $"/api/orders/{orderId}");
        beforeCancel!.Status.Should().Be("Pending");

        var cancelResponse = await _client.PatchAsync($"/api/orders/{orderId}/cancel", content: null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<CancelOrderResponse>();
        cancelled!.Status.Should().Be("Cancelled");

        var afterCancel = await _client.GetFromJsonAsync<GetOrderByIdResponse>(
            $"/api/orders/{orderId}");
        afterCancel!.Status.Should().Be("Cancelled");
    }

    /// <summary>
    /// Repeats the exact same <c>PATCH .../cancel</c> request against an order that was
    /// already cancelled by the first call. The endpoint is not idempotent: the second
    /// call must produce a different result (409 Conflict) instead of the first call's
    /// 200 OK, and the order's persisted status must remain unchanged by the rejection.
    /// </summary>
    [Fact]
    public async Task CancelOrder_ShouldReturnConflict_WhenTheSameCancelRequestIsRepeated()
    {
        await AuthenticateAsync();
        var orderId = await CreateOrderAsync();

        var firstAttempt = await _client.PatchAsync($"/api/orders/{orderId}/cancel", content: null);
        firstAttempt.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondAttempt = await _client.PatchAsync($"/api/orders/{orderId}/cancel", content: null);

        secondAttempt.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await secondAttempt.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be((int)HttpStatusCode.Conflict);
        problem.Title.Should().Be("Order cannot be cancelled");

        var orderAfterSecondAttempt = await _client.GetFromJsonAsync<GetOrderByIdResponse>(
            $"/api/orders/{orderId}");
        orderAfterSecondAttempt!.Status.Should().Be("Cancelled");
    }

    /// <summary>Cancelling an id that matches no order returns 404, not a conflict or a crash.</summary>
    [Fact]
    public async Task CancelOrder_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        await AuthenticateAsync();

        var response = await _client.PatchAsync($"/api/orders/{Guid.NewGuid()}/cancel", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CreateOrderAsync()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest(
                Guid.NewGuid(),
                [new CreateOrderItemRequest("Keyboard", 1, 100m)]));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        return created!.Id;
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
