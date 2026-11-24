using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Potions;

/// <summary>
/// StS1 - Smoke Bomb (Rare). Escape from a non-boss combat; receive no rewards. Combat only, self.
/// Authority: <c>.tmp/sts1-jar-dump/potions/SmokeBomb.txt:48-49</c> sets <c>AbstractRoom.smoked</c>;
/// <c>:85-113</c> refuses use while any monster has <c>BackAttack</c> or the room is <c>BOSS</c>;
/// <c>:120-123</c> returns potency 0 (no numeric effect). This class is the model/UI surface only: the
/// actual rewardless escape transaction is <see cref="LegacyCombatEscape"/>, so the potion never calls the
/// normal victory path and never grants rewards.
/// </summary>
public class SmokeBomb : Spire1Potion
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    // The shared Spire1Potion outline getter resolves the packed path (images/potions/<file>).
    // The potion outline layer lives in images/potions/outline/, so pass that sub-path explicitly.
    public override string? CustomPackedOutlinePath =>
        $"outline/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();

    // Localization lives in localization/{eng,zhs}/potions.json (JSON-only convention). No inline
    // PotionLoc here: it would overwrite the zhs table at ModelDb.Init.

    // The real UsePotionAction admission patch also validates this before the wrapper. The UI check is
    // deliberately the same predicate: live non-boss combat, no back-attack enemy, no pending escape.
    public override bool PassesCustomUsabilityCheck =>
        LegacyCombatEscape.CanUseSmokeBomb(this, out _);

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        LegacyCombatEscape.BindSmokeBomb(this, target);
        return Task.CompletedTask;
    }
}
