using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Feed (Rare Attack). Deal 10 damage; if this kills the enemy, permanently raise your Max HP by 3 (12 damage / 4 HP upgraded). Exhaust.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Feed() : Spire1Card(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10, ValueProp.Move),
        new MaxHpVar(3),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var target = play.Target ?? throw new ArgumentNullException(nameof(play.Target));

        // (2026-08-26 reverify fix) The predicate must match the engine's own Feed exactly:
        // All(p => p.ShouldOwnerDeathTriggerFatal()) — e.g. MinionPower overrides it to false
        // so minions are excluded from the max-HP reward. The previous !p.… inversion was the
        // same bug fixed in LessonLearned (3cfbcf1) but missed here.
        bool shouldTriggerFatal = target.Powers.All(p => p.ShouldOwnerDeathTriggerFatal());

        var attack = CommonActions.CardAttack(this, play);
        await attack.Execute(choiceContext);

        if (shouldTriggerFatal && attack.Results.SelectMany(hit => hit).Any(r => r.WasTargetKilled))
            await CreatureCmd.GainMaxHp(Owner.Creature, DynamicVars.MaxHp.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.MaxHp.UpgradeValueBy(1m);
    }
}
