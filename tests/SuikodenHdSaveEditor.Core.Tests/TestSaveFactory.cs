// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.Core.Tests;

internal static class TestSaveFactory
{
    internal const string Suikoden1Json = """
        {
          "version": 8,
          "party_data": {
            "chara_code": [8, 1, 2, 3, 4, 5],
            "player_kazu": 6,
            "mochi_kin": 1234,
            "party_item_kazu": 2,
            "party_item": [25, 73, 0, 0, 0, 0, 0, 0]
          },
          "shiro_data": { "level": 1, "unknown_hq": [7, 8] },
          "player_base": [
            {
              "chara_no": 8,
              "max_hp": 100,
              "hp": 90,
              "magic_point": [0, 3, 1, 0, 0],
              "level": 10,
              "exp": 20,
              "noryoku": [30, 31, 32, 33, 34, 35],
              "item_kazu": 1,
              "item": [{"item_id":25,"soubi":0,"data":6}],
              "buki_data": {"buki_id":1,"level":5,"monsyo":[0,0,0,0,0,0]},
              "monsyo_data": {"monsyo_id":1,"monsyo_level":0,"monsyo_exp":0},
              "unknown_character": {"kept":true}
            }
          ],
          "member_flag": [0, 0, 0, 0, 0, 0, 0, 0, 9],
          "playerName": "Synthetic Hero",
          "playerCName": "Synthetic HQ",
          "playTime": 100,
          "unknown_root": {"nested":[1,2,3]}
        }
        """;

    internal const string Suikoden2Json = """
        {
          "version": 100,
          "game_data": {
            "bozu_name": "Synthetic Hero",
            "base_name": "Synthetic Castle",
            "team_name": "Synthetic Army",
            "play_time": [1,2,3],
            "base_item": [{"item_no":1,"use_cnt":9}],
            "furo_item": [{"item_no":0,"use_cnt":64}],
            "room_item": [{"item_no":0,"use_cnt":64}],
            "unknown_game": 77
          },
          "chara_data": {
            "c_varia_dat": [
              {"level":1,"exp":0,"now_hp":20,"max_hp":20,"mp":[0,0,0,0],"para":[1,2,3,4,5,6,7],"buki_lv":1,"buki_mon":0,"mon_eqp":[0,0,0],"bogu_eqp":[0,0,0],"item_eqp":[{"item_no":0,"use_cnt":0},{"item_no":0,"use_cnt":0},{"item_no":0,"use_cnt":0}],"todome":0}
            ],
            "c_kotei_dat": [{}]
          },
          "party_data": {
            "party_cha_no": [1,2,3,4,5,6,0,0],
            "party_item": [{"item_no":1,"use_cnt":9}],
            "event_item": [0,0,0,0,0,0,0,0,0,0],
            "gold": 1234,
            "ninki": 5
          },
          "chara_flag": [0,71],
          "event_flag": [0,0],
          "t_box_flag": [0],
          "unknown_root": {"nested":[4,5,6]}
        }
        """;

    internal static SaveDocument Suikoden1() => SaveDocument.Parse(Suikoden1Json);

    internal static SaveDocument Suikoden2() => SaveDocument.Parse(Suikoden2Json);

    internal static JsonObject ParseObject(string json) => (JsonObject)JsonNode.Parse(json)!;
}

