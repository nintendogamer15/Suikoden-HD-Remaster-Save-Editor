// SPDX-License-Identifier: 0BSD
using SaveEditor.Ui.Codecs;
using SaveEditor.Ui.Editing;
using SaveEditor.Ui.Interaction;
using SaveEditor.Ui.Workflow;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.App.Saves;

/// <summary>
/// Adds this editor's two save-safety rules on top of the framework's session.
/// </summary>
/// <remarks>
/// Both rules exist because the framework's defaults are correct in general but weaker than
/// what this editor already promised about people's irreplaceable saves.
/// </remarks>
public sealed class SuikodenDocumentSession : DocumentSession<SaveDocument>
{
    private readonly IUserInteraction interaction;

    /// <summary>Creates the session.</summary>
    public SuikodenDocumentSession(
        SafeFileWorkflow<SaveDocument> workflow,
        IEditHistory history,
        ISaveCodec<SaveDocument> defaultCodec,
        IUserInteraction interaction)
        : base(workflow, history, defaultCodec) =>
        this.interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));

    /// <summary>Raised when a write finished in a state the user has to be told about.</summary>
    /// <remarks>
    /// Carries the text for the inline banner. The modal is raised here; the banner is the
    /// shell's, so it is surfaced as an event rather than reached for directly.
    /// </remarks>
    public event EventHandler<string>? WriteRefused;

    /// <inheritdoc />
    public override async ValueTask SaveAsAsync(CancellationToken cancellationToken = default)
    {
        await base.SaveAsAsync(cancellationToken).ConfigureAwait(true);
        await ReportAsync("Save As failed", cancellationToken).ConfigureAwait(true);
    }

    /// <inheritdoc />
    public override async ValueTask OverwriteWithBackupAsync(CancellationToken cancellationToken = default)
    {
        await base.OverwriteWithBackupAsync(cancellationToken).ConfigureAwait(true);
        await ReportAsync("Overwrite failed", cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Puts a verified backup back over the open document.</summary>
    /// <remarks>
    /// <para>
    /// Core used to restore the backup itself when a write failed after the destination had
    /// already been replaced. The framework's write path is atomic, so that recovery no longer
    /// happens implicitly — it creates and verifies a backup, reports where it is, and leaves
    /// putting it back to the application. This is that step, so recovering does not mean
    /// copying files by hand.
    /// </para>
    /// <para>
    /// The framework decodes and verifies the backup before anything is written, and backs up
    /// the current state first, so a restore that turns out to be the wrong file is itself
    /// recoverable.
    /// </para>
    /// </remarks>
    /// <returns><see langword="true"/> when the document was replaced.</returns>
    public async ValueTask<bool> RestoreFromBackupAsync(
        string backupPath,
        CancellationToken cancellationToken = default)
    {
        if (OpenFile is not { } open)
        {
            return false;
        }

        RestoreResult<SaveDocument> result = await Workflow
            .RestoreFromBackupAsync(backupPath, open, CreateProgress(), cancellationToken)
            .ConfigureAwait(true);

        RecordOutcome(result.Outcome, markSaved: result.Outcome.Status == SaveStatus.Succeeded);

        if (result.Outcome.Status == SaveStatus.Succeeded && result.Document is { } restored)
        {
            ReplaceDocument(restored);
            return true;
        }

        await RaiseAsync("Restore failed", result.Outcome.Message, cancellationToken).ConfigureAwait(true);
        return false;
    }

    private async ValueTask ReportAsync(string title, CancellationToken cancellationToken)
    {
        if (LastOutcome is not { } outcome)
        {
            return;
        }

        // A policy refusal is a Declined outcome, which the shell puts in the status bar and
        // nowhere else. This editor has always refused a destructive Save As with a modal and
        // a banner, and a one-line status message is easy to miss when the answer is "your
        // save was not written".
        if (outcome.Status == SaveStatus.Declined)
        {
            await RaiseAsync(title, outcome.Message, cancellationToken).ConfigureAwait(true);
            return;
        }

        if (outcome.Status != SaveStatus.Succeeded)
        {
            return;
        }

        // A successful write whose round trip was skipped or mismatched is not a write this
        // editor is willing to call clean. Core verified the temporary file before committing
        // and the destination afterwards; the framework reports the equivalent as a verdict, so
        // anything other than Verified is surfaced rather than swallowed. Scoped to Succeeded
        // deliberately: NotReached is the default on every non-write outcome, including a
        // successful open, so an unscoped check would report a failure after opening a file.
        if (outcome.RoundTrip != RoundTripVerification.Verified)
        {
            string detail = string.IsNullOrWhiteSpace(outcome.RoundTripDetail)
                ? "The write completed but its round-trip verification did not confirm the result."
                : outcome.RoundTripDetail;

            await RaiseAsync(
                "Save completed without verification",
                $"{detail}\n\nThe file on disk has not been confirmed to match the document you edited. "
                    + "Keep the backup until you have loaded the save in-game.",
                cancellationToken).ConfigureAwait(true);
        }
    }

    private async ValueTask RaiseAsync(string title, string message, CancellationToken cancellationToken)
    {
        WriteRefused?.Invoke(this, message);
        await interaction.ShowMessageAsync(
            new MessageRequest(title, message),
            cancellationToken).ConfigureAwait(true);
    }
}
