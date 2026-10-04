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
/// StS1 Watcher - Battle Hymn. At the start of each turn, add a Smite into your hand (one per stack).
/// Uses AfterPlayerTurnStart (not AfterSideTurnStart) because adding cards to hand can hand off to a
/// player-choice-driven pile add, and AfterPlayerTurnStart is the hook that carries a PlayerChoiceContext.
/// </summary>
public class BattleHymnPower : CustomPowerModel
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
            MainFile.Logger.Warn("[Spire1] Battle Hymn card grant skipped: cards content group is off");
    }

    private static void LogPowersGateOnce()
    {
        if (Interlocked.Exchange(ref _powersGateLogged, 1) == 0)
            MainFile.Logger.Warn("[Spire1] Battle Hymn card grant skipped: powers content group is off");
    }

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Battle Hymn",
            "#At the start of each turn, add {Amount} *Smite* into your hand.",
            "At the start of each turn, add a Smite into your hand.");

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        // Powers content group gate (r6, 2026-10-03): this Spire1 power's own hook grants a token
        // directly, so the powers gate must be checked as well as the cards gate. A stale old-save
        // instance must not keep granting cards after the powers group is switched off. The instance
        // stays mounted so old saves keep their power state; return before Flash and before any card
        // is constructed or added.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            LogPowersGateOnce();
            return;
        }

        // Cards content group gate (C13, 2026-10-02): Smite is a Spire1 token created directly by
        // AddToCombatAndPreview, bypassing the pool filters that carry the cards gate. Fail closed before
        // the flash and before any card is constructed; the existing power instance is left mounted so
        // old saves keep their power state instead of gaining a half-removed hook.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            LogCardsGateOnce();
            return;
        }

        Flash();
        await CardPileCmd.AddToCombatAndPreview<Smite>(Owner, PileType.Hand, Amount, player);
    }
}
