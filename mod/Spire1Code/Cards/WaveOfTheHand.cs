using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Wave of the Hand (Uncommon Skill). For the rest of this turn, every time you gain Block you apply
/// 1 Weak (2 upgraded) to ALL enemies.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class WaveOfTheHand() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WaveOfTheHandPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<WaveOfTheHandPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<WaveOfTheHandPower>().UpgradeValueBy(1m);
}
