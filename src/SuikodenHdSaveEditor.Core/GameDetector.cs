// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;

namespace SuikodenHdSaveEditor.Core;

public static class GameDetector
{
    public static GameKind Detect(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        bool s1 = root["party_data"] is JsonObject s1Party
            && s1Party["chara_code"] is JsonArray
            && root["shiro_data"] is JsonObject
            && root["player_base"] is JsonArray
            && root["member_flag"] is JsonArray;

        bool s2 = root["game_data"] is JsonObject
            && root["chara_data"] is JsonObject s2Characters
            && s2Characters["c_varia_dat"] is JsonArray
            && root["party_data"] is JsonObject s2Party
            && s2Party["party_cha_no"] is JsonArray
            && root["chara_flag"] is JsonArray;

        if (s1 && s2)
        {
            throw new SaveEditorException(
                SaveErrorCode.AmbiguousSchema,
                "The decrypted JSON contains signatures for both games; it cannot be edited safely.");
        }

        if (s1)
        {
            return GameKind.Suikoden1;
        }

        if (s2)
        {
            return GameKind.Suikoden2;
        }

        throw new SaveEditorException(
            SaveErrorCode.UnsupportedSchema,
            "The decrypted JSON does not match the verified Suikoden I or Suikoden II save schema.");
    }
}

