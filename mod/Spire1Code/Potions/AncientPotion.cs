using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Potions;

/// <summary>
/// StS1 - Ancient Potion (Uncommon). Gain 1 Artifact. Combat only.
/// Authority: <c>.tmp/sts1-jar-dump/potions/AncientPotion.txt</c> - the potion is UNCOMMON,
/// <c>getPotency</c> returns 1, and <c>use</c> applies exactly one <c>ArtifactPower</c> to the
/// player only while the room phase is COMBAT. No second effect, no target.
/// </summary>
public class AncientPotion : Spire1Potion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ArtifactPower>(1m)];

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ArtifactPower>()];

    // The shared Spire1Potion outline getter resolves the packed path (images/potions/<file>).
    // The potion outline layer lives in images/potions/outline/, so pass that sub-path explicitly.
    public override string? CustomPackedOutlinePath =>
        $"outline/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();

    // Localization lives in localization/{eng,zhs}/potions.json (JSON-only convention). No inline
    // PotionLoc here: it would overwrite the zhs table at ModelDb.Init.

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await PowerCmd.Apply<ArtifactPower>(
            choiceContext, Owner.Creature, DynamicVars.Power<ArtifactPower>().BaseValue, Owner.Creature, null);
    }
}
