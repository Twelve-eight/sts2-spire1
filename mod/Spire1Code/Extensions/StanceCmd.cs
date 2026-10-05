using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Interop;
using Spire1.Spire1Code.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Extensions;

public static class StanceCmd
{
    /// <summary>Vanilla StS1: every 10 Mantra converts into one entry to Divinity.</summary>
    private const int _mantraThreshold = 10;

    public static bool IsIn<TStance>(Player player) where TStance : StancePower
    {
        // Forms 独立 mod 可选桥接: 只在签名校验通过、运行可用, 且本局选择了形态修正时转发.
        // 未选 Forms 时 IsSelected 返回 false, 普通 Spire1 姿态路径不受影响; 已选 Forms 而桥不可用时显式失败.
        if (FormsCompatibilityBridge.IsSelected(player))
        {
            int kind = FormsCompatibilityBridge.KindOf(typeof(TStance));
            if (kind == FormsCompatibilityBridge.KindNone)
            {
                throw new System.NotSupportedException(
                    "This stance has no form-mode mapping: " + typeof(TStance).FullName);
            }
            return FormsCompatibilityBridge.CurrentKind(player) == kind;
        }
        // C12 r5 (content-layer fail-closed): a stale stance left on a creature must not be
        // treated as the live stance while the powers group is off, otherwise stance-driven
        // effects (Like Water / Rushdown / Mental Fortress) keep firing without any Harmony gate.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return false;
        }
        return player.Creature.GetPower<TStance>() != null;
    }

    public static StancePower? Current(Player player)
    {
        return player.Creature.Powers.OfType<StancePower>().FirstOrDefault();
    }

    public static async Task Enter<TStance>(PlayerChoiceContext ctx, Player player, CardModel? source)
        where TStance : StancePower
    {
        // Forms 独立 mod 可选桥接: 未选 Forms 时 IsSelected=false 走普通路径; 已选 Forms 而桥不可用时显式失败.
        if (FormsCompatibilityBridge.IsSelected(player))
        {
            int kind = FormsCompatibilityBridge.KindOf(typeof(TStance));
            if (kind == FormsCompatibilityBridge.KindNone)
                throw new System.NotSupportedException("This stance has no form-mode mapping: " + typeof(TStance).FullName);
            await FormsCompatibilityBridge.Enter(ctx, player, typeof(TStance), source);
            return;
        }

        StancePower? current = Current(player);
        // C12 r5 (content-layer fail-closed): the engine-level PowerCmd gate can drift or fail to
        // install. This method body is reachable without any Harmony target, so the powers content
        // group is re-checked before the same-stance early return and before mounting. A stale
        // stance on the creature is wound down first (C08 rule: removal stays allowed), then the
        // disabled path returns without entering a new stance. The Forms branch returned earlier,
        // so form runs are unaffected.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            if (current != null)
            {
                await PowerCmd.Remove(current);
            }
            return;
        }

        if (current is TStance)
        {
            return;
        }

        if (current != null)
        {
            await PowerCmd.Remove(current);
        }

        TStance? entered = await PowerCmd.Apply<TStance>(ctx, player.Creature, 1m, player.Creature, source);
        TStance? mounted = player.Creature.GetPower<TStance>();
        if (entered == null || mounted == null)
        {
            // PowerCmd.Apply<T> can return an unattached mutable phantom when the non-generic
            // Apply gate rejects the mount. Only the creature's live power is a successful entry.
            return;
        }

        if (mounted is DivinityPower)
        {
            await PlayerCmd.GainEnergy(3m, player);
        }

        await Dispatch(player, ctx, current, mounted);
    }

    public static async Task Exit(PlayerChoiceContext ctx, Player player, CardModel? source)
    {
        // Forms 独立 mod 可选桥接: 只有 Forms 存在、签名通过且本局选择形态修正时才转发.
        if (FormsCompatibilityBridge.IsSelected(player))
        {
            await FormsCompatibilityBridge.Exit(ctx, player, source);
            return;
        }
        StancePower? current = Current(player);
        if (current == null)
        {
            return;
        }

        await PowerCmd.Remove(current);
        // C12 r5: removal stays allowed (stale wind-down), but the stance-change dispatch is a
        // content-layer effect fan-out (Mental Fortress / Rushdown). Do not fire it while the
        // powers group is off; the removal itself still pays the native Calm exit bonus.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        await Dispatch(player, ctx, current, null);
    }

    public static async Task GainMantra(PlayerChoiceContext ctx, Player player, decimal amount, CardModel? source)
    {
        // C12 r5: same content-layer fail-closed as Enter<TStance>. A disabled powers group must
        // not create or stack Mantra through the engine funnel when that funnel is unproven, and
        // an existing stale Mantra instance is wound down at this first reachable entry instead of
        // being left to feed the Divinity threshold.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            MantraPower? stale = player.Creature.GetPower<MantraPower>();
            if (stale != null)
            {
                await PowerCmd.Remove(stale);
            }
            return;
        }

        MantraPower? mantra = await PowerCmd.Apply<MantraPower>(
            ctx,
            player.Creature,
            amount,
            player.Creature,
            source);
        // PowerCmd.Apply<T> can return an unattached mutable phantom when the non-generic Apply gate
        // rejects the mount (see Enter<TStance> above). Only the creature's live power proves a real
        // mount; a phantom must not drive the threshold loop below.
        MantraPower? mounted = player.Creature.GetPower<MantraPower>();
        if (mantra == null || mounted == null)
        {
            return;
        }
        mantra = mounted;

        // Vanilla: every 10 Mantra enters Divinity and the remainder carries over, so this must be a
        // loop (Prostrate+ can push past 20 at once). It MUST NOT be an unguarded `while`, though:
        // PowerCmd.ModifyAmount silently returns without touching the amount when the combat is
        // ending or the owner has no CombatState (PowerCmd.cs:221-231), and hooks may rewrite the
        // offset (:239-240), so the naive form can spin forever and hang the game. Bail out the
        // moment an iteration fails to reduce the counter.
        while (mantra.Amount >= _mantraThreshold)
        {
            int before = mantra.Amount;
            await PowerCmd.ModifyAmount(ctx, mantra, -_mantraThreshold, player.Creature, source);
            // C12 r5: only convert to Divinity after the counter actually paid the threshold.
            // ModifyAmount can be blocked, rewritten, or skipped (combat ending / no CombatState),
            // so the conversion must not be derived from a request that did not happen.
            if (mantra.Amount > before - _mantraThreshold)
            {
                break;
            }
            await Enter<DivinityPower>(ctx, player, source);
        }
    }

    internal static async Task Dispatch(
        Player player,
        PlayerChoiceContext ctx,
        StancePower? from,
        StancePower? to)
    {
        // C12 r5: second line of defence. Any future caller of this fan-out must also respect the
        // powers group gate; current callers already gate, so this is a no-op on the enabled path.
        // Independent Forms runs drive their own carrier/effect lifecycle through the reflection
        // bridge; the Powers-namespace listeners (Mental Fortress / Rushdown) correctly stay silent
        // when the group is off.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        List<IOnStanceChanged> listeners = new();
        foreach (IOnStanceChanged listener in player.Creature.Powers.OfType<IOnStanceChanged>())
        {
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        if (player.PlayerCombatState != null)
        {
            foreach (CardPile pile in player.PlayerCombatState.AllPiles)
            {
                foreach (CardModel card in pile.Cards)
                {
                    if (card is IOnStanceChanged listener && !listeners.Contains(listener))
                    {
                        listeners.Add(listener);
                    }
                }
            }
        }

        foreach (IOnStanceChanged listener in listeners)
        {
            await listener.OnStanceChanged(ctx, from, to);
        }
    }
}
