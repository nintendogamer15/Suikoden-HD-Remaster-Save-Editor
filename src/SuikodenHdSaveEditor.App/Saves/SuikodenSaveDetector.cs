// SPDX-License-Identifier: 0BSD
using System.Text;
using SaveEditor.Ui.Codecs;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.App.Saves;

/// <summary>
/// Recognises the encrypted <c>GR_DATA:</c> envelope from its leading bytes.
/// </summary>
/// <remarks>
/// The header identifies the envelope, not the game. Suikoden I and II share it, and
/// <see cref="GameDetector"/> needs the whole decrypted object to tell them apart, so one codec
/// covers both and the game is settled during decode.
/// </remarks>
public sealed class SuikodenSaveDetector : ISaveCodecDetector
{
    private static readonly byte[] HeaderBytes = Encoding.UTF8.GetBytes(SaveCrypto.Header);
    private static readonly byte[] Bom = [0xEF, 0xBB, 0xBF];

    /// <inheritdoc />
    public SaveFormatDescriptor Format { get; } = new(
        "suikoden.hd.grdata",
        "Suikoden I & II HD Remaster save",
        []);

    /// <inheritdoc />
    /// <remarks>Enough for the BOM plus the header, since the BOM is optional.</remarks>
    public int HeaderBytesRequired => Bom.Length + HeaderBytes.Length;

    /// <inheritdoc />
    public DetectionVerdict Detect(ReadOnlySpan<byte> header)
    {
        // SaveCrypto.DecryptEnvelope strips a BOM only if one is present, so a file written
        // without it still opens. Requiring the BOM here would reject saves the reader accepts.
        ReadOnlySpan<byte> body = header.StartsWith(Bom) ? header[Bom.Length..] : header;
        return body.StartsWith(HeaderBytes) ? DetectionVerdict.Confident : DetectionVerdict.Declined;
    }
}
