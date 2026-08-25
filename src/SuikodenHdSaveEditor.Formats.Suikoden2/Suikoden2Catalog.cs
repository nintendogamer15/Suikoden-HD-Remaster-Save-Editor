// SPDX-License-Identifier: MIT
// Substantially ported from faospark/suisaveeditor src/gamedata.js.
// Copyright (c) 2025 faospark. See LICENSES/suisaveeditor-MIT.txt and Data/README.md.
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace SuikodenHdSaveEditor.Formats.Suikoden2;

public enum Suikoden2ItemCategory
{
    Regular,
    Farming,
    Trade,
    Base,
    Food,
    Rune,
    Helmet,
    Armor,
    Shield,
    Accessory,
}

public sealed record Suikoden2CharacterDefinition(int Id, string Name, IReadOnlySet<string> Attributes);

public sealed record Suikoden2ItemDefinition(
    int Id,
    string Name,
    Suikoden2ItemCategory Category,
    int UseCount,
    IReadOnlySet<string> Attributes,
    bool StoryCritical);

public static class Suikoden2Catalog
{
    private static readonly HashSet<int> KeyItemIds =
    [
        29, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46,
        47, 48, 49, 50, 51, 52, 53, 54, 55, 72, 73, 74,
    ];

    private static readonly Lazy<CatalogData> Data = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IReadOnlyDictionary<int, Suikoden2CharacterDefinition> Characters => Data.Value.Characters;

    public static IReadOnlyList<Suikoden2ItemDefinition> Items => Data.Value.Items;

    public static IReadOnlySet<int> Beasts => Data.Value.Beasts;

    public static Suikoden2CharacterDefinition? Character(int id) => Characters.GetValueOrDefault(id);

    public static IReadOnlyList<Suikoden2ItemDefinition> SearchItems(string? text, Suikoden2ItemCategory? category = null)
    {
        string query = text?.Trim() ?? string.Empty;
        return Items
            .Where(item => category is null || item.Category == category)
            .Where(item => query.Length == 0
                || item.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Id.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Category.ToString().Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Id)
            .ToArray();
    }

    public static Suikoden2ItemDefinition FindItem(Suikoden2ItemCategory category, int id)
    {
        Suikoden2ItemDefinition? result = Items.FirstOrDefault(item => item.Category == category && item.Id == id);
        return result ?? throw new KeyNotFoundException($"No reviewed {category} item has ID {id}.");
    }

    public static bool IsRuneAllowed(int characterId, int slot, int runeId)
    {
        if (runeId == 0)
        {
            return true;
        }

        Suikoden2ItemDefinition? rune = Items.FirstOrDefault(item => item.Category == Suikoden2ItemCategory.Rune && item.Id == runeId);
        if (rune is null || rune.Attributes.Contains("ExR"))
        {
            return false;
        }

        string slotName = slot switch
        {
            0 => "HR",
            1 => "RH",
            2 => "LH",
            _ => throw new ArgumentOutOfRangeException(nameof(slot)),
        };
        bool hasSlotRestriction = rune.Attributes.Overlaps(["HR", "RH", "LH"]);
        bool passesSlot = !hasSlotRestriction || rune.Attributes.Contains(slotName);
        if (!passesSlot)
        {
            return false;
        }

        int[] characterRestrictions = rune.Attributes
            .Select(attribute => int.TryParse(attribute, NumberStyles.None, CultureInfo.InvariantCulture, out int id) ? id : -1)
            .Where(id => id >= 0)
            .ToArray();
        return characterRestrictions.Length == 0
            || rune.Attributes.Contains("N")
            || characterRestrictions.Contains(characterId);
    }

