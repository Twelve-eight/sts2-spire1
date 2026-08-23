using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 <c>com.megacrit.cardcrawl.powers.FadingPower</c> — Transient's countdown. 官方中文名：消逝。
/// <para>
/// Vanilla duringTurn fires at the START of the owner's turn: at 1 stack it detonates and dies
/// without acting; otherwise it decrements. A Fading 5 Transient therefore attacks four times
/// and dies as its fifth turn begins. The escape uses the engine's <see cref="CreatureCmd.Escape"/>
/// (same removal the mod's gremlins use) — no rewards, matching vanilla.
/// </para>
/// </summary>
public sealed class FadingPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Fading",
            "In {Amount} turns, this creature will vanish.",
            "This creature will vanish at the end of this turn.");

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Enemy || !participants.Contains(Owner))
        {
            return;
        }
        if ((int)Amount <= 1)
        {
            await PowerCmd.Remove(this);
            await CreatureCmd.Escape(Owner);
            return;
        }
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -1m, null, null);
    }
}
