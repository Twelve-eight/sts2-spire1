using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using Godot;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Rest-site backgrounds are consumed as scenes
/// (<c>ActModel.CreateRestSiteBackground → PreloadManager.Cache.GetScene(tscn)</c>), and our pck
/// pipeline cannot ship .tscn — PckPacker aborts the whole pack when it sees one. So instead of a
/// custom scene file, this patch replaces the instantiated scene with an equivalent code-built
/// Control: a full-rect TextureRect over the act's composited StS1 map background
/// (<c>res://Spire1/images/rest_site/campfire_bg.png</c>, pre-darkened in the asset pipeline).
/// Visual parity with StS1: the campfire room reuses the map backdrop.
/// </summary>
[HarmonyPatch(typeof(ActModel), "CreateRestSiteBackground")]
internal static class RestSiteBackgroundPatch
{
    private const string BgPath = "res://Spire1/images/rest_site/campfire_bg.png";

    [HarmonyPostfix]
    private static Control UseSts1Backdrop(Control __result, ActModel __instance)
    {
        if (__instance is not Acts.Spire1Act)
        {
            return __result; // vanilla acts keep their shipped scenes
        }

        var tex = ResourceLoader.Load<Texture2D>(BgPath);
        if (tex == null)
        {
            MainFile.Logger.Warn("Spire1 rest-site backdrop missing: " + BgPath);
            return __result;
        }

        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var rect = new TextureRect();
        rect.Texture = tex;
        rect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        rect.StretchMode = TextureRect.StretchModeEnum.Scale;
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(rect);

        // Engine contract (NRestSiteRoom._Ready): the returned control's subtree must expose
        // a scene-unique "%RestSiteLighting" control used for room-light modulation. Without
        // it GetNode("%RestSiteLighting") throws and the whole rest-site room fails to init.
        var lighting = new Control();
        lighting.Name = "RestSiteLighting";
        lighting.MouseFilter = Control.MouseFilterEnum.Ignore;
        lighting.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.AddChild(lighting);
        // Godot registers scene-unique names through the OWNER, not the parent: a runtime
        // child needs Owner set before UniqueNameInOwner takes effect, otherwise
        // GetNode("%RestSiteLighting") cannot resolve and room init aborts.
        lighting.Owner = root;
        lighting.UniqueNameInOwner = true;
        return root;
    }
}
