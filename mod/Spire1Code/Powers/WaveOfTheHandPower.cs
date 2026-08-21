using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher — Wave of the Hand. Whenever the owner gains Block this turn, apply Weak (this power's amount) to
/// ALL enemies. Expires at the end of the owner's turn.
/// Block-gain hook verified at .tmp/dllsrc/MegaCrit.Sts2.Core.Models/AbstractModel.cs:330
/// (dispatched from MegaCrit.Sts2.Core.Commands/CreatureCmd.cs:699 via Hooks/Hook.cs:143); shipped user
/// MegaCrit.Sts2.Core.Models.Powers/JuggernautPower.cs:17.
/// </summary>
public class WaveOfTheHandPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Wave of the Hand",
            "#Whenever you gain Block this turn, apply {Amount} Weak to ALL enemies.",
            "Whenever you gain Block this turn, apply Weak to ALL enemies.");

    // AfterBlockGained carries no PlayerChoiceContext, so the mod's standard hook context is used
    // (same as BrutalityPower / PhantasmalKillerPower).
    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner || amount <= 0m || Amount <= 0)
            return;
        var enemies = CombatState.HittableEnemies;
        if (enemies.Count == 0)
            return;
        Flash();
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), enemies, Amount, Owner, null);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (participants.Contains(Owner))
            await PowerCmd.Remove(this);
    }
}
