

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BepInEx;
using UnityEngine;
using WikiHelper.Models;

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
        WriteSkillFiles(actionData, traitData, sigTraitData);
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


    private static string FilePath(string name)
    {
        return Path.Combine(Paths.PluginPath, $"{name}_v{Application.version}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.txt");
    }

    public static void WriteSkillFiles(
        Dictionary<string, ActionData> action,
        Dictionary<string, TraitData> traits,
        Dictionary<string, SigTraitData> sigTraits
    )
    {
        WriteActionFile(action);
        WriteTraitFile(traits);
        WriteSigTraitFile(sigTraits);
    }

    private static void WriteActionFile(Dictionary<string, ActionData> actions)
    {
        using StreamWriter outputFile = new StreamWriter(FilePath("actions"));

        // Split by type
        List<ActionData> starting = actions.Values.Where(action => action.Starting).OrderBy(p => p.Name).ToList();
        List<ActionData> basic = actions.Values.Where(action => !action.Starting).OrderBy(p => p.Name).ToList();

        // Header
        outputFile.WriteLine("actionData = {");

        // Starting Actions
        outputFile.WriteLine("\t-- STARTING ACTIONS --");
        foreach (var action in starting)
        {
            WriteAction(action, outputFile);
        }

        // List of Actions
        outputFile.WriteLine("\t-- LIST OF ACTIONS --");
        foreach (var action in basic)
        {
            WriteAction(action, outputFile);
        }

        // Footer
        outputFile.WriteLine("}");
        outputFile.WriteLine("");
        outputFile.WriteLine("return actionData");
    }

    private static void WriteAction(ActionData action, StreamWriter outputFile)
    {
        outputFile.WriteLine($"\t[\"{action.Name.ToUpperInvariant()}\"] = {{");
        outputFile.WriteLine($"\t\tname\t\t= \"{action.Name}\",");
        outputFile.WriteLine($"\t\tstarting\t= {(action.Starting ? "true" : "false")},");
        outputFile.WriteLine($"\t\tmaverick\t= {(action.Maverick ? "true" : "false")},");
        outputFile.WriteLine($"\t\tattack\t\t= {(action.Attack ? "true" : "false")},");
        outputFile.WriteLine($"\t\tfree\t\t= {(action.Free ? "true" : "false")},");
        if (!action.Types.Any())
        {
            outputFile.WriteLine("\t\ttypes\t\t= {},");
        }
        else
        {
            string types = string.Join(", ", action.Types.Select(e => $"\"{e}\""));
            types = "{" + types + "}";
            outputFile.WriteLine($"\t\ttypes\t\t= {types},");
        }
        string elements = string.Join(", ", action.Elements.Select(e => $"\"{e}\""));
        elements = "{" + elements + "}";
        outputFile.WriteLine($"\t\telements\t= {elements},");
        outputFile.WriteLine($"\t\trequires\t= \"{action.Requires}\",");
        outputFile.WriteLine($"\t\teffect\t\t= \"{action.Effect}\",");
        if (!action.Key.Any())
        {
            outputFile.WriteLine("\t\tkey\t\t\t= {},");
        }
        else
        {
            string keys = string.Join(", ", action.Key.Select(e => $"\"{e}\""));
            keys = "{" + keys + "}";
            outputFile.WriteLine($"\t\tkey\t\t\t= {keys},");
        }
        outputFile.WriteLine($"\t\tcategory\t= {{}},");
        outputFile.WriteLine($"\t}},");
    }

    private static void WriteTraitFile(Dictionary<string, TraitData> traits)
    {
        using StreamWriter outputFile = new StreamWriter(FilePath("traits"));

        // Header
        outputFile.WriteLine("local traits = {");

        foreach (var trait in traits.Values.OrderBy(p => p.Name))
        {
            WriteTrait(trait, outputFile);
        }

        // Footer
        outputFile.WriteLine("}");
        outputFile.WriteLine("");
        outputFile.WriteLine("return traits");
    }

    private static void WriteTrait(TraitData trait, StreamWriter outputFile)
    {
        outputFile.WriteLine($"\t[\"{trait.Name.ToUpperInvariant()}\"] = {{");
        outputFile.WriteLine($"\t\tname\t\t= \"{trait.Name}\",");
        outputFile.WriteLine($"\t\tmaverick\t= {(trait.Maverick ? "true" : "false")},");
        outputFile.WriteLine($"\t\taura\t\t= {(trait.Aura ? "true" : "false")},");
        if (!trait.Types.Any())
        {
            outputFile.WriteLine("\t\ttypes\t\t= {},");
        }
        else
        {
            string types = string.Join(", ", trait.Types.Select(e => $"\"{e}\""));
            types = "{" + types + "}";
            outputFile.WriteLine($"\t\ttypes\t\t= {types},");
        }
        outputFile.WriteLine($"\t\trequires\t= \"{trait.Requires}\",");
        outputFile.WriteLine($"\t\teffect\t\t= \"{trait.Effect}\",");
        if (!trait.Key.Any())
        {
            outputFile.WriteLine("\t\tkey\t\t\t= {},");
        }
        else
        {
            string keys = string.Join(", ", trait.Key.Select(e => $"\"{e}\""));
            keys = "{" + keys + "}";
            outputFile.WriteLine($"\t\tkey\t\t\t= {keys},");
        }
        outputFile.WriteLine($"\t\tcategory\t= {{}},");
        outputFile.WriteLine($"\t}},");
    }

    private static void WriteSigTraitFile(Dictionary<string, SigTraitData> traits)
    {
        using StreamWriter outputFile = new StreamWriter(FilePath("sig_traits"));

        // Header
        outputFile.WriteLine("local traits = {");

        foreach (var trait in traits.Values.OrderBy(p => p.Name))
        {
            WriteSigTrait(trait, outputFile);
        }

        // Footer
        outputFile.WriteLine("}");
        outputFile.WriteLine("");
        outputFile.WriteLine("return traits");
    }

    private static void WriteSigTrait(SigTraitData trait, StreamWriter outputFile)
    {
        string index = trait.Shifted ? $"{trait.Name.ToUpperInvariant()}-S" : trait.Name.ToUpperInvariant();
        outputFile.WriteLine($"\t[\"{index}\"] = {{");
        outputFile.WriteLine($"\t\tname\t\t= \"{trait.Name}\",");
        outputFile.WriteLine($"\t\tshifted\t\t= {(trait.Shifted ? "true" : "false")},");
        outputFile.WriteLine($"\t\tmonster\t\t= {(trait.Aura ? "true" : "false")},");
        outputFile.WriteLine($"\t\taura\t\t= {(trait.Aura ? "true" : "false")},");
        outputFile.WriteLine($"\t\teffect\t\t= \"{trait.Effect}\",");
        if (!trait.Key.Any())
        {
            outputFile.WriteLine("\t\tkey\t\t\t= {},");
        }
        else
        {
            string keys = string.Join(", ", trait.Key.Select(e => $"\"{e}\""));
            keys = "{" + keys + "}";
            outputFile.WriteLine($"\t\tkey\t\t\t= {keys},");
        }
        outputFile.WriteLine($"\t\tcategory\t= {{}},");
        outputFile.WriteLine($"\t}},");
    }
}


