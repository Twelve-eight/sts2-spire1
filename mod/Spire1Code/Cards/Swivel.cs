using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Swivel (Uncommon Skill). Gain 8 Block (11 upgraded); the next Attack you play costs 0.
/// REUSE_SHIPPED_POWER: StS1's Swivel applies FreeAttackPower and StS2 ships an identical one
/// (.tmp/dllsrc/MegaCrit.Sts2.Core.Models.Powers/FreeAttackPower.cs:14 zeroes the cost of the next Attack in
/// Hand/Play via TryModifyEnergyCostInCombatLate, then decrements itself in BeforeCardPlayed), so no mod power
/// is defined for it.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Swivel() : Spire1Card(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(8, ValueProp.Move), new PowerVar<FreeAttackPower>(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        await CommonActions.ApplySelf<FreeAttackPower>(choiceContext, this);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
