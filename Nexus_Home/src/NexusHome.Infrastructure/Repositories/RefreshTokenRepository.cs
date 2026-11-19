namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of refresh token repository.
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly NexusHomeDbContext _context;

    public RefreshTokenRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        // We need to ignore the global query filter to check revoked/expired tokens
        return await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeAllForUserAsync(Guid userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke(reason);
        }

        if (tokens.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupOldTokensAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(rt => rt.RevokedAt != null && rt.RevokedAt < olderThan)
            .ToListAsync(cancellationToken);

        if (tokens.Count > 0)
        {
            _context.RefreshTokens.RemoveRange(tokens);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return tokens.Count;
    }
}