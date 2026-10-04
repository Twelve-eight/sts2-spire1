using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Extensions;
using EngineIroncladRelicPool = MegaCrit.Sts2.Core.Models.RelicPools.IroncladRelicPool;

namespace Spire1.Spire1Code.Character;

public class Spire1RelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Ironclad.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();

    /// <summary>
    /// C02 content gate (2026-10-02). RelicGrabBag.Populate and every relic reward/shop source go
    /// through <see cref="RelicPoolModel.GetUnlockedRelics"/>; the character's RelicPool getter is
    /// kept stable (ModelDb caches AllRelicPools at Preload and RelicModel.Pool resolves through
    /// that cache, so swapping the getter would strand old saves holding SPIRE1-* ids). When the
    /// relics group (or the per-run snapshot) is closed, this pool answers with the ENGINE
    /// Ironclad relic pool - original vanilla semantics, no SPIRE1-* relic can be rolled.
    /// </summary>
    public override IEnumerable<RelicModel> GetUnlockedRelics(UnlockState unlockState)
    {
        if (Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Relics))
        {
            return base.GetUnlockedRelics(unlockState);
        }
        return ModelDb.RelicPool<EngineIroncladRelicPool>().GetUnlockedRelics(unlockState);
    }
}