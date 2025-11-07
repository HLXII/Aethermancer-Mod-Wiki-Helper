using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using WikiHelper.Models;
using WikiHelper.Output;

namespace WikiHelper;

public static class SkillScraper
{
    public static void RunScrape(Monster monster)
    {
        Debug.Log("Running Skill Scrape...");

        // Getting skills
        (Dictionary<string, BaseAction> actions, Dictionary<string, Trait> traits) = ScrapeSkills();
        Debug.Log($"Skills obtained! - Actions: {actions.Count}, Traits: {traits.Count}");

        // Sanitizing raw skills
        Sanitize(actions, traits);

        // Getting test monster
        Debug.Log($"Test Monster obtained! - Monster: {monster.Name}");

        // Parsing to wiki data
        (
            Dictionary<string, ActionData> actionData,
            Dictionary<string, TraitData> traitData,
            Dictionary<string, SigTraitData> sigTraitData
        ) = ParseSkills(actions, traits, monster);
        Debug.Log($"Skills parsed! - Actions: {actionData.Count}, Traits: {traitData.Count}, Signature Traits: {sigTraitData.Count}");

        // Writing to file
        SkillWriter.WriteSkillFiles(actionData, traitData, sigTraitData);
    }

    private static (Dictionary<string, BaseAction>, Dictionary<string, Trait>) ScrapeSkills()
    {
        Dictionary<string, BaseAction> actions = new();
        Dictionary<string, Trait> traits = new();
        foreach (MonsterType monsterType in GameController.Instance.MonsterTypes)
        {
            foreach (BaseAction action in monsterType.Actions)
            {
                if (!actions.ContainsKey(action.Name))
                {
                    actions.Add(action.Name, action);
                }
            }

            foreach (Trait trait in monsterType.Traits)
            {
                string name = trait.IsShiftedTrait ? $"{trait.Name} (Shifted)" : trait.Name;
                if (!traits.ContainsKey(name))
                {
                    traits.Add(name, trait);
                }
            }
        }
        return (actions, traits);
    }

    private static void Sanitize(Dictionary<string, BaseAction> actions, Dictionary<string, Trait> traits)
    {
        actions.Remove("?????");
        actions.Remove("PoiseBreaker");
        actions.Remove("???");
        actions.Remove("Skip");

        traits.Remove("???");
        traits.Remove("Chernobog Arm Utility");
        traits.Remove("Control the Cursed");
        traits.Remove("Corrupted Aether");
        traits.Remove("Corruptions Grasp");
        traits.Remove("Torments Acolyte");
        traits.Remove("Unending");

        traits.Remove("?????");
    }

    private static (Dictionary<string, ActionData>, Dictionary<string, TraitData>, Dictionary<string, SigTraitData>) ParseSkills(
        Dictionary<string, BaseAction> actions, Dictionary<string, Trait> traits, Monster monster
    )
    {
        Dictionary<string, ActionData> actionData = actions.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToData(monster));

        Dictionary<string, TraitData> traitData = new();
        Dictionary<string, SigTraitData> sigTraitData = new();
        foreach (var traitEntry in traits)
        {
            if (traitEntry.Value.IsSignatureTrait())
            {
                sigTraitData.Add(traitEntry.Key, traitEntry.Value.ToSigData(monster));
            }
            else
            {
                traitData.Add(traitEntry.Key, traitEntry.Value.ToData(monster));
            }
        }

        return (actionData, traitData, sigTraitData);
    }
}


public static class SkillParseHelper
{
    public static List<string> ToList(this Aether aether)
    {
        var result = new List<string>();
        for (int i = 0; i < Aether.AetherMainTypesAndWild; i++)
        {
            for (int j = 0; j < aether.Get(i); j++)
            {
                switch (i)
                {
                    case 0:
                        result.Add("water");
                        break;
                    case 1:
                        result.Add("fire");
                        break;
                    case 2:
                        result.Add("wind");
                        break;
                    case 3:
                        result.Add("earth");
                        break;
                    case 4:
                        result.Add("wild");
                        break;
                }
            }
        }
        return result;
    }

