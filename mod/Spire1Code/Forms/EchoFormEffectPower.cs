using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Spire1.Spire1Code.Forms;

/// <summary>
/// "Echo form" half of the Echo-Celestial stance: the NEXT card the owner plays is played one extra
/// time, then this effect is consumed. Mechanism mirrors the shipped EchoFormPower.ModifyCardPlayCount
/// (research engine-dllsrc EchoFormPower.cs) which returns playCount + 1, but bounded to a single card
/// via a consumed flag rather than EchoForm's "first Amount plays this turn" history count.
/// </summary>
public sealed class EchoFormEffectPower : CustomPowerModel
{
    private sealed class Data
    {
        public bool consumed;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override bool IsVisibleInternal => false;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Echo Form",
            "#The next card you play is played an additional time.",
            "The next card is played an additional time.");

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (card.Owner.Creature != Owner)
        {
            return playCount;
        }

        if (GetInternalData<Data>().consumed || playCount <= 0 || playCount == int.MaxValue)
        {
            return playCount;
        }

        return playCount + 1;
    }

    // GeneratePlayCount runs before BeforeCardPlayed/OnPlay. The engine notifies only the
    // instances that actually modified this series; a form gained by its OnPlay is not included.
    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (card.Owner.Creature == Owner && !GetInternalData<Data>().consumed)
        {
            GetInternalData<Data>().consumed = true;
            Flash();
        }

        return Task.CompletedTask;
    }
}
