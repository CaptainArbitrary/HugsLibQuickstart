using HarmonyLib;
using Verse;

namespace HugsLibQuickstart.HarmonyPatches;

[HarmonyPatch(typeof(Root), nameof(Root.OnGUI))]
public class Root_OnGUI
{
    public static void Postfix()
    {
        Quickstart mod = LoadedModManager.GetMod<Quickstart>();
        mod.OnGUIUnfiltered();
    }
}