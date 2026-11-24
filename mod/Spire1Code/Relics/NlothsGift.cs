using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Runs;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 - N'loth's Gift (Special). Triple the effective rare-card chance in combat rewards.
/// The odds patch is deliberately kept separate from the relic model so the model keeps the
/// normal owner, obtain/remove, and save identity lifecycle.
/// </summary>
public sealed class NlothsGift : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /// <summary>
    /// The relic must not be offered when either the odds prefix or its CardFactory combat-reward
    /// context guard is unavailable. This keeps a failed Harmony installation fail-closed while
    /// preserving the canonical model for save loading.
    /// </summary>
    protected override bool IsAllowedInternal(IRunState runState) =>
        base.IsAllowedInternal(runState) && NlothsGiftOddsPatch.IsInstalled;
}
