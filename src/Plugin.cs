using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace WikiHelper;

[BepInPlugin("org.hlxii.plugin.wikiHelper", "Wiki Helper", "0.0.0")]
public class Plugin : BaseUnityPlugin
{
    private static Harmony _harmony;

    private void Awake()
    {
        _harmony = new Harmony("org.hlxii.plugin.wikiHelper");
        _harmony.PatchAll();
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }

    [HarmonyPatch(typeof(SkillPicker), "RollThreeSkills")]
    class SkillPickerPatch2
    {
        static void Postfix(SkillPicker __instance)
        {
            Debug.Log($"Skill Picker Rolled for {__instance.Monster.Name}");
            SkillScraper.RunScrape(__instance.Monster);
        }
    }
}