// SPDX-License-Identifier: 0BSD
namespace SuikodenHdSaveEditor.Core;

public static class Guard
{
    public static void Valid(bool condition, string message)
    {
        if (!condition)
        {
            throw new SaveEditorException(SaveErrorCode.ValidationFailed, message);
        }
    }

    public static void Index(int index, int count, string label)
    {
        if (index < 0 || index >= count)
        {
            throw new SaveEditorException(
                SaveErrorCode.ValidationFailed,
                $"{label} index {index} is outside the valid range 0 through {Math.Max(0, count - 1)}.");
        }
    }
}

