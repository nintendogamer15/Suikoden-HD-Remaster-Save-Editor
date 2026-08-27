// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;
using SuikodenHdSaveEditor.Core;

namespace SuikodenHdSaveEditor.App.Tests;

/// <summary>
/// Builds synthetic encrypted saves for both games in a throwaway directory.
/// </summary>
/// <remarks>
/// Carried over unchanged from the pre-migration suite. The schemas here are what
/// <see cref="GameDetector"/> keys on, so they must keep satisfying it or every test that opens
/// a document silently changes meaning.
/// </remarks>
internal sealed class TestSaves : IDisposable
{
    internal TestSaves()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"suikoden-app-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
        RecentPath = System.IO.Path.Combine(Path, "recent.json");
    }

    internal string Path { get; }

    internal string RecentPath { get; }

    internal string CreateSave()
    {
        const string json = """
            {
              "version": 8,
              "party_data": {"chara_code":[8,-1,-1,-1,-1,-1],"player_kazu":1,"mochi_kin":100,"party_item_kazu":0,"party_item":[0,0,0,0,0,0,0,0]},
              "shiro_data": {"level":1},
              "player_base": [{"chara_no":8,"max_hp":20,"hp":20,"magic_point":[0,0,0,0,0],"level":1,"exp":0,"noryoku":[1,1,1,1,1,1],"status":0,"item_kazu":0,"item":[{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0},{"item_id":0,"soubi":0,"data":0}],"buki_data":{"buki_id":1,"level":1,"monsyo":[0,0,0,0,0,0]},"monsyo_data":{"monsyo_id":1,"monsyo_level":0,"monsyo_exp":0}}],
              "member_flag": [0,0,0,0,0,0,0,0,9],
              "playerName": "Synthetic Hero",
              "playerCName": "Synthetic HQ",
              "playTime": 1,
              "private_unknown": {"keep":true}
            }
            """;
        string path = System.IO.Path.Combine(Path, "Data1");
        File.WriteAllText(path, SaveCrypto.EncryptJson(json));
        return path;
    }

    internal string CreateSuikoden2Save()
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
            },
            ["chara_data"] = new JsonObject
            {
                ["c_varia_dat"] = new JsonArray(Enumerable.Range(0, 85).Select(id => (JsonNode?)CreateSuikoden2Character(id)).ToArray()),
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
        };
        root["chara_flag"]![1] = 71;
        root["party_data"]!["party_item"]![0]!["item_no"] = 1;
        root["party_data"]!["party_item"]![0]!["use_cnt"] = 3;
        string path = System.IO.Path.Combine(Path, "Data2");
        File.WriteAllText(path, SaveCrypto.EncryptJson(root.ToJsonString()));
        return path;
    }

    private static JsonObject CreateSuikoden2Character(int id) => new()
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
    };

    private static JsonArray ItemArray(int count, int emptyUseCount = 0) => new(Enumerable.Range(0, count).Select(_ => (JsonNode?)new JsonObject
    {
        ["item_no"] = 0,
        ["use_cnt"] = emptyUseCount,
    }).ToArray());

    private static JsonArray IntArray(params int[] values) => new(values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());

    private static JsonArray IntArray(int count) => new(Enumerable.Repeat(0, count).Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());

    private static JsonArray StringArray(int count) => new(Enumerable.Range(0, count).Select(index => (JsonNode?)JsonValue.Create($"Alias {index}")).ToArray());

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
