using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Unlocks;
using Spire1.Spire1Code.Config;
using EngineSilentPotionPool = MegaCrit.Sts2.Core.Models.PotionPools.SilentPotionPool;

namespace Spire1.Spire1Code.Character;

public class SilentPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Silent.Color;

    // Reuse StS2 Silent's energy icon instead of mod charui/*.png (which render gray).
    public override string EnergyColorName => "silent";

    /// <summary>
    /// C02 content gate (2026-10-02). PotionFactory and every potion reward/shop source go through
    /// <see cref="PotionPoolModel.GetUnlockedPotions"/>; the character's PotionPool getter is kept
    /// stable (ModelDb caches AllPotionPools at Preload and PotionModel.Pool resolves through that
    /// cache, so swapping the getter would strand old saves holding SPIRE1-* ids). When the potions
    /// group (or the per-run snapshot) is closed, this pool answers with the ENGINE Silent potion
    /// pool - original vanilla semantics, no SPIRE1-* potion can be rolled.
    /// </summary>
    public override IEnumerable<PotionModel> GetUnlockedPotions(UnlockState unlockState)
    {
        if (Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Potions))
        {
            return base.GetUnlockedPotions(unlockState);
        }
        return ModelDb.PotionPool<EngineSilentPotionPool>().GetUnlockedPotions(unlockState);
    }
}