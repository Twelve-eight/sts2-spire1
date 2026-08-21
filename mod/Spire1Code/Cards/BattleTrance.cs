using Spire1.Spire1Code.Character;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Battle Trance (Uncommon Skill). Draw 3 cards, cannot draw more this turn (4 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class BattleTrance() : Spire1Card(0, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new CardsVar(3), new PowerVar<NoDrawPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.Draw(this, choiceContext);
        await CommonActions.ApplySelf<NoDrawPower>(choiceContext, this);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
