using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Patches;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Flex (Common Skill). Gain 2 Strength; at the end of your turn, lose 2 Strength (4 upgraded).</summary>
public class Flex() : Spire1Card(0, CardType.Skill, CardRarity.Common, TargetType.None)
{
    private static readonly ModelId SharedLossId = new("POWER", "SPIRE1_EVENTS-MUTAGENIC_STRENGTH_LOSS_POWER");

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FlexPower>(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        PowerModel? loss = ModelDb.GetByIdOrNull<PowerModel>(SharedLossId);
        if (loss is null)
        {
            if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
                assembly.GetName().Name == "Spire1Events"))
                throw new InvalidOperationException("Spire1Events is loaded but its Strength-loss power is not registered.");
            loss = ModelDb.Power<FlexPower>();
        }
        else if (loss.GetType().FullName != "Spire1Events.Rewards.MutagenicStrengthLossPower"
            || loss.GetType().Assembly.GetName().Name != "Spire1Events"
            || loss.Type != PowerType.Debuff || loss.StackType != PowerStackType.Counter)
        {
            throw new InvalidOperationException("The registered Spire1Events Strength-loss power has an unexpected identity or contract.");
        }

        decimal amount = DynamicVars.Power<FlexPower>().BaseValue;
        if (Spire1FlexLease.Available)
        {
            await Spire1FlexLease.Strength(choiceContext, this, amount);
            await Spire1FlexLease.Loss(choiceContext, this, loss, amount);
        }
        else
        {
            // Preserve the pre-existing fallback behavior if the exact 0.113 call sites could
            // not be instrumented. In that case a mid-play powers toggle is not latched.
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
            await PowerCmd.Apply(choiceContext, loss.ToMutable(), Owner.Creature, amount, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Power<FlexPower>().UpgradeValueBy(2m);
}
