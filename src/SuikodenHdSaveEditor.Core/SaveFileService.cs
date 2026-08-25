// SPDX-License-Identifier: 0BSD
using System.Text;
using System.Text.Json.Nodes;

namespace SuikodenHdSaveEditor.Core;

public sealed class SaveFileService
{
    private static readonly UTF8Encoding Utf8WithBom = new(true, true);
    private readonly TimeProvider timeProvider;
    private readonly Func<string, string> encryptJson;

    public SaveFileService(TimeProvider? timeProvider = null, Func<string, string>? encryptJson = null)
    {
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.encryptJson = encryptJson ?? SaveCrypto.EncryptJson;
    }

    public SaveWriteResult SaveAs(SaveDocument document, string destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        string fullDestination = Path.GetFullPath(destination);
        if (File.Exists(fullDestination))
        {
            throw new SaveEditorException(
                SaveErrorCode.DestinationExists,
                "Save As will not replace an existing file. Choose another path or use Overwrite with Backup.");
        }

        WriteValidated(document.Root, fullDestination, replaceExisting: false, recoveryPath: null);
        document.MarkSaved(fullDestination);
        return new SaveWriteResult(fullDestination, null);
    }

    public SaveWriteResult OverwriteWithBackup(SaveDocument document, string destination)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        string fullDestination = Path.GetFullPath(destination);
        if (!File.Exists(fullDestination))
        {
            throw new SaveEditorException(SaveErrorCode.SourceMissing, "The file selected for overwrite no longer exists.");
        }

        string backupDirectory = Path.Combine(Path.GetDirectoryName(fullDestination)!, "SuikodenSaveEditor Backups");
        Directory.CreateDirectory(backupDirectory);
        string timestamp = timeProvider.GetLocalNow().ToString("yyyyMMdd-HHmmss-fff", System.Globalization.CultureInfo.InvariantCulture);
        string backupPath = UniqueBackupPath(backupDirectory, timestamp, Path.GetFileName(fullDestination));
        File.Copy(fullDestination, backupPath, overwrite: false);

        try
        {
            WriteValidated(document.Root, fullDestination, replaceExisting: true, recoveryPath: backupPath);
            document.MarkSaved(fullDestination);
            return new SaveWriteResult(fullDestination, backupPath);
        }
        catch
        {
            // WriteValidated restores the destination after a post-commit failure.
            throw;
        }
    }

    private void WriteValidated(JsonObject intendedRoot, string destination, bool replaceExisting, string? recoveryPath)
    {
        string? directory = Path.GetDirectoryName(destination);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            throw new SaveEditorException(SaveErrorCode.FileAccess, "The destination directory does not exist.");
        }

        string temporary = Path.Combine(directory, $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");
        bool committed = false;
        try
        {
            string intendedJson = intendedRoot.ToJsonString();
            string envelope = encryptJson(intendedJson);
            using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            using (StreamWriter writer = new(stream, Utf8WithBom))
            {
                writer.Write(envelope);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            VerifyFile(temporary, intendedRoot);
            File.Move(temporary, destination, replaceExisting);
            committed = true;
            VerifyFile(destination, intendedRoot);
        }
        catch (SaveEditorException)
        {
            if (committed)
            {
                Recover(destination, recoveryPath);
            }

            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (committed)
            {
                Recover(destination, recoveryPath);
            }

            throw new SaveEditorException(SaveErrorCode.FileAccess, $"The save could not be written safely: {exception.Message}", exception);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private static void VerifyFile(string path, JsonObject intendedRoot)
    {
        string decrypted = SaveCrypto.DecryptEnvelope(SaveCrypto.ReadEnvelope(path));
        SaveDocument verified = SaveDocument.Parse(decrypted);
        if (!JsonNode.DeepEquals(intendedRoot, verified.Root))
        {
            throw new SaveEditorException(
                SaveErrorCode.OutputVerificationFailed,
                "The encrypted output did not decrypt to the intended edited document.");
        }
    }

    private static void Recover(string destination, string? recoveryPath)
    {
        if (recoveryPath is null || !File.Exists(recoveryPath))
        {
            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            return;
        }

        string restoreTemporary = destination + $".{Guid.NewGuid():N}.restore";
        File.Copy(recoveryPath, restoreTemporary, overwrite: false);
        File.Move(restoreTemporary, destination, overwrite: true);
    }

    private static string UniqueBackupPath(string directory, string timestamp, string fileName)
    {
        string candidate = Path.Combine(directory, $"{timestamp}_{fileName}");
        int suffix = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{timestamp}_{suffix}_{fileName}");
            suffix++;
        }

        return candidate;
    }
}

public sealed record SaveWriteResult(string DestinationPath, string? BackupPath);
