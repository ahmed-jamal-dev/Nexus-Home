namespace NexusHome.Application.Interfaces;

using NexusHome.Domain.Entities;

/// <summary>
/// Repository interface for Home entity.
/// </summary>
public interface IHomeRepository
{
    /// <summary>
    /// Gets a home by ID.
    /// </summary>
    Task<Home?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a home by ID with owner membership loaded.
    /// </summary>
    Task<Home?> GetByIdWithOwnerAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a home with details including owner, member count, device count, and provisioned by technician.
    /// </summary>
    Task<(Home Home, string OwnerEmail, int MemberCount, int DeviceCount, Guid? ProvisionedByTechnicianId)?> GetHomeWithDetailsAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Lists homes with filters, pagination, and Technician scope support.
    /// </summary>
    Task<List<(Home Home, string OwnerEmail, int MemberCount, int DeviceCount, Guid? ProvisionedByTechnicianId)>> GetHomesAsync(
        string? search = null,
        bool? isDeleted = null,
        List<Guid>? technicianScope = null,
        bool isPlatformAdminOrHigher = false,
        string? cursor = null,
        int limit = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Gets total count of homes with filters.
    /// </summary>
    Task<int> GetHomesCountAsync(
        string? search = null,
        bool? isDeleted = null,
        List<Guid>? technicianScope = null,
        bool isPlatformAdminOrHigher = false,
        CancellationToken ct = default);

    /// <summary>
    /// Gets home memberships.
    /// </summary>
    Task<List<(Membership Membership, string UserEmail)>> GetHomeMembershipsAsync(
        Guid homeId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets devices for a home.
    /// </summary>
    Task<List<Device>> GetHomeDevicesAsync(Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Gets rooms for a home.
    /// </summary>
    Task<List<Room>> GetHomeRoomsAsync(Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new home.
    /// </summary>
    Task AddAsync(Home home, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing home.
    /// </summary>
    Task UpdateAsync(Home home, CancellationToken ct = default);

    /// <summary>
    /// Checks if a home exists and is not deleted.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks if a home has an active owner.
    /// </summary>
    Task<bool> HasActiveOwnerAsync(Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user owns a specific home.
    /// </summary>
    Task<bool> UserOwnsHomeAsync(Guid userId, Guid homeId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user is the provisioned technician for a home.
    /// </summary>
    Task<bool> IsProvisionedByTechnicianAsync(Guid homeId, Guid technicianUserId, CancellationToken ct = default);

    /// <summary>
    /// Gets total home count.
    /// </summary>
    Task<int> GetTotalCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets active home count.
    /// </summary>
    Task<int> GetActiveCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets total device count.
    /// </summary>
    Task<int> GetTotalDeviceCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets online device count.
    /// </summary>
    Task<int> GetOnlineDeviceCountAsync(CancellationToken ct = default);
}