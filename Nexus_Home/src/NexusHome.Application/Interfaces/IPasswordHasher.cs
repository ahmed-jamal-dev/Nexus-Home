namespace NexusHome.Application.Interfaces;

/// <summary>
/// Service for password hashing and verification.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plaintext password against a hash.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}