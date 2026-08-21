using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Potions;

/// <summary>StS1 Ironclad — Explosive Potion (Common). Deal 10 damage to ALL enemies.</summary>
public class ExplosivePotion : Spire1Potion
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AllEnemies;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Unpowered)];

    public override List<(string, string)>? Localization =>
        new PotionLoc("Explosive Potion", "#Deal !D! damage to ALL enemies.");

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        DamageVar damage = DynamicVars.Damage;
        await CreatureCmd.Damage(choiceContext, Owner.Creature.CombatState.HittableEnemies, damage.BaseValue,
            damage.Props, Owner.Creature, null, null);
    }
}
