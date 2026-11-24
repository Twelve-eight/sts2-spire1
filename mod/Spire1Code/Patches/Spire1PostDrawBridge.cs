using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Config;
using LegacyDraw = Spire1.Spire1Code.Powers.DrawCardNextTurnPower;
using LegacyFumes = Spire1.Spire1Code.Powers.NoxiousFumesPower;
using LegacyTools = Spire1.Spire1Code.Powers.ToolsOfTheTradePower;

namespace Spire1.Spire1Code.Patches;

/// <summary>Only the three legacy postdraw consumers; never a replacement for the engine hook dispatcher.</summary>
internal static class Spire1PostDrawBridge
{
    private const string OwnerId = "Spire1.PostDraw";
    private const BindingFlags Methods = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly object InstallationLock = new();
    private static readonly AsyncLocal<Lease?> Current = new();
    private static readonly ConditionalWeakTable<CombatTurnState, ConditionalWeakTable<PlayerCombatState, Claim>> Claims = new();
    private static long _nextLease;
    private static bool _available;
    private static bool _failed;
    private static readonly MethodInfo DrawMethod = typeof(CardPileCmd).GetMethod(nameof(CardPileCmd.Draw),
        new[] { typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool) })!;
    private static readonly MethodInfo HookMethod = typeof(Hook).GetMethod(nameof(Hook.AfterPlayerTurnStart))!;
    private static Func<CombatManager, CombatTurnState, bool>? _isCombatEnding;

    internal static bool EnsureInstalled()
    {
        lock (InstallationLock)
        {
            if (_failed)
                return false;
            var harmony = new Harmony(OwnerId);
            try
            {
                MethodInfo kickoff = typeof(CombatManager).GetMethod("SetupPlayerTurn", Methods, null,
                    new[] { typeof(CombatTurnState), typeof(Player), typeof(HookPlayerChoiceContext) }, null)
                    ?? throw new InvalidOperationException("SetupPlayerTurn signature is unavailable");
                if (kickoff.ReturnType != typeof(Task) || kickoff.IsStatic)
                    throw new InvalidOperationException("SetupPlayerTurn contract changed");
                Type state = kickoff.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType
                    ?? throw new InvalidOperationException("SetupPlayerTurn has no async state machine");
                if (state.DeclaringType != typeof(CombatManager))
                    throw new InvalidOperationException("SetupPlayerTurn state machine owner changed");
                MethodInfo move = state.GetMethod("MoveNext", Methods)
                    ?? throw new InvalidOperationException("SetupPlayerTurn.MoveNext is unavailable");
                MethodInfo ending = typeof(CombatManager).GetMethod("IsCombatEnding", Methods, null,
                    new[] { typeof(CombatTurnState) }, null)
                    ?? throw new InvalidOperationException("Turn-relative combat ending predicate is unavailable");
                if (ending.IsStatic || ending.ReturnType != typeof(bool))
                    throw new InvalidOperationException("Turn-relative combat ending contract changed");
                _isCombatEnding ??= ending.CreateDelegate<Func<CombatManager, CombatTurnState, bool>>();
                if (!_available)
                {
                    ValidateSites(PatchProcessor.GetOriginalInstructions(move).ToList());
                    harmony.Patch(move, transpiler: new HarmonyMethod(Own(nameof(Transpile))));
                    harmony.Patch(kickoff, prefix: new HarmonyMethod(Own(nameof(Begin))),
                        postfix: new HarmonyMethod(Own(nameof(End))), finalizer: new HarmonyMethod(Own(nameof(Restore))));
                }
                Verify(move, nameof(Transpile), HarmonyPatchType.Transpiler);
                Verify(kickoff, nameof(Begin), HarmonyPatchType.Prefix);
                Verify(kickoff, nameof(End), HarmonyPatchType.Postfix);
                Verify(kickoff, nameof(Restore), HarmonyPatchType.Finalizer);
                _available = true;
                return true;
            }
            catch (Exception error)
            {
                _available = false;
                _failed = true;
                try { harmony.UnpatchAll(OwnerId); }
                finally { Spire1PowersGate.MarkDeterministicUnavailable("postdraw bridge unavailable: " + error.Message); }
                return false;
            }
        }
    }

    internal static void RequireAvailable()
    {
        if (!_available || Spire1PowersGate.ContentUnavailableActive)
            throw new InvalidOperationException("Spire1 postdraw bridge unavailable; restart with compatible game and mod versions.");
    }

    private static MethodInfo Own(string name) => typeof(Spire1PostDrawBridge).GetMethod(name, Methods)!;

    private static void Verify(MethodInfo target, string name, HarmonyPatchType kind)
    {
        var info = Harmony.GetPatchInfo(target) ?? throw new InvalidOperationException("Unpatched " + target);
        IEnumerable<Patch> patches = kind switch
        {
            HarmonyPatchType.Prefix => info.Prefixes,
            HarmonyPatchType.Postfix => info.Postfixes,
            HarmonyPatchType.Transpiler => info.Transpilers,
            HarmonyPatchType.Finalizer => info.Finalizers,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        Patch[] owned = patches.Where(p => p.owner == OwnerId).ToArray();
        if (owned.Length != 1 || owned[0].PatchMethod != Own(name))
            throw new InvalidOperationException("Missing exact postdraw patch " + name);
    }

    private static bool Calls(CodeInstruction i, MethodInfo method)
        => (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt) && Equals(i.operand, method);

    private static (int Draw, int Hook) ValidateSites(IReadOnlyList<CodeInstruction> body)
    {
        int[] draws = Enumerable.Range(0, body.Count).Where(i => Calls(body[i], DrawMethod)).ToArray();
        int[] hooks = Enumerable.Range(0, body.Count).Where(i => Calls(body[i], HookMethod)).ToArray();
        if (draws.Length != 1 || hooks.Length != 1 || draws[0] >= hooks[0])
            throw new InvalidOperationException("Expected one ordered setup Draw and AfterPlayerTurnStart");
        int a = draws[0], b = hooks[0];
        var cancel = typeof(CancellationToken).GetMethod(nameof(CancellationToken.ThrowIfCancellationRequested))!;
        int[] cancellations = Enumerable.Range(a + 1, b - a - 1).Where(i => Calls(body[i], cancel)).ToArray();
        if (a == 0 || body[a - 1].opcode != OpCodes.Ldc_I4_1 || cancellations.Length != 1 || cancellations[0] != b - 7)
            throw new InvalidOperationException("Setup base-draw flag or post-await cancellation boundary changed");
        int c = cancellations[0];
        if (c < 7 || body[c - 7].operand is not MethodInfo result || result.Name != "GetResult"
            || result.DeclaringType != typeof(TaskAwaiter<IEnumerable<CardModel>>) || body[c - 6].opcode != OpCodes.Pop
            || body[c - 5].opcode != OpCodes.Ldarg_0 || body[c - 4].opcode != OpCodes.Ldfld
            || body[c - 4].operand is not FieldInfo turn || turn.FieldType != typeof(CombatTurnState)
            || !Calls(body[c - 3], typeof(CombatTurnState).GetProperty(nameof(CombatTurnState.Ct))!.GetMethod!)
            || body[c - 2].opcode != OpCodes.Stloc_S || body[c - 1].opcode != OpCodes.Ldloca_S
            || !Equals(body[c - 2].operand, body[c - 1].operand))
            throw new InvalidOperationException("Setup Draw completion/cancellation producer changed");
        Type[] argumentTypes = { typeof(CombatState), typeof(HookPlayerChoiceContext), typeof(Player) };
        for (int i = 0; i < argumentTypes.Length; i++)
        {
            int load = c + 1 + i * 2;
            if (body[load].opcode != OpCodes.Ldarg_0 || body[load + 1].opcode != OpCodes.Ldfld
                || body[load + 1].operand is not FieldInfo field || field.FieldType != argumentTypes[i]
                || body[load].labels.Count != 0 || body[load + 1].labels.Count != 0
                || body[load].blocks.Count != 0 || body[load + 1].blocks.Count != 0)
                throw new InvalidOperationException("Setup hook arguments or cancellation branch changed");
        }
        if (body[b].labels.Count != 0 || body[b].blocks.Count != 0)
            throw new InvalidOperationException("A branch can bypass the setup cancellation boundary");
        ValidateAwait(body, a, b);
        return (a, b);
    }

    // This is the supported compiler's complete await grammar, not a GetResult presence check.
    // Recompile drift is rejected before either call is rewritten.
    private static void ValidateAwait(IReadOnlyList<CodeInstruction> body, int draw, int hook)
    {
        OpCode[] grammar = {
            OpCodes.Callvirt, OpCodes.Stloc_S, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Brtrue_S,
            OpCodes.Ldarg_0, OpCodes.Ldc_I4_3, OpCodes.Dup, OpCodes.Stloc_0, OpCodes.Stfld,
            OpCodes.Ldarg_0, OpCodes.Ldloc_S, OpCodes.Stfld, OpCodes.Ldarg_0, OpCodes.Ldflda,
            OpCodes.Ldloca_S, OpCodes.Ldarg_0, OpCodes.Call, OpCodes.Leave,
            OpCodes.Ldarg_0, OpCodes.Ldfld, OpCodes.Stloc_S, OpCodes.Ldarg_0, OpCodes.Ldflda,
            OpCodes.Initobj, OpCodes.Ldarg_0, OpCodes.Ldc_I4_M1, OpCodes.Dup, OpCodes.Stloc_0,
            OpCodes.Stfld, OpCodes.Ldloca_S, OpCodes.Call, OpCodes.Pop
        };
        void Need(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("Setup base Draw await control flow changed");
        }
        Need(draw >= 7 && hook - draw == 46);
        CodeInstruction At(int offset) => body[draw + offset];
        for (int i = 0; i < grammar.Length; i++)
            Need(At(i + 1).opcode == grammar[i]);
        Type awaiter = typeof(TaskAwaiter<IEnumerable<CardModel>>);
        Need(Equals(At(1).operand, typeof(Task<IEnumerable<CardModel>>).GetMethod("GetAwaiter"))
            && Equals(At(4).operand, awaiter.GetProperty("IsCompleted")!.GetMethod)
            && Equals(At(32).operand, awaiter.GetMethod("GetResult"))
            && Equals(At(25).operand, awaiter));
        Need(At(2).operand is LocalVariableInfo local && local.LocalType == awaiter);
        foreach (int offset in new[] { 3, 12, 16, 22, 31 })
            Need(Equals(At(offset).operand, At(2).operand));
        Need(At(10).operand is FieldInfo state && state.FieldType == typeof(int) && !state.IsStatic);
        var stateField = (FieldInfo)At(10).operand;
        Type machine = stateField.DeclaringType!;
        Need(machine.DeclaringType == typeof(CombatManager) && Equals(At(30).operand, stateField));
        Need(At(13).operand is FieldInfo stored && stored.FieldType == awaiter
            && stored.DeclaringType == machine && !stored.IsStatic
            && Equals(At(21).operand, stored) && Equals(At(24).operand, stored));
        Need(At(15).operand is FieldInfo builder && builder.FieldType == typeof(AsyncTaskMethodBuilder)
            && builder.DeclaringType == machine && !builder.IsStatic);
        MethodInfo register = typeof(AsyncTaskMethodBuilder).GetMethods().Single(m => m.Name == "AwaitUnsafeOnCompleted")
            .MakeGenericMethod(awaiter, machine);
        Need(Equals(At(18).operand, register));
        Need(body[0].opcode == OpCodes.Ldarg_0 && body[1].opcode == OpCodes.Ldfld
            && Equals(body[1].operand, stateField) && body[2].opcode == OpCodes.Stloc_0
            && body[3].opcode == OpCodes.Ldloc_0 && body[4].opcode == OpCodes.Switch
            && body[4].operand is Label[] labels && labels.Length == 5);
        Label resume = ((Label[])body[4].operand)[3];
        Need(At(5).operand is Label && At(19).operand is Label);
        Label completed = (Label)At(5).operand;
        Label exit = (Label)At(19).operand;
        Need(At(20).labels.Count == 1 && At(20).labels[0].Equals(resume)
            && At(31).labels.Count == 1 && At(31).labels[0].Equals(completed));
        Need(body.Any(i => i.opcode == OpCodes.Ret && i.labels.Contains(exit)));
        for (int i = draw - 6; i <= hook; i++)
            Need(body[i].blocks.Count == 0 && (i == draw + 20 || i == draw + 31 || body[i].labels.Count == 0));
        for (int i = 0; i < body.Count; i++)
        {
            if (body[i].operand is Label destination && (destination.Equals(resume) || destination.Equals(completed)))
                Need(i == draw + 5 && destination.Equals(completed));
            if (body[i].operand is Label[] destinations)
                for (int j = 0; j < destinations.Length; j++)
                    if (destinations[j].Equals(resume) || destinations[j].Equals(completed))
                        Need(i == 4 && j == 3 && destinations[j].Equals(resume));
        }
    }

    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
    {
        var body = instructions.Select(i => new CodeInstruction(i)).ToList();
        (int draw, int hook) = ValidateSites(body);
        body[draw].opcode = OpCodes.Call;
        body[draw].operand = Own(nameof(CaptureAndDraw));
        body[hook].opcode = OpCodes.Call;
        body[hook].operand = Own(nameof(FinishOwnThenEngineHooks));
        return body;
    }

    private struct Scope
    {
        internal bool Entered;
        internal bool TaskOwnsLease;
        internal Lease? Previous;
        internal Lease? Lease;
    }

    private static void Begin(CombatTurnState __0, Player __1, HookPlayerChoiceContext __2, out Scope __state)
    {
        __state = new Scope { Entered = true, Previous = Current.Value };
        try
        {
            // Mask a selected parent's scope even for a nested unselected invocation.
            Current.Value = null;
            if (Spire1PowersGate.ContentUnavailableActive || !Spire1.Spire1Code.Run.Spire1RunContent.ContentActiveThisRun)
                return;
            RequireAvailable();
            if (ExecutionContext.IsFlowSuppressed())
                throw new InvalidOperationException("Postdraw invocation requires ExecutionContext flow");
            if (__1.PlayerCombatState != null)
                Current.Value = __state.Lease = new Lease(__0, __1, __2);
        }
        catch
        {
            Current.Value = __state.Previous;
            __state.Entered = false;
            throw;
        }
    }

    private static void End(ref Task __result, ref Scope __state)
    {
        Lease? lease = __state.Lease;
        if (lease == null)
            return;
        if (__result == null)
            throw new InvalidOperationException("SetupPlayerTurn returned no Task");
        if (__result.IsCompleted)
            lease.Release();
        else
            __result = ReleaseAfter(__result, lease);
        __state.TaskOwnsLease = true;
    }

    private static async Task ReleaseAfter(Task original, Lease lease)
    {
        try { await original; }
        finally { lease.Release(); }
    }

    private static Exception? Restore(Exception? __exception, Scope __state)
    {
        if (__state.Entered)
        {
            Current.Value = __state.Previous;
            if (__exception != null || !__state.TaskOwnsLease)
                __state.Lease?.Release();
        }
        return __exception;
    }

    private static bool HasOwn(Player player)
        => player.Creature.GetPower<LegacyFumes>() != null || player.Creature.GetPower<LegacyDraw>() != null
            || player.Creature.GetPower<LegacyTools>() != null;

    private static Lease? Match(PlayerChoiceContext context, Player player, ICombatState? combat = null)
    {
        Lease? lease = Current.Value;
        if (lease == null)
        {
            if (!Spire1PowersGate.ContentUnavailableActive && HasOwn(player)
                && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
                throw new InvalidOperationException("Postdraw consumer lost its exact setup invocation");
            return null;
        }
        if (lease.Released)
            return null;
        if (!ReferenceEquals(lease.Context, context) || !ReferenceEquals(lease.Player, player)
            || !ReferenceEquals(lease.Pcs, player.PlayerCombatState) || lease.Turn != lease.Pcs.TurnNumber
            || (combat != null && !ReferenceEquals(lease.State.State, combat)))
            throw new InvalidOperationException("Postdraw setup identity changed");
        return lease;
    }

    private static Task<IEnumerable<CardModel>> CaptureAndDraw(PlayerChoiceContext context, decimal count, Player player, bool fromHandDraw)
    {
        Lease? lease = Match(context, player);
        if (lease != null)
        {
            lock (lease)
            {
                if (!lease.Released)
                {
                    if (!fromHandDraw || lease.Captured)
                        throw new InvalidOperationException("Postdraw capture called outside its single base draw");
                    lease.Captured = true;
                    var byPlayer = Claims.GetValue(lease.State, _ => new ConditionalWeakTable<PlayerCombatState, Claim>());
                    Claim claim = byPlayer.GetValue(lease.Pcs, _ => new Claim());
                    lock (claim)
                    {
                        if (claim.Winner == 0 || (lease.Turn > claim.Turn && claim.Terminal))
                        {
                            claim.Winner = lease.Id;
                            claim.Turn = lease.Turn;
                            claim.Terminal = false;
                            lease.Claim = claim;
                        }
                    }
                    if (lease.Claim != null && lease.CanContinue())
                    {
                        LegacyFumes? fumes = player.Creature.GetPower<LegacyFumes>();
                        LegacyDraw? draw = player.Creature.GetPower<LegacyDraw>();
                        LegacyTools? tools = player.Creature.GetPower<LegacyTools>();
                        if (fumes != null || draw != null || tools != null)
                            lease.Batch = new Batch(fumes, draw, tools, fumes == null ? Array.Empty<Creature>() : lease.State.State.HittableEnemies);
                    }
                }
            }
        }
        return CardPileCmd.Draw(context, count, player, fromHandDraw);
    }

    private static Task FinishOwnThenEngineHooks(ICombatState combat, PlayerChoiceContext context, Player player)
    {
        Lease? lease = Match(context, player, combat);
        if (lease == null)
            return Hook.AfterPlayerTurnStart(combat, context, player);
        Batch? batch;
        lock (lease)
        {
            if (lease.Released)
                batch = null;
            else
            {
                if (!lease.Captured || lease.Finished)
                    throw new InvalidOperationException("Postdraw finish has no unique preceding capture");
                lease.Finished = true;
                batch = lease.Batch;
            }
        }
        return batch == null ? Hook.AfterPlayerTurnStart(combat, context, player) : Finish(lease, combat, batch);
    }

    private static async Task Finish(Lease lease, ICombatState combat, Batch batch)
    {
        Player player = lease.Player;
        PlayerChoiceContext context = lease.Context;
        if (batch.Fumes != null && lease.CanContinue())
        {
            context.PushModel(batch.Fumes);
            try
            {
                foreach (Creature target in batch.Targets)
                {
                    if (!lease.CanContinue())
                        break;
                    if (ReferenceEquals(target.CombatState, combat) && target.IsAlive && target.IsHittable
                        && combat.Enemies.Contains(target))
                        await PowerCmd.Apply<PoisonPower>(context, target, batch.Poison, player.Creature, null);
                }
                if (lease.CanContinue())
                    batch.Fumes.InvokeExecutionFinished();
            }
            finally { context.PopModel(batch.Fumes); }
        }
        if (batch.Draw != null && lease.CanContinue())
        {
            context.PushModel(batch.Draw);
            try
            {
                await CardPileCmd.Draw(context, batch.DrawAmount, player);
                if (lease.CanContinue())
                    await PowerCmd.Remove(player.Creature.GetPower<LegacyDraw>());
                if (lease.CanContinue())
                    batch.Draw.InvokeExecutionFinished();
            }
            finally { context.PopModel(batch.Draw); }
        }
        if (batch.Tools != null && lease.CanContinue())
        {
            context.PushModel(batch.Tools);
            try
            {
                await CardPileCmd.Draw(context, batch.ToolsAmount, player);
                if (lease.CanContinue())
                {
                    var picked = (await CardSelectCmd.FromHandForDiscard(context, player,
                        new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, batch.ToolsAmount), null, batch.Tools)).ToList();
                    if (lease.CanContinue() && picked.Count != 0)
                        await CardCmd.Discard(context, picked);
                }
                if (lease.CanContinue())
                    batch.Tools.InvokeExecutionFinished();
            }
            finally { context.PopModel(batch.Tools); }
        }
        await Hook.AfterPlayerTurnStart(combat, context, player);
    }

    private sealed class Claim
    {
        internal int Turn;
        internal long Winner;
        internal bool Terminal;
    }

    private sealed class Batch(LegacyFumes? fumes, LegacyDraw? draw, LegacyTools? tools, IReadOnlyList<Creature> targets)
    {
        internal readonly LegacyFumes? Fumes = fumes;
        internal readonly LegacyDraw? Draw = draw;
        internal readonly LegacyTools? Tools = tools;
        internal readonly int Poison = fumes?.Amount ?? 0;
        internal readonly int DrawAmount = draw?.Amount ?? 0;
        internal readonly int ToolsAmount = tools?.Amount ?? 0;
        internal readonly IReadOnlyList<Creature> Targets = targets;
    }

    private sealed class Lease(CombatTurnState state, Player player, HookPlayerChoiceContext context)
    {
        internal readonly long Id = Interlocked.Increment(ref _nextLease);
        internal readonly CombatTurnState State = state;
        internal readonly Player Player = player;
        internal readonly PlayerCombatState Pcs = player.PlayerCombatState!;
        internal readonly int Turn = player.PlayerCombatState!.TurnNumber;
        internal readonly HookPlayerChoiceContext Context = context;
        internal Claim? Claim;
        internal Batch? Batch;
        internal bool Captured;
        internal bool Finished;
        private int _released;
        internal bool Released => Volatile.Read(ref _released) != 0;

        internal bool CanContinue()
        {
            if (Released)
                return false;
            State.Ct.ThrowIfCancellationRequested();
            return State.IsLive && ReferenceEquals(Player.PlayerCombatState, Pcs) && Turn == Pcs.TurnNumber
                && ReferenceEquals(Player.Creature.CombatState, State.State) && Player.Creature.IsAlive
                && Player.IsActiveForHooks && !_isCombatEnding!(CombatManager.Instance, State)
                && !Spire1PowersGate.ContentUnavailableActive && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers);
        }

        internal void Release()
        {
            lock (this)
            {
                if (Interlocked.Exchange(ref _released, 1) != 0)
                    return;
                Batch = null;
                if (Claim != null)
                {
                    lock (Claim)
                    {
                        if (Claim.Winner == Id && Claim.Turn == Turn)
                            Claim.Terminal = true;
                    }
                    Claim = null;
                }
            }
        }
    }
}
