// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SaveEditor.Ui.Editing;
using SuikodenHdSaveEditor.App.Editing;
using SuikodenHdSaveEditor.App.Sections;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.App.Tests;

/// <summary>
/// Pins the editing semantics the architecture documents: a failed operation cannot leave the
/// document partly changed, and undo restores the whole tree.
/// </summary>
/// <remarks>
/// The framework turns a throwing write into a rejection but is explicit that it will not
/// compensate a setter that mutated before it threw. The adapters do exactly that, so these
/// tests use setters that mutate first — a setter that validated before assigning would pass
/// even if the editor's own rollback were deleted.
/// </remarks>
public class EditingTests
{
    [Fact]
    public void ApplyingOneFieldThenUndoingRestoresTheWholeTree()
    {
        using TestSaves saves = new();
        SaveDocument document = SaveDocument.OpenEncrypted(saves.CreateSave());
        SnapshotEditHistory history = new();
        history.Bind(document.Root);

        SectionEditor section = BuildSection(document, history);
        string before = document.Root.ToJsonString();

        TextFieldViewModel name = section.Fields.OfType<TextFieldViewModel>().First();
        name.Draft = "Renamed";
        Assert.True(name.TryApply());

        Assert.NotEqual(before, document.Root.ToJsonString());
        Assert.True(history.CanUndo);

        history.Undo();

        Assert.Equal(before, document.Root.ToJsonString());
        Assert.False(history.IsDirty);
    }

    [Fact]
    public void UndoIsReportedCleanOnlyWhenTheDocumentIsBackAtTheSavedPoint()
    {
        // Dirty state has to be a comparison rather than a latch, or the exit guard keeps
        // asking about work the user already undid.
        using TestSaves saves = new();
        SaveDocument document = SaveDocument.OpenEncrypted(saves.CreateSave());
        SnapshotEditHistory history = new();
        history.Bind(document.Root);

        SectionEditor section = BuildSection(document, history);
        TextFieldViewModel name = section.Fields.OfType<TextFieldViewModel>().First();

        Assert.False(history.IsDirty);

        name.Draft = "Renamed";
        name.Apply();
        Assert.True(history.IsDirty);

        history.Undo();
        Assert.False(history.IsDirty);

        history.Redo();
        Assert.True(history.IsDirty);
    }

