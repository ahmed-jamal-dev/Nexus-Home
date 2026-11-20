namespace NexusHome.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using NexusHome.Application.Interfaces;
using NexusHome.Domain.Entities;
using NexusHome.Domain.Enums;
using NexusHome.Infrastructure.Data;

/// <summary>
/// EF Core implementation of membership repository.
/// </summary>
public sealed class MembershipRepository : IMembershipRepository
{
    private readonly NexusHomeDbContext _context;

    public MembershipRepository(NexusHomeDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        _context.Memberships.Add(membership);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        _context.Memberships.Update(membership);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Membership?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Membership?> GetActiveOwnerAsync(Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .FirstOrDefaultAsync(m => m.HomeId == homeId && m.IsActive && m.Role == Role.Owner, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsActiveMemberAsync(Guid userId, Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .AnyAsync(m => m.UserId == userId && m.HomeId == homeId && m.IsActive, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsActiveOwnerAsync(Guid userId, Guid homeId, CancellationToken cancellationToken = default)
    {
        return await _context.Memberships
            .AnyAsync(m => m.UserId == userId && m.HomeId == homeId && m.IsActive && m.Role == Role.Owner, cancellationToken);
    }
}