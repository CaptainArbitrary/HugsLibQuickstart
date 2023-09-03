using HarmonyLib;
using Verse;

namespace HugsLibQuickstart.HarmonyPatches;

[HarmonyPatch(typeof(Root), nameof(Root.OnGUI))]
public class RootOnGUI
{
    public static void Postfix()
    {
        QuickstartMod mod = LoadedModManager.GetMod<QuickstartMod>();
        mod.OnGUIUnfiltered();
    }
}
