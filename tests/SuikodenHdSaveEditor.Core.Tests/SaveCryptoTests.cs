// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.Core.Tests;

public sealed class SaveCryptoTests
{
    [Fact]
    public void EncryptDecryptRoundTripsUnicodeJson()
    {
        const string json = "{\"name\":\"Synthetic テスト\",\"value\":42}";

        string encrypted = SaveCrypto.EncryptJson(json);
        string decrypted = SaveCrypto.DecryptEnvelope(encrypted);

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(decrypted)));
    }

    [Fact]
    public void EncryptUsesFreshSalt()
    {
        string first = SaveCrypto.EncryptJson(TestSaveFactory.Suikoden1Json);
        string second = SaveCrypto.EncryptJson(TestSaveFactory.Suikoden1Json);

        Assert.NotEqual(first, second);
        Assert.StartsWith(SaveCrypto.Header, first, StringComparison.Ordinal);
        Assert.StartsWith(SaveCrypto.Header, second, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", SaveErrorCode.EmptyInput)]
    [InlineData("wrong", SaveErrorCode.InvalidHeader)]
    [InlineData("GR_DATA:!!!", SaveErrorCode.InvalidBase64)]
    [InlineData("GR_DATA:AA==", SaveErrorCode.TruncatedEnvelope)]
    public void DecryptReportsMalformedEnvelope(string value, SaveErrorCode expected)
    {
        SaveEditorException exception = Assert.Throws<SaveEditorException>(() => SaveCrypto.DecryptEnvelope(value));
        Assert.Equal(expected, exception.Code);
        Assert.False(string.IsNullOrWhiteSpace(exception.Message));
    }

    [Fact]
    public void DecryptReportsTruncatedCiphertextBlock()
    {
        string payload = Convert.ToBase64String(new byte[SaveCrypto.SaltLength + 17]);

        SaveEditorException exception = Assert.Throws<SaveEditorException>(
            () => SaveCrypto.DecryptEnvelope(SaveCrypto.Header + payload));

        Assert.Equal(SaveErrorCode.InvalidCiphertextLength, exception.Code);
    }

    [Fact]
    public void DecryptReportsInvalidPadding()
    {
        string encrypted = SaveCrypto.EncryptJson("{\"synthetic\":true}", new byte[16]);
        byte[] payload = Convert.FromBase64String(encrypted[SaveCrypto.Header.Length..]);
        payload[^1] ^= 0x7F;

        SaveEditorException exception = Assert.Throws<SaveEditorException>(
            () => SaveCrypto.DecryptEnvelope(SaveCrypto.Header + Convert.ToBase64String(payload)));

        Assert.Equal(SaveErrorCode.DecryptionFailed, exception.Code);
    }

    [Fact]
    public void DecryptAcceptsUtf8BomBeforeHeader()
    {
        string encrypted = SaveCrypto.EncryptJson("{\"synthetic\":true}", new byte[16]);

        string decrypted = SaveCrypto.DecryptEnvelope("\uFEFF" + encrypted);

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("{\"synthetic\":true}"), JsonNode.Parse(decrypted)));
    }
}
