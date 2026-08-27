// SPDX-License-Identifier: 0BSD
using System.Globalization;
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Formats.Suikoden2;

namespace SuikodenHdSaveEditor.App.Sections;

/// <summary>Suikoden II inventory fields.</summary>
/// <remarks>A direct port of the view model's <c>BuildSuikoden2Inventory</c> method.</remarks>
internal static class Suikoden2InventorySection
{
    public static void Build(Suikoden2Adapter adapter, SectionBuilder builder)
    {
        foreach ((Suikoden2Inventory inventory, string path, string warning) in new[]
        {
            (Suikoden2Inventory.Party, "party_data.party_item", string.Empty),
            (Suikoden2Inventory.Warehouse, "game_data.base_item", string.Empty),
            (Suikoden2Inventory.Bath, "game_data.furo_item", "Bath items include trade-category paintings and ornaments."),
            (Suikoden2Inventory.RoomExperimental, "game_data.room_item", "Experimental: room-item purpose remains uncertain. Edit at your own risk."),
        })
        {
            JsonArray array = inventory switch
            {
                Suikoden2Inventory.Party => adapter.Document.Root["party_data"]!["party_item"]!.AsArray(),
                Suikoden2Inventory.Warehouse => adapter.Document.Root["game_data"]!["base_item"]!.AsArray(),
                Suikoden2Inventory.Bath => adapter.Document.Root["game_data"]!["furo_item"]!.AsArray(),
                _ => adapter.Document.Root["game_data"]!["room_item"]!.AsArray(),
            };
            for (int index = 0; index < array.Count; index++)
            {
                int captured = index;
                JsonObject item = array[index]!.AsObject();
                int itemId = item["item_no"]!.GetValue<int>();
                int useCount = item["use_cnt"]!.GetValue<int>();
                string text = SectionText.FormatS2InventoryItem(inventory, itemId, useCount);
                IEnumerable<Suikoden2ItemDefinition> choices = InventoryChoices(inventory, captured);
                Dictionary<string, Suikoden2ItemDefinition> namedChoices = SectionText.BuildS2ItemNameChoices(choices);
                string slotKind = inventory == Suikoden2Inventory.Bath ? captured is 2 or 5 ? "painting" : "ornament" : "slot";
                builder.AddChoice(
                    $"{InventoryDisplayName(inventory)} {slotKind} {index + 1}",
                    $"{path}[{index}]",
                    text,
                    namedChoices.Keys.Order(StringComparer.OrdinalIgnoreCase),
                    value => adapter.SetInventorySlot(inventory, captured, SectionText.ParseNamedChoice(value, namedChoices, "item")),
                    warning.Length == 0
                        ? "Choose by item name. Stackable consumables start at their reviewed maximum quantity; use the quantity field to lower it."
                        : warning + " Choose by item name.");

                Suikoden2ItemDefinition? currentItem = Suikoden2Catalog.StoredItem(itemId, useCount);
                if (currentItem is { Category: Suikoden2ItemCategory.Regular, UseCount: > 1 })
                {
                    string quantityWarning = $"Verified quantity range for {currentItem.Name}: 1–{currentItem.UseCount}. Select None in the item field to remove the stack.";
                    if (warning.Length > 0)
                    {
                        quantityWarning = warning + " " + quantityWarning;
                    }

                    builder.AddChoice(
                        $"{InventoryDisplayName(inventory)} {slotKind} {index + 1} quantity",
                        $"{path}[{index}].use_cnt",
                        useCount.ToString(CultureInfo.InvariantCulture),
                        Enumerable.Range(1, currentItem.UseCount).Select(value => value.ToString(CultureInfo.InvariantCulture)),
                        value => adapter.SetInventoryQuantity(inventory, captured, SectionText.ParseInteger(value)),
                        quantityWarning);
                }
            }
        }

        JsonArray keyItems = adapter.Document.Root["party_data"]!["event_item"]!.AsArray();
        for (int index = 0; index < keyItems.Count; index++)
        {
            int captured = index;
            int current = keyItems[index]!.GetValue<int>();
            Suikoden2ItemDefinition? currentItem = Suikoden2Catalog.Items.FirstOrDefault(item => item.Category == Suikoden2ItemCategory.Regular && item.Id == current);
            IEnumerable<Suikoden2ItemDefinition> choices = Suikoden2Catalog.Items
                .Where(item => item.Category == Suikoden2ItemCategory.Regular && (item.Id == 0 || item.StoryCritical));
            Dictionary<string, Suikoden2ItemDefinition> namedChoices = SectionText.BuildS2ItemNameChoices(choices);
            builder.AddChoice(
                $"Key item slot {index + 1}",
                $"party_data.event_item[{index}]",
                SectionText.S2ItemDisplayName(currentItem),
                namedChoices.Keys.Order(StringComparer.OrdinalIgnoreCase),
                value => adapter.SetKeyItem(captured, SectionText.ParseNamedChoice(value, namedChoices, "key item").Id),
                "Story-critical: only reviewed key-item entries are offered. Select None to clear.");
        }
    }

    private static IEnumerable<Suikoden2ItemDefinition> InventoryChoices(Suikoden2Inventory inventory, int slot)
    {
        if (inventory == Suikoden2Inventory.Bath)
        {
            bool paintingSlot = slot is 2 or 5;
            return Suikoden2Catalog.Items.Where(item =>
            {
                bool painting = item.Id == 0 || item.Id is >= 18 and <= 22 or >= 42 and <= 44;
                bool ornament = item.Id == 0 || item.Id is >= 1 and <= 17 or >= 45 and <= 50;
                return item.Category == Suikoden2ItemCategory.Trade && (paintingSlot ? painting : ornament);
            });
        }

        return Suikoden2Catalog.Items
            .GroupBy(item => (item.Id, UseCount: item.Id == 0 ? 0 : item.UseCount))
            .Select(group => group.First());
    }

    private static string InventoryDisplayName(Suikoden2Inventory inventory) => inventory switch
    {
        Suikoden2Inventory.Party => "Party inventory",
        Suikoden2Inventory.Warehouse => "Warehouse",
        Suikoden2Inventory.Bath => "Bath / display item",
        Suikoden2Inventory.RoomExperimental => "Room item (experimental)",
        _ => inventory.ToString(),
    };
}
