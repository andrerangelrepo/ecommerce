using System.ComponentModel.DataAnnotations;

namespace ECommerce.API.Authentication;

/// <summary>
/// Represents the settings used to issue and validate JWT access tokens.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// The configuration section containing the JWT settings.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets the symmetric signing key.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the expected token issuer.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets the expected token audience.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// Gets the access token lifetime in minutes.
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Jwt:ExpirationMinutes must be greater than zero.")]
    public int ExpirationMinutes { get; init; }
}
