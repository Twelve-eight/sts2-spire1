using Spire1.Spire1Code.Character;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Rupture (Uncommon Power). Whenever you lose HP from a card, gain 1 Strength (2 upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Rupture() : Spire1Card(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
{
    // Owned power, not the shipped MegaCrit.Sts2.Core.Models.Powers.RupturePower. The dynamic var
    // keeps the existing "RupturePower" key so SPIRE1-RUPTURE.description's !RupturePower! markup
    // resolves without a localization edit; ApplySelf therefore receives the explicit amount.
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<Spire1RupturePower>("RupturePower", 1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        await CommonActions.ApplySelf<Spire1RupturePower>(choiceContext, this, DynamicVars["RupturePower"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["RupturePower"].UpgradeValueBy(1m);
}
