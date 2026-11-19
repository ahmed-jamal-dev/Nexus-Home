namespace NexusHome.Domain.Entities;

/// <summary>
/// Home entity.
/// Per DATABASE_MVP_SUMMARY.md and v1.3 baseline Section 6.2.3.
/// </summary>
public class Home : Entity
{
    /// <summary>
    /// Home name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Address line 1.
    /// </summary>
    public string? AddressLine1 { get; private set; }

    /// <summary>
    /// Address line 2.
    /// </summary>
    public string? AddressLine2 { get; private set; }

    /// <summary>
    /// City.
    /// </summary>
    public string? City { get; private set; }

    /// <summary>
    /// State/Province.
    /// </summary>
    public string? StateProvince { get; private set; }

    /// <summary>
    /// Postal code.
    /// </summary>
    public string? PostalCode { get; private set; }

    /// <summary>
    /// IANA timezone (e.g., "America/New_York").
    /// </summary>
    public string Timezone { get; private set; } = "UTC";

    /// <summary>
    /// Memberships in this home.
    /// </summary>
    public ICollection<Membership> Memberships { get; private set; } = new List<Membership>();

    /// <summary>
    /// Rooms in this home.
    /// </summary>
    public ICollection<Room> Rooms { get; private set; } = new List<Room>();

    /// <summary>
    /// Devices in this home.
    /// </summary>
    public ICollection<Device> Devices { get; private set; } = new List<Device>();

    /// <summary>
    /// Invitations for this home.
    /// </summary>
    public ICollection<Invitation> Invitations { get; private set; } = new List<Invitation>();

    /// <summary>
    /// Permissions in this home.
    /// </summary>
    public ICollection<Permission> Permissions { get; private set; } = new List<Permission>();

    protected Home() { }

    public Home(Guid id, string name, string timezone = "UTC")
        : base(id)
    {
        Name = name;
        Timezone = timezone;
    }

    /// <summary>
    /// Updates home address and details.
    /// </summary>
    public void UpdateDetails(string name, string? addressLine1, string? addressLine2,
        string? city, string? stateProvince, string? postalCode, string? timezone)
    {
        Name = name;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        StateProvince = stateProvince;
        PostalCode = postalCode;
        if (!string.IsNullOrWhiteSpace(timezone))
            Timezone = timezone;
        Touch();
    }
}