    public static bool IsEquipmentAllowed(int characterId, Suikoden2ItemCategory category, int itemId)
    {
        if (itemId == 0)
        {
            return true;
        }

        if (Beasts.Contains(characterId))
        {
            return false;
        }

        Suikoden2CharacterDefinition? character = Character(characterId);
        Suikoden2ItemDefinition? item = Items.FirstOrDefault(value => value.Category == category && value.Id == itemId);
        if (character is null || item is null)
        {
            return false;
        }

        if (item.Attributes.Count == 0)
        {
            return category != Suikoden2ItemCategory.Shield || character.Attributes.Contains("S");
        }

        string[] relevant = category switch
        {
            Suikoden2ItemCategory.Helmet => ["C", "E"],
            Suikoden2ItemCategory.Armor => ["L", "H", "V", "R"],
            Suikoden2ItemCategory.Shield => ["S"],
            _ => [],
        };
        return relevant.Length == 0 || item.Attributes.Overlaps(character.Attributes.Intersect(relevant));
    }

    private static CatalogData Load()
    {
        Assembly assembly = typeof(Suikoden2Catalog).Assembly;
        string resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith("Suikoden2GameData.json", StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resource)!;
        using JsonDocument document = JsonDocument.Parse(stream);
        JsonElement root = document.RootElement;

        Dictionary<int, Suikoden2CharacterDefinition> characters = [];
        foreach (JsonProperty property in root.GetProperty("CHARACTERS").EnumerateObject())
        {
            int id = int.Parse(property.Name, CultureInfo.InvariantCulture);
            string name = property.Value.GetProperty("name").GetString()!;
            HashSet<string> attributes = ReadAttributes(property.Value);
            characters[id] = new(id, name, attributes);
        }

        List<Suikoden2ItemDefinition> items = [];
        AddCategory(items, root, "ITEMS", Suikoden2ItemCategory.Regular, 0);
        AddCategory(items, root, "FARMING", Suikoden2ItemCategory.Farming, 48);
        AddCategory(items, root, "TRADE", Suikoden2ItemCategory.Trade, 64);
        AddCategory(items, root, "BASE_ITEM", Suikoden2ItemCategory.Base, 80);
        AddCategory(items, root, "FOOD", Suikoden2ItemCategory.Food, 101);
        AddCategory(items, root, "RUNES", Suikoden2ItemCategory.Rune, 32);
        AddCategory(items, root, "HELMET", Suikoden2ItemCategory.Helmet, 16);
        AddCategory(items, root, "ARMOR", Suikoden2ItemCategory.Armor, 16);
        AddCategory(items, root, "SHIELD", Suikoden2ItemCategory.Shield, 16);
        AddCategory(items, root, "OTHER_EQUIP_GEAR", Suikoden2ItemCategory.Accessory, 16);
        HashSet<int> beasts = root.GetProperty("BEASTS").EnumerateArray().Select(value => value.GetInt32()).ToHashSet();
        return new(characters, items, beasts);
    }

    private static void AddCategory(
        List<Suikoden2ItemDefinition> destination,
        JsonElement root,
        string sourceName,
        Suikoden2ItemCategory category,
        int defaultUseCount)
    {
        foreach (JsonProperty property in root.GetProperty(sourceName).EnumerateObject())
        {
            int id = int.Parse(property.Name, CultureInfo.InvariantCulture);
            string name = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()!
                : property.Value.GetProperty("name").GetString()!;
            HashSet<string> attributes = ReadAttributes(property.Value);
            int useCount = defaultUseCount;
            if (property.Value.ValueKind == JsonValueKind.Object
                && property.Value.TryGetProperty("attrs", out JsonElement attrs)
                && attrs.GetArrayLength() > 0
                && attrs[0].ValueKind == JsonValueKind.Number)
            {
                useCount = attrs[0].GetInt32();
            }

            bool storyCritical = category == Suikoden2ItemCategory.Regular && KeyItemIds.Contains(id);
            destination.Add(new(id, name, category, useCount, attributes, storyCritical));
        }
    }

    private static HashSet<string> ReadAttributes(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("attrs", out JsonElement attributes))
        {
            return [];
        }

        return attributes.EnumerateArray()
            .Where(attribute => attribute.ValueKind == JsonValueKind.String)
            .Select(attribute => attribute.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private sealed record CatalogData(
        IReadOnlyDictionary<int, Suikoden2CharacterDefinition> Characters,
        IReadOnlyList<Suikoden2ItemDefinition> Items,
        IReadOnlySet<int> Beasts);
}

