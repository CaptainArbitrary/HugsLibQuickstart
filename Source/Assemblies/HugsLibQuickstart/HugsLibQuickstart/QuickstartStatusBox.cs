using System;
using System.Text;
using UnityEngine;
using Verse;

namespace HugsLibQuickstart;

internal class QuickstartStatusBox
{
    public delegate void AbortHandler(bool abortAndDisable);

    private static readonly Vector2 StatusRectSize = new(240f, 75f);
    private static readonly Vector2 StatusRectPadding = new(26f, 18f);

    private readonly IOperationMessageProvider _pendingOperation;

    public QuickstartStatusBox(IOperationMessageProvider pendingOperation)
    {
        _pendingOperation = pendingOperation ?? throw new ArgumentNullException(nameof(pendingOperation));
    }

    public static bool ShiftIsHeld => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    public event AbortHandler AbortRequested;

    public void OnGUI()
    {
        string statusText = GetStatusBoxText();
        Rect boxRect = GetStatusBoxRect(statusText);
        DrawStatusBox(boxRect, statusText);
        HandleKeyPressEvents();
    }

    private string GetStatusBoxText()
    {
        StringBuilder sb = new("Quickstart is preparing to\n");
        sb.Append(_pendingOperation.Message);
        sb.AppendLine();
        sb.AppendLine();
        sb.Append("<color=#777777>");
        sb.AppendLine("Press Space to abort");
        sb.Append("Press Shift+Space to disable");
        sb.Append("</color>");
        return sb.ToString();
    }

    private static Rect GetStatusBoxRect(string statusText)
    {
        Vector2 statusTextSize = Text.CalcSize(statusText);
        float boxWidth = Mathf.Max(StatusRectSize.x, statusTextSize.x + StatusRectPadding.x * 2f);
        float boxHeight = Mathf.Max(StatusRectSize.y, statusTextSize.y + StatusRectPadding.y * 2f);
        Rect boxRect = new(
            (UI.screenWidth - boxWidth) / 2f,
            (UI.screenHeight / 2f - boxHeight) / 2f,
            boxWidth, boxHeight
        );
        boxRect = boxRect.Rounded();
        return boxRect;
    }

    private static void DrawStatusBox(Rect rect, string statusText)
    {
        Widgets.DrawShadowAround(rect);
        Widgets.DrawWindowBackground(rect);
        TextAnchor prevAnchor = Text.Anchor;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(rect, statusText);
        Text.Anchor = prevAnchor;
    }

    private void HandleKeyPressEvents()
    {
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
        {
            bool abortAndDisable = ShiftIsHeld;
            Event.current.Use();
            AbortRequested?.Invoke(abortAndDisable);
        }
    }

    public interface IOperationMessageProvider
    {
        string Message { get; }
    }

    public class LoadSaveOperation : IOperationMessageProvider
    {
        private readonly string _fileName;

        public LoadSaveOperation(string fileName)
        {
            _fileName = fileName;
        }

        public string Message => $"load save file: {_fileName}";
    }

    public class GenerateMapOperation : IOperationMessageProvider
    {
        private readonly int _mapSize;
        private readonly string _scenario;

        public GenerateMapOperation(string scenario, int mapSize)
        {
            _scenario = scenario;
            _mapSize = mapSize;
        }

        public string Message => $"generate map: {_scenario} ({_mapSize}x{_mapSize})";
    }
}
