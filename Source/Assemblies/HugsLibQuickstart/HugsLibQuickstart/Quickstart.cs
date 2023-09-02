using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Profile;

namespace HugsLibQuickstart;

public class Quickstart : Mod
{
    private bool _quickstartPending;
    private QuickstartSettings _settings;
    private QuickstartStatusBox _statusBox;

    public Quickstart(ModContentPack content) : base(content)
    {
        if (Settings.OperationMode == QuickstartSettings.QuickstartMode.Disabled) return;

        Log.Message($"Settings.OperationMode = {Settings.OperationMode}");

        Harmony harmony = new(content.PackageId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        _quickstartPending = true;
        _statusBox = new QuickstartStatusBox(GetStatusBoxOperation(Settings));
        _statusBox.AbortRequested += StatusBoxAbortRequestedHandler;

        QuickstartStatusBox.IOperationMessageProvider GetStatusBoxOperation(QuickstartSettings settings)
        {
            return settings.OperationMode switch
            {
                QuickstartSettings.QuickstartMode.LoadMap => new QuickstartStatusBox.LoadSaveOperation(
                    GetSaveNameToLoad() ?? string.Empty),
                QuickstartSettings.QuickstartMode.GenerateMap => new QuickstartStatusBox.GenerateMapOperation(
                    settings.ScenarioToGen, settings.MapSizeToGen),
                _ => throw new ArgumentOutOfRangeException("Unhandled operation mode: " + settings.OperationMode)
            };
        }
    }

    public QuickstartSettings Settings
    {
        get { return _settings ??= GetSettings<QuickstartSettings>(); }
    }

    private string GetSaveNameToLoad()
    {
        return Settings.SaveFileToLoad ?? TryGetMostRecentSaveFileName();
    }

    private string TryGetMostRecentSaveFileName()
    {
        string mostRecentFilePath = GenFilePaths.AllSavedGameFiles.FirstOrDefault()?.Name;
        return Path.GetFileNameWithoutExtension(mostRecentFilePath);
    }

    private void StatusBoxAbortRequestedHandler(bool abortAndDisable)
    {
        _quickstartPending = false;
        Log.Warning("Quickstart aborted: Space key was pressed.");
        if (abortAndDisable)
        {
            Settings.OperationMode = QuickstartSettings.QuickstartMode.Disabled;
            LongEventHandler.ExecuteWhenFinished(WriteSettings);
        }
    }

    public void OnGUIUnfiltered()
    {
        if (!_quickstartPending) return;
        _statusBox.OnGUI();
    }

    internal void OnLateInitialize()
    {
        // RetrofitSettingWithLabel();
        // EnumerateMapSizes();
        if (Prefs.DevMode) LongEventHandler.QueueLongEvent(InitiateQuickstart, null, false, null);
    }

    private void InitiateQuickstart()
    {
        if (!_quickstartPending) return;
        _quickstartPending = false;
        _statusBox = null;

        if (Settings.OperationMode == QuickstartSettings.QuickstartMode.Disabled) return;

        CheckForErrorsAndWarnings();

        if (Settings.OperationMode == QuickstartSettings.QuickstartMode.GenerateMap) InitiateMapGeneration();
        // } else if (Settings.OperationMode == QuickstartSettings.QuickstartMode.LoadMap)
        // {
        // InitiateSaveLoading();
    }

    private void CheckForErrorsAndWarnings()
    {
        if (Settings.StopOnErrors && Log.Messages.Any(m => m.type == LogMessageType.Error)) throw new WarningException("errors detected in log");
        if (Settings.StopOnWarnings && Log.Messages.Any(m => m.type == LogMessageType.Warning)) throw new WarningException("warnings detected in log");
    }

    private void InitiateMapGeneration()
    {
        Log.Message("Quickstarter generating map with scenario: " + GetMapGenerationScenario().name);
        LongEventHandler.QueueLongEvent(() =>
        {
            MemoryUtility.ClearAllMapsAndWorld();
            ApplyQuickstartConfiguration();
            PageUtility.InitGameStart();
            Find.TickManager.Pause();
        }, "GeneratingMap", true, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
    }

    private Scenario GetMapGenerationScenario()
    {
        // return TryGetScenarioByName(Settings.ScenarioToGen) ?? ScenarioDefOf.Crashlanded.scenario;
        return ScenarioDefOf.Crashlanded.scenario;
    }

    private void ApplyQuickstartConfiguration()
    {
        Current.ProgramState = ProgramState.Entry;
        Current.Game = new Game
        {
            InitData = new GameInitData(),
            Scenario = GetMapGenerationScenario()
        };
        Find.Scenario.PreConfigure();
        Current.Game.storyteller = new Storyteller(StorytellerDefOf.Cassandra, DifficultyDefOf.Rough);
        Current.Game.World = WorldGenerator.GenerateWorld(0.05f, GenText.RandomSeedString(),
            OverallRainfall.Normal, OverallTemperature.Normal, OverallPopulation.Normal);
        Find.GameInitData.ChooseRandomStartingTile();
        Find.GameInitData.mapSize = Settings.MapSizeToGen;
        Find.Scenario.PostIdeoChosen();
        Find.GameInitData.PrepForMapGen();
        Find.Scenario.PreMapGenerate();
    }
}
