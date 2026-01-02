

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class DescriptionHelper
{

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

        // Fix on single target attack (equips)
        description = description.Replace("{On single-target Attack}", "{On Single-Target Attack}");

        // Fix temp power casing (equips)
        description = description.Replace("{temporary Power}", "{Temporary Power}");

        // Fix wrong minion bracket (Cunning minions)
        description = description.Replace("[Minion]", "{Minion}");

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
            "Earth Damage", "Fire Damage", "Water Damage", "Wind Damage", "Burn Damage", "Sidekick Damage", "Crit Damage", "Minion Damage", "Weakness Reduction", "Regeneration Healing", "Minion Limit",
            // Misc
            "Retaliate", "Shield",
            "Minion", "Corruption", "Max Health", "Poise", "On Crit", "Critical Hit", "On Action", "On Attack", "On Dedicated Support Action", "Support Action",
        ];
        Dictionary<string, string> rewordGroup = new Dictionary<string, string>()
        {
            { "Shields", "Shield" }, { "Shielding", "Shield" }, { "Shielded", "Shield" },
            { "Purge", "Purge" }, { "Purges", "Purge" }, { "Purged", "Purge" },
            { "Steal", "Steal" }, { "Steals", "Steal" },  { "Stolen", "Steal" },
            { "Critical Hits", "Critical Hit" }, {"critical", "Critical Hit" }, { "On Crit from an ally", "Critical Hit" },
            { "on Dedicated Support Action", "On Dedicated Support Action" },
            { "On Water Action from any ally", "On Water Action" },
            { "On Wind Action from any ally", "On Wind Action" },
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