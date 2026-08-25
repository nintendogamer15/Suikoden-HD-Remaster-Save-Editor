// SPDX-License-Identifier: 0BSD
namespace SuikodenHdSaveEditor.Core;

public enum ValidationSeverity
{
    Information,
    Warning,
    Error,
}

public sealed record ValidationIssue(ValidationSeverity Severity, string Path, string Message);

