using UnityEngine;
using Verse;

namespace HugsLibQuickstart;

[StaticConstructorOnStartup]
public class Textures
{
    public static readonly Texture2D QuickstartIcon = ContentFinder<Texture2D>.Get("quickstartIcon");
}
