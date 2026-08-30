namespace ECommerce.API.Contracts.Auth;

/// <summary>
/// Represents the HTTP request to authenticate a user.
/// </summary>
/// <param name="Email">The user's email address.</param>
/// <param name="Password">The user's password.</param>
public sealed record LoginRequest(
    string Email,
    string Password);
