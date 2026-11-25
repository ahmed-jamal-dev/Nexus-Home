namespace NexusHome.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using NexusHome.Application.Interfaces;

/// <summary>
/// Mock email service for development and testing.
/// Logs the email content instead of actually sending.
/// </summary>
public sealed class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<bool> SendMemberInvitationAsync(
        string email,
        string plainToken,
        string homeName,
        string role,
        string inviterName,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "MOCK EMAIL: Member invitation sent to {Email} for home {HomeName} as {Role} by {InviterName}. " +
            "Token: {Token}, Expires: {ExpiresAt}",
            email, homeName, role, inviterName, plainToken, expiresAt);

        // In a real implementation, this would send an actual email
        // For now, we just log and return success
        return Task.FromResult(true);
    }
}