// SPDX-License-Identifier: 0BSD
using SaveEditor.Ui.Editing;
using SuikodenHdSaveEditor.App.Editing;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.App.Sections;

/// <summary>Identifies a section without relying on its display title.</summary>
/// <remarks>
/// The old view model dispatched on the visible title, so renaming a tab would silently change
/// behaviour. The framework wants a stable key anyway, so the key is what drives the code and
/// the title is only ever displayed.
/// </remarks>
public enum SectionKind
{
    /// <summary>Names, money, and other whole-save values.</summary>
    Overview,

    /// <summary>The battle party.</summary>
    Party,

    /// <summary>Per-character stats and equipment.</summary>
    Characters,

    /// <summary>Carried, stored, and container items.</summary>
    Inventory,

    /// <summary>Who has been recruited.</summary>
    Recruitment,

    /// <summary>Headquarters and story progress.</summary>
    Progress,

    /// <summary>The whole decrypted document, read-only.</summary>
    AdvancedData,

    /// <summary>Licences and attribution.</summary>
    Credits,
}

/// <summary>
/// The UI state a section needs beyond the document itself.
/// </summary>
/// <param name="SelectedCharacterId">
/// Which character the Characters section is showing, or <see langword="null"/> when the
/// filter matched nobody.
/// </param>
/// <param name="BetterLeonaEnabled">Whether the Better Leona mod's states are offered.</param>
/// <param name="KrakenRecruitmentEnabled">Whether the Kraken recruitment states are offered.</param>
public sealed record SectionContext(
    int? SelectedCharacterId = null,
    bool BetterLeonaEnabled = false,
    bool KrakenRecruitmentEnabled = false);

/// <summary>Builds the editable sections for whichever game a save turns out to be.</summary>
public static class SuikodenSectionFactory
{
    /// <summary>The sections, in sidebar order, with their display titles and subtitles.</summary>
    public static IReadOnlyList<(SectionKind Kind, string Title, string Subtitle)> Sections { get; } =
    [
        (SectionKind.Overview, "Overview", "Names, money, and whole-save values."),
        (SectionKind.Party, "Party", "Who is in the battle party."),
        (SectionKind.Characters, "Characters", "Levels, stats, equipment, and runes."),
        (SectionKind.Inventory, "Inventory", "Carried, stored, and container items."),
        (SectionKind.Recruitment, "Recruitment", "Who has joined."),
        (SectionKind.Progress, "Headquarters / Progress", "Headquarters level and story state."),
        (SectionKind.AdvancedData, "Advanced Data", "The whole decrypted save, read-only."),
        (SectionKind.Credits, "Credits / Licenses", "Attribution and licence texts."),
    ];

    /// <summary>Builds one section's fields for the open document.</summary>
    /// <remarks>
    /// Returns <see langword="null"/> for sections that are not field lists — Advanced Data and
    /// Credits render their own body — and for a section a given game has nothing to offer in.
    /// </remarks>
    public static SectionEditor? Create(
        SectionKind kind,
        SaveDocument document,
        SnapshotEditHistory history,
        SectionContext context)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(context);

        if (kind is SectionKind.AdvancedData or SectionKind.Credits)
        {
            return null;
        }

        SectionBuilder builder = new(document.Root, history);

        switch (document.Game)
        {
            case GameKind.Suikoden1:
                Suikoden1Sections.Build(kind, document, builder, context);
                break;
            case GameKind.Suikoden2:
                Suikoden2Sections.Build(kind, document, builder, context);
                break;
            default:
                return null;
        }

        (SectionKind Kind, string Title, string Subtitle) descriptor =
            Sections.Single(section => section.Kind == kind);

        return builder.Build(kind.ToString(), descriptor.Title);
    }
}
