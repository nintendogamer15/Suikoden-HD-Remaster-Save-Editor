// SPDX-License-Identifier: 0BSD
using System.Globalization;
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden2;

namespace SuikodenHdSaveEditor.App.Sections;

/// <summary>Suikoden II field definitions.</summary>
/// <remarks>
/// A direct port of the view model's <c>BuildSuikoden2*</c> methods. Labels, paths, warnings,
/// and the values written back are unchanged; only the field construction moved onto typed
/// descriptors, which is why numbers now carry real bounds instead of being parsed by hand.
/// </remarks>
internal static class Suikoden2Sections
{
    public static void Build(SectionKind kind, SaveDocument document, SectionBuilder builder, SectionContext context)
    {
        Suikoden2Adapter adapter = new(document);
        switch (kind)
        {
            case SectionKind.Overview:
                BuildOverview(adapter, builder);
                break;
            case SectionKind.Party:
                BuildParty(adapter, builder);
                break;
            case SectionKind.Characters:
                Suikoden2CharacterSection.Build(adapter, builder, context);
                break;
            case SectionKind.Inventory:
                Suikoden2InventorySection.Build(adapter, builder);
                break;
            case SectionKind.Recruitment:
                Suikoden2RecruitmentSection.Build(adapter, builder, context);
                break;
            case SectionKind.Progress:
                BuildProgress(adapter, builder);
                break;
            default:
                break;
        }
    }

    private static void BuildOverview(Suikoden2Adapter adapter, SectionBuilder builder)
    {
        JsonObject game = adapter.Document.Root["game_data"]!.AsObject();
        builder.AddReadOnly("Detected game", "schema", "Suikoden II");
        if (game["bozu_name"] is JsonValue heroName)
        {
            builder.AddString(
                "Hero / save-list name",
                "game_data.bozu_name + game_data.bozu_name2",
                heroName.GetValue<string>(),
                text => adapter.SetName("bozu_name", text),
                "Both paired hero-name fields are updated together. Every supplied Suikoden II save keeps these fields equal; changing only one can leave menus or the save list showing the old name.");
        }

        foreach ((string path, string label) in new[]
        {
            ("macd_name", "Imported Suikoden I hero"),
            ("base_name", "Castle name"), ("m_base_name", "Imported Suikoden I HQ"), ("team_name", "Army name"),
        })
        {
            if (game[path] is JsonValue value)
            {
                builder.AddString(label, $"game_data.{path}", value.GetValue<string>(), text => adapter.SetName(path, text));
            }
        }

        builder.AddNumber("Potch", "party_data.gold", adapter.Potch, value => adapter.SetGeneralNumber("gold", value));
        builder.AddNumber("Popularity", "party_data.ninki", adapter.Popularity, value => adapter.SetGeneralNumber("ninki", value));
        AddHeadquartersLevelChoice(builder, "Castle level", "game_data.base_lv", game["base_lv"]!.GetValue<int>(), value => adapter.SetGeneralNumber("base_lv", value));
        foreach ((string path, string label) in new[]
        {
            ("kaji_lv", "Blacksmith level"), ("area_no", "Area"),
            ("town_no", "Town"), ("map_no", "Map"),
        })
        {
            builder.AddNumber(label, $"game_data.{path}", game[path]!.GetValue<int>(), value => adapter.SetGeneralNumber(path, value));
        }

        builder.AddNumber("Player X", "px", adapter.Document.Root["px"]!.GetValue<int>(), value => adapter.SetGeneralNumber("px", value));
        builder.AddNumber("Player Y", "py", adapter.Document.Root["py"]!.GetValue<int>(), value => adapter.SetGeneralNumber("py", value));
        AddGameArrayFields(adapter, builder, "play_time", "Play time", null, "Hours", "Minutes", "Seconds");
        AddGameArrayFields(adapter, builder, "furo_info", "Bath information", null, "Bath level", "Bath value 2");
        foreach (string metadata in new[] { "save_slot", "save_num", "save_poi", "load_count", "date_time_now" })
        {
            JsonNode? node = metadata == "date_time_now" ? adapter.Document.Root[metadata] : game[metadata];
            if (node is not null)
            {
                builder.AddReadOnly($"Visible metadata · {metadata}", metadata, node.ToJsonString());
            }
        }
    }

