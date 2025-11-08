using BepInEx;
using HarmonyLib;

namespace WikiHelper;

[BepInPlugin("org.hlxii.plugin.wikiHelper", "Wiki Helper", "0.0.0")]
public class Plugin : BaseUnityPlugin
{
    private static Harmony _harmony;

    private void Awake()
    {
        _harmony = new Harmony("org.hlxii.plugin.wikiHelper");
        _harmony.PatchAll();

        InputHookManager.Initialize();
    }

    private void OnDestroy()
    {
        InputHookManager.Cleanup();
        _harmony?.UnpatchSelf();
    }
}