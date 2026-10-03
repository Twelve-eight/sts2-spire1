using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Spire1.Spire1Code.Run;

/// <summary>
/// 每局存档级的"内容登记"快照。它是一个真正的引擎 <see cref="ModifierModel"/> 子类,
/// 因此会被 BaseLib 经 ReflectionHelper 自动注册进 ModelDb(得到 ID SPIRE1-*),并可通过
/// 引擎 SerializableRun.Modifiers 的 [SavedProperty] 通道随存档持久化(与 AutoAnthony 的
/// ChaosPoolSnapshotModifier 同款做法,已验证)。
/// <para>
/// 它<b>不是</b>一个会在游戏里显示的玩法修正:不进 Neow 选项池(不在 GoodModifiers/
/// BadModifiers 里),不接收战斗钩子,并在存档加载后立即由 Spire1ContentSnapshotPatch 从
/// 活动 RunState.Modifiers 中剥离,只把它携带的布尔值搬进本会话的运行期闩锁。
/// </para>
/// </summary>
public sealed class Spire1ContentSnapshotModifier : ModifierModel
{
    /// <summary>该局创建时锁存的 Spire1Config.RegisterContentNextRun 值。
    /// 缺席(旧存档)时消费方按 true 处理。</summary>
    [SavedProperty]
    public bool ContentRegistered { get; set; } = true;

    public override bool ShouldReceiveCombatHooks => false;
}
