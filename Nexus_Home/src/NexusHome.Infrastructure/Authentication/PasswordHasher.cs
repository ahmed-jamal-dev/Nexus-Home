namespace NexusHome.Infrastructure.Authentication;

using BCrypt.Net;
using NexusHome.Application.Interfaces;

/// <summary>
/// BCrypt implementation of password hashing.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    /// <inheritdoc />
    public string HashPassword(string password)
    {
        // BCrypt automatically generates a salt and includes it in the hash
        // Work factor 12 is a good balance of security and performance
        return BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <inheritdoc />
    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash))
        {
            return false;
        }

        try
        {
            return BCrypt.Verify(password, passwordHash);
        }
        catch (SaltParseException)
        {
            // Invalid hash format
            return false;
        }
    }
}