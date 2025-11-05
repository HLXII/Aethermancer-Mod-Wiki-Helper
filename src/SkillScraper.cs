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

    /// <summary>
    /// Does additional processing on the description to make it wiki-friendly
    /// </summary>
    /// <param name="name">Name of the description. Only for logging purposes</param>
    /// <param name="description">Description to format</param>
    /// <returns>Formatted description</returns>
    public static string ReformatDescription(string name, string description)
    {
        // Remove new lines
        description = Regex.Replace(description, @"\s*\n\s*", "<br/>");

        // Remove apostrophes
        description = description.Replace("’", "'");

        // Escape quotes
        description = description.Replace("\"", "\\\"");

        // Remove empty italics
        description = description.Replace("<i><color=#74655eff></color></i>", "");

        description = description.Replace("<i><color=#74655eff>", "<i>");   // Beginning italics
        description = description.Replace("</color></i>", "</i>");   // Ending italics

        // Replace colors
        description = description.Replace("<color=#ff9900ff>", "[");    // Numbers
        description = description.Replace("<color=#71a4ffff>", "[");    // Water
        description = description.Replace("<color=#ff783cff>", "[");    // Fire
        description = description.Replace("<color=#99ff66ff>", "[");    // Wind
        description = description.Replace("<color=#c58c32ff>", "[");    // Earth
        description = description.Replace("<color=#ff99ccff>", "[");    // Wild


        description = description.Replace("</color>", "]"); // Ending blocks

        // Trim
        description = description.Trim();

        description = PostProcessingWeShouldNotHaveToDo(name, description);

        return description;
    }

    private static string PostProcessingWeShouldNotHaveToDo(string name, string description)
    {
        string oldDescription = description;

        // Broken On Interrupts
        description = description.Replace("[{On Interrupt}:]", "{On Interrupt}:");

        // Numbers that use braces for some reason
        description = Regex.Replace(description, @"\{(\d+%?)\}", "[$1]");

        // Ordinals using braces
        description = Regex.Replace(description, @"\{third\}", "[third]");
        description = Regex.Replace(description, @"\{second\}", "[second]");

        // Ordinals not using full word
        description = Regex.Replace(description, @"\[3rd\]", "[third]");

        // Target enemy using brackets
        description = Regex.Replace(description, @"\[target enemy\]", "{target enemy}");

        // Action/Actions not a real keyword
        description = description.Replace("{Action}", "[Action]");
        description = description.Replace("{Actions}", "[Actions]");

        // Attack/Attacks not a real keyword
        description = description.Replace("{Attack}", "[Attack]");
        description = description.Replace("{Attacks}", "[Attacks]");
        description = description.Replace("{attacked}", "[attacked]");

        // Damage is a modifier
        description = description.Replace("[Damage]", "{Damage}");

        // Burn is a buff
        description = description.Replace("[Burn]", "{Burn}");

        // No other consume actions format like this
        description = description.Replace("[Consumes 1 {Wild Aether}:]", "Consumes 1 [Wild] [Aether]:");

        // Elements should not be keywords
        description = description.Replace("{Water}", "[Water]");
        description = description.Replace("{Wind}", "[Wind]");
        description = description.Replace("{Fire}", "[Fire]");
        description = description.Replace("{Earth}", "[Earth]");
        description = description.Replace("{Wild}", "[Wild]");

        // Summon Kami description messed up
        description = description.Replace("Deals [1] additional damage for each {Aether} of any element.", "Deals [1] additional damage for each [Aether] of any element.");

        // Volcanic Unity description is messed up
        description = description.Replace("{Heals} self for [7] and generates Random {Aether}.", "Heals self for [7] and generates {Random Aether}.");

        // Fixing casing for random aether
        description = description.Replace("{random Aether}", "{Random Aether}");

        // Gemstone Fist double braces
        description = description.Replace("{{Shield}}", "{Shield}");

        // Ascension shouldn't use healing keyword
        description = description.Replace("For every [100] {healing} of your allies", "For every [100] healing of your allies");

        // Fix Wild Damage casing
        description = description.Replace("{Wild damage}", "{Wild Damage}");

        // Fix purge casings
        description = description.Replace("{purge}", "{Purge}");
        description = description.Replace("{purged}", "{Purged}");
        description = description.Replace("{purges}", "{Purges}");
        description = description.Replace("{steal}", "{Steal}");
        description = description.Replace("{steals}", "{Steals}");
        description = description.Replace("{stolen}", "{Stolen}");

        // Fix minion casings
        description = description.Replace("{minions}", "{Minions}");
        description = description.Replace("{minion}", "{Minion}");

        if (oldDescription != description)
        {
            Debug.LogWarning($"Item \"{name}\" required post-processing that we should not need to do but have to.");
            Debug.LogWarning($"\t{oldDescription}");
            Debug.LogWarning($"\t{description}");
        }

        return description;
    }

    public static List<string> ParseForKeys(string description)
    {
        ParseBracketedWords(description);

        List<string> bracedKeys = ExtractBracedKeywords(description);

        List<string> includeGroup = [
            // Status Conditions
            "Age", "Cooking", "Dodge", "Force", "Glory", "Power", "Temporary Power", "Redirect", "Regeneration", "Sidekick", "Bleed", "Burn", "Poison", "Terror", "Weakness", "Affliction",
            // Modifiers
            "Crit Chance", "Shield Generator", "Evasion", "Corruption Cleanse", "Terror Application", "Aether", "Health", "Damage", "Healing", "Defense",
            "Earth Damage", "Fire Damage", "Water Damage", "Wind Damage", "Burn Damage", "Sidekick Damage", "Crit Damage", "Minion Damage",
            // Misc
            "Retaliate", "Shield",
            "Minion", "Corruption", "Max Health", "Poise", "On Crit", "Critical Hit", "On Action", "On Attack", "On Dedicated Support Action", "Support Action",
        ];
        Dictionary<string, string> rewordGroup = new Dictionary<string, string>()
        {
            { "Shields", "Shield" }, { "Shielding", "Shield" }, { "Shielded", "Shield" },
            { "Purge", "Purge" }, { "Purges", "Purge" }, { "Purged", "Purge" },
            { "Steal", "Steal" }, { "Steals", "Steal" },  { "Stolen", "Steal" },
            { "Critical Hits", "Critical Hit" }, {"critical", "Critical Hit" },
            { "on Dedicated Support Action", "On Dedicated Support Action" },
            { "On Water Action from any ally", "On Earth Action" },
            { "On Wind Action from any ally", "On Earth Action" },
            { "On Earth Action from any ally", "On Earth Action" },
            { "On Support Action from any ally", "Support Action" },
            { "On Support Action", "Support Action" },
            { "Support Actions" , "Support Action" },
            { "Poise Damage", "Poise" },
            { "On Interrupt", "Interrupt" },
            { "Aura:", "Aura" },
            { "Free Action", "Free" },
            { "Aging", "Age" },
            { "Reactivates", "Reactivate" }, { "reactivate", "Reactivate" },
            { "Minions", "Minion" }, { "Summoning Action", "Minion" }, { "Essence", "Minion" }
        };
        List<string> knownKeywords = [
            // Misc we recognize but don't want to add a keyword for
            "Random Aether", "Wild Damage", "Wild Aether", "On Copied Action", "target enemy", "target ally", "Random Buff",
            // Minions
            "Yokai", "Wisp", "Kami", "Salamander", "Impundulu", "Faun", "Wind Lance", "Wind Totem", "Water Lance", "Water Totem", "Fire Lance", "Fire Totem", "Earth Lance", "Earth Totem",
        ];

        HashSet<string> filteredKeys = new();

        foreach (var key in bracedKeys)
        {
            if (includeGroup.Contains(key))
            {
                filteredKeys.Add(key);
            }
            else if (rewordGroup.ContainsKey(key))
            {
                filteredKeys.Add(rewordGroup[key]);
            }
            else if (!knownKeywords.Contains(key))
            {
                Debug.LogError($"Unrecognized braced key {key}");
                continue;
            }
        }

        List<string> result = filteredKeys.ToList();
        result.Sort();
        return result;
    }

    /// <summary>
    /// This will never add keys, as bracketed words aren't keywords
    /// However it's used to report on if any new words have been added
    /// </summary>
    /// <param name="description"></param>
    /// <returns></returns>
    private static void ParseBracketedWords(string description)
    {
        // Ignoring X and numbers/percents
        List<string> ignorePatterns = [
            @"^\d+%?$",
            @"^x|X$",
        ];

        // Known highlighted terms
        List<string> knownTerms = [
            "Water", "Wind", "Earth", "Fire", "Wild",
            "Aether",
            "Hit", "Hits", "Crit", "Crits",
            "Attack", "Attacks", "attacked", "attacks",
            "debuffs", "1 debuff", "2 debuffs", "3 debuffs", "4 debuffs",
            "Wish", "Unleash Tome",
            "second", "third",
            "Action", "Actions",
        ];

        List<string> bracketedKeys = ExtractBracketedKeywords(description);

        // Removing ignored patterns
        bracketedKeys = bracketedKeys.Where(key => !ignorePatterns.Any(pattern => Regex.Match(key, pattern).Success)).ToList();

        foreach (var key in bracketedKeys)
        {
            if (!knownTerms.Contains(key))
            {
                Debug.LogError($"Unrecognized bracketed key {key}");
                continue;
            }
        }
    }

    private static List<string> ExtractBracedKeywords(string text)
        => ExtractPatterns(text, @"\{(.*?)\}");
    private static List<string> ExtractBracketedKeywords(string text)
        => ExtractPatterns(text, @"\[(.*?)\]");
    private static List<string> ExtractPatterns(string text, string pattern)
    {
        // Perform the matching
        MatchCollection matches = Regex.Matches(text, pattern);

        // Extract and filter the captured groups
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