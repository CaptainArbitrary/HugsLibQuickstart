using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace HugsLibQuickstart.HarmonyPatches;

[HarmonyPatch(typeof(DebugWindowsOpener))]
[HarmonyPatch("DrawButtons")]
internal class DebugWindowsOpenerPatch
{
    private static bool _patched;

    [HarmonyPrepare]
    public static bool Prepare()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (!_patched) Log.Warning("DebugWindowsOpener_Patch could not be applied.");
        });
        return true;
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> DrawAdditionalButtons(IEnumerable<CodeInstruction> instructions)
    {
        _patched = false;
        CodeInstruction[] instructionsArr = instructions.ToArray();
        FieldInfo widgetRowField = AccessTools.Field(typeof(DebugWindowsOpener), "widgetRow");
        foreach (CodeInstruction inst in instructionsArr)
        {
            if (!_patched && widgetRowField != null && inst.opcode == OpCodes.Bne_Un)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, widgetRowField);
                yield return new CodeInstruction(OpCodes.Call,
                    ((Action<WidgetRow>)Quickstart.DrawDebugToolbarButton).Method);
                _patched = true;
            }

            yield return inst;
        }
    }
}

[HarmonyPatch(typeof(DebugWindowsOpener))]
[HarmonyPatch("DevToolStarterOnGUI")]
internal class DevToolStarterOnGUIPatch
{
    private static bool _patched;

    [HarmonyPrepare]
    public static bool Prepare()
    {
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            if (!_patched) Log.Error("DevToolStarterOnGUI_Patch could not be applied.");
        });
        return true;
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> ExtendButtonsWindow(IEnumerable<CodeInstruction> instructions)
    {
        _patched = false;
        foreach (CodeInstruction inst in instructions)
        {
            if (!_patched && inst.opcode == OpCodes.Ldc_R4 && 28f.Equals(inst.operand))
            {
                // add one to the number of expected buttons
                yield return new CodeInstruction(OpCodes.Ldc_R4, 1f);
                yield return new CodeInstruction(OpCodes.Add);
                _patched = true;
            }

            yield return inst;
        }
    }
}
