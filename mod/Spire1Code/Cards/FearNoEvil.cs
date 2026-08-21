using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Fear No Evil (Uncommon Attack). Deal 8 damage (11 upgraded); if the enemy intends to Attack,
/// enter Calm. Intent is checked after the attack, matching the shipped GoForTheEyes, which implements the same
/// "if the enemy intends to attack" clause.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class FearNoEvil() : Spire1Card(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        if (play.Target?.Monster?.IntendsToAttack == true)
        {
            await StanceCmd.Enter<CalmPower>(choiceContext, Owner, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
