// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Formats.Suikoden1;

namespace SuikodenHdSaveEditor.App.Sections;

internal static class Suikoden1InventorySection
{
    public static void Build(Suikoden1Adapter adapter, SectionBuilder builder, SectionContext context)
    {
        Dictionary<string, int> itemChoices = new(StringComparer.Ordinal);
        foreach ((int id, string name) in Suikoden1Catalog.Items.OrderBy(item => item.Key))
        {
            itemChoices.TryAdd(name, id);
        }

        JsonArray items = adapter.Document.Root["party_data"]!["party_item"]!.AsArray();
        for (int index = 0; index < items.Count; index++)
        {
            int captured = index;
            int id = items[index]!.GetValue<int>();
            builder.AddChoice(
                $"Party item {index + 1}",
                $"party_data.party_item[{index}]",
                Suikoden1Catalog.ItemName(id),
                itemChoices.Keys.Order(StringComparer.OrdinalIgnoreCase),
                value => adapter.SetPartyItem(captured, SectionText.ParseNamedChoice(value, itemChoices, "item")));
        }
    }
}
