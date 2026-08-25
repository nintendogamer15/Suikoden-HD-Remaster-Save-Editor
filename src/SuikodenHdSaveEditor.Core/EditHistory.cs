// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;

namespace SuikodenHdSaveEditor.Core;

public sealed class EditHistory
{
    private readonly Stack<Snapshot> undo = new();
    private readonly Stack<Snapshot> redo = new();

    public bool CanUndo => undo.Count > 0;

    public bool CanRedo => redo.Count > 0;

    public void Record(string description, JsonObject before, JsonObject after)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);

        if (JsonNode.DeepEquals(before, after))
        {
            return;
        }

        undo.Push(new Snapshot(description, (JsonObject)before.DeepClone(), (JsonObject)after.DeepClone()));
        redo.Clear();
    }

    public HistoryResult Undo(JsonObject current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (!undo.TryPop(out Snapshot? snapshot))
        {
            return new HistoryResult(false, string.Empty, current);
        }

        redo.Push(snapshot);
        return new HistoryResult(true, snapshot.Description, (JsonObject)snapshot.Before.DeepClone());
    }

    public HistoryResult Redo(JsonObject current)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (!redo.TryPop(out Snapshot? snapshot))
        {
            return new HistoryResult(false, string.Empty, current);
        }

        undo.Push(snapshot);
        return new HistoryResult(true, snapshot.Description, (JsonObject)snapshot.After.DeepClone());
    }

    public void Clear()
    {
        undo.Clear();
        redo.Clear();
    }

    private sealed record Snapshot(string Description, JsonObject Before, JsonObject After);
}

public sealed record HistoryResult(bool Applied, string Description, JsonObject Root);

