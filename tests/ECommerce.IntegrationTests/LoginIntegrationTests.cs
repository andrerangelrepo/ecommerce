using System.Net;
using System.Net.Http.Json;
using ECommerce.API.Contracts.Auth;
using ECommerce.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ECommerce.IntegrationTests;

/// <summary>
/// Verifies <c>POST /auth/login</c> status codes directly. Every other integration test
/// file logs in as a setup step, but none of them asserts the login endpoint's own
/// behavior for invalid credentials — that scenario has no coverage anywhere else.
/// </summary>
public sealed class LoginIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    /// <summary>Initializes a new instance of the <see cref="LoginIntegrationTests"/> class.</summary>
    /// <param name="factory">The API factory providing the in-process test server.</param>
    public LoginIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    /// <summary>The fixed user's real credentials must authenticate successfully.</summary>
    [Fact]
    public async Task Login_ShouldReturnOk_WithValidCredentials()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("dev@martech.com", "Senha@123"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        login!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>A wrong password for the fixed user must be rejected, not silently accepted.</summary>
    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WithInvalidCredentials()
    {
        var response = await _client.PostAsJsonAsync(
            "/auth/login",
            new LoginRequest("dev@martech.com", "wrong-password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
