namespace NexusHome.Infrastructure.Converters;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NexusHome.Domain.Enums;

/// <summary>
/// Value converter for Effect enum <-> PostgreSQL enum label strings.
/// Maps C# enum members to their corresponding PostgreSQL enum labels (e.g., Grant -> "Grant").
/// </summary>
public class EffectConverter : ValueConverter<Effect, string>
{
    private static readonly Dictionary<Effect, string> _toProvider = new()
    {
        { Effect.Grant, "Grant" },
        { Effect.Deny, "Deny" }
    };

    private static readonly Dictionary<string, Effect> _fromProvider = _toProvider
        .ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

    public EffectConverter()
        : base(
            v => ConvertToProvider(v),
            v => ConvertFromProvider(v))
    {
    }

    private static new string ConvertToProvider(Effect v) => _toProvider.TryGetValue(v, out var s) ? s : v.ToString();

    private static new Effect ConvertFromProvider(string v) => _fromProvider.TryGetValue(v, out var e) ? e : Enum.Parse<Effect>(v, true);
}