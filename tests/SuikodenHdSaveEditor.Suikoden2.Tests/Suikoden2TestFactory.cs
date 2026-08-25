// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.Suikoden2.Tests;

internal static class Suikoden2TestFactory
{
    internal static SaveDocument Create()
    {
        JsonObject root = new()
        {
            ["version"] = 100,
            ["game_data"] = new JsonObject
            {
                ["bozu_name"] = "Synthetic Hero",
                ["bozu_name2"] = "Synthetic Real Hero",
                ["kari_name"] = StringArray(6),
                ["macd_name"] = string.Empty,
                ["base_name"] = "Synthetic Castle",
                ["m_base_name"] = "Synthetic S1 HQ",
                ["team_name"] = "Synthetic Army",
                ["base_lv"] = 1,
                ["kaji_lv"] = 1,
                ["nakam_1_num"] = 0,
                ["play_time"] = IntArray(1, 2, 3),
                ["base_item"] = ItemArray(60),
                ["furo_item"] = ItemArray(8, 64),
                ["room_item"] = ItemArray(8, 64),
                ["furo_info"] = IntArray(2),
                ["food_menu"] = IntArray(7),
                ["food_resipi"] = IntArray(5),
                ["food_num"] = IntArray(12),
                ["tantei_lv"] = IntArray(65),
                ["hon_flag"] = IntArray(50),
                ["area_no"] = 0,
                ["s_area_no"] = 0,
                ["town_no"] = 0,
                ["s_town_no"] = 0,
                ["area_no2"] = 0,
                ["town_no2"] = 0,
                ["map_no"] = 0,
                ["s_map_no"] = 0,
                ["unknown_game"] = new JsonArray(7, 8, 9),
            },
            ["chara_data"] = new JsonObject
            {
                ["c_varia_dat"] = new JsonArray(Enumerable.Range(0, 85).Select(id => (JsonNode?)CreateCharacter(id)).ToArray()),
                ["c_kotei_dat"] = new JsonArray(Enumerable.Range(0, 85).Select(_ => (JsonNode?)new JsonObject()).ToArray()),
            },
            ["party_data"] = new JsonObject
            {
                ["party_cha_no"] = IntArray(1, 2, 3, 4, 5, 6, 0, 0),
                ["party_item"] = ItemArray(30),
                ["event_item"] = IntArray(10),
                ["ninki"] = 5,
                ["gold"] = 1000,
            },
            ["chara_flag"] = IntArray(128),
            ["event_flag"] = IntArray(256),
            ["t_box_flag"] = IntArray(32),
            ["px"] = 0,
            ["py"] = 0,
            ["unknown_root"] = new JsonObject { ["keep"] = true },
        };
        root["chara_flag"]![1] = 71;
        return SaveDocument.Parse(root.ToJsonString());
    }

    private static JsonObject CreateCharacter(int id) => new()
    {
        ["level"] = 10,
        ["exp"] = 20,
        ["now_hp"] = 90,
        ["max_hp"] = 100,
        ["mp"] = IntArray(0, 17, 34, 51),
        ["para"] = IntArray(10, 11, 12, 13, 14, 15, 16),
        ["buki_lv"] = 5,
        ["buki_mon"] = 0,
        ["mon_eqp"] = IntArray(3),
        ["bogu_eqp"] = IntArray(3),
        ["item_eqp"] = ItemArray(3),
        ["todome"] = id,
        ["unknown_character"] = id,
    };

    private static JsonArray ItemArray(int count, int emptyUseCount = 0) => new(Enumerable.Range(0, count).Select(_ => (JsonNode?)new JsonObject
    {
        ["item_no"] = 0,
        ["use_cnt"] = emptyUseCount,
    }).ToArray());

    private static JsonArray IntArray(params int[] values) => new(values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());

    private static JsonArray IntArray(int count) => new(Enumerable.Repeat(0, count).Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());

    private static JsonArray StringArray(int count) => new(Enumerable.Range(0, count).Select(index => (JsonNode?)JsonValue.Create($"Alias {index}")).ToArray());
}

