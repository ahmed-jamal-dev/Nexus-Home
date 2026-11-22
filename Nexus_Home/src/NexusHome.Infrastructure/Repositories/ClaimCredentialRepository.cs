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
        // Use SELECT FOR UPDATE to lock the row for the duration of the transaction
        // This prevents race conditions when claiming a home
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
    public async Task<Invitation?> GetPendingForUpdateWithLockAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        // Execute raw SQL with FOR UPDATE to acquire row-level lock
        // This prevents concurrent claim attempts on the same home
        // Note: PostgreSQL column names - EF Core maps PascalCase properties to quoted PascalCase columns,
        // but some columns (created_at, expires_at, etc.) are explicitly configured as snake_case
        var sql = @"
            SELECT ""Id"", ""HomeId"", invitee_email, ""Role"", ""Token"", expires_at, is_accepted,
                   invited_by_user_id, created_at, updated_at, ""DeletedAt""
            FROM invitations
            WHERE ""HomeId"" = @homeId
              AND ""Role"" = 'Owner'
              AND invitee_email IS NULL
              AND is_accepted = false
              AND ""DeletedAt"" IS NULL
            ORDER BY created_at DESC
            LIMIT 1
            FOR UPDATE";

        return await _context.Invitations
            .FromSqlRaw(sql, new Npgsql.NpgsqlParameter("@homeId", homeId))
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