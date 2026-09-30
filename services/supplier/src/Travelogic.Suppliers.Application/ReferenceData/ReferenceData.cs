using System.Text.RegularExpressions;
using Travelogic.Suppliers.Domain.Suppliers;

namespace Travelogic.Suppliers.Application.ReferenceData;

public sealed record Option(string Value, string Label);

public sealed record ReferenceDataResponse(
    IReadOnlyList<Option> SupplierTypes,
    IReadOnlyList<Option> ServiceCategories,
    IReadOnlyList<Option> PricingUnits);

public static partial class ReferenceDataProvider
{
    public static ReferenceDataResponse Get { get; } = new(
        Options<SupplierType>(),
        Options<ServiceCategory>(),
        Options<PricingUnit>());

    private static Option[] Options<TEnum>() where TEnum : struct, Enum =>
        [.. Enum.GetValues<TEnum>().Select(v => new Option(v.ToString(), Humanize(v.ToString())))];

    // "PerRoomPerNight" -> "Per room per night"
    private static string Humanize(string pascalCase)
    {
        var words = WordBoundary().Replace(pascalCase, " $1").ToLowerInvariant();
        return char.ToUpperInvariant(words[0]) + words[1..];
    }

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex WordBoundary();
}
