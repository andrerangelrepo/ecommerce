namespace ECommerce.API.Authentication;

internal static class FixedUser
{
    internal const string Email = "dev@martech.com";

    private const string Password = "Senha@123";

    internal static bool HasValidCredentials(string? email, string? password)
    {
        return string.Equals(email, Email, StringComparison.OrdinalIgnoreCase)
            && string.Equals(password, Password, StringComparison.Ordinal);
    }
}
