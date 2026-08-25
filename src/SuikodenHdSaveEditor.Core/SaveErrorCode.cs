// SPDX-License-Identifier: 0BSD
namespace SuikodenHdSaveEditor.Core;

public enum SaveErrorCode
{
    EmptyInput,
    InvalidHeader,
    InvalidBase64,
    TruncatedEnvelope,
    InvalidCiphertextLength,
    DecryptionFailed,
    InvalidUtf8,
    InvalidJson,
    UnsupportedSchema,
    AmbiguousSchema,
    ValidationFailed,
    DestinationExists,
    SourceMissing,
    FileAccess,
    OutputVerificationFailed,
}

