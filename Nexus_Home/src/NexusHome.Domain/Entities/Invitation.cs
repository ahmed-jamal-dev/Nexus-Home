namespace NexusHome.Domain.Entities;

using NexusHome.Domain.Enums;

/// <summary>
/// Home membership invitation.
/// Per DATABASE_MVP_SUMMARY.md and v1.3 baseline Section 6.2.7.
/// Role allows 'owner', 'member', 'guest'.
/// Business logic should restrict Owner invitations (I-05 unresolved).
/// </summary>
public class Invitation : Entity
{
    /// <summary>
    /// Home ID.
    /// </summary>
    public Guid HomeId { get; private set; }

    /// <summary>
    /// Invitee email address.
    /// </summary>
    public string InviteeEmail { get; private set; } = string.Empty;

    /// <summary>
    /// Proposed role (owner, member, guest).
    /// </summary>
    public Role Role { get; private set; }

    /// <summary>
    /// Unique invitation token.
    /// </summary>
    public string Token { get; private set; } = string.Empty;

    /// <summary>
    /// Expiration timestamp.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>
    /// Whether the invitation has been accepted.
    /// </summary>
    public bool IsAccepted { get; private set; } = false;

    /// <summary>
    /// User who sent the invitation.
    /// </summary>
    public Guid? InvitedByUserId { get; private set; }

    /// <summary>
    /// Navigation to home.
    /// </summary>
    public Home? Home { get; private set; }

    /// <summary>
    /// Navigation to inviter.
    /// </summary>
    public User? InvitedByUser { get; private set; }

    protected Invitation() { }

    public Invitation(Guid id, Guid homeId, string inviteeEmail, Role role,
        string token, DateTimeOffset expiresAt, Guid? invitedByUserId = null)
        : base(id)
    {
        HomeId = homeId;
        InviteeEmail = inviteeEmail;
        Role = role;
        Token = token;
        ExpiresAt = expiresAt;
        InvitedByUserId = invitedByUserId;
    }

    /// <summary>
    /// Marks the invitation as accepted.
    /// </summary>
    public void Accept()
    {
        IsAccepted = true;
        Touch();
    }

    /// <summary>
    /// Checks if the invitation is expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow > ExpiresAt;

    /// <summary>
    /// Checks if the invitation is valid (not accepted, not expired).
    /// </summary>
    public bool IsValid => !IsAccepted && !IsExpired;
}