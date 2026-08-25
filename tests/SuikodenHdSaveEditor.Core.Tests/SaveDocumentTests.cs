// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.Core.Tests;

public sealed class SaveDocumentTests
{
    [Fact]
    public void DetectsSuikoden1FromSchema()
    {
        Assert.Equal(GameKind.Suikoden1, TestSaveFactory.Suikoden1().Game);
    }

    [Fact]
    public void DetectsSuikoden2FromSchema()
    {
        Assert.Equal(GameKind.Suikoden2, TestSaveFactory.Suikoden2().Game);
    }

    [Fact]
    public void RejectsInvalidJsonWithUsefulCode()
    {
        SaveEditorException exception = Assert.Throws<SaveEditorException>(() => SaveDocument.Parse("{bad"));
        Assert.Equal(SaveErrorCode.InvalidJson, exception.Code);
    }

    [Fact]
    public void RejectsUnknownSchema()
    {
        SaveEditorException exception = Assert.Throws<SaveEditorException>(() => SaveDocument.Parse("{\"version\":1}"));
        Assert.Equal(SaveErrorCode.UnsupportedSchema, exception.Code);
    }

    [Fact]
    public void LosslessTreePreservesUnknownPropertiesAndArrayOrder()
    {
        SaveDocument document = TestSaveFactory.Suikoden1();
        JsonNode? unknownBefore = document.Root["unknown_root"]!.DeepClone();
        document.Root["party_data"]!["mochi_kin"] = 4321;

        SaveDocument reparsed = SaveDocument.Parse(document.ToJson());

        Assert.True(JsonNode.DeepEquals(unknownBefore, reparsed.Root["unknown_root"]));
        Assert.Equal(4321, reparsed.Root["party_data"]!["mochi_kin"]!.GetValue<int>());
    }

    [Theory]
    [InlineData("Data0", 0)]
    [InlineData("Data16", 16)]
    [InlineData("data7", 7)]
    public void DetectsSlotFromExactDataName(string name, int expected)
    {
        Assert.Equal(expected, SlotDetector.FromPath(Path.Combine("somewhere", name)));
    }

    [Theory]
    [InlineData("Data17")]
    [InlineData("Data1.json")]
    [InlineData("NotData1")]
    public void RejectsUnrecognizedSlotNames(string name)
    {
        Assert.Null(SlotDetector.FromPath(name));
    }
}

