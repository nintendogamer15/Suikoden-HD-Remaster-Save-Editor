// SPDX-License-Identifier: 0BSD
using System.Globalization;
using SaveEditor.Ui.Editing;
using SuikodenHdSaveEditor.App.Editing;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden1;
using SuikodenHdSaveEditor.Formats.Suikoden2;

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
    ];

    /// <summary>Builds one section's fields for the open document.</summary>
    /// <remarks>
    /// Returns <see langword="null"/> for Advanced Data, which renders its own body, and for a
    /// section a given game has nothing to offer in.
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

        if (kind is SectionKind.AdvancedData)
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

    /// <summary>How the Characters picker narrows the cast.</summary>
    /// <remarks>
    /// Carried over from the pre-migration editor. A save can hold well over a hundred
    /// characters, so picking one out of an unfiltered list is impractical — which is the whole
    /// reason this existed.
    /// </remarks>
    public enum CharacterFilter
    {
        /// <summary>Everyone the save knows about.</summary>
        All,

        /// <summary>Characters who have joined.</summary>
        Recruited,

        /// <summary>Characters who have not joined.</summary>
        Unrecruited,

        /// <summary>The active battle party.</summary>
        CurrentParty,
    }

    /// <summary>The characters the Characters section can show, in display order.</summary>
    /// <remarks>
    /// Suikoden II is filtered to catalogued characters with a positive id, matching what the
    /// editor offered before: the raw array carries entries that are not selectable people.
    /// </remarks>
    public static IReadOnlyList<CharacterChoice> Characters(
        SaveDocument document,
        CharacterFilter filter = CharacterFilter.All,
        string? search = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        IEnumerable<CharacterChoice> choices = All(document).Where(choice => Matches(document, choice, filter));

        string query = search?.Trim() ?? string.Empty;
        if (query.Length > 0)
        {
            choices = choices.Where(choice =>
                choice.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || choice.Id.ToString(CultureInfo.InvariantCulture).Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return [.. choices];
    }

    private static bool Matches(SaveDocument document, CharacterChoice choice, CharacterFilter filter)
    {
        if (filter == CharacterFilter.All)
        {
            return true;
        }

        bool inParty;
        bool recruited;
        if (document.Game == GameKind.Suikoden1)
        {
            Suikoden1Adapter adapter = new(document);
            inParty = adapter.PartyCharacterIds.Contains(choice.Id);
            recruited = adapter.RecruitedCharacterIds.Contains(choice.Id);
        }
        else
        {
            Suikoden2CharacterView character = new Suikoden2Adapter(document).Characters[choice.Id];
            inParty = character.IsInParty;

            // 70 and 71 are the reviewed joined states; anything else is not recruited.
            recruited = character.RecruitmentStatus is 70 or 71;
        }

        return filter switch
        {
            CharacterFilter.Recruited => recruited,
            CharacterFilter.Unrecruited => !recruited,
            CharacterFilter.CurrentParty => inParty,
            _ => true,
        };
    }

    private static IReadOnlyList<CharacterChoice> All(SaveDocument document)
    {
        return document.Game switch
        {
            GameKind.Suikoden1 =>
            [
                .. new Suikoden1Adapter(document).Characters
                    .OrderBy(character => character.Id)
                    .Select(character => new CharacterChoice(character.Id, character.Name)),
            ],
            GameKind.Suikoden2 =>
            [
                .. new Suikoden2Adapter(document).Characters
                    .Where(character => character.Id > 0 && Suikoden2Catalog.Character(character.Id) is not null)
                    .Select(character => new CharacterChoice(character.Id, character.Name)),
            ],
            _ => [],
        };
    }
}

/// <summary>A character the Characters section can be pointed at.</summary>
public sealed record CharacterChoice(int Id, string Name)
{
    /// <summary>What the picker shows.</summary>
    public override string ToString() => $"{Id}: {Name}";
}
