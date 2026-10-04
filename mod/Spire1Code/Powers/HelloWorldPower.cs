using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using System.Linq;
using System.Threading;

using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

public class HelloWorldPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // r7 (2026-10-03): one-shot, thread-safe gate diagnostics. This hook runs every side turn, so an
    // unconditional Warn in the powers-off branch would flood the log and add main-thread cost for a
    // stale old-save instance. Interlocked.Exchange claims the latch atomically and logs at most once
    // per gate per session. The gates themselves are unchanged from r6.
    private static int _cardsGateLogged;
    private static int _powersGateLogged;

    private static void LogCardsGateOnce()
    {
        if (Interlocked.Exchange(ref _cardsGateLogged, 1) == 0)
            MainFile.Logger.Warn("[Spire1] Hello World card grant skipped: cards content group is off");
    }

    private static void LogPowersGateOnce()
    {
        if (Interlocked.Exchange(ref _powersGateLogged, 1) == 0)
            MainFile.Logger.Warn("[Spire1] Hello World card grant skipped: powers content group is off");
    }

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Hello World",
            "#At the start of your turn, add {Amount} random Common card(s) into your hand.",
            "#At the start of your turn, add random Common cards into your hand.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner) || Amount <= 0)
            return;
        // Powers content group gate (r6, 2026-10-03): this Spire1 power's own hook grants cards
        // directly, so the powers gate must be checked as well as the cards gate. A stale old-save
        // instance must not keep granting cards after the powers group is switched off. The instance
        // stays mounted so old saves keep their power state; return before Flash and before any card
        // is constructed or added.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            LogPowersGateOnce();
            return;
        }

        // Cards content group gate (C18 r2, 2026-10-03): this Spire1 power grants cards directly through AddGeneratedCardsToCombat; the gate runs before Flash so the off state is never signalled as a successful grant. The existing power stays mounted so old saves keep their power state.
        // Fail closed before any card is constructed or added; the card is not granted.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            LogCardsGateOnce();
            return;
        }

        Flash();
        var cards = CardFactory.GetDistinctForCombat(
            Owner.Player,
            Owner.Player.Character.CardPool
                .GetUnlockedCards(Owner.Player.UnlockState, Owner.Player.RunState.CardMultiplayerConstraint)
                .Where(c => c.Rarity == CardRarity.Common),
            Amount,
            Owner.Player.RunState.Rng.CombatCardGeneration);
        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, Owner.Player);
    }
}
