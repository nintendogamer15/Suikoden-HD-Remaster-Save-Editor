// SPDX-License-Identifier: 0BSD
namespace SuikodenHdSaveEditor.Core;

public sealed class SaveEditorException : Exception
{
    public SaveEditorException(SaveErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public SaveEditorException(SaveErrorCode code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public SaveErrorCode Code { get; }
}

