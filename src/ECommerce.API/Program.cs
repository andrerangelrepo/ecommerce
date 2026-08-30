using System.ComponentModel.DataAnnotations;
using System.Text;
using ECommerce.API.Authentication;
using ECommerce.API.Endpoints.Auth;
using ECommerce.API.Endpoints.Orders;
using ECommerce.API.ExceptionHandling;
using ECommerce.API.OpenApi;
using ECommerce.Application;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetRequiredSection(JwtOptions.SectionName);
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

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<BearerSecurityRequirementOperationTransformer>();
});
builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services
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
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

await app.Services.ApplyMigrationsAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/openapi/v1.json", "ECommerce API v1"));
}

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapOrderEndpoints();

app.Run();

/// <summary>
/// Exposes the entry point for <c>WebApplicationFactory&lt;Program&gt;</c> in integration tests.
/// </summary>
public partial class Program;
