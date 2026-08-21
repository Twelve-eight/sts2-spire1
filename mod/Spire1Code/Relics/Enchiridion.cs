using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Enchiridion (Event; one of the three mutually exclusive Cursed Tome rewards).
/// At the start of each combat, add a random Power card into your hand. It costs 0 for that turn.
/// </summary>
public class Enchiridion : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    // StS1 Enchiridion.numbers is just the literal 0 it hands to setCostForTurn; there is no tunable
    // constant to bind, so there is nothing to expose here (as in GoldenIdol.cs:18, SpiritPoop.cs:18).
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    // StS1 DESCRIPTION verbatim, with the #y/#b colour codes stripped (DEVLOG.md:287).
    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Enchiridion",
            "#At the start of each combat, add a random Power card into your hand. It costs 0 for that turn.",
            "The legendary journal of an ancient lich.");

    // StS1 atPreBattle():
    //     flash();
    //     AbstractCard c = AbstractDungeon.returnTrulyRandomCardInCombat(CardType.POWER).makeCopy();
    //     if (c.cost != -1) c.setCostForTurn(0);
    //     UnlockTracker.markCardAsSeen(c.cardID);
    //     addToBot(new MakeTempCardInHandAction(c));
    // BeforeCombatStart (AbstractModel.cs:498) is the StS2 pre-battle hook, and it is safe to add generated
    // cards from it: CombatManager.cs:592 sets turnState.IsInProgress = true immediately before
    // CombatManager.cs:594 dispatches Hook.BeforeCombatStart, so the IsInProgress guard inside
    // AddGeneratedCardsToCombat (CardPileCmd.cs:288-291) passes rather than silently dropping the card.
    // Shipped template for the body is Crossbow.cs:17-37, which is the same "generate one card of a given
    // type, make it free this turn, put it in hand" effect; it differs only in firing per turn and
    // filtering to Attack.
    public override async Task BeforeCombatStart()
    {
        IReadOnlyList<CardModel> powers = Owner.Character.CardPool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Type == CardType.Power)
            .ToList();

        if (powers.Count == 0)
            return;

        // CardFactory.GetDistinctForCombat (CardFactory.cs:119-129) is the shipped entry point for
        // in-combat card generation, used by both Crossbow.cs:31 and Discovery.cs:27. It routes through
        // CardFactory.FilterForCombat (CardFactory.cs:159-162), which drops cards whose
        // CanBeGeneratedInCombat is false and drops the Basic, Ancient and Event rarities — the StS2
        // spelling of the same exclusions StS1 applies inside returnTrulyRandomCardInCombat. Asking for one
        // card is the direct analogue of StS1's single returnTrulyRandomCardInCombat call.
        // The deck-reward path (CardCreationOptions / CreateForReward) is deliberately NOT used: it rolls
        // rarity and upgrades (CardFactory.cs:89-109), which this effect must not do.
        List<CardModel> generated = CardFactory
            .GetDistinctForCombat(Owner, powers, 1, Owner.RunState.Rng.CombatCardGeneration)
            .ToList();

        if (generated.Count == 0)
            return;

        Flash();

        foreach (CardModel card in generated)
        {
            // SetToFreeThisTurn (CardModel.cs:1267-1271) is the correct member, NOT SetToFreeThisCombat
            // (CardModel.cs:1273-1277): StS1 calls setCostForTurn(0) — per TURN — and the official English
            // description says "It costs 0 for that turn." It resolves to
            // EnergyCost.SetThisTurnOrUntilPlayed(0), whose modifier is dropped by EndOfTurnCleanup
            // (CardEnergyCost.cs:331-335). Applying it here, before turn 1 exists, therefore keeps the card
            // free for the whole of turn 1 and expires at that turn's end — exactly vanilla.
            // StS1's explicit `if (c.cost != -1)` guard needs no counterpart: GetWithModifiers returns _base
            // early when CostsX (CardEnergyCost.cs:105-108), so the modifier is inert on X-cost Powers and
            // they keep their cost by construction, just as in StS1.
            card.SetToFreeThisTurn();
        }

        // Mid-combat generated cards must go through AddGeneratedCardsToCombat (CardPileCmd.cs:281) so the
        // combat history records them, exactly as Crossbow.cs:36 does. This is the StS2 stand-in for StS1's
        // MakeTempCardInHandAction, and it is also what keeps the card combat-only: it was created in the
        // combat state by GetDistinctForCombat (CardFactory.cs:128) and never enters PileType.Deck.
        await CardPileCmd.AddGeneratedCardsToCombat(generated, PileType.Hand, Owner);
    }
}
