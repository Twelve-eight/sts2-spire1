using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Reboot: notify shuffle listeners before moving cards, shuffle the discard pile,
/// move discard and randomly selected hand cards onto the draw pile, shuffle it, then draw 4 (6).
/// ShuffleAllAction's Soul.shuffle transfer really adds discard cards to the draw pile;
/// it is not just VFX. The two shuffles each consume a Shuffle RNG seed, even for an empty pile.
/// </summary>
[Pool(typeof(Spire1LegacyPool))]
public class Reboot() : Spire1Card(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    public override bool CanBeGeneratedInCombat => Spire1RebootTransaction.Available && base.CanBeGeneratedInCombat;
    public override bool CanBeGeneratedByModifiers => Spire1RebootTransaction.Available && base.CanBeGeneratedByModifiers;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(4)];

    public override bool ShouldPlay(MegaCrit.Sts2.Core.Models.CardModel card, AutoPlayType autoPlayType)
        => (card.GetType() != typeof(Reboot) || Spire1RebootTransaction.Available) && base.ShouldPlay(card, autoPlayType);

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Missing capability is a failure, not a successful no-op (including direct/nopile invocation).
        Spire1RebootTransaction.RequireAvailable();
        if (CombatManager.Instance.IsOverOrEnding)
            return;
        using var transaction = Spire1RebootTransaction.Begin(this, play);
        try
        {
            if (!transaction.CanContinue())
                return;
            var player = Owner;
            var combat = player.Creature.CombatState!;
            var pcs = player.PlayerCombatState!;
            var run = player.RunState;

            // ShuffleAllAction's constructor invokes relic onShuffle before constructing DrawCardAction.
            await Hook.AfterShuffle(combat, choiceContext, player);
            if (!transaction.CanContinue())
                return;
            var drawCount = DynamicVars.Cards.BaseValue;

            // Real first seed, even for an empty discard. Initial-shuffle modifiers are not applicable.
            var discard = pcs.DiscardPile;
            var discarded = discard.Cards.ToList();
            discarded.UnstableShuffle(run.Rng.Shuffle);
            await CardPileCmd.Add(discarded, discard, CardPilePosition.Bottom, null, skipVisuals: true);
            if (!transaction.CanContinue())
                return;

            // Soul.shuffle removes discard members and puts each on TOP before PutOnDeckAction runs.
            await CardPileCmd.Add(discarded, pcs.DrawPile, CardPilePosition.Top, null, skipVisuals: true);
            if (!transaction.CanContinue())
                return;

            // PutOnDeckAction(99,true): one current-hand random selection per move, including the last.
            int handMoves = Math.Min(99, pcs.Hand.Cards.Count);
            for (int i = 0; i < handMoves && pcs.Hand.Cards.Count > 0; i++)
            {
                if (!transaction.CanContinue())
                    return;
                var card = pcs.Hand.Cards[run.Rng.CombatCardSelection.NextInt(pcs.Hand.Cards.Count)];
                await CardPileCmd.Add(card, pcs.DrawPile, CardPilePosition.Top);
            }
            if (!transaction.CanContinue())
                return;

            // ShuffleAction(drawPile,false): second real seed, no second shuffle notification.
            var cards = pcs.DrawPile.Cards.ToList();
            cards.UnstableShuffle(run.Rng.Shuffle);
            Hook.ModifyShuffleOrder(combat, player, cards, isInitialShuffle: false);
            if (!transaction.CanContinue())
                return;
            await CardPileCmd.Add(cards, pcs.DrawPile, CardPilePosition.Bottom, null, skipVisuals: true);
            if (!transaction.CanContinue())
                return;

            await CardPileCmd.Draw(choiceContext, drawCount, player);
            if (transaction.CanContinue())
                await transaction.Drain();
        }
        catch (Spire1RebootTransaction.Stopped stopped) when (transaction.Owns(stopped))
        {
            // Only this play's normal stop. Original task faults and captured CTS cancellation propagate.
        }
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(2m);
}
