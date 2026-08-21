using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Wreath of Flame (Uncommon Skill). Your next Attack deals 5 additional damage (8 upgraded).
/// REUSE_SHIPPED_POWER: StS1's Wreath of Flame applies VigorPower and StS2 ships an identical one
/// (.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Powers/VigorPower.cs:58 adds its amount to the next powered attack via
/// ModifyDamageAdditive and clears itself in AfterAttack), so no mod power is defined for it.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class WreathOfFlame() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VigorPower>(5)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.ApplySelf<VigorPower>(choiceContext, this);

    protected override void OnUpgrade() => DynamicVars.Power<VigorPower>().UpgradeValueBy(3m);
}
