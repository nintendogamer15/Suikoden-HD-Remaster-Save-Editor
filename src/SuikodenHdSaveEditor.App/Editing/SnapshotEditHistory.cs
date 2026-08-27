// SPDX-License-Identifier: 0BSD
// Backs SaveEditor.Ui's IEditHistory with whole-document JSON snapshots so a failed edit
// restores the entire tree, preserving docs/ARCHITECTURE.md:19-20.
using System.Text.Json.Nodes;
using SaveEditor.Ui.Editing;

namespace SuikodenHdSaveEditor.App.Editing;

public sealed class SnapshotEditHistory : IEditHistory
{
    private JsonObject? live;
    private readonly List<Step> steps = [];
    private int position;          // number of applied steps
    private int savedPosition;
    private JsonObject? staged;         // before-tree stashed by the write wrapper
    private JsonObject? transactionBefore;   // non-null while a transaction is open
    private string? transactionLabel;

    /// <summary>
    /// Points the history at a document's tree, discarding any history for the previous one.
    /// </summary>
    /// <remarks>
    /// The session is built before any document exists and the tree is replaced on every open,
    /// reload, and restore, so the history is bound on <c>DocumentChanged</c> rather than at
    /// construction. Until then it holds nothing and every operation is a no-op.
    /// </remarks>
    public void Bind(JsonObject? document)
    {
        live = document;
        Clear();
    }

    public event EventHandler? Changed;

    public bool CanUndo => live is not null && position > 0;

    public bool CanRedo => live is not null && position < steps.Count;

    // Revision comparison, not a latch: undoing back to the saved point reports clean again.
    public bool IsDirty => position != savedPosition;

    /// <summary>
    /// Stashes the pre-write tree. The framework mutates inside CommitDraft() and only then
    /// calls Record(), so by record time the before-state is gone; the field Write wrapper
    /// stages it here first and Record consumes it.
    /// </summary>
    public void StageBefore(JsonObject before) => staged = Clone(before);

    public void Record(HistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (live is null)
        {
            staged = null;
            return;
        }

        // Inside a transaction the operation folds into it; the transaction's own
        // before-snapshot already covers the batch.
        if (transactionBefore is not null)
        {
            staged = null;
            return;
        }

        JsonObject before = staged ?? Clone(live);
        staged = null;
        Push(entry.Label, before, Clone(live!));
    }

    public IEditTransaction BeginTransaction(string label)
    {
        transactionBefore = live is null ? null : Clone(live);
        transactionLabel = label;
        return new Transaction(this);
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        // Restore first, move the pointer only if it succeeded: a failed undo must leave the
        // history exactly as it was rather than being counted as a successful one.
        Restore(steps[position - 1].Before);
        position--;
        Raise();
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        Restore(steps[position].After);
        position++;
        Raise();
    }

    public void MarkSaved()
    {
        savedPosition = position;
        Raise();
    }

    public void Clear()
    {
        steps.Clear();
        position = 0;
        savedPosition = 0;
        staged = null;
        transactionBefore = null;
        transactionLabel = null;
        Raise();
    }

    /// <summary>Raised after a wholesale restore, so sections can refresh. Never on Record.</summary>
    public event EventHandler? Restored;

    private void Push(string label, JsonObject before, JsonObject after)
    {
        if (JsonNode.DeepEquals(before, after))
        {
            return;   // no-op edits leave no undo step
        }

        if (position < steps.Count)
        {
            steps.RemoveRange(position, steps.Count - position);
            if (savedPosition > position)
            {
                savedPosition = -1;   // the saved state is no longer reachable
            }
        }

        steps.Add(new Step(label, before, after));
        position++;
        Raise();
    }

    /// <summary>
    /// Copies <paramref name="source"/> over <paramref name="target"/> in place.
    /// </summary>
    /// <remarks>
    /// Mutates the existing instance rather than replacing it, so every descriptor Read/Write
    /// closure captured over this document stays bound. Replacing the document object, as the
    /// pre-migration code did, orphans all of them.
    /// </remarks>
    public static void RestoreInPlace(JsonObject target, JsonObject source)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        target.Clear();
        foreach (KeyValuePair<string, JsonNode?> property in source)
        {
            target[property.Key] = property.Value?.DeepClone();
        }
    }

    private void Restore(JsonObject snapshot)
    {
        RestoreInPlace(live!, snapshot);
        Restored?.Invoke(this, EventArgs.Empty);
    }

    private void Raise() => Changed?.Invoke(this, EventArgs.Empty);

    private static JsonObject Clone(JsonObject source) => (JsonObject)source.DeepClone();

    private sealed record Step(string Label, JsonObject Before, JsonObject After);

    private void CommitTransaction()
    {
        JsonObject? before = transactionBefore;
        string label = transactionLabel ?? "Edit";
        transactionBefore = null;   // cleared on both exits, so a later batch can still start
        transactionLabel = null;

        if (before is not null)
        {
            Push(label, before, Clone(live!));
        }
    }

    private void AbortTransaction()
    {
        JsonObject? before = transactionBefore;
        transactionBefore = null;   // cleared on both exits
        transactionLabel = null;

        if (before is not null)
        {
            Restore(before);
        }
    }

    private sealed class Transaction(SnapshotEditHistory history) : IEditTransaction
    {
        private bool finished;

        public void Commit()
        {
            if (finished)
            {
                return;   // committing after Abort does nothing
            }

            finished = true;
            history.CommitTransaction();
        }

        public void Abort()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            history.AbortTransaction();
        }

        public void Dispose() => Abort();   // disposing without committing aborts
    }
}
