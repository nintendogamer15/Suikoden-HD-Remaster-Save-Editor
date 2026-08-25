// SPDX-License-Identifier: 0BSD
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.Core.Tests;

public sealed class CompatibilityTests
{
    [Fact]
    public void DecryptsSyntheticEnvelopeProducedByUpstreamOracle()
    {
        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "upstream-synthetic-s1.save");

        SaveDocument document = SaveDocument.OpenEncrypted(fixture);

        Assert.Equal(GameKind.Suikoden1, document.Game);
        Assert.Equal("Synthetic Hero", document.Root["playerName"]!.GetValue<string>());
    }

    [Fact]
    public void PrivateOptInChecksCopiedSavesAndOracleBothDirections()
    {
        string? saveRoot = Environment.GetEnvironmentVariable("SUIKODEN_PRIVATE_SAVE_ROOT");
        if (string.IsNullOrWhiteSpace(saveRoot) || !Directory.Exists(saveRoot))
        {
            return;
        }

        string[] originals = Directory.EnumerateFiles(saveRoot, "Data*", SearchOption.AllDirectories)
            .Where(path => SlotDetector.FromPath(path).HasValue)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(originals);
        Dictionary<string, byte[]> hashes = originals.ToDictionary(
            path => path,
            path => SHA256.HashData(File.ReadAllBytes(path)),
            StringComparer.Ordinal);

        using TestDirectory temporary = new();
        List<string> ourOutputs = [];
        foreach (string original in originals)
        {
            string gameFolder = new DirectoryInfo(Path.GetDirectoryName(original)!).Name;
            string copiedFolder = Path.Combine(temporary.Path, gameFolder);
            Directory.CreateDirectory(copiedFolder);
            string copy = Path.Combine(copiedFolder, Path.GetFileName(original));
            File.Copy(original, copy);

            SaveDocument opened = SaveDocument.OpenEncrypted(copy);
            GameKind expected = gameFolder.Equals("gsd1", StringComparison.OrdinalIgnoreCase)
                ? GameKind.Suikoden1
                : GameKind.Suikoden2;
            Assert.Equal(expected, opened.Game);

            string noEdit = Path.Combine(copiedFolder, Path.GetFileName(original) + ".roundtrip");
            new SaveFileService().SaveAs(opened.DeepClone(), noEdit);
            Assert.True(JsonNode.DeepEquals(opened.Root, SaveDocument.OpenEncrypted(noEdit).Root));

            SaveDocument edited = opened.DeepClone();
            string valuePath = expected == GameKind.Suikoden1 ? "mochi_kin" : "gold";
            JsonObject party = edited.Root["party_data"]!.AsObject();
            int previous = party[valuePath]!.GetValue<int>();
            party[valuePath] = previous + 1;
            string output = Path.Combine(copiedFolder, Path.GetFileName(original) + ".edited");
            new SaveFileService().SaveAs(edited, output);
            SaveDocument reopened = SaveDocument.OpenEncrypted(output);
            Assert.Equal(previous + 1, reopened.Root["party_data"]![valuePath]!.GetValue<int>());
            Assert.True(JsonNode.DeepEquals(edited.Root, reopened.Root));
            ourOutputs.Add(output);
        }

        RunUpstreamOracleIfAvailable(ourOutputs, temporary.Path);

        foreach ((string original, byte[] before) in hashes)
        {
            Assert.Equal(before, SHA256.HashData(File.ReadAllBytes(original)));
        }
    }

    private static void RunUpstreamOracleIfAvailable(IReadOnlyList<string> encryptedFiles, string outputDirectory)
    {
        string? oracle = Environment.GetEnvironmentVariable("SUIKODEN_UPSTREAM_ORACLE_DLL");
        if (string.IsNullOrWhiteSpace(oracle) || !File.Exists(oracle))
        {
            return;
        }

        string dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        foreach (string encrypted in encryptedFiles.Take(2))
        {
            string jsonOutput = Path.Combine(outputDirectory, $"oracle-{Guid.NewGuid():N}.json");
            ProcessStartInfo startInfo = new(dotnet)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            startInfo.Environment["DOTNET_ROLL_FORWARD"] = "Major";
            startInfo.ArgumentList.Add(oracle);
            startInfo.ArgumentList.Add("decrypt");
            startInfo.ArgumentList.Add("-i");
            startInfo.ArgumentList.Add(encrypted);
            startInfo.ArgumentList.Add("-o");
            startInfo.ArgumentList.Add(jsonOutput);

            using Process process = Process.Start(startInfo)!;
            Assert.True(process.WaitForExit(30_000));
            string error = process.StandardError.ReadToEnd();
            Assert.True(process.ExitCode == 0, error);
            SaveDocument oracleDocument = SaveDocument.Parse(File.ReadAllText(jsonOutput));
            Assert.True(JsonNode.DeepEquals(SaveDocument.OpenEncrypted(encrypted).Root, oracleDocument.Root));
        }
    }
}

