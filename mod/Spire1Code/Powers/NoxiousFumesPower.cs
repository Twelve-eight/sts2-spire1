using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Powers;

/// <summary>Amount is consumed once by the exact setup postdraw bridge, not by side-turn hooks.</summary>
public sealed class NoxiousFumesPower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override string CustomPackedIconPath => ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.NoxiousFumesPower>().PackedIconPath;
    public override string CustomBigIconPath => ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.NoxiousFumesPower>().ResolvedBigIconPath;
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PoisonPower>()];
    public override List<(string, string)>? Localization => new PowerLoc(
        "Noxious Fumes", "#At the start of your turn, apply {Amount} *Poison* to ALL enemies.",
        "At the start of your turn, apply {Amount} *Poison* to ALL enemies.");

    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        Spire1PostDrawBridge.RequireAvailable();
        return Task.CompletedTask;
    }
}
