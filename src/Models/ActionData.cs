

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WikiHelper.Models;


public class ActionData
{
    public string Name { get; set; } = "";
    public bool Starting { get; set; } = false;
    public bool Maverick { get; set; } = false;
    public bool Attack { get; set; } = false;
    public bool Free { get; set; } = false;
    public List<string> Types { get; set; } = new();
    public List<string> Elements { get; set; } = new();
    public string Requires { get; set; } = "";
    public string Monster { get; set; } = "";
    public string Effect { get; set; } = "";
    public List<string> Key { get; set; } = new();
    public List<string> Category { get; set; } = new();
};

public static class BaseActionExtensions
{
    public static ActionData ToData(this BaseAction action, Monster monster)
    {
        var data = new ActionData();
        data.Name = action.Name;
        data.Starting = action.IsStartingAction();
        data.Maverick = action.MaverickSkill;
        data.Attack = action.IsAttackingAction();
        data.Free = action.IsFreeAction();
        data.Types = action.GetSkillTypes();
        data.Elements = action.Cost.ToList();
        data.Requires = action.GetRequirement();
        SkillPicker.WeightedSkill weightedSkill = new SkillPicker.WeightedSkill(action, 1);
        SkillInstance skillInstance = new SkillInstance(weightedSkill.Action, monster);
        data.Effect = DescriptionHelper.ReformatDescription(action.Name, action.GetDescription(skillInstance));
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

    public static bool IsStartingAction(this BaseAction action)
    {
        return action.Types.Any(type => type.GetComponent<MonsterType>().GetName() == "Starting Actions");
    }

    public static bool IsAttackingAction(this BaseAction action)
    {
        return !action.IsActionSubType(EActionSubType.NonDamagingAction);
    }
}