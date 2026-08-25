// SPDX-License-Identifier: 0BSD
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.Core.Tests;

public sealed class SaveSlotBrowserTests
{
    [Fact]
    public void FindsGsdFoldersAndOnlyRecognizedSlots()
    {
        using TestDirectory directory = new();
        Directory.CreateDirectory(Path.Combine(directory.Path, "gsd1"));
        Directory.CreateDirectory(Path.Combine(directory.Path, "gsd2"));
        File.WriteAllText(Path.Combine(directory.Path, "gsd1", "Data1"), "synthetic");
        File.WriteAllText(Path.Combine(directory.Path, "gsd2", "Data16"), "synthetic");
        File.WriteAllText(Path.Combine(directory.Path, "gsd2", "notes.txt"), "synthetic");

        IReadOnlyList<SaveSlotEntry> entries = SaveSlotBrowser.Discover(directory.Path);

        Assert.Collection(
            entries,
            first =>
            {
                Assert.Equal(GameKind.Suikoden1, first.GameHint);
                Assert.Equal(1, first.Slot);
            },
            second =>
            {
                Assert.Equal(GameKind.Suikoden2, second.GameHint);
                Assert.Equal(16, second.Slot);
            });
    }
}

