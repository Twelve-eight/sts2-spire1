using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Worship (Uncommon Skill). Gain 5 Mantra. The upgrade only adds Retain; the amount is unchanged.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Worship() : Spire1Card(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MantraPower>(5)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await StanceCmd.GainMantra(choiceContext, Owner, DynamicVars.Power<MantraPower>().BaseValue, this);

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
