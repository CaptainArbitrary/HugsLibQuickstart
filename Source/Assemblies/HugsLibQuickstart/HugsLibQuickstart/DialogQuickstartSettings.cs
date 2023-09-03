using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HugsLibQuickstart;

public class DialogQuickstartSettings : Window
{
    private readonly List<FileEntry> _saveFiles = new();

    public DialogQuickstartSettings()
    {
        closeOnCancel = true;
        closeOnAccept = false;
        doCloseButton = false;
        doCloseX = true;
        resizeable = false;
        draggable = true;
    }

    public override Vector2 InitialSize => new(600f, 500f);

    public override void PreOpen()
    {
        base.PreOpen();
        CacheSavedGameFiles();
        EnsureSettingsHaveValidFiles(QuickstartMod.Settings);
    }

    public override void PostClose()
    {
        QuickstartMod.Instance.WriteSettings();
    }

    public override void DoWindowContents(Rect inRect)
    {
        const float categoryPadding = 10f;
        const float categoryInset = 30f;
        const float radioLabelInset = 40f;
        const float mainListingSpacing = 6f;
        const float subListingSpacing = 6f;
        const float subListingLabelWidth = 100f;
        const float subListingRowHeight = 30f;
        const float checkboxListingWidth = 280f;
        const float listingColumnSpacing = 17f;
        QuickstartSettings settings = QuickstartMod.Settings;
        Listing_Standard mainListing = new();
        mainListing.verticalSpacing = mainListingSpacing;
        mainListing.Begin(inRect);
        Text.Font = GameFont.Medium;
        mainListing.Label("Quickstart settings");
        Text.Font = GameFont.Small;
        mainListing.GapLine();
        mainListing.Gap();
        OperationModeRadioButton(mainListing, radioLabelInset, "Quickstart off", settings, QuickstartSettings.QuickstartMode.Disabled,
            "Quickstart functionality is disabled.\nThe game starts normally.");
        OperationModeRadioButton(mainListing, radioLabelInset, "Quickstart: load save file", settings, QuickstartSettings.QuickstartMode.LoadMap,
            "Load the selected saved game right after launch.");
        float expectedHeight = categoryPadding * 2 + (subListingRowHeight + subListingSpacing) * 1;
        MakeSubListing(mainListing, 0, expectedHeight, categoryPadding, categoryInset, subListingSpacing, (sub, width) =>
        {
            sub.ColumnWidth = subListingLabelWidth;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect rect = sub.GetRect(subListingRowHeight);
            Widgets.Label(rect, "Save file:");
            Text.Anchor = TextAnchor.UpperLeft;
            sub.NewColumn();
            sub.ColumnWidth = width - subListingLabelWidth - listingColumnSpacing;
            MakeSelectSaveButton(sub, settings);
        });
        OperationModeRadioButton(mainListing, radioLabelInset, "Quickstart: generate map", settings, QuickstartSettings.QuickstartMode.GenerateMap,
            "Generate a new map right after launch.\nWorks the same as using the \"quicktest\" command line option.");
        expectedHeight = categoryPadding * 2 + (subListingRowHeight + subListingSpacing) * 2;
        MakeSubListing(mainListing, 0, expectedHeight, categoryPadding, categoryInset, subListingSpacing, (sub, width) =>
        {
            sub.ColumnWidth = subListingLabelWidth;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect rect = sub.GetRect(subListingRowHeight);
            Widgets.Label(rect, "Scenario:");
            sub.Gap(subListingSpacing);
            rect = sub.GetRect(subListingRowHeight);
            Widgets.Label(rect, "Map size:");
            Text.Anchor = TextAnchor.UpperLeft;
            sub.NewColumn();
            sub.ColumnWidth = width - subListingLabelWidth - listingColumnSpacing;
            MakeSelectScenarioButton(sub, settings);
            MakeSelectMapSizeButton(sub, settings);
        });
        expectedHeight = categoryPadding * 2 + (subListingRowHeight + subListingSpacing) * 3;
        MakeSubListing(mainListing, checkboxListingWidth, expectedHeight, categoryPadding, 0f, subListingSpacing, (sub, width) =>
        {
            sub.CheckboxLabeled("Abort quickstart on error", ref settings.StopOnErrors, "Prevent quickstart if errors are detected during startup.");
            sub.CheckboxLabeled("Abort quickstart on warning", ref settings.StopOnWarnings, "Prevent quickstart if warnings are detected during startup.");
            sub.CheckboxLabeled("Ignore version & mod config mismatch", ref settings.BypassSafetyDialog, "Skip the mod config mismatch dialog and load all saved games regardless.");
        });
        mainListing.End();
        Text.Anchor = TextAnchor.UpperLeft;

        Vector2 btnSize = new(180f, 40f);
        float buttonYStart = inRect.height - btnSize.y;
        if (Widgets.ButtonText(new Rect(inRect.width - btnSize.x, buttonYStart, btnSize.x, btnSize.y), "Close")) Close();
    }

