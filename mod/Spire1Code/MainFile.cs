using System.Reflection;
using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Patches;
using Spire1.Spire1Code.Character;
using BaseLib.Patches.Localization;

namespace Spire1.Spire1Code;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Spire1"; // used for resource filepath (res://Spire1) and ID prefix (SPIRE1-)
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        // Enable BaseLib SimpleLoc so cards.json !D!/!B!/*word* tokens are converted at load.
        SimpleLoc.EnableSimpleLoc(ModId);
        // Register C# scripts used by any Godot scenes shipped in the .pck.
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        // Runtime content toggles (Settings -> Mod Settings).
        ModConfigRegistry.Register(ModId, new Spire1Config());

        // LEAN-CODE RULE (DEVELOP.md 7a): shipped StS2 cards that are identical to their StS1
        // counterparts are added to our pools instead of being reimplemented. Must run before the
        // game generates any pool, because ModHelper freezes modded pool content on first use.
        SharedCardReuse.Register();

        // Apply Harmony patches declared in this assembly — one try/catch PER TYPE so a single
        // bad patch can never abort the whole set (PatchAll aborts on first failure, which
        // silently stripped every other patch for an entire night run on 2026-08-24).
        Harmony harmony = new(ModId);
        int failed = 0;
        foreach (var type in typeof(MainFile).Assembly.GetTypes())
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
            {
                continue;
            }
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception e)
            {
                failed++;
                Logger.Error($"Harmony patch {type.Name} failed: {e.Message}");
            }
        }
        if (failed > 0)
        {
            Logger.Error($"Harmony: {failed} patch class(es) failed to apply");
        }

        // 第三方（RitsuLib）弹窗抑制不能进上面的属性扫描——目标类型缺失时 AccessTools
        // 解析会抛异常，会让注册循环每次启动都记一条失败。显式调用、内部自兜底。
        if (Spire1Config.IgnoreMpModDifferences)
        {
            Logger.Info(RitsuLibPopupSuppressionPatch.Apply(harmony)
                ? "[Spire1] MP ignore-mod-diff: RitsuLib divergence popup suppressed"
                : "[Spire1] MP ignore-mod-diff: RitsuLib popup type not found (mod absent?) — skipped");
        }

        // 归档角色门控的启动期预热：触发静态扫描并让"隐藏 N 张卡"计数进启动日志
        //（否则静态字段懒初始化，首次进总览才执行，现场排障少一条关键证据）。
        _ = Patches.CharacterArchive.ArchivedModelTypes;
    }
}
