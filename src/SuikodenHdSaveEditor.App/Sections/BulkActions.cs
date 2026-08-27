// SPDX-License-Identifier: 0BSD
using SaveEditor.Ui.Editing;
using SaveEditor.Ui.Interaction;
using SuikodenHdSaveEditor.App.Editing;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden1;
using SuikodenHdSaveEditor.Formats.Suikoden2;

namespace SuikodenHdSaveEditor.App.Sections;

/// <summary>The convenience actions that change many fields at once.</summary>
/// <remarks>
/// Every one of these is confirmation-gated and undoable in a single step. They are the most
/// destructive things the editor can do on purpose, so the wording is deliberately specific
/// about what is replaced and what is preserved, and the accept button names the action rather
/// than saying "OK".
/// </remarks>
public static class BulkActions
{
    /// <summary>Runs the max-stats-and-equipment action after confirming it.</summary>
    /// <returns>A sentence describing the outcome, or <see langword="null"/> if declined.</returns>
    public static async ValueTask<string?> MaximizeAndEquipPartyAsync(
        SaveDocument document,
        SnapshotEditHistory history,
        IUserInteraction interaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(interaction);

        bool accepted = await interaction.ConfirmAsync(
            new ConfirmationRequest
            {
                Title = "Max and optimize the active battle party?",
                Message = "This sets every active battle character to level 99, 9,999 current/maximum HP, maximum MP, maximum base stats, and weapon level 16 where a weapon exists. It also replaces removable gear with researched, class-compatible end-game equipment and physical or magic accessories. Fixed weapon identities, runes, known locked gear, and unrelated data are preserved.\n\nSome equipment choices are informed recommendations rather than official character builds. Use Undo if the result is not what you want, and verify a copied save in game before overwriting anything important.",
                AcceptLabel = "Max and equip party",
                IsDestructive = true,
            },
            cancellationToken).ConfigureAwait(true);

        if (!accepted)
        {
            return null;
        }

        PartyOptimizationResult result = Run(
            history,
            "Maximized stats and equipped recommended party gear",
            () => document.Game == GameKind.Suikoden1
                ? new Suikoden1Adapter(document).MaximizeAndEquipParty()
                : new Suikoden2Adapter(document).MaximizeAndEquipParty());

        return $"Maximized {result.CharactersUpdated} active battle character(s); updated "
            + $"{result.EquipmentSlotsUpdated} gear slot(s) and preserved "
            + $"{result.LockedOrUnavailableSlotsPreserved} locked or unavailable slot(s).";
    }

    /// <summary>Fills empty Suikoden II party-inventory slots after confirming it.</summary>
    /// <returns>A sentence describing the outcome, or <see langword="null"/> if declined.</returns>
    public static async ValueTask<string?> GiveAllSafeItemsAsync(
        SaveDocument document,
        SnapshotEditHistory history,
        IUserInteraction interaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(interaction);

        if (document.Game != GameKind.Suikoden2)
        {
            return null;
        }

        bool accepted = await interaction.ConfirmAsync(
            new ConfirmationRequest
            {
                Title = "Fill empty party-inventory slots?",
                Message = "This fills only empty party-inventory slots with reviewed, non-key regular items. It excludes story-critical items, keeps existing items and ordering, and never grows the container.",
                AcceptLabel = "Give safe items",
            },
            cancellationToken).ConfigureAwait(true);

        if (!accepted)
        {
            return null;
        }

        int added = Run(
            history,
            "Filled empty party-inventory slots with safe items",
            () => new Suikoden2Adapter(document).GiveAllSafePartyItems());

        return added == 0
            ? "No empty party-inventory slots or new reviewed safe items were available."
            : $"Added {added} reviewed non-key item(s) to empty party-inventory slots.";
    }

    /// <summary>
    /// Runs a bulk edit as one undoable step, rolling the whole thing back if it fails partway.
    /// </summary>
    /// <remarks>
    /// A bulk action touches many characters and slots, so a failure halfway through is exactly
    /// the partial change the editor promises cannot happen. The transaction aborts on any
    /// exception, restoring the tree the batch started from.
    /// </remarks>
    private static T Run<T>(SnapshotEditHistory history, string label, Func<T> edit)
    {
        using IEditTransaction transaction = history.BeginTransaction(label);
        T result = edit();
        transaction.Commit();
        return result;
    }
}
