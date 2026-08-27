// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Formats.Suikoden1;

namespace SuikodenHdSaveEditor.App.Sections;

internal static class Suikoden1RecruitmentSection
{
    public static void Build(Suikoden1Adapter adapter, SectionBuilder builder, SectionContext context)
    {
        JsonArray flags = adapter.Document.Root["member_flag"]!.AsArray();
        int maximum = Math.Min(flags.Count, Suikoden1Catalog.Characters.Keys.Max() + 1);
        for (int id = 0; id < maximum; id++)
        {
            int captured = id;
            int current = flags[id]!.GetValue<int>();
            builder.AddChoice(
                $"{id}: {Suikoden1Catalog.CharacterName(id)}",
                $"member_flag[{id}]",
                SectionText.FormatSuikoden1Recruitment(current),
                new[] { 0, 9, current }.Distinct().Select(SectionText.FormatSuikoden1Recruitment),
                value =>
                {
                    int selected = SectionText.ParseLabeledInteger(value, "member flag");
                    if (selected == current && selected is not (0 or 9))
                    {
                        return;
                    }

                    adapter.SetRecruited(captured, SectionText.ParseRecruitmentBoolean(value));
                },
                "Flag 0 means not recruited; flag 9 means recruited. Recruitment edits can break story progression or required-party events.");
        }
    }
}
