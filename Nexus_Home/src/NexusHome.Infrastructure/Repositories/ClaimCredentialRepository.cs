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
        // Get the latest pending claim credential for a home (no lock)
        // Use GetPendingForUpdateWithLockAsync when a lock is needed
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
        // Use AsNoTracking to avoid EF Core tracking conflicts with subsequent AddAsync calls
        var sql = @"
            SELECT ""Id"", ""HomeId"", invitee_email, ""Role"", ""Token"", expires_at, is_accepted,
                   invited_by_user_id, created_at, updated_at, deleted_at
            FROM invitations
            WHERE ""HomeId"" = @homeId
              AND ""Role"" = 'Owner'
              AND invitee_email IS NULL
              AND is_accepted = false
              AND deleted_at IS NULL
            ORDER BY created_at DESC
            LIMIT 1
            FOR UPDATE";

        return await _context.Invitations
            .FromSqlRaw(sql, new Npgsql.NpgsqlParameter("@homeId", homeId))
            .AsNoTracking()
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

    /// <inheritdoc />
    public async Task<bool> TryAcquireGenerationLockAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        // Use a hash of the homeId to generate a 64-bit lock key
        // PostgreSQL advisory locks use int64 keys
        var lockKey = homeId.GetHashCode();

        // Use raw SQL execution with Npgsql directly to avoid EF Core scalar query issues
        var sql = "SELECT pg_try_advisory_xact_lock(@lockKey)";
        var connection = _context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@lockKey";
            parameter.Value = lockKey;
            command.Parameters.Add(parameter);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is true or 1;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    /// <inheritdoc />
    public async Task ReleaseGenerationLockAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        // Advisory transaction locks are automatically released at transaction end
        // This method exists for symmetry and potential future use of session locks
        await Task.CompletedTask;
    }
}