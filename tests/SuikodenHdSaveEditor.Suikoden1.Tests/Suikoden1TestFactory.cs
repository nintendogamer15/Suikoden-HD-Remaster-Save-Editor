// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.Suikoden1.Tests;

internal static class Suikoden1TestFactory
{
    internal static SaveDocument Create()
    {
        JsonObject root = new()
        {
            ["version"] = 8,
            ["party_data"] = new JsonObject
            {
                ["chara_code"] = IntArray(8, 1, 2, 3, 4, 5),
                ["player_kazu"] = 6,
                ["mochi_kin"] = 1000,
                ["party_item_kazu"] = 1,
                ["party_item"] = IntArray(25, 0, 0, 0, 0, 0, 0, 0),
            },
            ["shiro_data"] = new JsonObject { ["level"] = 2, ["unknown"] = 99 },
            ["player_base"] = new JsonArray(CreateCharacter(8), CreateCharacter(1), CreateCharacter(2), CreateCharacter(3), CreateCharacter(4), CreateCharacter(5)),
            ["member_flag"] = IntArray(128),
            ["playerName"] = "Synthetic Hero",
            ["playerCName"] = "Synthetic HQ",
            ["playTime"] = 123,
            ["unknown_root"] = new JsonObject { ["keep"] = true },
        };
        root["member_flag"]![8] = 9;
        return SaveDocument.Parse(root.ToJsonString());
    }

    private static JsonObject CreateCharacter(int id) => new()
    {
        ["chara_no"] = id,
        ["max_hp"] = 100,
        ["hp"] = 90,
        ["magic_point"] = IntArray(0, 3, 2, 1, 0),
        ["level"] = 10,
        ["exp"] = 20,
        ["noryoku"] = IntArray(10, 11, 12, 13, 14, 15),
        ["status"] = 0,
        ["seicho_type"] = IntArray(7),
        ["item_kazu"] = 1,
        ["item"] = new JsonArray(Enumerable.Range(0, 9).Select(index => (JsonNode?)new JsonObject
        {
            ["item_id"] = index == 0 ? 25 : 0,
            ["soubi"] = 0,
            ["data"] = index == 0 ? 6 : 0,
        }).ToArray()),
        ["buki_data"] = new JsonObject { ["buki_id"] = id + 1, ["level"] = 5, ["monsyo"] = IntArray(6) },
        ["monsyo_data"] = new JsonObject { ["monsyo_id"] = 0, ["monsyo_level"] = 0, ["monsyo_exp"] = 0 },
        ["unknown"] = new JsonArray(1, 2, 3),
    };

    private static JsonArray IntArray(params int[] values) => new(values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());

    private static JsonArray IntArray(int count) => new(Enumerable.Repeat(0, count).Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());
}

