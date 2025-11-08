using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WikiHelper.Models;
using WikiHelper.Output;

namespace WikiHelper;

public static class MonsterScraper
{
    public static void RunScrape()
    {
        Debug.Log("Running Monster Scrape...");

        Dictionary<int, Monster> monsters = MonsterManager.Instance.AllMonsters;
        Debug.Log($"Monsters obtained! - Monsters: {monsters.Count}");

        Dictionary<int, Monster> sanitizedMonsters = Sanitize(monsters);

        // Getting soul bond costs
        Dictionary<string, int> soulbondCosts = GetSoulbondCosts();

        // Parsing to wiki data
        Dictionary<string, MonsterData> monsterData = Parse(sanitizedMonsters, soulbondCosts);
        Debug.Log($"Monsters parsed! - Monsters: {monsterData.Count}");

        // Writing to file
        MonsterWriter.WriteFiles(monsterData);
    }

    private static Dictionary<int, Monster> Sanitize(Dictionary<int, Monster> monsters)
    {
        Dictionary<int, Monster> copy = monsters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        copy.Remove(619); // Target Dummy

        return copy;
    }

    private static Dictionary<string, int> GetSoulbondCosts()
    {
        Dictionary<string, int> costs = new();

        var witchUpgrades = ProgressManager.Instance.MetaUpgrades.FirstOrDefault(upgrade => upgrade.Npc == EMetaUpgradeNPC.Witch);
        foreach (var upgrade in witchUpgrades.MetaUpgrades)
        {
            var metaUpgrade = upgrade.GetComponent<MetaUpgrade>();
            costs.Add(metaUpgrade.Name, metaUpgrade.Cost);
        }

        return costs;
    }

    private static Dictionary<string, MonsterData> Parse(Dictionary<int, Monster> monsters, Dictionary<string, int> soulbondCosts)
    {
        Dictionary<string, MonsterData> monsterData = new();
        foreach (var monster in monsters)
        {
            (MonsterData baseData, MonsterData shiftData) = ParseMonster(monster.Key, monster.Value);
            baseData.Soulbond = soulbondCosts.ContainsKey(baseData.Name) ? soulbondCosts[baseData.Name] : 0;
            monsterData.Add(baseData.Name.ToUpperInvariant(), baseData);
            monsterData.Add($"{shiftData.Name.ToUpperInvariant()}-S", shiftData);
        }

        return monsterData;
    }

    // Hard coding
    private static string[] Starters = ["Cherufe", "Jotunn", "Minokawa", "Nixe"];
    private static string[] Hiddens = ["Jotunn", "Catzerker", "Cherufe", "Dark Elder", "Minokawa", "Nixe"];
    private static string[] Champions = ["Warden", "Star Spawn", "Hecatoncheires"];

