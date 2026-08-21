using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Wallop (Uncommon Attack). Deal 9 damage (12 upgraded) and gain Block equal to the unblocked
/// damage actually dealt. The real number comes off the executed attack command's DamageResults, the same structure
/// the mod's Sunder reads.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Wallop() : Spire1Card(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(9, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var attack = CommonActions.CardAttack(this, play);
        await attack.Execute(choiceContext);
        int unblocked = attack.Results.SelectMany(hit => hit).Sum(result => result.UnblockedDamage);
        if (unblocked > 0)
        {
            // Unpowered: StS1 grants this Block directly, so Dexterity does not scale it.
            await CreatureCmd.GainBlock(Owner.Creature, unblocked, ValueProp.Unpowered, play);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
