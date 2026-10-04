using System.Threading;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher - Collect. At the start of each of your next Amount turns, put an upgraded Miracle into your
/// hand and tick one turn off the counter. The counter is set by Collect to its X value (+1 when upgraded).
/// </summary>
public class CollectPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private static int _cardsGateLogged;
    private static int _powersGateLogged;

    // r7 (2026-10-03): one-shot, thread-safe gate diagnostics. Interlocked.Exchange claims the
    // latch atomically, so concurrent hook entries log at most once per gate instead of racing
    // a plain-bool check-then-set. The gates themselves are unchanged from r6.
    private static void LogCardsGateOnce()
    {
        if (Interlocked.Exchange(ref _cardsGateLogged, 1) == 0)
            MainFile.Logger.Warn("[Spire1] Collect card grant skipped: cards content group is off");
    }

    private static void LogPowersGateOnce()
    {
        if (Interlocked.Exchange(ref _powersGateLogged, 1) == 0)
            MainFile.Logger.Warn("[Spire1] Collect card grant skipped: powers content group is off");
    }

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Collect",
            "#At the start of your next {Amount} turns, put a *Miracle+* into your hand.",
            "At the start of your turn, put an upgraded Miracle into your hand.");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        // Powers content group gate (r6, 2026-10-03): this Spire1 power's own hook grants a token
        // directly, so the powers gate must be checked as well as the cards gate. A stale old-save
        // instance must not keep granting cards after the powers group is switched off. Mirror the
        // cards-off branch below: keep ticking so the instance expires on its original schedule
        // instead of being frozen as a permanently mounted, effect-less power; return before the
        // card is constructed and before Flash.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            LogPowersGateOnce();
            await PowerCmd.Decrement(this);
            return;
        }

        // Cards content group gate (C13, 2026-10-02): Miracle is a Spire1 token created directly by
        // CombatState.CreateCard, bypassing the pool filters that carry the cards gate. Fail closed before
        // the card is constructed, before the flash and before the upgrade. The counter still ticks, which
        // is deliberate: CardsEnabled is ANDed with the per-run snapshot (Spire1RunContent), so the group
        // cannot come back mid-run, and letting the power expire on its original schedule avoids leaving a
        // permanently mounted, effect-less power on the buff bar. Existing instances therefore finish their
        // normal lifecycle instead of being frozen by the gate.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            LogCardsGateOnce();
            await PowerCmd.Decrement(this);
            return;
        }

        CardModel? miracle = Owner.CombatState?.CreateCard<Miracle>(player);
        if (miracle == null)
            return;
        Flash();
        CardCmd.Upgrade(miracle);
        await CardPileCmd.AddGeneratedCardToCombat(miracle, PileType.Hand, player);
        await PowerCmd.Decrement(this);
    }
}
