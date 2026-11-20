namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of home repository.
/// </summary>
public sealed class HomeRepository : IHomeRepository
{
    private readonly NexusHomeDbContext _context;

    public HomeRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Home?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Homes
            .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Home?> GetByIdWithOwnerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Homes
            .Include(h => h.Memberships.Where(m => m.IsActive && m.Role == NexusHome.Domain.Enums.Role.Owner))
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(h => h.Id == id && h.DeletedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Home home, CancellationToken cancellationToken = default)
    {
        _context.Homes.Add(home);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Home home, CancellationToken cancellationToken = default)
    {
        _context.Homes.Update(home);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Homes
            .AnyAsync(h => h.Id == id && h.DeletedAt == null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasActiveOwnerAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .AnyAsync(m => m.HomeId == homeId && m.IsActive && m.Role == NexusHome.Domain.Enums.Role.Owner, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> UserOwnsHomeAsync(Guid userId, Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .AnyAsync(m => m.UserId == userId && m.HomeId == homeId && m.IsActive && m.Role == NexusHome.Domain.Enums.Role.Owner, cancellationToken);
    }
}