    public static string GetRequirement(this BaseSkill skill)
    {
        var reqs = skill.GetComponents<AcquisitionRequirement>();

        List<string> resultStrs = new();

        // Really should only have one
        for (int i = 0; i < reqs.Length; i++)
        {
            var req = reqs[i];

            List<string> subReqs = new();
            foreach (var selfSubReq in req.Requirements)
            {
                var tempReq = selfSubReq.ToReqString();
                if (!string.IsNullOrEmpty(tempReq))
                {
                    subReqs.Add($"Self{tempReq}");
                }
            }
            foreach (var selfSubReq in req.RequirementsTeam)
            {
                var tempReq = selfSubReq.ToReqString();
                if (!string.IsNullOrEmpty(tempReq))
                {
                    subReqs.Add($"An ally{tempReq}");
                }
            }

            if (req.AndComparison)
            {
                resultStrs.Add(string.Join(" AND ", subReqs));
            }
            else
            {
                resultStrs.Add(string.Join(" OR ", subReqs));
            }
        }

        if (resultStrs.Count > 1)
        {
            return string.Join(" AND ", resultStrs.Select(str => $"({str})"));
        }
        else if (resultStrs.Any())
        {
            return resultStrs.First();
        }
        else
        {
            return "";
        }
    }

    public static string ToReqString(this EAcquisitionRequirements req)
    {
        switch (req)
        {
            case EAcquisitionRequirements.DamageActions:
                return " has a {Damage Action}.";
            case EAcquisitionRequirements.SupportActions:
                return " has a {Support Action}.";
            case EAcquisitionRequirements.FreeActions:
                return " has a {Free Action}.";
            case EAcquisitionRequirements.Criticals:
                return " has {Crit Chance} or can apply {Force}.";
            case EAcquisitionRequirements.Evasion:
                return " has {Evasion} or can apply {Dodge}.";
            case EAcquisitionRequirements.Heals:
                return " has a {Healing Action}.";
            case EAcquisitionRequirements.Shields:
                return " has a {Shielding Action}.";
            case EAcquisitionRequirements.Buffs:
                return " can apply buffs.";
            case EAcquisitionRequirements.Debuffs:
                return " can apply debuffs.";
            case EAcquisitionRequirements.DamagingDebuffs:
                return " can apply damaging debuffs ({Bleed}, {Burn}, {Poison}, {Terror}).";
            case EAcquisitionRequirements.Summons:
                return " can summon {Minions}.";
            case EAcquisitionRequirements.DamagingSummons:
                return " can summon damaging {Minions}.";
            case EAcquisitionRequirements.Sidekick:
                return " can apply {Sidekick}.";
            case EAcquisitionRequirements.Power:
                return " can apply {Power}.";
            case EAcquisitionRequirements.Regeneration:
                return " can apply {Regeneration}.";
            case EAcquisitionRequirements.Age:
                return " can apply {Age}.";
            case EAcquisitionRequirements.Redirect:
                return " can apply {Redirect}.";
            case EAcquisitionRequirements.Cooking:
                return " can apply {Cooking}.";
            case EAcquisitionRequirements.Dodge:
                return " can apply {Dodge}.";
            case EAcquisitionRequirements.Force:
                return " can apply {Force}.";
            case EAcquisitionRequirements.Poison:
                return " can apply {Poison}.";
            case EAcquisitionRequirements.Burn:
                return " can apply {Burn}.";
            case EAcquisitionRequirements.Weakness:
                return " can apply {Weakness}.";
            case EAcquisitionRequirements.Affliction:
                return " can apply {Affliction}.";
            case EAcquisitionRequirements.Terror:
                return " can apply {Terror}.";
            case EAcquisitionRequirements.Water:
                return " has a [Water Action].";
            case EAcquisitionRequirements.Fire:
                return " has a [Fire Action].";
            case EAcquisitionRequirements.Wind:
                return " has a [Wind Action].";
            case EAcquisitionRequirements.Earth:
                return " has an [Earth Action].";
            case EAcquisitionRequirements.WaterDamage:
                return " can apply [Water Damage].";
            case EAcquisitionRequirements.FireDamage:
                return " can apply [Fire Damage].";
            case EAcquisitionRequirements.WindDamage:
                return " can apply [Wind Damage].";
            case EAcquisitionRequirements.EarthDamage:
                return " can apply [Earth Damage].";
            case EAcquisitionRequirements.TriggerPoison:
                return " can trigger {Poison}.";
            case EAcquisitionRequirements.Purge:
                return " can {Purge} or {Steal} [Aether].";
            case EAcquisitionRequirements.DedicatedSupportAction:
                return " has a {Support Action}. <abbr title=\\\"Bugged, supposed to be Dedicated Support Action.\\\" style='font-size: 120%'>*</abbr>";
            default:
                Debug.LogError($"Unrecognized requirement {req}");
                return "";
        }
    }

    public static List<string> GetSkillTypes(this BaseSkill skill)
    {
        var result = new List<string>();
        foreach (GameObject type in skill.Types)
        {
            MonsterType component = type.GetComponent<MonsterType>();
            if (component != null && component.IsDisplayedInTooltip)
            {
                result.Add(component.GetName().ToLowerInvariant());
            }
        }

        result.Sort();

        return result;
    }
}