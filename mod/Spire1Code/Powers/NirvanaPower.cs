using BaseLib.Abstracts;
using BaseLib.Hooks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher — Nirvana. Whenever you Scry, gain Amount Block.
/// Hooked through BaseLib's IAfterScryed, which only fires for scries that actually resolved (amount &gt; 0 and a
/// non-empty draw pile), matching StS1 where an empty Scry grants nothing.
/// </summary>
public class NirvanaPower : CustomPowerModel, IAfterScryed
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Nirvana",
            "#Whenever you Scry, gain {Amount} *Block*.",
            "Whenever you Scry, gain Block.");

    public async Task AfterScryed(
        PlayerChoiceContext ctx,
        Player player,
        int scryAmount,
        int discardAmount,
        List<CardModel> seen,
        List<CardModel> discarded)
    {
        if (player != Owner.Player || Amount <= 0)
            return;
        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }
}
