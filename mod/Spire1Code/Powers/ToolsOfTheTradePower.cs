using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Silent - Tools of the Trade. At the start of your turn, draw 1 card and discard 1 card (per stack).</summary>
public class ToolsOfTheTradePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Tools of the Trade",
            "#At the start of your turn, draw {Amount} {Amount:plural:card|cards} and discard {Amount} {Amount:plural:card|cards}.",
            "At the start of your turn, draw {Amount} {Amount:plural:card|cards} and discard {Amount} {Amount:plural:card|cards}.");

    // The setup bridge owns the captured draw/discard batch, even if this source is removed.
    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        Spire1PostDrawBridge.RequireAvailable();
        return Task.CompletedTask;
    }
}
