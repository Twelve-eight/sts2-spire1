using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Potions;

/// <summary>
/// StS1 - Cultist Potion (Rare). Gain 1 Ritual. Combat only.
/// Authority: <c>.tmp/sts1-jar-dump/potions/CultistPotion.txt</c> - RARE,
/// <c>getPotency</c> returns 1, and <c>use</c> applies <c>RitualPower(player, 1, true)</c>
/// while the room phase is COMBAT. The <c>true</c> flag marks the power as player-owned, which in
/// StS1 makes it gain Strength at the end of the player's turn; the host
/// <see cref="RitualPower"/> already does exactly that for a player owner (its
/// enemy-first-turn skip only applies to <c>Owner.IsEnemy</c>). The presentation uses the host's own
/// Calcified Cultist INCANTATION sound and banter line, the same legitimate host carrier already used by
/// <c>Relics/CultistHeadpiece.cs:30-38,76-81</c>; StS1's own voice asset is not shipped and is not
/// synthesized. The presentation is cosmetic and runs before the Ritual application.
/// </summary>
public class CultistPotion : Spire1Potion
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.Self;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<RitualPower>(1m)];

    public override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<RitualPower>()];

    // The shared Spire1Potion outline getter resolves the packed path (images/potions/<file>).
    // The potion outline layer lives in images/potions/outline/, so pass that sub-path explicitly.
    public override string? CustomPackedOutlinePath =>
        $"outline/{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();

    // Localization lives in localization/{eng,zhs}/potions.json (JSON-only convention). No inline
    // PotionLoc here: it would overwrite the zhs table at ModelDb.Init.

    // Host presentation boundary copied from Relics/CultistHeadpiece.cs:30-38: the shipped Calcified
    // Cultist INCANTATION sound and the shipped "monsters" banter key. Neither is an StS1 asset.
    private const string CultistBuffSfx = "event:/sfx/enemy/enemy_attacks/cultists/cultists_buff_calcified";

    private static readonly LocString CawCawBanter =
        new LocString("monsters", "CALCIFIED_CULTIST.moves.INCANTATION.banter");

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        SfxCmd.Play(CultistBuffSfx);
        TalkCmd.Play(CawCawBanter, Owner.Creature, VfxColor.Purple, VfxDuration.Standard);
        await PowerCmd.Apply<RitualPower>(
            choiceContext, Owner.Creature, DynamicVars.Power<RitualPower>().BaseValue, Owner.Creature, null);
    }
}
