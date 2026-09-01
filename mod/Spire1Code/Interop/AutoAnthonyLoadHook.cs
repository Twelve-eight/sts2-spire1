using HarmonyLib;

namespace Spire1.Spire1Code.Interop;

/// <summary>
/// AutoAnthony 桥接的加载时序兜底。
///
/// 时序事实（ModManager.cs L124-127 + L786-877）：ModManager.Initialize 按拓扑序对每个
/// mod 依次 TryLoadMod → LoadFromAssemblyPath → 调 initializer。Spire1 与 AutoAnthony
/// 之间无依赖边，相对顺序由用户 mod 列表（settings.save 的手动排序）决定：
/// - AutoAnthony 先加载 → 本 initializer 调 TryApplyBridge 时它已在 AppDomain，直接应用；
/// - Spire1 先加载 → AssemblyLoad 事件在 AutoAnthony 装载瞬间重试。
/// 事件在 Apply 成功或游戏退出前不摘除（重复装载同名 mod 不发生：RemoveDisabledMods 去重）。
/// </summary>
internal static class AutoAnthonyLoadHook
{
    private static Harmony? _harmony;
    private static bool _hooked;

    internal static void TryApplyBridge(Harmony harmony)
    {
        _harmony = harmony;

        // (2026-09-01 CodeQualityCritic blocker fix) cctor 只含 int 折叠常量后此 try/catch
        // 属纵深防御:任何残留的 AutoAnthony 类型解析失败都不能炸掉 Spire1 initializer
        // (ModManager 会把整个 mod 标记为 errored,连累后续 RitsuLib 弹窗抑制等初始化)。
        bool applied;
        try
        {
            applied = AutoAnthonyCompatBridge.Apply(harmony);
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[Spire1] AutoAnthony bridge failed at init (mod present?): {e.Message}");
            applied = false;
        }
        if (applied)
        {
            return; // 已应用（AutoAnthony 先于我们加载）
        }

        // 尚未加载 → 挂 AssemblyLoad 兜底。已挂过（多次初始化）则复用。
        if (_hooked)
        {
            return;
        }
        _hooked = true;
        AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        MainFile.Logger.Info("[Spire1] AutoAnthony bridge deferred — waiting for its assembly to load.");
    }

    private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        if (args.LoadedAssembly.GetName().Name != "AutoAnthony" || _harmony == null)
        {
            return;
        }
        try
        {
            if (AutoAnthonyCompatBridge.Apply(_harmony))
            {
                AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
                _hooked = false;
            }
        }
        catch (Exception e)
        {
            MainFile.Logger.Error($"[Spire1] AutoAnthony bridge failed on assembly load: {e.Message}");
        }
    }
}
