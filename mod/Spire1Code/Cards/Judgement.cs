using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Judgment (Rare Skill). If the enemy has 30 or less HP (40 upgraded), set their HP to 0.
/// The kill goes through CreatureCmd.Kill (the game's normal death path, which still honours death-prevention and
/// on-death triggers); no HP field is written directly.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Judgement() : Spire1Card(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Threshold", 30)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var target = play.Target;
        if (target == null || target.IsDead)
            return;
        if (target.CurrentHp > DynamicVars["Threshold"].IntValue)
            return;
        await CreatureCmd.Kill(target);
    }

    protected override void OnUpgrade() => DynamicVars["Threshold"].UpgradeValueBy(10m);
}
