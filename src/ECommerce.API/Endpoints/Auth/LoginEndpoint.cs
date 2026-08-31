using ECommerce.API.Authentication;
using ECommerce.API.Contracts.Auth;

namespace ECommerce.API.Endpoints.Auth;

internal static class LoginEndpoint
{
    internal static IResult Handle(
        LoginRequest request,
        ITokenService tokenService)
    {
        if (!FixedUser.HasValidCredentials(request.Email, request.Password))
        {
            return Results.Unauthorized();
        }

        var (accessToken, expiresAt) = tokenService.GenerateToken(FixedUser.Email);
        var response = new LoginResponse(accessToken, expiresAt);

        return Results.Ok(response);
    }
}
