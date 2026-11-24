using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Potions;

/// <summary>
/// StS1 - Essence of Steel (Uncommon). Gain 4 Plated Armor. Combat only.
/// Authority: <c>.tmp/sts1-jar-dump/potions/EssenceOfSteel.txt</c> - UNCOMMON,
/// <c>getPotency</c> returns 4, and <c>use</c> applies exactly four <c>PlatedArmorPower</c> to
/// the player only while the room phase is COMBAT. The stack mechanics live in the mod's own
/// StS1-faithful <see cref="PlatedArmorPower"/> (gain Block at end of turn, lose a stack on
/// unblocked damage), not in the shipped Plating power.
/// </summary>
public class EssenceOfSteel : Spire1Potion
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    // Essence of Steel's only effect is the mod's Plated Armor, whose block/stack mechanics are gated by
    // the Powers content group (Powers/PlatedArmorPower.cs). A potion obtained from the pool/reward path
    // must not be drinkable while that effect would be inert. The real UsePotionAction admission patch
    // repeats the same check before the wrapper, so this is not only a hand-UI gate.
    public override bool PassesCustomUsabilityCheck =>
        Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers);

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PlatedArmorPower>(4m)];

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PlatedArmorPower>()];

    // The shared Spire1Potion outline getter resolves the packed path (images/potions/<file>).
    // The potion outline layer lives in images/potions/outline/, so pass that sub-path explicitly.
    public override string? CustomPackedOutlinePath =>
        $"outline/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();

    // Localization lives in localization/{eng,zhs}/potions.json (JSON-only convention). No inline
    // PotionLoc here: it would overwrite the zhs table at ModelDb.Init.

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        await PowerCmd.Apply<PlatedArmorPower>(
            choiceContext, Owner.Creature, DynamicVars.Power<PlatedArmorPower>().BaseValue, Owner.Creature, null);
    }
}
