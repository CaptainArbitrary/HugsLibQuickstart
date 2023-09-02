using Verse;

namespace HugsLibQuickstart
{
    public class Quickstart : Mod
    {
        private readonly QuickStartSettings _settings;

        public Quickstart(ModContentPack content) : base(content)
        {
            _settings = GetSettings<QuickStartSettings>();
        }
    }
}