

using System;
using System.Collections.Generic;
using UnityEngine;

namespace WikiHelper.Models;


public class EquipmentData
{
    public string Name { get; set; } = "";
    public string Common { get; set; } = "";
    public string Rare { get; set; } = "";
    public string Epic { get; set; } = "";
    public List<string> Key { get; set; }
    public List<string> Category { get; set; }
};

public static class EquipmentInstanceExtensions
{
    public static EquipmentData ToData(this ItemManager.EquipmentItemInstance equip, Monster monster)
    {
        var data = new EquipmentData();
        data.Name = equip.BaseItem.Name;
        EquipmentInstance equipInstance = new EquipmentInstance(equip.BaseItem as Equipment, monster, 1, false, 0);
        data.Common = DescriptionHelper.ReformatDescription(data.Name, (equip.BaseItem as Equipment).GetDescription(equipInstance));
        EquipmentInstance rareEquipInstance = new EquipmentInstance(equip.RareItem as Equipment, monster, 1, false, 0);
        data.Rare = DescriptionHelper.ReformatDescription(data.Name, (equip.RareItem as Equipment).GetDescription(rareEquipInstance));
        EquipmentInstance epicEquipInstance = new EquipmentInstance(equip.EpicItem as Equipment, monster, 1, false, 0);
        data.Epic = DescriptionHelper.ReformatDescription(data.Name, (equip.EpicItem as Equipment).GetDescription(epicEquipInstance));

        string allDescriptions = string.Join(" ", [data.Common, data.Rare, data.Epic]);
        try
        {
            data.Key = DescriptionHelper.ParseForKeys(allDescriptions);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse keys for {data.Name} -  \"{allDescriptions}\" - {ex}");
        }
        return data;
    }
}