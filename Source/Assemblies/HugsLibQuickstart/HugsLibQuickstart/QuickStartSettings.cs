using System;
using Verse;

namespace HugsLibQuickstart
{
    public class QuickStartSettings : ModSettings
    {
        public enum QuickstartMode {
            Disabled = 0,
            LoadMap = 1,
            GenerateMap = 2
        }

        public QuickstartMode OperationMode = QuickstartMode.Disabled;
        public string SaveFileToLoad;
        public string ScenarioToGen;
        // public int MapSizeToGen = QuickstartController.DefaultMapSize;
        public bool StopOnErrors = true;
        public bool StopOnWarnings;
        public bool BypassSafetyDialog;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref OperationMode, "OperationMode");
            Scribe_Values.Look(ref SaveFileToLoad, "SaveFileToLoad");
            Scribe_Values.Look(ref ScenarioToGen, "ScenarioToGen");
            Scribe_Values.Look(ref StopOnErrors, "StopOnErrors");
            Scribe_Values.Look(ref StopOnWarnings, "StopOnWarnings");
            Scribe_Values.Look(ref BypassSafetyDialog, "BypassSafetyDialog");

            base.ExposeData();
        }
        
    }
}