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
/// StS1 Watcher — Vigilance (Basic). Gain 8 Block (12 upgraded) and enter Calm.
///
/// The stance half is real, not approximated: the mod ships its own stance subsystem
/// (Powers/StancePower.cs + CalmPower/WrathPower/DivinityPower, driven by Extensions/StanceCmd.cs),
/// which is what every other Watcher card in the pool uses (see Blasphemy.cs).
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Vigilance() : Spire1Card(2, CardType.Skill, CardRarity.Basic, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(8, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        await StanceCmd.Enter<CalmPower>(choiceContext, Owner, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m); // vanilla Vigilance+ grants 12
}
