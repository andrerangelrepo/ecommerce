namespace ECommerce.API.Authentication;

/// <summary>
/// The single in-memory user this API authenticates. A static analyzer may flag
/// <see cref="Password"/> as a hardcoded credential — it is one, deliberately: the
/// technical test this project implements (<c>INSTRUCOES.md</c>) specifies this exact
/// email/password pair as the required login, and states "usuário fixo em memória é
/// suficiente". It is not a leaked real secret; there is no external system, account,
/// or production environment it grants access to.
/// </summary>
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
