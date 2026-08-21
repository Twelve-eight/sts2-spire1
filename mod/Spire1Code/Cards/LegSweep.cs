using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Leg Sweep (Uncommon Skill). Apply 2 Weak and gain 11 Block (3 / 14 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class LegSweep() : Spire1Card(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WeakPower>(2), new BlockVar(11, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Apply<WeakPower>(choiceContext, play.Target!, this);
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Power<WeakPower>().UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}
