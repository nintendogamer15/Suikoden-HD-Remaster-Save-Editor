// SPDX-License-Identifier: 0BSD
using System.Text;
using System.Text.Json.Nodes;
using SaveEditor.Ui.Codecs;
using SaveEditor.Ui.Workflow;
using SuikodenHdSaveEditor.App.Saves;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.App.Tests;

/// <summary>
/// Pins the guarantees the editor makes about not damaging a save.
/// </summary>
/// <remarks>
/// These cover the seams introduced by moving onto the shared GUI framework, where the
/// framework trusts the application to supply something correct and cannot check it: the
/// document comparer, the round-trip equivalence relation, and the envelope the codec writes.
/// A wrong answer in any of them is silent, so each is tested in both directions.
/// </remarks>
public class SaveSafetyTests
{
    [Fact]
    public void ComparerRejectsAChangeToAFieldTheEditorNeverExposes()
    {
        // The framework's pre-replace guard asks this comparer whether what it decoded matches
        // what is in memory. A comparer that only looked at the fields the UI edits would make
        // that check pass unconditionally and report a lossy write as "Saved."
        using TestSaves saves = new();
        SaveDocument left = SaveDocument.OpenEncrypted(saves.CreateSave());
        SaveDocument right = SaveDocument.OpenEncrypted(saves.CreateSave());

        Assert.True(SuikodenDocumentComparer.Instance.Equals(left, right));

        right.Root["private_unknown"]!["keep"] = false;

        Assert.False(SuikodenDocumentComparer.Instance.Equals(left, right));
    }

    [Fact]
    public void ComparerTreatsIdenticalContentInDifferentInstancesAsEqual()
    {
        // SaveDocument is a mutable class with no equality contract, so the default comparer
        // would compare references, never match, and fail every save.
        using TestSaves saves = new();
        SaveDocument first = SaveDocument.OpenEncrypted(saves.CreateSave());
        SaveDocument second = SaveDocument.OpenEncrypted(saves.CreateSave());

        Assert.False(ReferenceEquals(first, second));
        Assert.True(SuikodenDocumentComparer.Instance.Equals(first, second));
    }

    [Fact]
    public async Task RoundTripEquivalenceHoldsAcrossFreshSaltsAndFailsOnRealDifferences()
    {
        // Encrypting the same document twice produces different bytes by design: the salt is
        // random and the key and IV are derived from it. Byte equality would report the
        // preservation claim false on every open, and the only way to make it true would be to
        // pin the salt, which means reusing an AES-CBC key and IV across differing plaintexts.
        using TestSaves saves = new();
        SuikodenSaveCodec codec = new();
        SaveDocument document = SaveDocument.OpenEncrypted(saves.CreateSave());

        byte[] first = await SerializeAsync(codec, document);
        byte[] second = await SerializeAsync(codec, document);

        Assert.False(first.AsSpan().SequenceEqual(second), "a fresh salt should change the bytes");
        Assert.True(codec.RoundTripEquivalent(first, second), "same document, so it is equivalent");

        document.Root["private_unknown"]!["keep"] = false;
        byte[] changed = await SerializeAsync(codec, document);

        // The negative half matters as much as the positive one: a relation that always
        // returned true would earn a clean verdict for a codec that loses data.
        Assert.False(codec.RoundTripEquivalent(first, changed));
    }

    [Fact]
    public async Task CodecRoundTripsUnknownDataAndKeepsTheEnvelopeTheGameWrites()
    {
        using TestSaves saves = new();
        SuikodenSaveCodec codec = new();
        SaveDocument original = SaveDocument.OpenEncrypted(saves.CreateSave());

        byte[] written = await SerializeAsync(codec, original);

        // The pre-migration writer emitted a UTF-8 BOM ahead of the header. Dropping it would
        // change the bytes of every save this editor produces.
        Assert.Equal([0xEF, 0xBB, 0xBF], written.Take(3));
        Assert.StartsWith(SaveCrypto.Header, Encoding.UTF8.GetString(written.AsSpan(3)), StringComparison.Ordinal);

        using MemoryStream source = new(written);
        SaveDocument decoded = await codec.DecodeAsync(source);

        Assert.True(SaveDocument.SemanticallyEquals(original.Root, decoded.Root));
        Assert.True(decoded.Root["private_unknown"]!["keep"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DetectorAcceptsTheEnvelopeWithAndWithoutABom(bool withBom)
    {
        // DecryptEnvelope strips a BOM only if one is present, so a detector that required it
        // would refuse saves the reader accepts.
        SuikodenSaveDetector detector = new();
        byte[] header = withBom
            ? [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(SaveCrypto.Header)]
            : Encoding.UTF8.GetBytes(SaveCrypto.Header);

        Assert.Equal(DetectionVerdict.Confident, detector.Detect(header));
    }

    [Fact]
    public void DetectorDeclinesSomethingThatIsNotASave()
    {
        SuikodenSaveDetector detector = new();

        Assert.Equal(DetectionVerdict.Declined, detector.Detect(Encoding.UTF8.GetBytes("PKnot a save")));
    }

    [Fact]
    public async Task ValidationSurfacesAdapterIssuesWithoutPromotingInformationToAnError()
    {
        // Core has three severities and the framework has two. Mapping Information onto Error
        // would block saves that are fine.
        using TestSaves saves = new();
        SuikodenSaveCodec codec = new();
        SaveDocument document = SaveDocument.OpenEncrypted(saves.CreateSave());

        ValidationReport report = await codec.ValidateAsync(document);

        Assert.All(report.Messages, message => Assert.False(
            message.Severity == SaveEditor.Ui.Codecs.ValidationSeverity.Error && message.Text.Value.Length == 0));
        Assert.False(report.HasErrors);
    }

    [Fact]
    public async Task WritePolicyRefusesSaveAsOntoAnExistingFileAndAllowsEverythingElse()
    {
        SuikodenWritePolicy policy = new();

        WriteDecision refused = await policy.EvaluateAsync(new PlannedWrite
        {
            Kind = PlannedWriteKind.SaveAs,
            DestinationPath = "Data3",
            DestinationExists = true,
            IsCurrentDocument = false,
            BackupWillBeWritten = false,
        });

        Assert.False(refused.IsAllowed);
        Assert.Equal(SuikodenWritePolicy.RefusalMessage, refused.Message);

        // Overwrite and Restore are the deliberate, backed-up paths and must not be narrowed.
        foreach (PlannedWriteKind kind in new[] { PlannedWriteKind.Overwrite, PlannedWriteKind.Restore })
        {
            WriteDecision allowed = await policy.EvaluateAsync(new PlannedWrite
            {
                Kind = kind,
                DestinationPath = "Data3",
                DestinationExists = true,
                IsCurrentDocument = true,
                BackupWillBeWritten = true,
            });

            Assert.True(allowed.IsAllowed);
        }

        WriteDecision newPath = await policy.EvaluateAsync(new PlannedWrite
        {
            Kind = PlannedWriteKind.SaveAs,
            DestinationPath = "Data9",
            DestinationExists = false,
            IsCurrentDocument = false,
            BackupWillBeWritten = false,
        });

        Assert.True(newPath.IsAllowed);
    }

    private static async Task<byte[]> SerializeAsync(SuikodenSaveCodec codec, SaveDocument document)
    {
        using MemoryStream buffer = new();
        await codec.SerializeAsync(document, buffer);
        return buffer.ToArray();
    }
}
