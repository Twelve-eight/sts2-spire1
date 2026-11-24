using Spire1.Spire1Code.Character;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Rage (Uncommon). Whenever you play an Attack this turn, gain 3 Block (5 upgraded).</summary>
/// <remarks>
/// Applies the owned <see cref="Spire1RagePower"/>, not the shipped
/// <c>MegaCrit.Sts2.Core.Models.Powers.RagePower</c>: the shipped power pays every Attack after its
/// damage and reads the live amount, while the jar-sourced old queue pays an X-expansion
/// (Whirlwind) block before its expanded hits and freezes the amount at play start. The dynamic var
/// keeps the existing "RagePower" key so SPIRE1-RAGE.description's !RagePower! markup resolves
/// without a localization edit; ApplySelf therefore receives the explicit amount.
/// </remarks>
[Pool(typeof(Spire1LegacyPool))]
public class Rage() : Spire1Card(0, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<Spire1RagePower>("RagePower", 3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        await CommonActions.ApplySelf<Spire1RagePower>(choiceContext, this, DynamicVars["RagePower"].BaseValue);
    }

    protected override void OnUpgrade() => DynamicVars["RagePower"].UpgradeValueBy(2m);
}
