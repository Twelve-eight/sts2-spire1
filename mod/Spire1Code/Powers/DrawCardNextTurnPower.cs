using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Powers;

/// <summary>All legacy producers stack here; setup captures its amount and later removes the current instance.</summary>
public sealed class DrawCardNextTurnPower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => ModelDb.Power<DrawCardsNextTurnPower>().PackedIconPath;
    public override string CustomBigIconPath => ModelDb.Power<DrawCardsNextTurnPower>().ResolvedBigIconPath;
    public override List<(string, string)>? Localization => new PowerLoc(
        "Draw Next Turn", "#At the start of your next turn, draw {Amount} additional {Amount:plural:card|cards}.",
        "At the start of your next turn, draw {Amount} additional {Amount:plural:card|cards}.");

    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        Spire1PostDrawBridge.RequireAvailable();
        return Task.CompletedTask;
    }
}
