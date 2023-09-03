using Verse;

namespace HugsLibQuickstart;

[StaticConstructorOnStartup]
public class LateInitializer
{
    static LateInitializer()
    {
        if (ModsConfig.IsActive("unlimitedhugs.hugslib")) return;
        LoadedModManager.GetMod<QuickstartMod>().OnLateInitialize();
    }
}
