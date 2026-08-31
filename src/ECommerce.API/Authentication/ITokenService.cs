namespace ECommerce.API.Authentication;

/// <summary>
/// Issues access tokens for authenticated users.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates an access token for the specified user.
    /// </summary>
    /// <param name="email">The authenticated user's email address.</param>
    /// <returns>The encoded access token and its UTC expiration date.</returns>
    (string AccessToken, DateTime ExpiresAt) GenerateToken(string email);
}
