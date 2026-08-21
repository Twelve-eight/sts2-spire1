using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Finisher (Uncommon Attack). Deal 6 damage for each Attack played this turn (8 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Finisher() : Spire1Card(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // One hit per Attack whose play has finished this turn (the game's own Finisher uses this exact query;
        // the current play is not yet in CardPlaysFinished, so with 0 attacks played the card deals no damage).
        int attacks = CombatManager.Instance.History.CardPlaysFinished
            .Count(e => e.HappenedThisTurn(CombatState) && e.CardPlay.Card.Type == CardType.Attack && e.CardPlay.Player == Owner);
        await CommonActions.CardAttack(this, play, hitCount: attacks).Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
