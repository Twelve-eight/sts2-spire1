using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Silent - Choke. Whenever the player plays a card this turn, this creature loses HP equal to the power amount.
/// Applied to an enemy; expires at the end of the applying player's turn.
/// Uses the game's AfterimagePower timing pattern (BeforeCardPlayed records plays that started while the power was
/// already active) so the Choke card that applies this power does not trigger it on its own play.
/// </summary>
public class ChokePower : CustomPowerModel
{
    private class Data
    {
        public readonly Dictionary<CardPlay, decimal> PlaysInProgress = new();
        public Creature? Applier;
    }

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;
    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Choked",
            "#Whenever the player plays a card this turn, this creature loses {Amount} HP.",
            "Whenever the player plays a card this turn, this creature loses HP.");

    protected override object InitInternalData() => new Data();

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        Data data = GetInternalData<Data>();
        if (data.Applier != null && cardPlay.Card.Owner.Creature == data.Applier)
            data.PlaysInProgress[cardPlay] = Amount;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Data data = GetInternalData<Data>();
        if (data.Applier == null || cardPlay.Card.Owner.Creature != data.Applier)
        {
            return;
        }
        // No record means the play started before this power was applied (e.g. Choke itself) - ignore it.
        if (!data.PlaysInProgress.Remove(cardPlay, out decimal damage))
        {
            return;
        }
        if (!Owner.IsAlive)
        {
            return;
        }
        Flash();
        await CreatureCmd.Damage(choiceContext, Owner, damage, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (applier != null)
        {
            GetInternalData<Data>().Applier = applier;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        Data data = GetInternalData<Data>();
        if (data.Applier == null || !participants.Contains(data.Applier))
            return;
        await PowerCmd.Remove(this);
    }
}
