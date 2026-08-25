// SPDX-License-Identifier: 0BSD
using System.Text.Json.Nodes;

namespace SuikodenHdSaveEditor.Formats.Suikoden2;

public sealed class Suikoden2CharacterView
{
    internal Suikoden2CharacterView(int id, JsonObject data, int recruitmentStatus, bool inParty)
    {
        Id = id;
        Data = data;
        RecruitmentStatus = recruitmentStatus;
        IsInParty = inParty;
    }

    internal JsonObject Data { get; }

    public int Id { get; }

    public string Name => Suikoden2Catalog.Character(Id)?.Name ?? $"Character {Id}";

    public int RecruitmentStatus { get; }

    public bool IsInParty { get; }

    public int Level => Value("level");

    public int Experience => Value("exp");

    public int CurrentHp => Value("now_hp");

    public int MaximumHp => Value("max_hp");

    public int WeaponLevel => Value("buki_lv");

    public int WeaponRune => Value("buki_mon");

    public int KilledEnemies => Value("todome");

    public IReadOnlyList<int> MagicPoints => Array("mp");

    public IReadOnlyList<int> Stats => Array("para");

    public IReadOnlyList<int> Runes => Array("mon_eqp");

    public IReadOnlyList<int> Equipment => Array("bogu_eqp");

    private int Value(string name) => Data[name]!.GetValue<int>();

    private int[] Array(string name) => Data[name]!.AsArray().Select(node => node!.GetValue<int>()).ToArray();
}

