

using System.Collections.Generic;

namespace WikiHelper.Models;

public class MonsterData
{
    public int Id { get; set; }
    public int MonsterId { get; set; }
    public string Name { get; set; } = "";
    public bool Shifted { get; set; }
    public bool Starter { get; set; }
    public bool Ambush { get; set; }
    public bool Hidden { get; set; }
    public bool Champion { get; set; }
    public List<string> Elements { get; set; }
    public List<string> Types { get; set; }
    public int Health { get; set; }
    public List<string> StartingActions { get; set; }
    public string SignatureTrait { get; set; }
    public List<(string, float)> Perks { get; set; }
    public string Archetype { get; set; }
    public int Soulbond { get; set; }
    public bool Available { get; set; } = true;

    public (string, float) Poise { get; set; }
    public string ResetAction { get; set; }
    public List<(string, string)> EnemyTraits { get; set; }
    public List<(string, string)> EnemyActions { get; set; }
    public List<(string, float, string)> EnemyPerks { get; set; }
};