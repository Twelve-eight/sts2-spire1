using Spire1.Spire1Code.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

using BaseLib.Utils;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Ironclad — Burn (Status). Unplayable. At the end of your turn, take 2 damage.
/// Mirror of the base-game Burn (which deals via HasTurnEndInHandEffect + OnTurnEndInHand).
/// Burn+ (4 damage) not implemented: base-game Burn has MaxUpgradeLevel 0 and statuses never upgrade.
/// </summary>
[Pool(typeof(Spire1LegacyPool))]
public class Burn() : Spire1Card(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(2m, ValueProp.Unpowered | ValueProp.Move)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        SfxCmd.Play("event:/sfx/characters/attack_fire");
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.Damage, this, null);
    }
}
