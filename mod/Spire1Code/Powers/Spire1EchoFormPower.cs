using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Patches;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// EchoPower.onUseCard budgets against every play, including other replay sources' purge copies.
/// Only this power's own doubles are subtracted. Native first-in-series counting is not equivalent.
/// </summary>
public class Spire1EchoFormPower : Spire1Power
{
    private static AccessTools.FieldRef<CombatHistoryEntry, Dictionary<ulong, int>>? _turns;
    private static AccessTools.FieldRef<CombatHistoryEntry, int>? _round;
    private static AccessTools.FieldRef<CombatHistoryEntry, CombatSide>? _side;

    internal static void RequireHistoryLayout()
    {
        if (_turns != null && _round != null && _side != null)
            return;
        _turns = HistoryField<Dictionary<ulong, int>>("_playerTurnNumbers");
        _round = HistoryField<int>("<RoundNumber>k__BackingField");
        _side = HistoryField<CombatSide>("<CurrentSide>k__BackingField");
    }

    private static AccessTools.FieldRef<CombatHistoryEntry, T> HistoryField<T>(string name)
    {
        var field = typeof(CombatHistoryEntry).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null || field.DeclaringType != typeof(CombatHistoryEntry) || field.IsStatic || field.FieldType != typeof(T))
            throw new InvalidOperationException("Echo Form history field changed: " + name);
        return AccessTools.FieldRefAccess<CombatHistoryEntry, T>(field);
    }

    private sealed class Data
    {
        internal ICombatState? Combat;
        internal PlayerCombatState? PlayerState;
        internal CombatId? CombatId;
        internal int Round;
        internal CombatSide Side;
        internal int Turn;
        internal int Doubled;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override object InitInternalData() => new Data();

    public override string CustomPackedIconPath =>
        ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.EchoFormPower>().PackedIconPath;

    public override string CustomBigIconPath =>
        ModelDb.Power<MegaCrit.Sts2.Core.Models.Powers.EchoFormPower>().ResolvedBigIconPath;

    public override Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource)
    {
        Spire1EchoReplay.RequireInstalled();
        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        // IsDupe is the host's purge-on-use identity, not an autoplay test: Mayhem/Vakuu originals
        // remain eligible, whereas a replay from any dupe producer must not recurse into Echo.
        if (!ReferenceEquals(card.Owner.Creature, Owner) || card.IsDupe || Amount <= 0
            || !Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
            return playCount;

        Spire1EchoReplay.RequireInstalled();
        var combat = CombatState;
        var state = Owner.Player?.PlayerCombatState;
        if (combat == null || state == null || !CombatManager.Instance.IsCurrentLiveCombat(CombatManager.Instance.CurrentCombatId)
            || CombatManager.Instance.IsOverOrEnding || !ReferenceEquals(card.Owner.PlayerCombatState, state))
            return playCount;
        Data data = GetInternalData<Data>();
        int doubled = SameTurn(data, combat, state) ? data.Doubled : 0;
        int played = Spire1EchoReplay.UnpublishedStarts(Owner.Player!, combat, state);
        foreach (var entry in CombatManager.Instance.History.CardPlaysStarted)
        {
            // HappenedThisTurn checks *all* players. An ally's extra turn must not erase this
            // owner's starts or refresh their Echo budget. Read only this actor's stored turn.
            if (ReferenceEquals(entry.Actor, Owner) && _round!(entry) == combat.RoundNumber
                && _side!(entry) == combat.CurrentSide && _turns!(entry).TryGetValue(Owner.Player!.NetId, out int turn)
                && turn == state.TurnNumber)
                played++;
        }

        // The host asks before logging the original's start. StS1 asks after appending it.
        // Do not subtract other sources' replays, or count this original as a whole series.
        return played + 1 - doubled <= Amount ? checked(playCount + 1) : playCount;
    }

    internal void CommitDouble()
    {
        CurrentBudget().Doubled++;
        Flash();
    }

    private static bool SameTurn(Data data, ICombatState combat, PlayerCombatState state) =>
        ReferenceEquals(data.Combat, combat) && ReferenceEquals(data.PlayerState, state)
        && data.CombatId == CombatManager.Instance.CurrentCombatId && data.Round == combat.RoundNumber
        && data.Side == combat.CurrentSide && data.Turn == state.TurnNumber;

    private Data CurrentBudget()
    {
        Data data = GetInternalData<Data>();
        ICombatState combat = CombatState
            ?? throw new InvalidOperationException("Echo Form has no combat state.");
        var state = Owner.Player!.PlayerCombatState
            ?? throw new InvalidOperationException("Echo Form has no player combat state.");
        if (!SameTurn(data, combat, state))
        {
            data.Combat = combat;
            data.PlayerState = state;
            data.CombatId = CombatManager.Instance.CurrentCombatId;
            data.Round = combat.RoundNumber;
            data.Side = combat.CurrentSide;
            data.Turn = state.TurnNumber;
            data.Doubled = 0;
        }
        return data;
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner))
        {
            Data data = CurrentBudget();
            data.Doubled = 0;
        }
        return Task.CompletedTask;
    }
}
