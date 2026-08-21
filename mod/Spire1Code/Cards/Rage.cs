using Spire1.Spire1Code.Character;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Rage (Uncommon). Whenever you play an Attack this turn, gain 3 Block (5 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Rage() : Spire1Card(0, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<RagePower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<RagePower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<RagePower>().UpgradeValueBy(2m);
}
