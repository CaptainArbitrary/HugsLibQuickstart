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

    private const int DefaultMapSize = 250;
    public bool BypassSafetyDialog;
    public int MapSizeToGen = DefaultMapSize;
    public QuickstartMode OperationMode = QuickstartMode.Disabled;
    public string SaveFileToLoad;
    public string ScenarioToGen;
    public bool StopOnErrors = true;
    public bool StopOnWarnings;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref OperationMode, "OperationMode");
        Scribe_Values.Look(ref SaveFileToLoad, "SaveFileToLoad", string.Empty);
        Scribe_Values.Look(ref ScenarioToGen, "ScenarioToGen");
        Scribe_Values.Look(ref StopOnErrors, "StopOnErrors");
        Scribe_Values.Look(ref StopOnWarnings, "StopOnWarnings");
        Scribe_Values.Look(ref BypassSafetyDialog, "BypassSafetyDialog");
        Scribe_Values.Look(ref MapSizeToGen, "MapSizeToGen");

        base.ExposeData();
    }
}
