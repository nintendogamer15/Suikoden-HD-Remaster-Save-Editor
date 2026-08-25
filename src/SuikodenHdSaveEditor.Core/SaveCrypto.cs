// SPDX-License-Identifier: MIT
// Substantially ported from d3xMachina/SuikodenSaveDecrypter Crypto.cs.
// Copyright (c) 2025 d3xMachina. See LICENSES/SuikodenSaveDecrypter-MIT.txt.
using System.Security.Cryptography;
using System.Text;

namespace SuikodenHdSaveEditor.Core;

public static class SaveCrypto
{
    public const string Header = "GR_DATA:";
    public const int SaltLength = 16;

    private const string Password = "auDotXSC3fCBCwQk1nQ3aT7Xe1Vk3BmG";
    private const int KeyLength = 32;
    private const int IvLength = 16;
    private static readonly UTF8Encoding StrictUtf8WithBom = new(true, true);

    public static string DecryptEnvelope(string envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.Length == 0)
        {
            throw new SaveEditorException(SaveErrorCode.EmptyInput, "The save file is empty.");
        }

        ReadOnlySpan<char> encoded = envelope.AsSpan();
        if (encoded[0] == '\uFEFF')
        {
            encoded = encoded[1..];
        }

        if (!encoded.StartsWith(Header, StringComparison.Ordinal))
        {
            throw new SaveEditorException(
                SaveErrorCode.InvalidHeader,
                $"The save does not begin with the required {Header} header.");
        }

        encoded = encoded[Header.Length..];
        if (encoded.Length == 0)
        {
            throw new SaveEditorException(SaveErrorCode.TruncatedEnvelope, "The save contains a header but no encrypted data.");
        }

        byte[] encryptedBytes;
        try
        {
            encryptedBytes = Convert.FromBase64String(encoded.ToString());
        }
        catch (FormatException exception)
        {
            throw new SaveEditorException(SaveErrorCode.InvalidBase64, "The save payload is not valid Base64.", exception);
        }

        if (encryptedBytes.Length < SaltLength + 16)
        {
            throw new SaveEditorException(
                SaveErrorCode.TruncatedEnvelope,
                "The encrypted payload is too short to contain its 16-byte salt and ciphertext.");
        }

        int cipherLength = encryptedBytes.Length - SaltLength;
        if (cipherLength % 16 != 0)
        {
            throw new SaveEditorException(
                SaveErrorCode.InvalidCiphertextLength,
                "The encrypted payload is truncated: its ciphertext is not a whole AES block.");
        }

        ReadOnlySpan<byte> salt = encryptedBytes.AsSpan(0, SaltLength);
        (byte[] key, byte[] iv) = CreateKeyAndIv(salt);

        try
        {
            using Aes aes = CreateAes(key, iv);
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            using MemoryStream input = new(encryptedBytes, SaltLength, cipherLength, false);
            using CryptoStream cryptoStream = new(input, decryptor, CryptoStreamMode.Read);
            using StreamReader reader = new(cryptoStream, new UTF8Encoding(false, true), true);
            return reader.ReadToEnd();
        }
        catch (DecoderFallbackException exception)
        {
            throw new SaveEditorException(SaveErrorCode.InvalidUtf8, "The decrypted payload is not valid UTF-8.", exception);
        }
        catch (CryptographicException exception)
        {
            throw new SaveEditorException(
                SaveErrorCode.DecryptionFailed,
                "The save could not be decrypted. Its password, salt, ciphertext, or PKCS#7 padding is invalid.",
                exception);
        }
    }

    public static string EncryptJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        byte[] salt = RandomNumberGenerator.GetBytes(SaltLength);
        return EncryptJson(json, salt);
    }

    public static string EncryptJson(string json, ReadOnlySpan<byte> salt)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (salt.Length != SaltLength)
        {
            throw new ArgumentException($"The salt must be exactly {SaltLength} bytes.", nameof(salt));
        }

        (byte[] key, byte[] iv) = CreateKeyAndIv(salt);
        using MemoryStream output = new();
        output.Write(salt);

        using (Aes aes = CreateAes(key, iv))
        using (ICryptoTransform encryptor = aes.CreateEncryptor())
        using (CryptoStream cryptoStream = new(output, encryptor, CryptoStreamMode.Write, true))
        using (StreamWriter writer = new(cryptoStream, StrictUtf8WithBom, leaveOpen: false))
        {
            writer.Write(json);
        }

        return Header + Convert.ToBase64String(output.ToArray());
    }

    public static string ReadEnvelope(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        try
        {
            return File.ReadAllText(path, new UTF8Encoding(true, true));
        }
        catch (DecoderFallbackException exception)
        {
            throw new SaveEditorException(SaveErrorCode.InvalidUtf8, "The encrypted save envelope is not valid UTF-8.", exception);
        }
        catch (IOException exception)
        {
            throw new SaveEditorException(SaveErrorCode.FileAccess, $"The save could not be read: {exception.Message}", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new SaveEditorException(SaveErrorCode.FileAccess, $"The save could not be read: {exception.Message}", exception);
        }
    }

    private static (byte[] Key, byte[] Iv) CreateKeyAndIv(ReadOnlySpan<byte> salt)
    {
        byte[] password = Encoding.UTF8.GetBytes(Password);
        Span<byte> result = stackalloc byte[KeyLength + IvLength];
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        Span<byte> temporary = stackalloc byte[result.Length + password.Length + salt.Length];

        for (int index = 0; index < result.Length;)
        {
            if (index > 0)
            {
                result[..index].CopyTo(temporary);
            }

            password.CopyTo(temporary[index..]);
            salt.CopyTo(temporary[(index + password.Length)..]);
            int inputLength = index + password.Length + salt.Length;
            if (!SHA256.TryHashData(temporary[..inputLength], hash, out int bytesWritten))
            {
                throw new CryptographicException("SHA-256 key derivation failed.");
            }

            bytesWritten = Math.Min(bytesWritten, result.Length - index);
            hash[..bytesWritten].CopyTo(result[index..]);
            index += bytesWritten;
        }

        return (result[..KeyLength].ToArray(), result[KeyLength..].ToArray());
    }

    private static Aes CreateAes(byte[] key, byte[] iv)
    {
        Aes aes = Aes.Create();
        aes.BlockSize = 128;
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;
        return aes;
    }
}

