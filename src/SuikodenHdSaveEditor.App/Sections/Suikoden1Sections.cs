// SPDX-License-Identifier: 0BSD
using System.Globalization;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden1;

namespace SuikodenHdSaveEditor.App.Sections;

/// <summary>Suikoden I field definitions.</summary>
/// <remarks>
/// A direct port of the view model's <c>BuildSuikoden1*</c> methods. Labels, paths, warnings,
/// and the values written back are unchanged; only the field construction moved onto typed
/// descriptors, which is why numbers now carry real bounds instead of being parsed by hand.
/// </remarks>
internal static class Suikoden1Sections
{
    public static void Build(SectionKind kind, SaveDocument document, SectionBuilder builder, SectionContext context)
    {
        Suikoden1Adapter adapter = new(document);

        switch (kind)
        {
            case SectionKind.Overview:
                BuildOverview(adapter, builder);
                break;
            case SectionKind.Party:
                BuildParty(adapter, builder);
                break;
            case SectionKind.Characters:
                BuildCharacters(adapter, builder, context);
                break;
            case SectionKind.Inventory:
                BuildInventory(adapter, builder, context);
                break;
            case SectionKind.Recruitment:
                BuildRecruitment(adapter, builder, context);
                break;
            case SectionKind.Progress:
                BuildProgress(adapter, builder);
                break;
            default:
                break;
        }
    }

    private static void BuildOverview(Suikoden1Adapter adapter, SectionBuilder builder)
    {
        builder.AddReadOnly("Detected game", "schema", "Suikoden I");
        builder.AddString("Hero name", "playerName", adapter.HeroName,
            value => adapter.SetNames(value, adapter.HeadquartersName));
        builder.AddString("Headquarters name", "playerCName", adapter.HeadquartersName,
            value => adapter.SetNames(adapter.HeroName, value));
        builder.AddNumber("Potch", "party_data.mochi_kin", adapter.Potch, adapter.SetPotch, minimum: 0);
        builder.AddReadOnly(
            "Play time (raw seconds/ticks)",
            "playTime",
            adapter.PlayTime.ToString(CultureInfo.InvariantCulture));
        AddHeadquartersLevel(adapter, builder);
    }

    private static void BuildParty(Suikoden1Adapter adapter, SectionBuilder builder)
    {
        string[] characterChoices =
        [
            SectionText.FormatCharacterChoice(-1, "Empty"),
            .. adapter.Characters
                .OrderBy(character => character.Id)
                .Select(character => SectionText.FormatCharacterChoice(character.Id, character.Name)),
        ];

        int[] values = [.. adapter.PartyCharacterIds];
        for (int index = 0; index < values.Length; index++)
        {
            int captured = index;
            string name = values[index] == -1 ? "Empty" : Suikoden1Catalog.CharacterName(values[index]);
            builder.AddChoice(
                $"Party slot {index + 1} · {name}",
                $"party_data.chara_code[{index}]",
                SectionText.FormatCharacterChoice(values[index], name),
                characterChoices,
                value =>
                {
                    int[] changed = [.. adapter.PartyCharacterIds];
                    changed[captured] = SectionText.ParseLabeledInteger(value, "character");
                    adapter.SetParty(changed);
                },
                "Tir must remain somewhere in the six slots. Only characters with a battle record in this save are offered.");
        }
    }

    private static void AddHeadquartersLevel(Suikoden1Adapter adapter, SectionBuilder builder)
    {
        string[] choices =
        [
            "Level 0 — Pre-headquarters state",
            "Level 1",
            "Level 2",
            "Level 3",
            "Level 4 — Maximum",
        ];

        int value = adapter.HeadquartersLevel;
        string selected = choices.SingleOrDefault(choice => SectionText.ParseHeadquartersLevel(choice) == value)
            ?? $"Level {value} — Outside reviewed range";

        builder.AddChoice(
            "Headquarters level",
            "shiro_data.level",
            selected,
            choices,
            text => adapter.SetHeadquartersLevel(SectionText.ParseHeadquartersLevel(text)),
            "Reviewed range: 0–4. Level 0 is retained for pre-headquarters saves; playable headquarters levels are 1–4 and level 4 is the cap. Direct changes can desynchronize story-driven facilities.");
    }

    private static void BuildProgress(Suikoden1Adapter adapter, SectionBuilder builder)
    {
        AddHeadquartersLevel(adapter, builder);
        builder.AddReadOnly(
            "Unexposed headquarters fields",
            "shiro_data",
            "Preserved in Advanced Data",
            "Other shiro_data fields are intentionally read-only because their meanings are not sufficiently verified.");
        builder.AddReadOnly(
            "Story flags",
            "tmpEventFlagS / storyFlagS",
            "Preserved in Advanced Data",
            "Meanings and safe transitions are not sufficiently documented for normal editing.");
    }

    private static void BuildCharacters(Suikoden1Adapter adapter, SectionBuilder builder, SectionContext context) =>
        Suikoden1CharacterSection.Build(adapter, builder, context);

    private static void BuildInventory(Suikoden1Adapter adapter, SectionBuilder builder, SectionContext context) =>
        Suikoden1InventorySection.Build(adapter, builder, context);

    private static void BuildRecruitment(Suikoden1Adapter adapter, SectionBuilder builder, SectionContext context) =>
        Suikoden1RecruitmentSection.Build(adapter, builder, context);
}
