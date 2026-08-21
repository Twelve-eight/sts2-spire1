using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Brilliance (Rare Attack). Deal 12 damage (16 upgraded) plus the total Mantra gained this combat.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Brilliance() : Spire1Card(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    // Calculated damage so the printed number already includes the Mantra bonus. The multiplier lambda MUST stay
    // static (BaseLib rejects instance-field capture on reward clones), so the total is recomputed from the combat
    // history on every evaluation instead of being cached on the card.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        ..CustomCardModel.MakeCalculatedDamage(12, static (card, target) => MantraGainedThisCombat(card))
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play).Execute(choiceContext);

    protected override void OnUpgrade() => DynamicVars.CalculationBase.UpgradeValueBy(4m);

    /// <summary>
    /// Total Mantra GAINED by this card's owner in the current combat.
    /// MantraPower is a plain counter that the stance infrastructure reduces by 10 whenever it converts to Divinity,
    /// so its live Amount is not the running total. PowerCmd.Apply and PowerCmd.ModifyAmount both log every change
    /// as a PowerReceivedEntry, so summing only the POSITIVE entries for MantraPower on this creature yields the
    /// gained total and ignores the -10 conversions. No new counter is added to the shared stance infrastructure.
    /// </summary>
    private static decimal MantraGainedThisCombat(CardModel card)
    {
        if (card.CombatState == null)
            return 0m;
        decimal total = 0m;
        foreach (var entry in CombatManager.Instance.History.Entries)
        {
            if (entry is PowerReceivedEntry received
                && received.Actor == card.Owner.Creature
                && received.Amount > 0m
                && received.Power is MantraPower)
            {
                total += received.Amount;
            }
        }
        return total;
    }
}
