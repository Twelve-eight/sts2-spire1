using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 Ironclad — Bronze Scales (Common). Start each combat with 3 Thorns.</summary>
public class BronzeScales : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ThornsPower>(3m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Bronze Scales",
            "#Start each combat with !ThornsPower! Thorns.",
            "The sharp scales of the Guardian. Rearranges itself to protect its user.");

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
            return;
        Flash();
        await PowerCmd.Apply<ThornsPower>(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars.Power<ThornsPower>().BaseValue, Owner.Creature, null);
    }
}
