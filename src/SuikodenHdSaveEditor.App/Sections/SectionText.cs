// SPDX-License-Identifier: 0BSD
using System.Globalization;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden2;

namespace SuikodenHdSaveEditor.App.Sections;

/// <summary>
/// Formats reviewed values for display and parses them back.
/// </summary>
/// <remarks>
/// Lifted verbatim from the view model that used to build every field, so the strings the user
/// sees and the values written back are unchanged by the move onto typed fields. The parsers
/// stay strict: an unrecognised choice raises rather than guessing, because the editor only
/// ever offers values it has reviewed.
/// </remarks>
internal static class SectionText
{
    public static Suikoden2ItemDefinition ParseS2Item(string value)
    {
        int separator = value.LastIndexOf(" — ", StringComparison.Ordinal);
        string encoded = separator >= 0 ? value[(separator + 3)..] : value;
        string[] parts = encoded.Split(':', 2, StringSplitOptions.TrimEntries);
        Guard.Valid(parts.Length == 2, "Choose an item by name, ID, or category from the reviewed list.");
        Guard.Valid(Enum.TryParse(parts[0], true, out Suikoden2ItemCategory category), "The item category is not recognized.");
        int id = ParseInteger(parts[1]);
        return Suikoden2Catalog.FindItem(category, id);
    }

    public static string FormatS2Item(int id, int useCount)
    {
        Suikoden2ItemDefinition? item = Suikoden2Catalog.Items.FirstOrDefault(value => value.Id == id && (id == 0 || value.UseCount == useCount));
        return FormatS2CatalogChoice(item, item?.Category ?? Suikoden2ItemCategory.Regular, id);
    }

    public static string FormatS2InventoryItem(Suikoden2Inventory inventory, int id, int useCount)
    {
        if (inventory == Suikoden2Inventory.Bath)
        {
            Suikoden2ItemDefinition? bathItem = Suikoden2Catalog.Items.FirstOrDefault(item => item.Category == Suikoden2ItemCategory.Trade && item.Id == id);
            return S2ItemDisplayName(bathItem);
        }

        return S2ItemDisplayName(Suikoden2Catalog.StoredItem(id, useCount));
    }

    public static Dictionary<string, Suikoden2ItemDefinition> BuildS2ItemNameChoices(IEnumerable<Suikoden2ItemDefinition> choices)
    {
        Dictionary<string, Suikoden2ItemDefinition> result = new(StringComparer.Ordinal);
        foreach (IGrouping<string, Suikoden2ItemDefinition> nameGroup in choices
            .GroupBy(S2ItemDisplayName, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            Suikoden2ItemCategory[] categories = nameGroup.Select(item => item.Category).Distinct().ToArray();
            if (categories.Length == 1)
            {
                result.TryAdd(nameGroup.Key, nameGroup.First());
                continue;
            }

            foreach (IGrouping<Suikoden2ItemCategory, Suikoden2ItemDefinition> categoryGroup in nameGroup.GroupBy(item => item.Category))
            {
                result.TryAdd($"{nameGroup.Key} ({FriendlyItemCategory(categoryGroup.Key)})", categoryGroup.First());
            }
        }

        return result;
    }

    public static string FormatS2CatalogChoice(Suikoden2ItemDefinition item) =>
        FormatS2CatalogChoice(item, item.Category, item.Id);

    public static string FormatS2CatalogChoice(Suikoden2ItemDefinition? item, Suikoden2ItemCategory category, int id) =>
        $"{(id == 0 ? "None" : item?.Name ?? $"Unknown item {id}")}{(item?.StoryCritical == true ? " [Story-critical]" : string.Empty)} — {category}:{id}";

    public static string FormatNamedId(string name, string kind, int id) => $"{name} — {kind} {id}";

    public static string FormatCharacterChoice(int id, string name) => $"{name} — character {id}";

    public static string FormatSuikoden1Recruitment(int flag) => flag switch
    {
        0 => "Not recruited — member flag 0",
        9 => "Recruited — member flag 9",
        _ => $"Other state (preserved) — member flag {flag}",
    };

    public static string FormatSuikoden2RecruitmentStatus(int status) => status switch
    {
        0 => "Not recruited — 0",
        1 => "Spoken to, not yet recruited — 1",
        70 => "Recruited automatically — 70",
        71 => "Recruited manually — 71",
        86 => "Event-locked, unavailable for party — 86",
        212 => "Deceased — 212",
        213 => "On leave — 213",
        _ => $"Unknown state (preserved) — {status}",
    };

    public static int ParseInteger(string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
        {
            throw new SaveEditorException(SaveErrorCode.ValidationFailed, $"'{value}' is not a valid whole number.");
        }

        return result;
    }

    public static T ParseNamedChoice<T>(string value, IReadOnlyDictionary<string, T> choices, string label)
    {
        Guard.Valid(choices.TryGetValue(value, out T? result), $"Choose a reviewed {label} by name from the list.");
        return result!;
    }

    public static bool ParseRecruitmentBoolean(string value) => value.Trim().ToLowerInvariant() switch
    {
        "recruited" or "true" or "9" => true,
        "unrecruited" or "false" or "0" => false,
        _ when ParseLabeledInteger(value, "member flag") == 9 => true,
        _ when ParseLabeledInteger(value, "member flag") == 0 => false,
        _ => throw new SaveEditorException(SaveErrorCode.ValidationFailed, "Choose Recruited (flag 9) or Not recruited (flag 0)."),
    };

    public static int ParseLabeledInteger(string value, string label)
    {
        string marker = $"— {label} ";
        int markerIndex = value.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        Guard.Valid(markerIndex >= 0, $"Choose a reviewed {label} value from the list.");
        return ParseInteger(value[(markerIndex + marker.Length)..]);
    }

    public static int ParseTrailingInteger(string value)
    {
        int separator = value.LastIndexOf(" — ", StringComparison.Ordinal);
        Guard.Valid(separator >= 0, "Choose a reviewed status from the list.");
        return ParseInteger(value[(separator + 3)..]);
    }

    public static int ParseHeadquartersLevel(string value)
    {
        Guard.Valid(value.StartsWith("Level ", StringComparison.OrdinalIgnoreCase), "Choose a reviewed headquarters level from the list.");
        string number = value[6..].Split(' ', 2)[0];
        return ParseInteger(number);
    }

    public static string S2ItemDisplayName(Suikoden2ItemDefinition? item) => item switch
    {
        null => "Unknown item (preserved)",
        { Id: 0 } => "None",
        { StoryCritical: true } => $"{item.Name} [Story-critical]",
        _ => item.Name,
    };

    public static string FriendlyItemCategory(Suikoden2ItemCategory category) => category switch
    {
        Suikoden2ItemCategory.Regular => "Regular item",
        Suikoden2ItemCategory.Farming => "Farming item",
        Suikoden2ItemCategory.Trade => "Trade item",
        Suikoden2ItemCategory.Base => "Headquarters item",
        Suikoden2ItemCategory.Food => "Food",
        Suikoden2ItemCategory.Rune => "Rune",
        Suikoden2ItemCategory.Helmet => "Helmet",
        Suikoden2ItemCategory.Armor => "Armor",
        Suikoden2ItemCategory.Shield => "Shield",
        Suikoden2ItemCategory.Accessory => "Accessory",
        _ => category.ToString(),
    };
}
