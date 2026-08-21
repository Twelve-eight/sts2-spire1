using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher — Mental Fortress. Whenever you change Stances, gain Amount Block.
/// Fires on every stance transition StanceCmd dispatches (enter, swap and exit); StanceCmd.Enter is a no-op when
/// the requested stance is already active, so re-entering the same stance does not trigger this, as in StS1.
/// </summary>
public class MentalFortressPower : CustomPowerModel, IOnStanceChanged
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Mental Fortress",
            "#Whenever you change Stances, gain {Amount} *Block*.",
            "Whenever you change Stances, gain Block.");

    public async Task OnStanceChanged(PlayerChoiceContext ctx, StancePower? from, StancePower? to)
    {
        if (Amount <= 0)
            return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }
}
