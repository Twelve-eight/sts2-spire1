using System.Threading;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Run;

/// <summary>
/// C14 r11 (2026-10-03): immutable per-operation Cards gate snapshot. Every lifecycle
/// operation that can reach LargeCapsule captures exactly ONE gate decision before it calls
/// into the engine, and that same value is what the model-level final grant boundary
/// (Spire1Card.ShouldAddToDeck) and the engine-point Tags fuse read. A concurrent settings
/// toggle therefore cannot make a prefix and the original engine body disagree about the
/// same operation. Nesting is ref-counted, so an outer lifecycle keeps its decision while
/// an inner operation (relic obtain inside AfterObtained) still sees the same value.
/// </summary>
internal static class Spire1CardsGateSnapshot
{
    private static readonly AsyncLocal<int> Depth = new();
    private static readonly AsyncLocal<bool> ClosedValue = new();

    internal static bool IsActive => Depth.Value > 0;

    /// <summary>The gate decision for the current lifecycle operation, or the live value when
    /// no operation scope is active.</summary>
    internal static bool Closed => Depth.Value > 0 ? ClosedValue.Value : Spire1Config.LiveCardsGateClosed;

    /// <summary>Captures one decision for the whole operation. Nested operations keep the
    /// outermost decision so an inner engine call can never disagree with its caller.</summary>
    internal static void Enter(bool closed)
    {
        if (Depth.Value == 0)
        {
            ClosedValue.Value = closed;
        }
        Depth.Value++;
    }

    internal static void Exit()
    {
        if (Depth.Value > 0)
        {
            Depth.Value--;
        }
    }

    /// <summary>Enters a scope that freezes the live gate value for the duration of the
    /// operation. AsyncLocal flows into await continuations, so the engine body after an
    /// await still reads the same decision the prefix captured.</summary>
    internal static void EnterLive() => Enter(Spire1Config.LiveCardsGateClosed);
}

/// <summary>
/// 本会话内"当前这一局是否登记了 Spire1 奖励内容"的运行期闩锁。
/// <para>
/// 单一事实来源: 新对局在 <see cref="Patches.Spire1ContentSnapshotPatch"/> 里由全局设置
/// Spire1Config.RegisterContentNextRun 锁存; 读档时由该局存档内的
/// <see cref="Spire1ContentSnapshotModifier"/> 恢复; 缺快照的旧存档回落为 true。
/// 这样切换全局设置只影响之后新开的对局, 而任一存档始终以它创建时的值运行。
/// </para>
/// <para>
/// 默认 true: 尚无对局时(主菜单/图鉴)与旧存档都表现为"内容开启", 保持既有可见行为不变。
/// 卡牌/遗物/事件的三个 gate helper AND 上本闩锁; 角色可见性属选人期决策, 不经此处。
/// </para>
/// <para>
/// 生命周期: 新局创建与读档都会覆盖该值; 离开对局 (RunManager.CleanUp) 由
/// <see cref="Patches.Spire1ContentSnapshotPatch"/> 复位为 true, 因此主菜单期间不会保留
/// 上一局的 enabled 状态。复位只影响下一次建局前的默认值, 不影响任何已保存的每局快照。
/// </para>
/// </summary>
internal static class Spire1RunContent
{
    private static bool _contentActiveThisRun = true;

    /// <summary>当前对局是否登记了本 mod 的奖励内容。默认 true(见类型注释)。</summary>
    public static bool ContentActiveThisRun => Volatile.Read(ref _contentActiveThisRun);

    /// <summary>
    /// C14 r10: publish-first writer. Spire1Config.SetRunContentLatch writes the combined Cards
    /// gate before calling this, so a concurrent reader never sees a torn master/cards/run chain.
    /// Do not call directly; every latch/restore/reset goes through Spire1Config.
    /// </summary>
    internal static void WriteContentActiveThisRun(bool value) =>
        Volatile.Write(ref _contentActiveThisRun, value);

    /// <summary>新对局创建时调用: 用全局设置锁存本局的登记决定。</summary>
    public static void LatchForNewRun(bool registerContent)
    {
        Spire1Config.SetRunContentLatch(registerContent);
    }

    /// <summary>读档时调用: 用该存档快照恢复本局的登记决定(缺快照按 true)。</summary>
    public static void RestoreFromSave(bool registeredInSave)
    {
        Spire1Config.SetRunContentLatch(registeredInSave);
    }

    /// <summary>
    /// 离开对局(主菜单/结束/回放)时调用: 清除上一局的登记状态, 回到默认开启。
    /// 幂等; 不触碰任何存档快照 (每局决定已随 SerializableRun.Modifiers 持久化)。
    /// </summary>
    public static void ResetForMenu()
    {
        Spire1Config.SetRunContentLatch(true);
    }
}