    private void OperationModeRadioButton(Listing_Standard listing, float labelInset, string label, QuickstartSettings settings, QuickstartSettings.QuickstartMode assignedMode, string tooltip)
    {
        const float labelTopMargin = -4f;
        const float fontSize = 16f;
        float lineHeight = Text.LineHeight;
        Rect entryRect = listing.GetRect(lineHeight + listing.verticalSpacing);
        Rect labelRect = new(entryRect.x + labelInset, entryRect.y + labelTopMargin, entryRect.width - labelInset, entryRect.height - labelTopMargin);
        Rect rowRect = new(entryRect.x, entryRect.y, entryRect.width, entryRect.height);
        if (tooltip != null)
        {
            if (Mouse.IsOver(rowRect)) Widgets.DrawHighlight(rowRect);
            TooltipHandler.TipRegion(rowRect, tooltip);
        }

        if (Widgets.ButtonInvisible(rowRect))
            if (settings.OperationMode != assignedMode)
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                QuickstartMod.Settings.OperationMode = assignedMode;
            }

        Widgets.RadioButton(entryRect.x, entryRect.y, settings.OperationMode == assignedMode);

        Text.Font = GameFont.Medium;
        string emphasizedLabel = string.Format("<size={0}>{1}</size>", fontSize, label);
        Widgets.Label(labelRect, emphasizedLabel);
        Text.Font = GameFont.Small;
    }

    private void MakeSubListing(Listing_Standard mainListing, float width, float allocatedHeight, float padding, float extraInset, float verticalSpacing, Action<Listing_Standard, float> drawContents)
    {
        Rect subRect = mainListing.GetRect(allocatedHeight);
        width = width > 0 ? width : subRect.width - (padding + extraInset);
        subRect = new Rect(subRect.x + padding + extraInset, subRect.y + padding, width, subRect.height - padding * 2f);
        Listing_Standard sub = new() { verticalSpacing = verticalSpacing };
        sub.Begin(subRect);
        drawContents(sub, width);
        sub.End();
    }

    private void MakeSelectSaveButton(Listing_Standard sub, QuickstartSettings settings)
    {
        const float loadNowWidth = 120f;
        const float horizontalSpacing = 6f;
        const float buttonHeight = 30f;
        const string latestSaveFileLabel = "Most recent save file";
        Rect buttonRect = sub.GetRect(buttonHeight);
        Rect leftHalf = new(buttonRect) { xMax = buttonRect.xMax - (loadNowWidth + horizontalSpacing) };
        Rect rightHalf = new(buttonRect) { xMin = buttonRect.xMin + leftHalf.width + horizontalSpacing };
        string selectedSaveLabel = settings.SaveFileToLoad ?? latestSaveFileLabel;
        if (Widgets.ButtonText(leftHalf, selectedSaveLabel)) ShowSaveFileSelectionFloatMenu();
        if (Widgets.ButtonText(rightHalf, "Load now"))
        {
            if (EventUtility.ShiftIsHeld) settings.OperationMode = QuickstartSettings.QuickstartMode.LoadMap;
            QuickstartMod.Instance.InitiateSaveLoading();
            Close();
        }

        sub.Gap(sub.verticalSpacing);

        void ShowSaveFileSelectionFloatMenu()
        {
            List<FloatMenuOption> options = new()
            {
                new FloatMenuOption(latestSaveFileLabel, () => settings.SaveFileToLoad = null)
            };
            options.AddRange(GetSaveFileFloatMenuOptions(settings));
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    private IEnumerable<FloatMenuOption> GetSaveFileFloatMenuOptions(QuickstartSettings settings)
    {
        const float versionLabelOffset = 10f;
        return _saveFiles.Select(s =>
        {
            return new FloatMenuOption(s.Label, () => { settings.SaveFileToLoad = s.Name; },
                MenuOptionPriority.Default, null, null, Text.CalcSize(s.VersionLabel).x + versionLabelOffset,
                rect =>
                {
                    Color prevColor = GUI.color;
                    GUI.color = s.FileInfo.VersionColor;
                    Text.Anchor = TextAnchor.MiddleLeft;
                    Widgets.Label(new Rect(rect.x + versionLabelOffset, rect.y, 200f, rect.height), s.VersionLabel);
                    Text.Anchor = TextAnchor.UpperLeft;
                    GUI.color = prevColor;
                    return false;
                }
            );
        });
    }

    private void MakeSelectScenarioButton(Listing_Standard sub, QuickstartSettings settings)
    {
        const float generateNowWidth = 120f;
        const float horizontalSpacing = 6f;
        const float buttonHeight = 30f;
        Rect buttonRect = sub.GetRect(buttonHeight);
        Rect leftHalf = new(buttonRect) { xMax = buttonRect.xMax - (generateNowWidth + horizontalSpacing) };
        Rect rightHalf = new(buttonRect) { xMin = buttonRect.xMin + leftHalf.width + horizontalSpacing };
        string selected = settings.ScenarioToGen;
        if (Widgets.ButtonText(leftHalf, selected ?? "Select a scenario"))
        {
            FloatMenu menu = new(ScenarioLister.AllScenarios().Select(s => { return new FloatMenuOption(s.name, () => { settings.ScenarioToGen = s.name; }); }).ToList());
            Find.WindowStack.Add(menu);
        }

        if (Widgets.ButtonText(rightHalf, "Generate now"))
        {
            if (EventUtility.ShiftIsHeld) settings.OperationMode = QuickstartSettings.QuickstartMode.GenerateMap;
            QuickstartMod.Instance.InitiateMapGeneration();
            Close();
        }

        sub.Gap(sub.verticalSpacing);
    }

    private void MakeSelectMapSizeButton(Listing_Standard sub, QuickstartSettings settings)
    {
        List<QuickstartMod.MapSizeEntry> allSizes = QuickstartMod.MapSizes;
        string selected = allSizes.Select(s => s.Size == settings.MapSizeToGen ? s.Label : null).FirstOrDefault(s => s != null);
        if (sub.ButtonText(selected ?? "Select a map size"))
        {
            FloatMenu menu = new(allSizes.Select(s => { return new FloatMenuOption(s.Label, () => { settings.MapSizeToGen = s.Size; }); }).ToList());
            Find.WindowStack.Add(menu);
        }
    }

    private void CacheSavedGameFiles()
    {
        _saveFiles.Clear();
        foreach (FileInfo current in GenFilePaths.AllSavedGameFiles)
            try
            {
                _saveFiles.Add(new FileEntry(current));
            }
            catch (Exception)
            {
                // we don't care. just skip the file
            }
    }

    private void EnsureSettingsHaveValidFiles(QuickstartSettings settings)
    {
        if (_saveFiles.Select(s => s.Name).All(s => s != settings.SaveFileToLoad)) settings.SaveFileToLoad = null;
        if (settings.ScenarioToGen != null && ScenarioLister.AllScenarios().All(s => s.name != settings.ScenarioToGen)) settings.ScenarioToGen = null;
        if (settings.ScenarioToGen == null) settings.ScenarioToGen = ScenarioDefOf.Crashlanded.defName;
    }

    private class FileEntry
    {
        public readonly SaveFileInfo FileInfo;
        public readonly string Label;
        public readonly string Name;
        public readonly string VersionLabel;

        public FileEntry(FileInfo file)
        {
            FileInfo = new SaveFileInfo(file);
            Name = Path.GetFileNameWithoutExtension(FileInfo.FileInfo.Name);
            Label = Name;
            VersionLabel = string.Format("({0})", FileInfo.GameVersion);
        }
    }
}
