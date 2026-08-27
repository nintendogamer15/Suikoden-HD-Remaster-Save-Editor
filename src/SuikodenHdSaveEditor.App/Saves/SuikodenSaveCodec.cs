// SPDX-License-Identifier: 0BSD
using System.Text;
using System.Text.Json.Nodes;
using SaveEditor.Ui.Codecs;
using SaveEditor.Ui.Interaction;
using SuikodenHdSaveEditor.Core;
using SuikodenHdSaveEditor.Formats.Suikoden1;
using SuikodenHdSaveEditor.Formats.Suikoden2;

namespace SuikodenHdSaveEditor.App.Saves;

/// <summary>
/// Presents the encrypted <c>GR_DATA:</c> envelope to SaveEditor.Ui's file workflow.
/// </summary>
/// <remarks>
/// The codec is deliberately thin: every format decision stays in
/// <see cref="SaveCrypto"/>, <see cref="SaveDocument"/>, and the two adapters, which are the
/// audited parts. Both games share one codec because they share one envelope, and
/// <see cref="GameDetector"/> cannot tell them apart until the payload is decrypted.
/// </remarks>
public sealed class SuikodenSaveCodec : ISaveCodec<SaveDocument>
{
    // The game writes the envelope with a UTF-8 BOM and the pre-migration writer matched it.
    // Changing that would alter the bytes of every save this editor produces.
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true, throwOnInvalidBytes: true);
    private static readonly UTF8Encoding Utf8BomTolerant = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <inheritdoc />
    public SaveFormatDescriptor Format { get; } = new(
        "suikoden.hd.grdata",
        "Suikoden I & II HD Remaster save",
        // The game's slots are extensionless files named Data0 through Data16.
        []);

    /// <inheritdoc />
    /// <remarks>
    /// The document keeps the parsed <see cref="JsonObject"/> verbatim and writes it back
    /// whole, so properties this editor does not understand survive a round trip.
    /// </remarks>
    public bool PreservesUnknownData => true;

    /// <summary>
    /// Compares a re-serialized round trip against the original by decrypted content.
    /// </summary>
    /// <remarks>
    /// The framework's default is byte equality, which this format can never satisfy:
    /// <see cref="SaveCrypto.EncryptJson(string)"/> draws a fresh random salt for every write,
    /// and the key and IV are both derived from it. Reproducing the original bytes would mean
    /// pinning that salt across saves — reusing an AES-CBC key and IV across differing
    /// plaintexts — so byte equality here would have to be bought with a real cryptographic
    /// regression. Comparing the decrypted documents tests what the claim actually means.
    /// </remarks>
    public bool RoundTripEquivalent(ReadOnlySpan<byte> original, ReadOnlySpan<byte> reserialized)
    {
        try
        {
            JsonNode? left = JsonNode.Parse(SaveCrypto.DecryptEnvelope(ReadEnvelope(original)));
            JsonNode? right = JsonNode.Parse(SaveCrypto.DecryptEnvelope(ReadEnvelope(reserialized)));
            return JsonNode.DeepEquals(left, right);
        }
        catch (Exception exception) when (exception is SaveEditorException or System.Text.Json.JsonException)
        {
            // A side that will not decrypt or parse is not equivalent to anything.
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask<SaveDocument> DecodeAsync(Stream source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        using MemoryStream buffer = new();
        await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        string envelope = ReadEnvelope(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
        string json = SaveCrypto.DecryptEnvelope(envelope);

        // Parse also runs GameDetector, which refuses an ambiguous or unrecognised schema.
        return SaveDocument.Parse(json);
    }

    /// <inheritdoc />
    public async ValueTask SerializeAsync(
        SaveDocument document,
        Stream destination,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(destination);

        string envelope = SaveCrypto.EncryptJson(document.Root.ToJsonString());
        byte[] bytes = Utf8WithBom.GetPreamble().Concat(Utf8BomTolerant.GetBytes(envelope)).ToArray();
        await destination.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask<ValidationReport> ValidateAsync(
        SaveDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<ValidationIssue> issues = document.Game switch
        {
            GameKind.Suikoden1 => new Suikoden1Adapter(document).Validate(),
            GameKind.Suikoden2 => new Suikoden2Adapter(document).Validate(),
            _ => [],
        };

        return ValueTask.FromResult(new ValidationReport
        {
            Messages = [.. issues.Select(Translate)],
        });
    }

    private static ValidationMessage Translate(ValidationIssue issue) => new(
        // Core distinguishes Information from Warning; the framework has no third level, and
        // reporting information as an error would block saves that are fine.
        issue.Severity == Core.ValidationSeverity.Error
            ? SaveEditor.Ui.Codecs.ValidationSeverity.Error
            : SaveEditor.Ui.Codecs.ValidationSeverity.Warning,
        new UntrustedText(issue.Message),
        issue.Path);

    private static string ReadEnvelope(ReadOnlySpan<byte> content) =>
        // DecryptEnvelope strips a leading BOM itself, so decoding without one is safe either
        // way; this only avoids double-handling it.
        Utf8BomTolerant.GetString(content);
}
