using System.Security.Cryptography;

namespace UTMPro.Data.Helpers;

public static class IdGenerator
{
    private const string AlphaNumeric =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string NewExternalId(string prefix)
    {
        return prefix + GenerateRandom(20);
    }

    public static string NewSlug(int length = 7)
    {
        return GenerateRandom(length);
    }

    public static string GenerateRandom(int length)
    {
        return string.Create(length, (object?)null, (span, _) =>
        {
            for (int i = 0; i < span.Length; i++)
                span[i] = AlphaNumeric[RandomNumberGenerator.GetInt32(AlphaNumeric.Length)];
        });
    }

    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
