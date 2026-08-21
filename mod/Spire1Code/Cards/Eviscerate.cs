using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Silent — Eviscerate (Uncommon Attack). Deal 7 damage 3 times (9 upgraded).
/// Costs 1 less for each card discarded this turn.
/// </summary>
[Pool(typeof(SilentCardPool))]
public class Eviscerate() : Spire1Card(3, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play, hitCount: 3).Execute(choiceContext);

    /// <summary>
    /// Runs after any card is discarded. Every card in a combat pile is a hook listener, so each copy of this card
    /// sees every discard this turn and discounts itself by 1. AddThisTurn modifiers are wiped at end of turn by the
    /// game (same pattern as the game's Pinpoint/Stomp), which gives the "this turn" reset for free.
    /// </summary>
    public override Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (CombatState == null || card.Owner != Owner)
        {
            return Task.CompletedTask;
        }
        EnergyCost.AddThisTurn(-1, reduceOnly: true);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Fires when a card enters combat with no previous pile (fresh generation mid-combat). Catch this copy up on
    /// discards that happened earlier this turn by recounting the discard history (mirrors Pinpoint/Stomp).
    /// </summary>
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card != this || IsClone || CombatState == null)
        {
            return Task.CompletedTask;
        }
        int discarded = CombatManager.Instance.History.Entries
            .OfType<CardDiscardedEntry>()
            .Count(e => e.HappenedThisTurn(CombatState) && e.Card.Owner == Owner);
        if (discarded > 0)
        {
            EnergyCost.AddThisTurn(-discarded, reduceOnly: true);
        }
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
