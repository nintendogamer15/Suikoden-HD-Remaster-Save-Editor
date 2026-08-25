// SPDX-License-Identifier: 0BSD
// Factual labels transcribed from credited asilverthorn research; see Data/README.md.
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace SuikodenHdSaveEditor.Formats.Suikoden1;

public static class Suikoden1Catalog
{
    private static readonly Lazy<CatalogData> Data = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IReadOnlyDictionary<int, string> Characters => Data.Value.Characters;

    public static IReadOnlyDictionary<int, string> Items => Data.Value.Items;

    public static IReadOnlyDictionary<int, string> EquipmentSlots => Data.Value.EquipmentSlots;

    public static IReadOnlyDictionary<int, string> Runes => Data.Value.Runes;

    public static string CharacterName(int id) => Characters.TryGetValue(id, out string? name) ? name : $"Character {id}";

    public static string ItemName(int id) => Items.TryGetValue(id, out string? name) ? name : $"Item {id}";

    private static CatalogData Load()
    {
        Assembly assembly = typeof(Suikoden1Catalog).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith("Suikoden1ResearchData.json", StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement root = document.RootElement;
        return new CatalogData(
            ReadMap(root.GetProperty("characters")),
            ReadMap(root.GetProperty("items")),
            ReadMap(root.GetProperty("equipmentSlots")),
            ReadMap(root.GetProperty("runes")));
    }

    private static Dictionary<int, string> ReadMap(JsonElement element)
    {
        return element.EnumerateObject().ToDictionary(
            property => int.Parse(property.Name, CultureInfo.InvariantCulture),
            property => property.Value.GetString()!,
            EqualityComparer<int>.Default);
    }

    private sealed record CatalogData(
        IReadOnlyDictionary<int, string> Characters,
        IReadOnlyDictionary<int, string> Items,
        IReadOnlyDictionary<int, string> EquipmentSlots,
        IReadOnlyDictionary<int, string> Runes);
}
