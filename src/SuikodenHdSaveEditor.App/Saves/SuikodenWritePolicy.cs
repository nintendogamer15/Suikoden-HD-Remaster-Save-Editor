// SPDX-License-Identifier: 0BSD
using SaveEditor.Ui.Workflow;

namespace SuikodenHdSaveEditor.App.Saves;

/// <summary>
/// Keeps Save As non-destructive, as this editor has always been.
/// </summary>
/// <remarks>
/// <para>
/// Core's <c>SaveFileService.SaveAs</c> refuses outright when the destination exists, raising
/// <see cref="Core.SaveErrorCode.DestinationExists"/> with the message "Save As will not
/// replace an existing file. Choose another path or use Overwrite with Backup." SaveEditor.Ui
/// is more permissive: it takes a verified backup and asks. That is a reasonable framework
/// default, but it is a wider blast radius than this editor has ever offered, and the whole
/// point of Save As here is that it cannot touch a file the user already has.
/// </para>
/// <para>
/// Refusal covers the file safety. It does not on its own reach the user — a refused write
/// becomes <c>SaveOutcome.Declined</c>, which the shell shows only in the status bar, whereas
/// this editor raised a modal and a persistent banner. <see cref="SuikodenDocumentSession"/>
/// restores that.
/// </para>
/// </remarks>
public sealed class SuikodenWritePolicy : IWritePolicy
{
    /// <summary>The message shown when a Save As is refused.</summary>
    public const string RefusalMessage =
        "Save As will not replace an existing file. Choose another path, or use " +
        "Overwrite + Backup to replace this save deliberately.";

    /// <inheritdoc />
    public ValueTask<WriteDecision> EvaluateAsync(
        PlannedWrite plan,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        cancellationToken.ThrowIfCancellationRequested();

        // Overwrite and Restore are the deliberate, backed-up paths; only Save As is narrowed.
        return ValueTask.FromResult(
            plan is { Kind: PlannedWriteKind.SaveAs, DestinationExists: true }
                ? WriteDecision.Refuse(RefusalMessage)
                : WriteDecision.Proceed);
    }
}
