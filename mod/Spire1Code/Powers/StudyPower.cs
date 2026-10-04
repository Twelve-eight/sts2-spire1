using System.Threading;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher - Study. At the end of the owner's turn, shuffle Insight cards (one per stack) into the draw pile.
/// </summary>
public class StudyPower : CustomPowerModel
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
            MainFile.Logger.Warn("[Spire1] Study card grant skipped: cards content group is off");
    }

    private static void LogPowersGateOnce()
    {
        if (Interlocked.Exchange(ref _powersGateLogged, 1) == 0)
            MainFile.Logger.Warn("[Spire1] Study card grant skipped: powers content group is off");
    }

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Study",
            "#At the end of your turn, shuffle {Amount} Insight into your draw pile.",
            "At the end of your turn, shuffle an Insight into your draw pile.");

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Amount <= 0 || !participants.Contains(Owner))
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

        // Cards content group gate (C13, 2026-10-02): Insight is a Spire1 token created directly by
        // AddToCombatAndPreview, bypassing the pool filters that carry the cards gate. Fail closed before
        // the flash and before any card is constructed; the existing power instance is left mounted so
        // old saves keep their power state instead of gaining a half-removed hook.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            LogCardsGateOnce();
            return;
        }

        Flash();
        await CardPileCmd.AddToCombatAndPreview<Insight>(
            Owner,
            PileType.Draw,
            Amount,
            Owner.Player,
            CardPilePosition.Random);
    }
}
