using MegaCrit.Sts2.Core.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.ShiftingPower</c> - Transient's "lose Strength on
/// damage" buffer. 官方中文名: 变化.
/// <para>
/// Bytecode: "每当 X 受到伤害, 它将在回合结束前失去相应点数的力量". The effect only matters
/// when the owner has Strength to lose; Transient never gains Strength in this port, so the
/// power is behaviourally inert (exactly like vanilla, where Transient's only Strength source
/// would be an external buff from another enemy - absent in the Beyond act).
/// </para>
/// </summary>
public sealed class ShiftingPower : CustomPowerModel
{
    // Accumulated Strength lost this turn, to be restored at the end of the turn.
    private int _pendingRestore;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Shifting",
            "Whenever this creature loses HP, it loses that much Strength until the end of the turn.",
            "Whenever this creature loses HP, it loses Strength until the end of the turn.");

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta >= 0 || !Owner.HasPower<StrengthPower>())
        {
            return;
        }
        // C12: the Strength loss is a new effect triggered by this Spire1 power; when the powers
        // group is off an existing instance stops applying losses (and therefore never needs a
        // restore). The paired restore below stays unconditional as the wind-down: a loss that
        // happened while the group was on must still be undone if the toggle flips mid-turn.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            return;
        }
        int lost = (int)-delta;
        _pendingRestore += lost;
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner, -lost, Owner, null);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        // participants = the side that just ended its turn. Without this gate (same idiom as
        // CombustPower/MetallicizePower), any other side's turn end would restore the Strength
        // early - in MP that means the Transient gets its Strength back before its own turn ends.
        if (!participants.Contains(Owner) || _pendingRestore <= 0)
        {
            return;
        }
        int restore = _pendingRestore;
        _pendingRestore = 0;
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, restore, Owner, null);
    }
}