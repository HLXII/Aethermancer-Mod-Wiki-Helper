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

    [HarmonyPatch(typeof(UIController), "Initialize")]
    class InputPatch
    {
        static void Postfix(UIController __instance)
        {
            Debug.Log("Patching anotha hook onto UIController on Initialize");
            __instance.gameObject.AddComponent<InputHook>().Init(__instance);
        }
    }
}