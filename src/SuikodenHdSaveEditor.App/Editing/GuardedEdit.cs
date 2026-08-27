// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SaveEditor.Ui.Editing;

namespace SuikodenHdSaveEditor.App.Editing;

/// <summary>
/// Wraps a field write so a failed edit cannot leave the document partly changed.
/// </summary>
/// <remarks>
/// <para>
/// SaveEditor.Ui turns a throwing write into a rejection, but it is explicit that it "cannot
/// un-ring a setter that mutated state before throwing, and it deliberately does not attempt a
/// compensating write back". The adapters raise <see cref="Core.SaveEditorException"/> from
/// setters that have already touched the tree, so the guarantee in docs/ARCHITECTURE.md that a
/// failed operation cannot partially change the document has to be made here.
/// </para>
/// <para>
/// The pre-write clone doubles as the history's before-state. The framework mutates inside
/// <c>CommitDraft()</c> and only then calls <c>Record</c>, so by record time the before-tree is
/// gone unless it was staged first.
/// </para>
/// </remarks>
public static class GuardedEdit
{
    /// <summary>Wraps <paramref name="write"/> with snapshot staging and rollback.</summary>
    public static Action<T> Wrap<T>(JsonObject root, SnapshotEditHistory history, Action<T> write)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(write);

        return value =>
        {
            JsonObject before = (JsonObject)root.DeepClone();
            history.StageBefore(before);
            try
            {
                write(value);
            }
            catch
            {
                SnapshotEditHistory.RestoreInPlace(root, before);
                throw;
            }
        };
    }

    /// <summary>
    /// Refreshes a section after a wholesale restore without swallowing a rejection.
    /// </summary>
    /// <remarks>
    /// <see cref="FieldViewModel.RefreshFromDocument"/> re-runs the descriptor's validation and
    /// clears the message a rejected write left behind, and <c>ValidationError</c> has no
    /// accessible setter, so it cannot be put back. The framework's own Apply All never
    /// refreshes after an abort for the same reason: with per-field history the abort replays
    /// only the fields that actually applied, leaving the failing field's message and draft
    /// intact. Skipping fields that carry an error reproduces that.
    /// </remarks>
    public static void RefreshPreservingRejections(SectionEditor section)
    {
        ArgumentNullException.ThrowIfNull(section);

        foreach (FieldViewModel field in section.Fields)
        {
            if (string.IsNullOrEmpty(field.ValidationError))
            {
                field.RefreshFromDocument();
            }
        }
    }
}
