using System;
using System.Security.Cryptography;
using Sonic.Application.Auth;
using Sonic.Application.Auth.interfaces;

namespace Sonic.Infrastructure.Auth;

/// <summary>
/// PBKDF2-based password hasher using Rfc2898DeriveBytes.Pbkdf2 (non-obsolete).
/// Format: v1:iterations:saltBase64:hashBase64
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;       // 128-bit
    private const int KeySize = 32;       // 256-bit (bytes)
    private const int Iterations = 100_000;
    private const string FormatMarker = "v1";

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        var salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);

        var key = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: Iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: KeySize);

        var saltBase64 = Convert.ToBase64String(salt);
        var keyBase64 = Convert.ToBase64String(key);

        return $"{FormatMarker}:{Iterations}:{saltBase64}:{keyBase64}";
    }

    public bool Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        var parts = passwordHash.Split(':', 4, StringSplitOptions.None);
        if (parts.Length != 4)
        {
            return false;
        }

        if (!string.Equals(parts[0], FormatMarker, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] storedKey;

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            storedKey = Convert.FromBase64String(parts[3]);
        }
        catch
        {
            return false;
        }

        var computedKey = Rfc2898DeriveBytes.Pbkdf2(
            password: password,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: storedKey.Length);

        return CryptographicOperations.FixedTimeEquals(storedKey, computedKey);
    }
}