    private static (MonsterData, MonsterData) ParseMonster(int index, Monster monster)
    {
        Debug.Log($"Parsing Monster {index} - {monster.Name}");

        MonsterData data = new();
        MonsterData shiftData = new();
        data.Id = index;
        shiftData.Id = index;
        data.MonsterId = monster.MonID;
        shiftData.MonsterId = monster.MonID;
        data.Name = monster.Name;
        shiftData.Name = monster.Name;
        data.Shifted = false;
        shiftData.Shifted = true;

        data.Starter = Starters.Contains(data.Name);
        shiftData.Starter = data.Starter;
        data.Hidden = Hiddens.Contains(data.Name);
        shiftData.Hidden = data.Hidden;
        data.Champion = Champions.Contains(data.Name);
        shiftData.Champion = data.Champion;

        data.Ambush = IsAmbush(monster);
        shiftData.Ambush = data.Ambush;

        data.Health = monster.Stats.BaseMaxHealth;
        data.Perks = monster.Stats.PerkInfosList?.Select(ParsePerk).ToList() ?? [];

        data.Archetype = monster.SkillManager.MainType.ToDataString();
        data.SignatureTrait = monster.SkillManager.SignatureTrait.GetComponent<Trait>().Name;

        data.Types = monster.SkillManager.MonsterTypes.Select(type => type.GetComponent<MonsterType>().Type.ToString()).ToList();
        data.Elements = monster.SkillManager.Elements.Select(element => element.ToString()).ToList();
        data.StartingActions = monster.SkillManager.StartActions.Select(action => action.GetComponent<BaseAction>().Name).ToList();

        data.ResetAction = monster.AI.ResetAction.GetComponent<BaseAction>().Name.Replace("Reset Action: ", "");

        // Getting Shift data
        MonsterShift shift = monster.GetShiftOverride(EMonsterShift.Shifted);
        shiftData.Health = shift.HealthOverride != 0 ? shift.HealthOverride : data.Health;
        shiftData.Perks = shift.PerksOverride != null && shift.PerksOverride.Any() ? shift.PerksOverride.Select(ParsePerk).ToList() : data.Perks;
        shiftData.Archetype = shift.ChangeMainType ? shift.MainTypeOverride.ToString() : data.Archetype;
        // TODO: Add back in when we handle shifted in the trait name
        // shiftData.SignatureTrait = shift.SignatureTraitOverride != null
        //     ? shift.SignatureTraitOverride.GetComponent<Trait>().Name + " (Shifted)"
        //     : data.SignatureTrait;
        shiftData.SignatureTrait = data.SignatureTrait;
        shiftData.Types = shift.MonsterTypesOverride != null && shift.MonsterTypesOverride.Any()
            ? shift.MonsterTypesOverride.Select(t => t.GetComponent<MonsterType>().Type.ToString()).ToList()
            : data.Types;
        shiftData.Elements = shift.ElementsOverride != null && shift.ElementsOverride.Any()
            ? shift.ElementsOverride.Select(e => e.ToString()).ToList()
            : data.Elements;
        shiftData.StartingActions = shift.StartActionsOverride != null && shift.StartActionsOverride.Any()
            ? shift.StartActionsOverride.Select(a => a.GetComponent<BaseAction>().Name).ToList()
            : data.StartingActions;
        shiftData.ResetAction = shift.ResetPoiseActionOverride != null
            ? shift.ResetPoiseActionOverride.GetComponent<BaseAction>().Name.Replace("Reset Action: ", "")
            : data.ResetAction;

        List<StaggerDefine> poise = monster.SkillManager.StaggerDefines;
        if (poise.Count == 1)
        {
            data.Poise = (poise.First().Element.ToString(), poise.First().Hits);
            shiftData.Poise = data.Poise;
        }
        else
        {
            Debug.LogError($"Monster {data.Name} has irregular Poise");
        }

        data.EnemyTraits = [];
        shiftData.EnemyTraits = [];
        if (data.Champion && monster.SkillManager.EliteTrait != null)
        {
            string eliteTrait = monster.SkillManager.EliteTrait.GetComponent<Trait>().Name;
            data.EnemyTraits.Add((eliteTrait, "Champion"));
            shiftData.EnemyTraits.Add((eliteTrait, "Champion"));
        }
        foreach (var enemyTrait in monster.AI.Traits)
        {
            (string trait, string restrict) = ParseEnemyTrait(enemyTrait);

            if (enemyTrait.HasShiftRestriction)
            {
                if (enemyTrait.ShiftRestriction == EMonsterShift.Normal)
                {
                    data.EnemyTraits.Add((trait, restrict));
                }
                else if (enemyTrait.ShiftRestriction == EMonsterShift.Shifted)
                {
                    shiftData.EnemyTraits.Add((trait, restrict));
                }
                else
                {
                    Debug.LogError($"Enemy trait of {data.Name}, {enemyTrait.Trait.name}, found to have shift restriction of auto?");
                }
            }
            else
            {
                data.EnemyTraits.Add((trait, restrict));
                shiftData.EnemyTraits.Add((trait, restrict));
            }
        }

        data.EnemyActions = data.StartingActions.Select(action => (action, "")).ToList();
        shiftData.EnemyActions = shiftData.StartingActions.Select(action => (action, "")).ToList();
        foreach (var enemyAction in monster.AI.Scripting)
        {
            if (enemyAction.Conditions.Any(condition => condition.OrCondition))
            {
                Debug.LogError($"OR Condition Found! We cannot handle this and this script needs to be updated");
                continue;
            }

            string action = enemyAction.Action.GetComponent<BaseAction>().Name;

            // Skip unleash tome
            if (action == "Unleash Tome")
            {
                continue;
            }

            // Getting all requirements
            List<string> allRequirements = enemyAction.Conditions
                .Where(condition => condition.Condition != MonsterAIActionCondition.ECondition.MonsterShift)
                .Select(condition => condition.ToDataString())
                .ToList();
            string requirements = string.Join("; ", allRequirements);

            // First checking for monster shift requirement
            var shiftReq = enemyAction.Conditions.FirstOrDefault(condition => condition.Condition == MonsterAIActionCondition.ECondition.MonsterShift);
            if (shiftReq != null)
            {
                if (shiftReq.MonsterShift == EMonsterShift.Normal)
                {
                    AddNewEnemyAction(data, action, requirements);
                }
                else if (shiftReq.MonsterShift == EMonsterShift.Shifted)
                {
                    AddNewEnemyAction(shiftData, action, requirements);
                }
                else
                {
                    Debug.LogError($"Enemy action of {data.Name}, {enemyAction.Action.name}, found to have shift restriction of auto?");
                }
            }
            else
            {
                AddNewEnemyAction(data, action, requirements);
                AddNewEnemyAction(shiftData, action, requirements);
            }
        }

        data.EnemyPerks = [];
        shiftData.EnemyPerks = [];
        var voidPerks1 = monster.AI.VoidPerks.Select(ParsePerk);
        data.EnemyPerks.AddRange(voidPerks1.Select(perk => (perk.Item1, perk.Item2, "")));
        shiftData.EnemyPerks.AddRange(voidPerks1.Select(perk => (perk.Item1, perk.Item2, "")));

        var voidPerks2 = monster.AI.VoidPerksTier2.Select(ParsePerk);
        data.EnemyPerks.AddRange(voidPerks2.Select(perk => (perk.Item1, perk.Item2, "Biome Tier 2 or higher")));
        shiftData.EnemyPerks.AddRange(voidPerks2.Select(perk => (perk.Item1, perk.Item2, "Biome Tier 2 or higher")));

        var voidPerks3 = monster.AI.VoidPerksTier3.Select(ParsePerk);
        data.EnemyPerks.AddRange(voidPerks3.Select(perk => (perk.Item1, perk.Item2, "Biome Tier 3")));
        shiftData.EnemyPerks.AddRange(voidPerks3.Select(perk => (perk.Item1, perk.Item2, "Biome Tier 3")));

        if (data.Champion)
        {
            var championPerks = monster.AI.ChampionPerks.Select(ParsePerk);
            data.EnemyPerks.AddRange(championPerks.Select(perk => (perk.Item1, perk.Item2, "Champion")));
            shiftData.EnemyPerks.AddRange(championPerks.Select(perk => (perk.Item1, perk.Item2, "Champion")));
        }

        return (data, shiftData);
    }

