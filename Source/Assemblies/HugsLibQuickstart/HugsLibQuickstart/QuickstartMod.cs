using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Profile;

namespace HugsLibQuickstart;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class QuickstartMod : Mod
{
    public static readonly List<MapSizeEntry> MapSizes = new();
    private bool _quickstartPending;
    private QuickstartStatusBox _statusBox;

    public QuickstartMod(ModContentPack content) : base(content)
    {
        Instance = this;
        Settings = GetSettings<QuickstartSettings>();

        if (ModsConfig.IsActive("unlimitedhugs.hugslib")) return;

        Harmony harmony = new(content.PackageId);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        if (Settings.OperationMode == QuickstartSettings.QuickstartMode.Disabled || !Prefs.DevMode) return;

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

    public static QuickstartMod Instance { get; private set; }

    public static QuickstartSettings Settings { get; private set; }

    private string GetSaveNameToLoad()
    {
        return !Settings.SaveFileToLoad.NullOrEmpty() ? Settings.SaveFileToLoad : TryGetMostRecentSaveFileName();
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
        EnumerateMapSizes();
        if (Prefs.DevMode) LongEventHandler.QueueLongEvent(InitiateQuickstart, null, false, null);
    }

    private static void EnumerateMapSizes()
    {
        int[] vanillaSizes = Traverse.Create<Dialog_AdvancedGameConfig>().Field("MapSizes").GetValue<int[]>();
        if (vanillaSizes == null)
        {
            Log.Error("Could not reflect required field: Dialog_AdvancedGameConfig.MapSizes");
            return;
        }

        MapSizes.Clear();
        MapSizes.Add(new MapSizeEntry(75, "75x75 (Encounter)"));
        foreach (int size in vanillaSizes)
        {
            string desc = null;
            switch (size)
            {
                case 200:
                    desc = "MapSizeSmall".Translate();
                    break;
                case 250:
                    desc = "MapSizeMedium".Translate();
                    break;
                case 300:
                    desc = "MapSizeLarge".Translate();
                    break;
                case 350:
                    desc = "MapSizeExtreme".Translate();
                    break;
            }

            string label = string.Format("{0}x{0}", size) + (desc != null ? $" ({desc})" : "");
            MapSizes.Add(new MapSizeEntry(size, label));
        }
        SnapSettingsMapSizeToClosestValue(Settings, MapSizes);
    }

    private static void SnapSettingsMapSizeToClosestValue(QuickstartSettings settings, List<MapSizeEntry> sizes) {
        Settings.MapSizeToGen = sizes.OrderBy(e => Mathf.Abs(e.Size - settings.MapSizeToGen)).First().Size;
    }

    private void InitiateQuickstart()
    {
        if (!_quickstartPending) return;
        _quickstartPending = false;
        _statusBox = null;

        if (Settings.OperationMode == QuickstartSettings.QuickstartMode.Disabled) return;

        CheckForErrorsAndWarnings();

        if (Settings.OperationMode == QuickstartSettings.QuickstartMode.GenerateMap)
            InitiateMapGeneration();
        else if (Settings.OperationMode == QuickstartSettings.QuickstartMode.LoadMap) InitiateSaveLoading();
    }

    private void CheckForErrorsAndWarnings()
    {
        if (Settings.StopOnErrors && Log.Messages.Any(m => m.type == LogMessageType.Error)) throw new WarningException("errors detected in log");
        if (Settings.StopOnWarnings && Log.Messages.Any(m => m.type == LogMessageType.Warning)) throw new WarningException("warnings detected in log");
    }

    internal void InitiateMapGeneration()
    {
        Log.Message("Quickstart generating map with scenario: " + GetMapGenerationScenario().name);
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
        return ScenarioLister.AllScenarios().FirstOrDefault(s => s.name == Settings.ScenarioToGen) ?? ScenarioDefOf.Crashlanded.scenario;
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

    internal void InitiateSaveLoading()
    {
        string saveName = GetSaveNameToLoad() ?? throw new WarningException("save filename not set");
        string filePath = GenFilePaths.FilePathForSavedGame(saveName);
        if (!File.Exists(filePath)) throw new WarningException("save file not found: " + saveName);
        Log.Message("Quickstart is loading saved game: " + saveName);

        void LoadAction()
        {
            GameDataSaveLoader.LoadGame(saveName);
        }

        if (Settings.BypassSafetyDialog)
            LoadAction();
        else
            PreLoadUtility.CheckVersionAndLoad(filePath, ScribeMetaHeaderUtility.ScribeHeaderMode.Map, LoadAction);
    }

    internal static void DrawDebugToolbarButton(WidgetRow widgets)
    {
        const string quickstartButtonTooltip = "Open the quickstart settings.\n\n"
                                               + "This lets you automatically generate a map or load an existing save when the game is started.\n"
                                               + "Shift-click to quick-generate a new map.";
        if (widgets.ButtonIcon(Textures.QuickstartIcon, quickstartButtonTooltip))
        {
            WindowStack stack = Find.WindowStack;
            if (EventUtility.ShiftIsHeld)
            {
                stack.TryRemove(typeof(DialogQuickstartSettings));
                Instance.InitiateMapGeneration();
            }
            else
            {
                if (stack.IsOpen<DialogQuickstartSettings>())
                    stack.TryRemove(typeof(DialogQuickstartSettings));
                else
                    stack.Add(new DialogQuickstartSettings());
            }
        }
    }

    public class MapSizeEntry
    {
        public readonly string Label;
        public readonly int Size;

        public MapSizeEntry(int size, string label)
        {
            Size = size;
            Label = label;
        }
    }
}
