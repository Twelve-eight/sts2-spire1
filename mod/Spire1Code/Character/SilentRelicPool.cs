using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using Spire1.Spire1Code.Config;
using EngineSilentRelicPool = MegaCrit.Sts2.Core.Models.RelicPools.SilentRelicPool;

namespace Spire1.Spire1Code.Character;

public class SilentRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Silent.Color;

    // Reuse StS2 Silent's energy icon instead of mod charui/*.png (which render gray).
    public override string EnergyColorName => "silent";

    /// <summary>
    /// C02 content gate (2026-10-02). RelicGrabBag.Populate and every relic reward/shop source go
    /// through <see cref="RelicPoolModel.GetUnlockedRelics"/>; the character's RelicPool getter is
    /// kept stable (ModelDb caches AllRelicPools at Preload and RelicModel.Pool resolves through
    /// that cache, so swapping the getter would strand old saves holding SPIRE1-* ids). When the
    /// relics group (or the per-run snapshot) is closed, this pool answers with the ENGINE
    /// Silent relic pool - original vanilla semantics, no SPIRE1-* relic can be rolled.
    /// </summary>
    public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState)
    {
        if (Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Relics))
        {
            return base.GetUnlockedRelics(unlockState);
        }
        return ModelDb.RelicPool<EngineSilentRelicPool>().GetUnlockedRelics(unlockState);
    }
}