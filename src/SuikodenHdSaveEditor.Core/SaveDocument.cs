// SPDX-License-Identifier: 0BSD
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SuikodenHdSaveEditor.Core;

public sealed class SaveDocument
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
        MaxDepth = 256,
    };

    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        Encoder = JavaScriptEncoder.Default,
        WriteIndented = false,
    };

    private static readonly JsonSerializerOptions PrettyOptions = new(CompactOptions)
    {
        WriteIndented = true,
    };

    private SaveDocument(JsonObject root, GameKind game, string? originalPath)
    {
        Root = root;
        Game = game;
        OriginalPath = originalPath;
        Slot = SlotDetector.FromPath(originalPath);
    }

    public JsonObject Root { get; }

    public GameKind Game { get; }

    public string? OriginalPath { get; private set; }

    public int? Slot { get; private set; }

    public bool HasUnsavedChanges { get; private set; }

    public static SaveDocument OpenEncrypted(string path)
    {
        string envelope = SaveCrypto.ReadEnvelope(path);
        string json = SaveCrypto.DecryptEnvelope(envelope);
        return Parse(json, Path.GetFullPath(path));
    }

    public static SaveDocument Parse(string json, string? originalPath = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new SaveEditorException(SaveErrorCode.InvalidJson, "The decrypted save JSON is empty.");
        }

        try
        {
            JsonNode? node = JsonNode.Parse(json, null, DocumentOptions);
            if (node is not JsonObject root)
            {
                throw new SaveEditorException(SaveErrorCode.InvalidJson, "The decrypted save must contain one JSON object at its root.");
            }

            return new SaveDocument(root, GameDetector.Detect(root), originalPath);
        }
        catch (SaveEditorException)
        {
            throw;
        }
        catch (JsonException exception)
        {
            throw new SaveEditorException(
                SaveErrorCode.InvalidJson,
                $"The decrypted save contains invalid JSON near line {exception.LineNumber}, byte {exception.BytePositionInLine}.",
                exception);
        }
    }

    public SaveDocument DeepClone()
    {
        JsonObject clone = (JsonObject)Root.DeepClone();
        SaveDocument result = new(clone, Game, OriginalPath)
        {
            HasUnsavedChanges = HasUnsavedChanges,
        };
        return result;
    }

    public string ToJson(bool indented = false) => Root.ToJsonString(indented ? PrettyOptions : CompactOptions);

    public void MarkChanged() => HasUnsavedChanges = true;

    public void MarkSaved(string path)
    {
        OriginalPath = Path.GetFullPath(path);
        Slot = SlotDetector.FromPath(OriginalPath);
        HasUnsavedChanges = false;
    }

    public static bool SemanticallyEquals(JsonNode? left, JsonNode? right) => JsonNode.DeepEquals(left, right);
}
