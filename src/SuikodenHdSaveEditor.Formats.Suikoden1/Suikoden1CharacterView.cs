// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;

namespace SuikodenHdSaveEditor.Formats.Suikoden1;

public sealed class Suikoden1CharacterView
{
    internal Suikoden1CharacterView(JsonObject data)
    {
        Data = data;
    }

    internal JsonObject Data { get; }

    public int Id => RequiredInt("chara_no");

    public string Name => Suikoden1Catalog.CharacterName(Id);

    public int Level => RequiredInt("level");

    public int Experience => RequiredInt("exp");

    public int CurrentHp => RequiredInt("hp");

    public int MaximumHp => RequiredInt("max_hp");

    public IReadOnlyList<int> CurrentMagicPoints => RequiredIntArray("magic_point");

    public IReadOnlyList<int> Stats => RequiredIntArray("noryoku");

    public int WeaponId => Data["buki_data"]!["buki_id"]!.GetValue<int>();

    public int WeaponLevel => Data["buki_data"]!["level"]!.GetValue<int>();

    public int RuneId => Data["monsyo_data"]!["monsyo_id"]!.GetValue<int>();

    public IReadOnlyList<int> WeaponRunePieces => RequiredIntArrayFrom(Data["buki_data"]!.AsObject(), "monsyo");

    public int ItemCount => RequiredInt("item_kazu");

    public JsonArray Items => Data["item"]!.AsArray();

    private int RequiredInt(string name) => Data[name]!.GetValue<int>();

    private int[] RequiredIntArray(string name) => Data[name]!.AsArray().Select(node => node!.GetValue<int>()).ToArray();

    private static int[] RequiredIntArrayFrom(JsonObject owner, string name) => owner[name]!.AsArray().Select(node => node!.GetValue<int>()).ToArray();
}
