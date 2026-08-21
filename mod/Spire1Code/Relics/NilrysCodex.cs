using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Nilry's Codex (Event; one of the three mutually exclusive Cursed Tome rewards).
/// At the end of your turn, you may shuffle 1 of 3 random cards into your draw pile.
/// </summary>
public class NilrysCodex : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    /// <summary>StS1 <c>CodexAction.generateCardChoices()</c> loops until it holds 3 distinct cardIDs.</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    // StS1 DESCRIPTION verbatim, with the #b colour codes stripped (DEVLOG.md:287).
    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Nilry's Codex",
            "#At the end of your turn, you may shuffle 1 of 3 random cards into your draw pile.",
            "Crafted by the infamous game master himself. Said to expand one's mind.");

    // StS1 onPlayerEndTurn() queues a RelicAboveCreatureAction plus a CodexAction; CodexAction.update() does
    // the real work. BeforeSideTurnEnd (AbstractModel.cs:1388) is the StS2 end-of-turn hook, and the only
    // turn-end hook that still hands over a PlayerChoiceContext, which the selection screen requires.
    // Shipped relic precedents for this exact override: CloakClasp.cs:24, ScreamingFlagon.cs:21,
    // StoneCalendar.cs:86.
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // Scope to the owner only, as CloakClasp.cs:26 does with the same expression. Without this the relic
        // would also fire on the enemy side's turn end and, in multiplayer, on other players' turns.
        if (!participants.Contains(Owner.Creature))
            return;

        // StS1 CodexAction.update() aborts immediately on
        // AbstractDungeon.getMonsters().areMonstersBasicallyDead(). IsOverOrEnding is the StS2 spelling and
        // is the property the engine itself documents as the right one for "skip this effect, combat is not
        // running" (CombatManager.cs:218-222); CardCmd.cs:174 guards the same way.
        if (CombatManager.Instance.IsOverOrEnding)
            return;

        // CardFactory.GetDistinctForCombat (CardFactory.cs:119-129) is distinct by construction, which is
        // the equivalent of StS1's "loop returnTrulyRandomCardInCombat() until 3 distinct cardIDs are held".
        // Note this is the UNTYPED returnTrulyRandomCardInCombat, so unlike Enchiridion there is no
        // card-type filter here — the whole unlocked pool is eligible. Discovery.cs:27 is the shipped call
        // with the same pool expression, the same count and the same Rng.
        List<CardModel> choices = CardFactory.GetDistinctForCombat(
                Owner,
                Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint),
                DynamicVars.Cards.IntValue,
                Owner.RunState.Rng.CombatCardGeneration)
            .ToList();

        // FromChooseACardScreen logs a softlock report if handed an empty list (CardSelectCmd.cs:258-262),
        // so bail before tripping that. The upper bound needs no guard: it throws only above 3
        // (CardSelectCmd.cs:254-257) and CanonicalVars pins the count at exactly 3.
        if (choices.Count == 0)
            return;

        Flash();

        // StS1 opens cardRewardScreen.customCombatOpen(choices, CardRewardScreen.TEXT[1], true) — the
        // trailing `true` makes the pick cancellable — so canSkip: true is required to match, and the return
        // is nullable precisely because of it (CardSelectCmd.cs:252). Opening this from a turn-end hook is
        // supported: FromChooseACardScreen calls UndoEndTurnIfNecessary(player) at CardSelectCmd.cs:263.
        // The overload takes no prompt argument, so StS1's TEXT[1] needs no localization key.
        CardModel? chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, choices, Owner, canSkip: true);

        if (chosen == null)
            return;

        // CardPilePosition.Random, NOT Top. StS1 hands the copy to
        //     new ShowCardAndAddToDrawPileEffect(c, WIDTH / 2f, HEIGHT / 2f, true)
        // and that 4-argument constructor chains through the 5-argument one to the 6-argument one, passing
        // its boolean straight through: verified in desktop-1.0.jar bytecode, where the 6-arg ctor stores
        // `iload 4` into `putfield randomSpot:Z`, and CodexAction passes iconst_1 for it. So the card lands
        // at a random depth — a genuine shuffle-in. That also agrees with the official description ("you may
        // shuffle 1 of 3 random cards into your draw pile") and with relics.json's behavior field.
        // CardPilePosition.Random is a real member (CardPilePosition.cs:8) and resolves to a uniformly
        // random index via Rng.Shuffle.NextInt(pile.Cards.Count + 1) (CardPileCmd.cs:510).
        // PileType.Draw is the draw pile (PileType.cs:13).
        // AddGeneratedCardToCombat (CardPileCmd.cs:267) is mandatory for mid-combat generated cards. It is
        // also what makes this a temporary combat copy, matching StS1's makeStatEquivalentCopy(): the card
        // was created inside the combat state by GetDistinctForCombat (CardFactory.cs:128) and never enters
        // PileType.Deck, which is the pile that persists between rooms (PileType.cs:31-36).
        await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Draw, Owner, CardPilePosition.Random);
    }
}
