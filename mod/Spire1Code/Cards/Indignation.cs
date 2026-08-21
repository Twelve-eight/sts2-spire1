using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Indignation (Uncommon Skill). If you are in Wrath, apply 3 Vulnerable (5 upgraded) to ALL
/// enemies; otherwise enter Wrath.
/// The card has no target type (StS1 CardTarget.NONE), so the AoE apply is handed the hittable enemies directly
/// rather than going through card.GetTargets(), which returns nothing for TargetType.None.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Indignation() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!StanceCmd.IsIn<WrathPower>(Owner))
        {
            await StanceCmd.Enter<WrathPower>(choiceContext, Owner, this);
            return;
        }

        await CommonActions.Apply<VulnerablePower>(choiceContext, CombatState.HittableEnemies, this);
    }

    protected override void OnUpgrade() => DynamicVars.Vulnerable.UpgradeValueBy(2m);
}