public static class ParseHelper
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
                subReqs.Add($"Self{selfSubReq.ToReqString()}");
            }
            foreach (var selfSubReq in req.RequirementsTeam)
            {
                subReqs.Add($"An ally{selfSubReq.ToReqString()}");
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
                return " as a {Water Action}.";
            case EAcquisitionRequirements.Fire:
                return " as a {Fire Action}.";
            case EAcquisitionRequirements.Wind:
                return " as a {Wind Action}.";
            case EAcquisitionRequirements.Earth:
                return " as a {Earth Action}.";
            case EAcquisitionRequirements.WaterDamage:
                return " can apply {Water Damage}.";
            case EAcquisitionRequirements.FireDamage:
                return " can apply {Water Damage}.";
            case EAcquisitionRequirements.WindDamage:
                return " can apply {Wind Damage}.";
            case EAcquisitionRequirements.EarthDamage:
                return " can apply {Earth Damage}.";
            case EAcquisitionRequirements.TriggerPoison:
                return " can trigger {Poison}.";
            case EAcquisitionRequirements.Purge:
                return " can {Purge} or {Steal} [Aether].";
            case EAcquisitionRequirements.DedicatedSupportAction:
                return " has a {Support Action}. <abbr title=\"Bugged, supposed to be Dedicated Support Action.\" style='font-size: 120%'>*</abbr>";
            default:
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

    public static string ReformatDescription(string description)
    {
        // Remove new lines
        description = Regex.Replace(description, @"\s*\n\s*", "<br/>");

        // Remove apostrophes
        description = description.Replace("’", "'");

        // Remove empty italics
        description = description.Replace("<i><color=#74655eff></color></i>", "");

        description = description.Replace("<i><color=#74655eff>", "<i>");   // Beginning italics
        description = description.Replace("</color></i>", "</i>");   // Ending italics

        // Replace colors
        // Numbers
        description = description.Replace("<color=#ff9900ff>", "[");    // Numbers
        description = description.Replace("<color=#71a4ffff>", "[");    // Water
        description = description.Replace("<color=#ff783cff>", "[");    // Fire
        description = description.Replace("<color=#99ff66ff>", "[");    // Wind
        description = description.Replace("<color=#c58c32ff>", "[");    // Earth
        description = description.Replace("<color=#ff99ccff>", "[");    // Wild


        description = description.Replace("</color>", "]"); // Ending blocks

        // Trim
        description = description.Trim();

        return description;
    }

    public static List<string> ParseForKeys(string description)
    {
        List<string> rawKeys = ExtractBracketedKeywords(description);

        List<string> ignorePatterns = [
            @"^\d+%?$",
            @"^x|X$",
        ];

        List<string> includeGroup = [
            "Age", "Cooking", "Dodge", "Force", "Glory", "Power", "Temporary Power", "Redirect", "Regeneration", "Sidekick", "Bleed", "Burn", "Poison", "Terror", "Weakness",
            "Retaliate", "Affliction", "Shield",
            "Minion", "Corruption", "Max Health", "Poise Damage",
            "Crit Chance", "Shield Generator", "Evasion", "Corruption Cleanse", "Terror Application", "Burn Damage", "Sidekick Damage", "Crit Damage", "Minion Damage"
        ];
        Dictionary<string, string> rewordGroup = new Dictionary<string, string>()
        {
            { "Shields", "Shield" }, { "Shielding", "Shield" }, { "Shielded", "Shield" },
            { "Purges", "Purge" }, { "purge", "Purge" }, { "purges", "Purge" }, { "purged", "Purge" },
            { "steals", "Steal" },  { "Steals", "Steal" },  { "stolen", "Steal" }, { "steal", "Steal" },
            { "Heals", "Heal" }, { "Healing", "Heal" }, { "healing", "Heal" },
            { "On Interrupt", "Interrupt" },
            { "{On Interrupt}:", "Interrupt" },
            { "Aura:", "Aura" },
            { "Free Action", "Free" },
            { "Aging", "Age" },
            { "Reactivates", "Reactivate" }, { "reactivate", "Reactivate" },
            { "minion", "Minion" }, { "minions", "Minion" }, { "Minions", "Minion" }, { "Summoning Action", "Minion" }
        };
        List<string> excludeGroup = [
            "Water", "Wind", "Earth", "Fire", "Wild", "Aether",
            "Essence", "Wisp", "Kami", "Salamander",
            "Hit", "On Crit", "On Support Action", "On Attack", "On Action", "Attack", "Critical Hit", "Critical Hits",
            "random Aether", "Action", "On Dedicated Support Action", "Hits"
        ];

        // Removing ignored patterns
        rawKeys = rawKeys.Where(key => !ignorePatterns.Any(pattern => Regex.Match(key, pattern).Success)).ToList();

        HashSet<string> filteredKeys = new();

        foreach (var key in rawKeys)
        {
            if (includeGroup.Contains(key))
            {
                filteredKeys.Add(key);
            }
            else if (rewordGroup.ContainsKey(key))
            {
                filteredKeys.Add(rewordGroup[key]);
            }
            else if (!excludeGroup.Contains(key))
            {
                Debug.LogWarning($"Unrecognized raw key {key}");
                continue;
            }
        }

        List<string> result = filteredKeys.ToList();
        result.Sort();
        return result;
    }

    private static List<string> ExtractBracketedKeywords(string text)
    {
        // 1. Define the Regex Pattern
        // This pattern looks for content inside both [] square brackets AND {} curly braces.
        // It captures only the content *between* the brackets.
        const string pattern = @"\[(.*?)\]|\{(.*?)\}";

        // 2. Perform the matching
        MatchCollection matches = Regex.Matches(text, pattern);

        // 3. Extract and filter the captured groups
        List<string> keywords = matches
            .Cast<Match>()
            .SelectMany(match => match.Groups.Cast<Group>()
                                             .Skip(1) // Skip Group 0 (the full match, e.g., "[test]")
                                             .Where(g => g.Success && !string.IsNullOrWhiteSpace(g.Value)) // Check if group captured anything
                                             .Select(g => g.Value))
            .ToList();

        return keywords;
    }
}