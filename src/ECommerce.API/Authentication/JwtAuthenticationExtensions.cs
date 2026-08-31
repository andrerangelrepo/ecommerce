using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.API.Authentication;

/// <summary>
/// Provides dependency injection registration for JWT authentication.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Registers and configures JWT bearer authentication and authorization.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>Jwt</c> configuration section is missing or invalid.
    /// </exception>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetRequiredSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is invalid.");

        var jwtValidationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(
                jwtOptions,
                new ValidationContext(jwtOptions),
                jwtValidationResults,
                validateAllProperties: true))
        {
            var errors = string.Join("; ", jwtValidationResults.Select(result => result.ErrorMessage));
            throw new InvalidOperationException($"Invalid JWT configuration: {errors}");
        }

        services.Configure<JwtOptions>(jwtSection);
        services.AddSingleton<ITokenService, JwtTokenService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Key)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }
}
