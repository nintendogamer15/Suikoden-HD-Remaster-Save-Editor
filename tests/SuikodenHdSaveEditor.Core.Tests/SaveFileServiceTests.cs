// SPDX-License-Identifier: 0BSD
using System.Security.Cryptography;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.Core.Tests;

public sealed class SaveFileServiceTests
{
    [Fact]
    public void SaveAsWritesRevalidatedEncryptedOutput()
    {
        using TestDirectory directory = new();
        string destination = Path.Combine(directory.Path, "Data3");
        SaveDocument document = TestSaveFactory.Suikoden1();

        SaveWriteResult result = new SaveFileService().SaveAs(document, destination);

        Assert.Equal(destination, result.DestinationPath);
        Assert.Null(result.BackupPath);
        Assert.True(File.Exists(destination));
        Assert.Equal(3, document.Slot);
        Assert.True(SaveDocument.SemanticallyEquals(document.Root, SaveDocument.OpenEncrypted(destination).Root));
    }

    [Fact]
    public void SaveAsRefusesExistingDestination()
    {
        using TestDirectory directory = new();
        string destination = Path.Combine(directory.Path, "Data3");
        File.WriteAllText(destination, "unchanged");

        SaveEditorException exception = Assert.Throws<SaveEditorException>(
            () => new SaveFileService().SaveAs(TestSaveFactory.Suikoden1(), destination));

        Assert.Equal(SaveErrorCode.DestinationExists, exception.Code);
        Assert.Equal("unchanged", File.ReadAllText(destination));
    }

    [Fact]
    public void OverwriteCreatesTimestampedBackupAndValidOutput()
    {
        using TestDirectory directory = new();
        string destination = Path.Combine(directory.Path, "Data4");
        File.WriteAllText(destination, "original bytes");
        byte[] originalHash = SHA256.HashData(File.ReadAllBytes(destination));
        SaveFileService service = new(new FixedTimeProvider());

        SaveWriteResult result = service.OverwriteWithBackup(TestSaveFactory.Suikoden2(), destination);

        Assert.NotNull(result.BackupPath);
        Assert.True(File.Exists(result.BackupPath));
        Assert.Equal(originalHash, SHA256.HashData(File.ReadAllBytes(result.BackupPath)));
        Assert.Equal(GameKind.Suikoden2, SaveDocument.OpenEncrypted(destination).Game);
    }

    [Fact]
    public void EncryptionFailureLeavesExistingDestinationUnchanged()
    {
        using TestDirectory directory = new();
        string destination = Path.Combine(directory.Path, "Data5");
        File.WriteAllText(destination, "irreplaceable bytes");
        byte[] original = File.ReadAllBytes(destination);
        SaveFileService service = new(encryptJson: _ => throw new IOException("Synthetic failure"));

        SaveEditorException exception = Assert.Throws<SaveEditorException>(
            () => service.OverwriteWithBackup(TestSaveFactory.Suikoden1(), destination));

        Assert.Equal(SaveErrorCode.FileAccess, exception.Code);
        Assert.Equal(original, File.ReadAllBytes(destination));
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp", SearchOption.AllDirectories));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 25, 12, 34, 56, TimeSpan.Zero);
    }
}
