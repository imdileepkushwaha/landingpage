namespace SoftflipSolutions.Services;

public static class PasswordHelper
{
    public static bool LooksHashed(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        (value.StartsWith("$2a$") || value.StartsWith("$2b$") || value.StartsWith("$2y$"));

    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public static bool Verify(string password, string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return false;
        if (LooksHashed(stored))
            return BCrypt.Net.BCrypt.Verify(password, stored);
        return string.Equals(stored, password, StringComparison.Ordinal);
    }

    /// <summary>Verify and upgrade plaintext to BCrypt when needed.</summary>
    public static bool VerifyAndUpgrade(string password, ref string? stored, out bool upgraded)
    {
        upgraded = false;
        if (!Verify(password, stored)) return false;
        if (!LooksHashed(stored))
        {
            stored = Hash(password);
            upgraded = true;
        }
        return true;
    }
}
