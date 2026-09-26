using System.Security.Cryptography;
using System.Text;

namespace Vigia.Application.Common.Security;

/// <summary>
/// Random secrets (webhook tokens, worker enrollment tokens and credentials). Only the SHA-256 hash is stored.
/// </summary>
public static class SecretTokens
{
    /// <summary>Creates a random 256-bit URL-safe secret, with an optional recognizable prefix, and its hash.</summary>
    /// <param name="prefix">For example <c>vw_</c>, so leaked secrets are easy to identify and scan for.</param>
    public static (string Token, string Hash) Create(string prefix = "")
    {
        var token = prefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (token, Hash(token));
    }

    /// <summary>Hex SHA-256 of a secret.</summary>
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
