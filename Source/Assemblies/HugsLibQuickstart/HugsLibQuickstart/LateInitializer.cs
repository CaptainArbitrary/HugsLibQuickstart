using Verse;

namespace HugsLibQuickstart;

[StaticConstructorOnStartup]
public class LateInitializer
{
    static LateInitializer()
    {
        LoadedModManager.GetMod<Quickstart>().OnLateInitialize();
    }
}