using System;
using Verse;

namespace HugsLibQuickstart;

public class QuickstartSettings : ModSettings
{
    public enum QuickstartMode
    {
        Disabled = 0,
        LoadMap = 1,
        GenerateMap = 2
    }

    public bool BypassSafetyDialog;
    public int MapSizeToGen = 250;

    public QuickstartMode OperationMode = QuickstartMode.Disabled;
    public string SaveFileToLoad;
    public string ScenarioToGen;
    public bool StopOnErrors = true;
    public bool StopOnWarnings;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref OperationMode, "OperationMode");
        Scribe_Values.Look(ref SaveFileToLoad, "SaveFileToLoad", String.Empty);
        Scribe_Values.Look(ref ScenarioToGen, "ScenarioToGen");
        Scribe_Values.Look(ref StopOnErrors, "StopOnErrors");
        Scribe_Values.Look(ref StopOnWarnings, "StopOnWarnings");
        Scribe_Values.Look(ref BypassSafetyDialog, "BypassSafetyDialog");

        base.ExposeData();
    }
}