    private static string ToDataString(this EMonsterMainType mainType)
    {
        switch (mainType)
        {
            case EMonsterMainType.Support: return "Support";
            case EMonsterMainType.Hybrid: return "Hybrid";
            case EMonsterMainType.Attacker: return "Attacker";
            case EMonsterMainType.FullSupport: return "Full Support";
            default:
                Debug.LogError($"Unrecognized Monster Main Type {mainType}");
                return "";
        }
    }

    private static bool IsAmbush(Monster monster)
    {
        OverworldMonsterBehaviour monsterBehaviour = monster.GetComponent<OverworldMonsterBehaviour>();
        if (monsterBehaviour == null)
        {
            Debug.LogWarning($"Monster {monster.Name} missing overworld behavior");
            return false;
        }
        return monsterBehaviour.IsAmbushMonster;
    }

    private static (string, float) ParsePerk(PerkInfos perk)
    {
        return (perk.Perk.GetComponent<Perk>().Name, perk.Multiplier);
    }

    private static (string, string) ParseEnemyTrait(MonsterAI.MonsterAITrait enemyTrait)
    {
        string trait = enemyTrait.Trait.GetComponent<Trait>().Name;
        string restrict = "";
        switch (enemyTrait.MinDifficulty)
        {
            case EDifficulty.Heroic: restrict = "Heroic Difficulty"; break;
            case EDifficulty.Mythic: restrict = "Mythic Difficulty"; break;
        }

        return (trait, restrict);
    }

    private static string ToDataString(this MonsterAIActionCondition condition)
    {
        switch (condition.Condition)
        {
            case MonsterAIActionCondition.ECondition.UseOnce: return "Once per combat";
            case MonsterAIActionCondition.ECondition.HealthBelowPercent: return $"Ally Health is {condition.Value * 100:F0}% or lower";
            case MonsterAIActionCondition.ECondition.BiomeTierEqualAbove: return $"Biome Tier {condition.Value:F0} or higher";
            case MonsterAIActionCondition.ECondition.DontConsumeWildAether: return "Will not consume Wild Aether on use";
            case MonsterAIActionCondition.ECondition.CombatTurnEqualAbove: return $"Rounds {condition.Value:F0} or later";
            case MonsterAIActionCondition.ECondition.CasterHealthBelowPercent: return $"Current Health is {condition.Value * 100:F0}% or lower";
            case MonsterAIActionCondition.ECondition.NoOtherMonsterPicksThisAction: return "Allies have not chosen this Action this round";

                // case MonsterAIActionCondition.ECondition.HasDebuffAmount: return "";
                // case MonsterAIActionCondition.ECondition.MonsterShift: return "";
        }
        Debug.LogError($"Unhandled action condition {condition.Condition}");
        return "";
    }

    private static void AddNewEnemyAction(MonsterData data, string action, string requirements)
    {
        bool updatedStartingAction = false;
        for (int i = 0; i < data.EnemyActions.Count; i++)
        {
            if (data.EnemyActions[i].Item1 == action)
            {
                data.EnemyActions[i] = (action, requirements);
                updatedStartingAction = true;
            }
        }
        if (!updatedStartingAction)
        {
            data.EnemyActions.Add((action, requirements));
        }
    }
}