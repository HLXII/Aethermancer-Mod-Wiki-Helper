

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WikiHelper.Models;

public class TraitData
{
    public string Name { get; set; } = "";
    public bool Maverick { get; set; } = false;
    public bool Aura { get; set; } = false;
    public List<string> Types { get; set; } = new();
    public string Requires { get; set; } = "";
    public string Effect { get; set; } = "";
    public List<string> Key { get; set; } = new();
    public List<string> Category { get; set; } = new();
};

public class SigTraitData
{
    public string Name { get; set; } = "";
    public bool Shifted { get; set; } = false;
    public bool Aura { get; set; } = false;
    public string Effect { get; set; } = "";
    public List<string> Key { get; set; } = new();
    public List<string> Category { get; set; } = new();
};

public static class TraitExtensions
{
    public static TraitData ToData(this Trait trait, Monster monster)
    {
        var data = new TraitData();
        data.Name = trait.Name;
        data.Maverick = trait.MaverickSkill;
        data.Aura = trait.Aura;
        data.Types = trait.GetSkillTypes();
        data.Requires = trait.GetRequirement();

        SkillPicker.WeightedSkill weightedSkill = new SkillPicker.WeightedSkill(trait, 1);
        PassiveInstance passiveInstance = new PassiveInstance(weightedSkill.Skill, monster);
        data.Effect = DescriptionHelper.ReformatDescription(trait.Name, trait.GetDescription(passiveInstance));
        try
        {
            data.Key = DescriptionHelper.ParseForKeys(data.Effect);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse keys for {data.Name} -  \"{data.Effect}\" - {ex}");
        }
        return data;
    }

    public static SigTraitData ToSigData(this Trait trait, Monster monster)
    {
        var data = new SigTraitData();
        data.Name = trait.Name;
        data.Shifted = trait.IsShiftedTrait;
        data.Aura = trait.Aura;

        SkillPicker.WeightedSkill weightedSkill = new SkillPicker.WeightedSkill(trait, 1);
        PassiveInstance passiveInstance = new PassiveInstance(weightedSkill.Skill, monster);
        data.Effect = DescriptionHelper.ReformatDescription(trait.Name, trait.GetDescription(passiveInstance));
        try
        {
            data.Key = DescriptionHelper.ParseForKeys(data.Effect);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse keys for {data.Name} -  \"{data.Effect}\" - {ex}");
        }

        return data;
    }

    public static bool IsSignatureTrait(this Trait trait)
    {
        return trait.Types.Any(type => type.GetComponent<MonsterType>().GetName() == "Signature Traits");
    }
}