    [Fact]
    public void ARejectedWriteLeavesTheDocumentUntouchedEvenWhenTheSetterMutatedFirst()
    {
        using TestSaves saves = new();
        SaveDocument document = SaveDocument.OpenEncrypted(saves.CreateSave());
        SnapshotEditHistory history = new();
        history.Bind(document.Root);

        string before = document.Root.ToJsonString();
        SectionBuilder builder = new(document.Root, history);
        builder.AddNumber(
            "Rejecting field",
            "party_data.mochi_kin",
            () => document.Root["party_data"]!["mochi_kin"]!.GetValue<int>(),
            _ =>
            {
                document.Root["party_data"]!["mochi_kin"] = 4321;
                throw new SaveEditorException(SaveErrorCode.ValidationFailed, "Rejected.");
            });

        SectionEditor section = builder.Build("test", "Test");
        NumericFieldViewModel field = section.Fields.OfType<NumericFieldViewModel>().Single();

        // A numeric field's pending state is driven by its Text, not its Draft: HasPendingEdit
        // is overridden to compare the typed text against the committed value. Setting Draft
        // alone leaves Text stale and the field reports nothing to apply.
        field.Text = "99";

        Assert.False(field.TryApply());
        Assert.Equal(before, document.Root.ToJsonString());
        Assert.False(string.IsNullOrEmpty(field.ValidationError));
        Assert.True(field.HasPendingEdit);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void ApplyAllIsAllOrNothingWhenOneFieldRejects()
    {
        // The whole point of Apply All being transactional: a batch that fails partway cannot
        // leave the earlier fields written.
        using TestSaves saves = new();
        SaveDocument document = SaveDocument.OpenEncrypted(saves.CreateSave());
        SnapshotEditHistory history = new();
        history.Bind(document.Root);

        string before = document.Root.ToJsonString();
        SectionBuilder builder = new(document.Root, history);
        builder.AddString(
            "Accepting field",
            "playerName",
            () => document.Root["playerName"]!.GetValue<string>(),
            value => document.Root["playerName"] = value);
        builder.AddNumber(
            "Rejecting field",
            "party_data.mochi_kin",
            () => document.Root["party_data"]!["mochi_kin"]!.GetValue<int>(),
            _ =>
            {
                document.Root["party_data"]!["mochi_kin"] = 4321;
                throw new SaveEditorException(SaveErrorCode.ValidationFailed, "Rejected.");
            });

        SectionEditor section = builder.Build("test", "Test");
        history.Restored += (_, _) => GuardedEdit.RefreshPreservingRejections(section);

        section.Fields.OfType<TextFieldViewModel>().Single().Draft = "Applied";
        section.Fields.OfType<NumericFieldViewModel>().Single().Text = "99";

        section.ApplyAll();

        Assert.Equal(before, document.Root.ToJsonString());
        Assert.False(history.CanUndo);

        // The rejection has to survive the rollback refresh, or the batch reverts with no
        // explanation of which field caused it.
        Assert.False(string.IsNullOrEmpty(section.Fields.OfType<NumericFieldViewModel>().Single().ValidationError));
    }

    [Fact]
    public void ApplyingOneFieldDoesNotDiscardAnotherFieldsPendingDraft()
    {
        // Refreshing on every recorded edit would wipe drafts across the editor, because
        // refresh re-reads the committed value.
        using TestSaves saves = new();
        SaveDocument document = SaveDocument.OpenEncrypted(saves.CreateSave());
        SnapshotEditHistory history = new();
        history.Bind(document.Root);

        SectionBuilder builder = new(document.Root, history);
        builder.AddString("First", "playerName", () => document.Root["playerName"]!.GetValue<string>(), value => document.Root["playerName"] = value);
        builder.AddString("Second", "playerCName", () => document.Root["playerCName"]!.GetValue<string>(), value => document.Root["playerCName"] = value);

        SectionEditor section = builder.Build("test", "Test");
        history.Restored += (_, _) => GuardedEdit.RefreshPreservingRejections(section);

        TextFieldViewModel first = (TextFieldViewModel)section.Fields[0];
        TextFieldViewModel second = (TextFieldViewModel)section.Fields[1];

        second.Draft = "TypedButNotApplied";
        first.Draft = "Applied";
        first.Apply();

        Assert.True(second.HasPendingEdit);
        Assert.Equal("TypedButNotApplied", second.Draft);
    }

    [Fact]
    public void RebindingToAnotherDocumentDropsHistoryForThePreviousOne()
    {
        // Undo steps describe a tree that is no longer open; replaying one into a different
        // save would write values from another file.
        using TestSaves saves = new();
        SaveDocument first = SaveDocument.OpenEncrypted(saves.CreateSave());
        SnapshotEditHistory history = new();
        history.Bind(first.Root);

        SectionEditor section = BuildSection(first, history);
        section.Fields.OfType<TextFieldViewModel>().First().Draft = "Renamed";
        section.Fields.OfType<TextFieldViewModel>().First().Apply();
        Assert.True(history.CanUndo);

        SaveDocument second = SaveDocument.OpenEncrypted(saves.CreateSuikoden2Save());
        history.Bind(second.Root);

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.IsDirty);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyingAFieldClearsItsPendingStateInEverySection(bool suikoden2)
    {
        // The regression this exists for: FieldViewModel.Committed calls the descriptor's reader
        // every time it is asked, so a reader closing over a value captured while the section was
        // built reports the pre-edit value forever. The document was written correctly, but every
        // field stayed pending, its Apply button never settled, and the exit guard believed there
        // was unapplied work in a save that had none. Asserting on the document alone missed it,
        // which is why this asserts on the field.
        using TestSaves saves = new();
        SaveDocument document = SaveDocument.OpenEncrypted(
            suikoden2 ? saves.CreateSuikoden2Save() : saves.CreateSave());
        SnapshotEditHistory history = new();
        history.Bind(document.Root);

        int? character = SuikodenSectionFactory.Characters(document) is [{ } first, ..] ? first.Id : null;
        SectionContext context = new(character);

        foreach (SectionKind kind in Enum.GetValues<SectionKind>())
        {
            if (SuikodenSectionFactory.Create(kind, document, history, context) is not { } section)
            {
                continue;
            }

            foreach (FieldViewModel field in section.Fields)
            {
                // Nothing has been typed, so nothing may report itself as pending. A stale
                // reader shows up here even before anything is applied.
                Assert.False(field.HasPendingEdit, $"{kind}/{field.Label} was pending before any edit");
            }

            if (section.Fields.OfType<TextFieldViewModel>().FirstOrDefault(f => !f.IsReadOnly) is { } text)
            {
                text.Draft = "Edited";
                if (text.TryApply())
                {
                    Assert.Equal("Edited", text.Committed);
                    Assert.False(text.HasPendingEdit);
                }
            }

            Assert.False(section.HasPendingEdits, $"{kind} still reported pending edits after applying");
        }
    }

    private static SectionEditor BuildSection(SaveDocument document, SnapshotEditHistory history) =>
        SuikodenSectionFactory.Create(SectionKind.Overview, document, history, new SectionContext())
            ?? throw new InvalidOperationException("Overview should always build.");
}
