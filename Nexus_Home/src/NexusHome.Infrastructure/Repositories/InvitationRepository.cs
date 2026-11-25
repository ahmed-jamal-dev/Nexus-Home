namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of member invitation repository.
/// Uses the Invitations table with InviteeEmail set for member invitations.
/// </summary>
public sealed class InvitationRepository : IInvitationRepository
{
    private readonly NexusHomeDbContext _context;

    public InvitationRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .Where(i => i.Token == tokenHash
                && i.InviteeEmail != null
                && i.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Invitation?> GetByTokenHashWithLockAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        // Execute raw SQL with FOR UPDATE to acquire row-level lock
        // This prevents concurrent acceptance attempts on the same invitation
        var sql = @"
            SELECT ""Id"", ""HomeId"", invitee_email, ""Role"", ""Token"", expires_at, is_accepted,
                   invited_by_user_id, created_at, updated_at, deleted_at
            FROM invitations
            WHERE ""Token"" = @tokenHash
              AND invitee_email IS NOT NULL
              AND deleted_at IS NULL
            LIMIT 1
            FOR UPDATE";

        return await _context.Invitations
            .FromSqlRaw(sql, new Npgsql.NpgsqlParameter("@tokenHash", tokenHash))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsPendingForEmailAsync(Guid homeId, string email, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .AnyAsync(i => i.HomeId == homeId
                && i.InviteeEmail == email
                && i.IsAccepted == false
                && i.DeletedAt == null, cancellationToken);
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

    /// <inheritdoc />
    public async Task<List<Invitation>> ListPendingByHomeAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Invitations
            .Where(i => i.HomeId == homeId
                && i.InviteeEmail != null
                && i.IsAccepted == false
                && i.DeletedAt == null)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}