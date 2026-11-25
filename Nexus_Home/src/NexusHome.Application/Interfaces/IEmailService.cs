namespace NexusHome.Application.Interfaces;

/// <summary>
/// Service for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a member invitation email.
    /// </summary>
    /// <param name="email">Recipient email address</param>
    /// <param name="plainToken">Plaintext invitation token (Base64URL encoded)</param>
    /// <param name="homeName">Name of the home</param>
    /// <param name="role">Invited role (Member or Guest)</param>
    /// <param name="inviterName">Name of the user who sent the invitation</param>
    /// <param name="expiresAt">Expiration timestamp</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email was sent (or mocked), false if failed</returns>
    Task<bool> SendMemberInvitationAsync(
        string email,
        string plainToken,
        string homeName,
        string role,
        string inviterName,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
}