namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of claim credential repository.
/// Uses the Invitation table with Role=Owner and InviteeEmail=null for claim credentials.
/// </summary>
public sealed class ClaimCredentialRepository : IClaimCredentialRepository
{
    private readonly NexusHomeDbContext _context;

    public ClaimCredentialRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Invitation?> GetPendingForUpdateAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        // Use SELECT FOR UPDATE equivalent via EF Core's locking
        // We'll load the entity and rely on optimistic concurrency via xmin
        return await _context.Invitations
            .Where(i => i.HomeId == homeId
                && i.Role == NexusHome.Domain.Enums.Role.Owner
                && i.InviteeEmail == null
                && i.IsAccepted == false
                && i.DeletedAt == null)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .Where(i => i.Token == tokenHash
                && i.Role == NexusHome.Domain.Enums.Role.Owner
                && i.InviteeEmail == null
                && i.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsValidCredentialAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .AnyAsync(i => i.HomeId == homeId
                && i.Role == NexusHome.Domain.Enums.Role.Owner
                && i.InviteeEmail == null
                && i.IsAccepted == false
                && i.DeletedAt == null
                && i.Token != ""
                && i.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        _context.Invitations.Update(invitation);
        await _context.SaveChangesAsync(cancellationToken);
    }
}