    private static void BuildParty(Suikoden2Adapter adapter, SectionBuilder builder)
    {
        int[] values = [.. adapter.PartyCharacterIds];
        for (int index = 0; index < values.Length; index++)
        {
            int captured = index;
            string name = Suikoden2Catalog.Character(values[index])?.Name ?? "Empty / NPC";
            string type = index < Suikoden2Adapter.BattlePartySize ? "Battle" : "Convoy";
            int maximum = index < Suikoden2Adapter.BattlePartySize ? 83 : 124;
            string[] choices = [.. Enumerable.Range(0, maximum + 1)
                .Where(id => id == 0 || Suikoden2Catalog.Character(id) is not null)
                .Select(id => SectionText.FormatCharacterChoice(id, id == 0 ? "Empty" : Suikoden2Catalog.Character(id)!.Name))];
            builder.AddChoice($"{type} slot {index + 1}", $"party_data.party_cha_no[{index}]", SectionText.FormatCharacterChoice(values[index], name), choices, value =>
            {
                int[] changed = [.. adapter.PartyCharacterIds];
                changed[captured] = SectionText.ParseLabeledInteger(value, "character");
                adapter.SetParty(changed);
            }, index < Suikoden2Adapter.BattlePartySize
                ? "Battle slots accept the reviewed battle-character range."
                : "Convoy slots also offer named support characters from the reviewed catalogue.");
        }
    }

    private static void BuildProgress(Suikoden2Adapter adapter, SectionBuilder builder)
    {
        JsonObject game = adapter.Document.Root["game_data"]!.AsObject();
        AddHeadquartersLevelChoice(builder, "Castle level", "game_data.base_lv", game["base_lv"]!.GetValue<int>(), value => adapter.SetGeneralNumber("base_lv", value));
        builder.AddReadOnly("Imported Suikoden I recruit count", "game_data.nakam_1_num", game["nakam_1_num"]!.GetValue<int>().ToString(CultureInfo.InvariantCulture), "Read-only: McDohl/Gremio import semantics are not safe to synthesize.");
        JsonArray aliases = game["kari_name"]!.AsArray();
        for (int index = 0; index < aliases.Count; index++)
        {
            int captured = index;
            builder.AddString($"Greenhill alias {index + 1}", $"game_data.kari_name[{index}]", aliases[index]!.GetValue<string>(), value => adapter.SetGreenhillAlias(captured, value));
        }

        AddGameArrayFields(adapter, builder, "food_menu", "Castle food menu", null);
        AddGameArrayFields(adapter, builder, "food_resipi", "Recipe flags", null);
        AddGameArrayFields(adapter, builder, "food_num", "Food / recipe values", null);

        JsonArray events = adapter.Document.Root["event_flag"]!.AsArray();
        CookOffStage? current = events.Count > 153
            ? Suikoden2Adapter.CookOffStages.FirstOrDefault(stage => stage.EventByte152 == events[152]!.GetValue<int>() && stage.EventByte153 == events[153]!.GetValue<int>())
            : null;
        builder.AddNumber("Cook-off battles won", "event_flag[152..153]", current?.BattlesWon ?? 0, adapter.SetCookOffStage, "Dangerous story progress edit. Only the 13 reviewed stages are accepted.");
        AddGameArrayFields(adapter, builder, "tantei_lv", "Richmond detective clue byte", "Experimental progress flags: each value is a byte (0–255).");
        AddGameArrayFields(adapter, builder, "hon_flag", "Castle / farm flag byte", "Experimental castle and farm flags; indices 30–32 are upstream-researched but can affect progression.");

        JsonArray treasure = adapter.Document.Root["t_box_flag"]!.AsArray();
        for (int index = 0; index < treasure.Count; index++)
        {
            int captured = index;
            builder.AddNumber($"Treasure-chest flag byte {index}", $"t_box_flag[{index}]", treasure[index]!.GetValue<int>(), value => adapter.SetTreasureFlagByte(captured, value), "Experimental: each byte controls eight chest flags.");
        }
    }

    internal static void AddGameArrayFields(Suikoden2Adapter adapter, SectionBuilder builder, string field, string label, string? warning, params string[] labels)
    {
        JsonArray array = adapter.Document.Root["game_data"]![field]!.AsArray();
        for (int index = 0; index < array.Count; index++)
        {
            int captured = index;
            string itemLabel = index < labels.Length ? labels[index] : $"{label} {index}";
            builder.AddNumber(itemLabel, $"game_data.{field}[{index}]", array[index]!.GetValue<int>(), value => adapter.SetGameDataArrayValue(field, captured, value), warning);
        }
    }

    internal static void AddHeadquartersLevelChoice(SectionBuilder builder, string label, string path, int value, Action<int> apply)
    {
        string[] choices =
        [
            "Level 0 — Pre-headquarters state",
            "Level 1",
            "Level 2",
            "Level 3",
            "Level 4 — Maximum",
        ];
        string selected = choices.SingleOrDefault(choice => SectionText.ParseHeadquartersLevel(choice) == value) ?? $"Level {value} — Outside reviewed range";
        builder.AddChoice(
            label,
            path,
            selected,
            choices,
            text => apply(SectionText.ParseHeadquartersLevel(text)),
            "Reviewed range: 0–4. Level 0 is retained for pre-headquarters saves; playable headquarters levels are 1–4 and level 4 is the cap. Direct changes can desynchronize story-driven facilities.");
    }
}
