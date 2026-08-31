using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ECommerce.API.Contracts.Auth;
using ECommerce.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ECommerce.IntegrationTests;

/// <summary>
/// Verifies the HTTP-level not-found mapping for <c>GET /api/orders/{id}</c>.
/// </summary>
public sealed class GetOrderByIdIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of the <see cref="GetOrderByIdIntegrationTests"/> class.</summary>
    /// <param name="factory">The API factory providing the in-process test server.</param>
    public GetOrderByIdIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>
    /// The endpoint returns null from the handler when no order matches the id; this confirms
    /// that null is translated into an actual HTTP 404, not just asserted at the handler level.
    /// </summary>
    [Fact]
    public async Task GetOrderById_ShouldReturnNotFound_WhenOrderDoesNotExist()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
