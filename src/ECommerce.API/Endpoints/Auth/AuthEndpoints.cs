namespace ECommerce.API.Endpoints.Auth;

/// <summary>
/// Maps HTTP endpoints for authentication.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps all authentication endpoints under the <c>/auth</c> route group.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The same endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth");

        group.MapPost("/login", LoginEndpoint.Handle);

        return endpoints;
    }
}
