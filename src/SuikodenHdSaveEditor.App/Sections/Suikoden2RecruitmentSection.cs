// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Formats.Suikoden2;

namespace SuikodenHdSaveEditor.App.Sections;

/// <summary>Suikoden II recruitment fields.</summary>
/// <remarks>A direct port of the view model's <c>BuildSuikoden2Recruitment</c> method.</remarks>
internal static class Suikoden2RecruitmentSection
{
    public static void Build(Suikoden2Adapter adapter, SectionBuilder builder, SectionContext context)
    {
        JsonArray flags = adapter.Document.Root["chara_flag"]!.AsArray();
        for (int id = 1; id < flags.Count; id++)
        {
            int captured = id;
            Suikoden2CharacterDefinition? definition = Suikoden2Catalog.Character(id);
            if (definition is null)
            {
                continue;
            }

            string name = definition.Name;
            builder.AddChoice(
                $"{id}: {name}",
                $"chara_flag[{id}]",
                () => SectionText.FormatSuikoden2RecruitmentStatus(flags[captured]!.GetValue<int>()),
                Suikoden2Adapter.RecruitmentStatuses.Order().Select(SectionText.FormatSuikoden2RecruitmentStatus),
                value =>
                {
                    int selected = SectionText.ParseTrailingInteger(value);
                    int current = flags[captured]!.GetValue<int>();
                    if (selected == current && !Suikoden2Adapter.RecruitmentStatuses.Contains(selected))
                    {
                        return;
                    }

                    adapter.SetRecruitmentStatus(captured, selected);
                },
                "Auto Join and Manual Recruit are both recruited states. Other states reflect story availability; changing them can affect required-party and story events.");
        }

        IReadOnlyList<string> notes = adapter.CompatibilityNotes(context.BetterLeonaEnabled, context.KrakenRecruitmentEnabled);
        for (int index = 0; index < notes.Count; index++)
        {
            int captured = index;
            builder.AddReadOnly("Optional-mod compatibility note", "compatibility", () => adapter.CompatibilityNotes(context.BetterLeonaEnabled, context.KrakenRecruitmentEnabled)[captured]);
        }
    }
}
