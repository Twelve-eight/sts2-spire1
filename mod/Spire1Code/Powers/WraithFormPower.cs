using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Collections.Generic;
using System.Linq;

namespace Spire1.Spire1Code.Powers;

/// <summary>StS1 Silent — Wraith Form. At the end of your turn, lose 1 Dexterity (per stack).</summary>
public class WraithFormPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Wraith Form",
            "#At the end of your turn, lose {Amount} *Dexterity*.",
            "At the end of your turn, lose Dexterity.");

    // StS1 applies the Dexterity loss at END of turn (unlike the game's own turn-start power), so the
    // turn-end hook signature is copied from the mod's CombustPower / the decompiled DoubleDamagePower.
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner))
            return;
        Flash();
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, -Amount, Owner, null);
    }
}
