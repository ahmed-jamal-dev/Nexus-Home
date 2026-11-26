namespace NexusHome.Infrastructure.Converters;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexusHome.Domain.Enums;

/// <summary>
/// Value converter for PermissionType enum <-> PostgreSQL enum label strings.
/// Maps C# enum members to their corresponding PostgreSQL enum labels (e.g., DeviceControl -> "device:control").
/// </summary>
public class PermissionTypeConverter : ValueConverter<PermissionType, string>
{
    private static readonly Dictionary<PermissionType, string> _toProvider = new()
    {
        { PermissionType.DeviceControl, "device:control" },
        { PermissionType.DeviceView, "device:view" },
        { PermissionType.DeviceManage, "device:manage" },
        { PermissionType.HomeManage, "home:manage" },
        { PermissionType.MemberManage, "member:manage" },
        { PermissionType.CameraStream, "camera:stream" },
        { PermissionType.CameraView, "camera:view" },
        { PermissionType.MicrophoneStream, "microphone:stream" },
        { PermissionType.SettingsManage, "settings:manage" },
        { PermissionType.AiPropose, "ai:propose" },
        { PermissionType.AiExecute, "ai:execute" }
    };

    private static readonly Dictionary<string, PermissionType> _fromProvider = _toProvider
        .ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

    public PermissionTypeConverter()
        : base(
            v => ConvertToProvider(v),
            v => ConvertFromProvider(v))
    {
    }

    private static new string ConvertToProvider(PermissionType v) => _toProvider.TryGetValue(v, out var s) ? s : v.ToString();

    private static new PermissionType ConvertFromProvider(string v) => _fromProvider.TryGetValue(v, out var e) ? e : Enum.Parse<PermissionType>(v, true);
}