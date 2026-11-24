using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Sever Soul: the non-Attack specific exhaust actions resolve in reverse hand order,
/// skipping cards no longer in that hand. Its one DamageAction is queued even for an empty batch;
/// only captured FNP/DE bottom-of-queue payments resolve after it.
/// </summary>
public class SeverSoul() : Spire1Card(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(16, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        using var transaction = new Spire1ExhaustTransaction(this, play, choiceContext);
        var targets = transaction.Hand.Cards.Where(c => c.Type != CardType.Attack).ToArray();
        for (int i = targets.Length - 1; i >= 0; i--)
        {
            if (!transaction.CanExhaust()) break;
            await transaction.Exhaust(targets[i]);
        }

        await transaction.Attack(1);
        await transaction.Complete();
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6m);
}
