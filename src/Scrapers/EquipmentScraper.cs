using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WikiHelper.Models;
using WikiHelper.Output;

namespace WikiHelper;

public static class EquipmentScraper
{
    public static void RunScrape(Monster monster)
    {
        Debug.Log("Running Equipment Scrape...");

        List<ItemManager.EquipmentItemInstance> equipmentItemInstances = ItemManager.Instance.Equipments;

        var equipData = equipmentItemInstances.Select(instance => instance.ToData(monster)).ToList().OrderBy(equip => equip.Name);

        Debug.Log($"Equipment parsed! - Equips: {equipData.Count()}");

        // Writing to file
        EquipWriter.WriteFiles(equipData);
    }
}