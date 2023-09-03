using UnityEngine;

namespace HugsLibQuickstart;

internal static class EventUtility
{
    public static bool ShiftIsHeld => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
}
