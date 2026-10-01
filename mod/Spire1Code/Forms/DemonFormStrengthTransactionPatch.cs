using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Forms;

/// <summary>
/// Attributes the real PowerModel.SetAmount delta of one DemonFormPower Strength grant to that grant.
///
/// PowerCmd.ModifyAmount only writes the amount after an awaited BeforePowerAmountChanged hook
/// (PowerCmd.cs:231, then :239-241), so reading power.Amount across that await cannot separate this
/// grant from another source that wrote in the same window. This adapter observes the exact
/// PowerModel._amount write boundary and records the stored delta even if a later event throws.
/// </summary>
internal static class DemonFormStrengthTransaction
{
    private sealed class Scope
    {
        public PowerModel Target = null!;
        public Action<decimal> Record = null!;
        public int Depth;
        public bool Armed;
        public bool Captured;
        public SetAmountState? ActiveSetAmount;
    }

    internal sealed class SetAmountState
    {
        internal int Before;
        internal int Stored;
        internal bool Wrote;
    }

    private static readonly AsyncLocal<Scope?> CurrentScope = new();

    /// <summary>
    /// Runs one PowerCmd.ModifyAmount call as a DemonForm grant transaction. Accepted deltas are
    /// delivered to <paramref name="record"/> from the real SetAmount write.
    /// </summary>
    internal static async Task ModifyAmountAsync(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal offset,
        Creature? applier,
        Action<decimal> record)
    {
        Scope scope = new()
        {
            Target = power,
            Record = record
        };
        Scope? previous = CurrentScope.Value;
        CurrentScope.Value = scope;
        try
        {
            await PowerCmd.ModifyAmount(choiceContext, power, offset, applier, null, silent: true);
        }
        finally
        {
            CurrentScope.Value = previous;
        }
    }

    /// <summary>
    /// PowerCmd.ModifyAmount prefix. Only invocations for this transaction's own instance take part,
    /// and the depth lets SetAmount reject writes made by commands nested inside the transaction.
    /// </summary>
    internal static bool EnterModifyAmount(PowerModel power)
    {
        Scope? scope = CurrentScope.Value;
        if (scope == null || !ReferenceEquals(scope.Target, power))
        {
            return false;
        }

        scope.Depth++;
        return true;
    }

    internal static Task<int> ExitModifyAmountOnCompletion(Task<int> result, bool entered)
    {
        if (!entered)
        {
            return result;
        }

        Scope? scope = CurrentScope.Value;
        return scope == null ? result : Unwind(result, scope);
    }

    private static async Task<int> Unwind(Task<int> result, Scope scope)
    {
        try
        {
            return await result;
        }
        finally
        {
            scope.Depth--;
        }
    }

    /// <summary>
    /// CombatHistory.PowerReceived postfix. In PowerCmd.ModifyAmount this call is the last statement
    /// before the amount is computed and written, so arming here excludes earlier awaited hooks.
    /// </summary>
    internal static void ArmAfterPowerReceived(PowerModel power)
    {
        Scope? scope = CurrentScope.Value;
        if (scope == null || scope.Armed || scope.Captured || scope.ActiveSetAmount != null
            || scope.Depth != 1 || !ReferenceEquals(scope.Target, power))
        {
            return;
        }

        scope.Armed = true;
    }

    /// <summary>
    /// PowerModel.SetAmount prefix. Consumes the arm on the first outermost write to this
    /// transaction's own instance and records the pre-write amount.
    /// </summary>
    internal static SetAmountState? BeginSetAmount(PowerModel power)
    {
        Scope? scope = CurrentScope.Value;
        if (scope == null || !scope.Armed || scope.Captured || scope.ActiveSetAmount != null
            || scope.Depth != 1 || !ReferenceEquals(scope.Target, power))
        {
            return null;
        }

        scope.Armed = false;
        var state = new SetAmountState { Before = power.Amount };
        scope.ActiveSetAmount = state;
        return state;
    }

