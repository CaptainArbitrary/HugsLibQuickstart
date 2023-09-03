using UnityEngine;
using Verse;

namespace HugsLibQuickstart;

[StaticConstructorOnStartup]
public static class Textures
{
    public static readonly Texture2D QuickstartIcon = ContentFinder<Texture2D>.Get("quickstartIcon");
}
