// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.Core.Tests;

public sealed class EditHistoryTests
{
    [Fact]
    public void UndoRedoRestoresWholeLosslessTree()
    {
        JsonObject before = TestSaveFactory.Suikoden1().Root;
        JsonObject after = (JsonObject)before.DeepClone();
        after["party_data"]!["mochi_kin"] = 9000;
        EditHistory history = new();
        history.Record("Change Potch", before, after);

        HistoryResult undo = history.Undo(after);
        HistoryResult redo = history.Redo(undo.Root);

        Assert.True(undo.Applied);
        Assert.True(JsonNode.DeepEquals(before, undo.Root));
        Assert.True(redo.Applied);
        Assert.True(JsonNode.DeepEquals(after, redo.Root));
    }

    [Fact]
    public void NoOpDoesNotCreateHistoryEntry()
    {
        JsonObject value = TestSaveFactory.Suikoden1().Root;
        EditHistory history = new();
        history.Record("No change", value, (JsonObject)value.DeepClone());

        Assert.False(history.CanUndo);
    }
}

