namespace ECommerce.API.Contracts.Auth;

/// <summary>
/// Represents the HTTP response returned after successful authentication.
/// </summary>
/// <param name="AccessToken">The JWT access token.</param>
/// <param name="ExpiresAt">The date and time when the access token expires.</param>
public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt);
