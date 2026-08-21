using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Lesson Learned (Rare Attack). Deal 10 damage (13 upgraded); if Fatal, permanently Upgrade a random
/// card in your deck. Exhaust.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class LessonLearned() : Spire1Card(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var target = play.Target ?? throw new ArgumentNullException(nameof(play));

        // Same guard the mod's Feed uses: a target whose power wants to run a death trigger has not really died yet.
        bool shouldTriggerFatal = target.Powers.All(p => !p.ShouldOwnerDeathTriggerFatal());

        var attack = CommonActions.CardAttack(this, play);
        await attack.Execute(choiceContext);

        if (!shouldTriggerFatal || !attack.Results.SelectMany(hit => hit).Any(r => r.WasTargetKilled))
            return;

        UpgradeRandomDeckCard();
    }

    /// <summary>
    /// Upgrades one random upgradable card in the run deck through CardCmd.Upgrade, which is the game's deck-upgrade
    /// command (it also records the upgrade in the run's map-point history and plays the upgrade VFX). No deck field
    /// is mutated directly.
    /// Combat piles hold clones of the deck cards (Player.StartCombat clones each deck card and points the clone's
    /// DeckVersion at it), so the matching in-combat copies are upgraded too. In StS1 the deck and the combat piles
    /// share one card object, so upgrading mid-combat is immediately visible for the rest of the fight.
    /// </summary>
    private void UpgradeRandomDeckCard()
    {
        var deckCards = PileType.Deck.GetPile(Owner).Cards.Where(c => c.IsUpgradable).ToList();
        if (deckCards.Count == 0)
            return;
        CardModel? pick = Owner.RunState.Rng.CombatCardSelection.NextItem(deckCards);
        if (pick == null)
            return;
        CardCmd.Upgrade(pick);

        var combatState = Owner.PlayerCombatState;
        if (combatState == null)
            return;
        foreach (CardModel copy in combatState.AllCards.ToList())
        {
            if (copy.DeckVersion == pick && copy.IsUpgradable)
                CardCmd.Upgrade(copy, CardPreviewStyle.None);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