    /// <summary>
    /// Called immediately after the engine's `_amount = amount` stfld and before its display and
    /// owner events. This is the only reliable write boundary: event subscribers may mutate the
    /// amount or throw after this point.
    /// </summary>
    internal static void CaptureStoredAmount(PowerModel power)
    {
        Scope? scope = CurrentScope.Value;
        SetAmountState? state = scope?.ActiveSetAmount;
        if (scope == null || state == null || scope.Captured || scope.Depth != 1
            || !ReferenceEquals(scope.Target, power))
        {
            return;
        }

        state.Stored = power.Amount;
        state.Wrote = true;
        // Prevent a PowerReceived raised by a post-write event from arming a second write for the
        // same transaction. The active state remains until the SetAmount finalizer records it.
        scope.Captured = true;
    }

    /// <summary>
    /// PowerModel.SetAmount finalizer. A write boundary crossed is enough to record the stored
    /// delta, even when DisplayAmountChanged or Owner.InvokePowerModified throws afterwards.
    /// </summary>
    internal static void RecordSetAmount(PowerModel power, SetAmountState state, Exception? exception)
    {
        Scope? scope = CurrentScope.Value;
        if (scope == null || !ReferenceEquals(scope.ActiveSetAmount, state)
            || !ReferenceEquals(scope.Target, power))
        {
            return;
        }

        scope.ActiveSetAmount = null;
        scope.Captured = true;
        if (!state.Wrote)
        {
            return;
        }

        decimal accepted = (decimal)state.Stored - state.Before;
        if (accepted != 0m)
        {
            scope.Record(accepted);
        }
    }
}

/// <summary>
/// Tracks how deep the active transaction is inside PowerCmd.ModifyAmount. Commands nested inside the
/// transaction must not be attributed to the outer grant, and the counter must unwind when the nested
/// task actually finishes, not when its async kickoff returns.
/// </summary>
[HarmonyPatch(typeof(PowerCmd), nameof(PowerCmd.ModifyAmount))]
internal static class DemonFormModifyAmountDepthPatch
{
    [HarmonyPrefix]
    private static void Prefix(PowerModel power, out bool __state)
    {
        __state = DemonFormStrengthTransaction.EnterModifyAmount(power);
    }

    [HarmonyPostfix]
    private static void Postfix(ref Task<int> __result, bool __state)
    {
        __result = DemonFormStrengthTransaction.ExitModifyAmountOnCompletion(__result, __state);
    }
}

/// <summary>
/// Arms the transaction at PowerCmd's last pre-write statement. The engine calls
/// CombatManager.History.PowerReceived immediately before it computes and stores the new amount.
/// </summary>
[HarmonyPatch(typeof(CombatHistory), nameof(CombatHistory.PowerReceived))]
internal static class DemonFormArmTransactionPatch
{
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(PowerModel power)
    {
        DemonFormStrengthTransaction.ArmAfterPowerReceived(power);
    }
}

/// <summary>
/// Observes the real write boundary. The transpiler callback runs after the amount field is written,
/// while the prefix and finalizer preserve the pre-write value and exception behavior.
/// </summary>
[HarmonyPatch(typeof(PowerModel), nameof(PowerModel.SetAmount))]
internal static class DemonFormSetAmountCapturePatch
{
    [HarmonyPrefix]
    private static void Prefix(PowerModel __instance, out object? __state)
    {
        __state = DemonFormStrengthTransaction.BeginSetAmount(__instance);
    }

    [HarmonyFinalizer]
    private static Exception? Finalizer(
        PowerModel __instance,
        object? __state,
        Exception? __exception)
    {
        if (__state is DemonFormStrengthTransaction.SetAmountState state)
        {
            DemonFormStrengthTransaction.RecordSetAmount(__instance, state, __exception);
        }

        return __exception;
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> code = instructions.ToList();
        FieldInfo amountField = AccessTools.Field(typeof(PowerModel), "_amount")
            ?? throw new InvalidOperationException("PowerModel._amount field was not found.");
        int matches = 0;
        for (int i = 0; i < code.Count; i++)
        {
            if (code[i].opcode != OpCodes.Stfld || !Equals(code[i].operand, amountField))
            {
                continue;
            }

            matches++;
            code.InsertRange(i + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(DemonFormStrengthTransaction),
                        nameof(DemonFormStrengthTransaction.CaptureStoredAmount))
                    ?? throw new InvalidOperationException("CaptureStoredAmount method was not found."))
            });
            i += 2;
        }

        if (matches != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one PowerModel._amount write in SetAmount, found {matches}.");
        }

        return code;
    }
}
