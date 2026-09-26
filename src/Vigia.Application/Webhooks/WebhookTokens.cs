using System.Security.Cryptography;
using System.Text;

namespace Vigia.Application.Webhooks;

/// <summary>
/// Generates and verifies webhook tokens. Only the SHA-256 hash is stored.
/// </summary>
public static class WebhookTokens
{
    /// <summary>Creates a random 256-bit token (URL-safe) and its hash.</summary>
    public static (string Token, string Hash) Create()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (token, Hash(token));
    }

    /// <summary>Hex SHA-256 of a token.</summary>
    public static string Hash(string token)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    /// <summary>Whether <paramref name="token"/> matches <paramref name="hash"/>, in constant time.</summary>
    public static bool Matches(string? token, string? hash)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(hash))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(token)), Encoding.ASCII.GetBytes(hash));
    }
}
