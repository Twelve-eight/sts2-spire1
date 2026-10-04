using System.Runtime.CompilerServices;
using System.Threading;
using MegaCrit.Sts2.Core.Runs;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Run;

/// <summary>
/// C14 r11 (2026-10-03): immutable per-operation Cards gate snapshot. Every lifecycle
/// operation that can reach LargeCapsule captures exactly ONE gate decision before it calls
/// into the engine, and that same value is what the model-level final grant boundary
/// (Spire1Card.ShouldAddToDeck) and the engine-point Tags fuse read. A concurrent settings
/// toggle therefore cannot make a prefix and the original engine body disagree about the
/// same operation. Nesting is ref-counted, so an outer lifecycle keeps its decision while
/// an inner operation (relic obtain inside AfterObtained) still sees the same value.
/// </summary>
internal static class Spire1CardsGateSnapshot
{
    private static readonly AsyncLocal<int> Depth = new();
    private static readonly AsyncLocal<bool> ClosedValue = new();

    internal static bool IsActive => Depth.Value > 0;

    /// <summary>The gate decision for the current lifecycle operation, or the live value when
    /// no operation scope is active.</summary>
    internal static bool Closed => Depth.Value > 0 ? ClosedValue.Value : Spire1Config.LiveCardsGateClosed;

    /// <summary>Captures one decision for the whole operation. Nested operations keep the
    /// outermost decision so an inner engine call can never disagree with its caller.</summary>
    internal static void Enter(bool closed)
    {
        if (Depth.Value == 0)
        {
            ClosedValue.Value = closed;
        }
        Depth.Value++;
    }

    internal static void Exit()
    {
        if (Depth.Value > 0)
        {
            Depth.Value--;
        }
    }

    /// <summary>Enters a scope that freezes the live gate value for the duration of the
    /// operation. AsyncLocal flows into await continuations, so the engine body after an
    /// await still reads the same decision the prefix captured.</summary>
    internal static void EnterLive() => Enter(Spire1Config.LiveCardsGateClosed);

    /// <summary>
    /// C14 r12: scoped entry used by the direct LargeCapsule.AfterObtained boundary. Returns a
    /// token that restores the caller's AsyncLocal state exactly, so the kickoff prefix can hand
    /// the frozen decision to the async body and then restore the calling context without
    /// leaving a depth leak behind. The body's await continuations captured the entered value
    /// and keep it until the operation completes; the patch's paired Harmony finalizer restores
    /// the caller context after the kickoff returns (normal, faulted or canceled).
    /// </summary>
    internal static SnapshotToken EnterScoped(bool closed)
    {
        SnapshotToken token = new(Depth.Value, ClosedValue.Value);
        if (token.PreviousDepth == 0)
        {
            ClosedValue.Value = closed;
        }
        Depth.Value = token.PreviousDepth + 1;
        return token;
    }

    /// <summary>Restores the exact AsyncLocal state captured by <see cref="EnterScoped"/> in the
    /// current execution context. Called by the AfterObtained scope finalizer after the async
    /// body has captured the entered context, so the caller never sees the nested depth. A
    /// default token (scope not entered) is a strict no-op.</summary>
    internal static void Restore(SnapshotToken token)
    {
        if (!token.Entered)
        {
            return;
        }
        Depth.Value = token.PreviousDepth;
        ClosedValue.Value = token.PreviousClosedValue;
    }

    internal readonly struct SnapshotToken
    {
        internal SnapshotToken(int previousDepth, bool previousClosedValue)
        {
            Entered = true;
            PreviousDepth = previousDepth;
            PreviousClosedValue = previousClosedValue;
        }

        internal bool Entered { get; }

        internal int PreviousDepth { get; }

        internal bool PreviousClosedValue { get; }
    }
}

/// <summary>
/// C14 r13 (2026-10-04): per-run "did this run register Spire1 reward content" decision.
/// <para>
/// The decision has three carriers instead of one process-wide field:
/// 1) an AsyncLocal ambient binding, set while a run is created, loaded or canonicalized and
///    restored by the matching Harmony finalizer when that operation fails;
/// 2) a ConditionalWeakTable binding from the concrete RunState instance to its own value,
///    used by save-time code that has the instance but not the ambient context;
/// 3) a versioned process-wide fallback that keeps the previous behaviour for execution
///    contexts the AsyncLocal value does not flow into (Godot callbacks that run in a fresh
///    ExecutionContext).
/// </para>
/// <para>
/// The fallback and the combined Cards gate are process-wide, so a failed load or a canonicalize
/// only rewinds the exact write this operation made while it is still the latest publish. A
/// newer write from another thread (or a settings toggle) is preserved, so an interleaved
/// operation can never be clobbered by an older capture.
/// </para>
/// <para>
/// Default true: with no binding at all (main menu, compendium, saves created before this
/// feature existed) the mod behaves exactly as before. The per-run value only ever narrows
/// that default after a run is actually created or loaded.
/// </para>
/// <para>
/// Cards/relics/events/powers/potions AND this value into their gates; character visibility is
/// a select-time decision and deliberately does not consult it. Leaving a run (RunManager.CleanUp)
/// resets the binding to the default true, so the menu never inherits the previous run's value.
/// </para>
/// </summary>
internal static class Spire1RunContent
{
    private sealed class AmbientBinding
    {
        internal AmbientBinding(bool value)
        {
            Value = value;
        }

        internal bool Value { get; }
    }

    private static readonly object BindingGate = new();
    private static readonly AsyncLocal<AmbientBinding?> Ambient = new();
    private static readonly AsyncLocal<AmbientSnapshot?> ActiveCapture = new();
    private static readonly ConditionalWeakTable<object, StrongBox<bool>> InstanceBindings = new();
    private static bool _processFallback = true;
    private static long _processFallbackVersion;

    /// <summary>Process-wide fallback used when the current execution context has no ambient
    /// binding. Defaults to true (old-save / menu semantics).</summary>
    public static bool ProcessFallback => Volatile.Read(ref _processFallback);

    /// <summary>Current run's registration decision. Returns the ambient binding when the
    /// current execution context has one, otherwise the process fallback (default true).</summary>
    public static bool ContentActiveThisRun =>
        Ambient.Value is { } ambient ? ambient.Value : ProcessFallback;

    /// <summary>
    /// C14 r10/r13: single write point used by <see cref="Spire1Config.SetRunContentLatch"/>.
    /// Publishes the ambient binding first and the process fallback second; the combined Cards
    /// gate is published by the caller before this runs, so a reader that takes one atomic gate
    /// read never observes a torn master/cards/run chain. Do not call directly; latch/restore/reset
    /// go through Spire1Config.
    /// </summary>
    internal static void WriteContentActiveThisRun(bool value, long cardsLatchVersion, bool ownedByActiveCapture)
    {
        Ambient.Value = new AmbientBinding(value);
        long fallbackVersion;
        lock (BindingGate)
        {
            Volatile.Write(ref _processFallback, value);
            fallbackVersion = ++_processFallbackVersion;
        }

        if (!ownedByActiveCapture)
        {
            return;
        }

        // Record this write as owned by every capture active in this execution context. A later
        // restore may undo exactly this write, but never a newer write made by another thread.
        for (AmbientSnapshot? node = ActiveCapture.Value; node is not null; node = node.Parent)
        {
            node.LastOwnFallbackVersion = fallbackVersion;
            node.LastOwnCardsLatchVersion = cardsLatchVersion;
        }
    }

    /// <summary>
    /// Snapshot of the lifecycle carriers captured by <see cref="CaptureAmbient"/>: the ambient
    /// binding for this execution context, the process-wide fallback that readers without an
    /// ambient binding observe, and the run-latch version the combined Cards gate was published
    /// from. The fallback carrier stores only the version this operation itself last wrote; the
    /// latch carrier additionally stores the version it had at capture time. A restore only rewinds
    /// its own temporary write and never a newer publish made by another thread. The Cards gate is
    /// recomputed from the restored process-wide latch and the current switch values, so a
    /// concurrent settings toggle is preserved.
    /// </summary>
    private sealed class AmbientSnapshot
    {
        internal AmbientSnapshot(
            AmbientBinding? ambient,
            bool processFallback,
            long cardsLatchVersion,
            AmbientSnapshot? parent)
        {
            AmbientValue = ambient;
            ProcessFallbackValue = processFallback;
            CardsLatchVersion = cardsLatchVersion;
            Parent = parent;
        }

        internal AmbientBinding? AmbientValue { get; }

        internal bool ProcessFallbackValue { get; }

        internal long CardsLatchVersion { get; }

        internal AmbientSnapshot? Parent { get; }

        internal long LastOwnFallbackVersion { get; set; }

        internal long LastOwnCardsLatchVersion { get; set; }
    }

    /// <summary>Snapshot the current execution context's binding, the process fallback and the
    /// run-latch version behind the combined Cards gate, so a Harmony finalizer can restore the
    /// exact pre-call state when the guarded engine call fails. Captures nest: an inner capture
    /// (FromSerializable inside CanonicalizeSave) is attributed together with the outer one and
    /// popped independently.</summary>
    internal static object? CaptureAmbient()
    {
        long cardsLatchVersion = Spire1Config.CaptureRunLatchVersion();
        bool fallbackValue;
        lock (BindingGate)
        {
            fallbackValue = _processFallback;
        }

        var snapshot = new AmbientSnapshot(
            Ambient.Value,
            fallbackValue,
            cardsLatchVersion,
            ActiveCapture.Value);
        ActiveCapture.Value = snapshot;
        return snapshot;
    }

    /// <summary>
    /// Release a capture marker after a successful guarded call without rewinding any carrier: a
    /// successful load must keep the value it latched. Only the ownership bookkeeping is popped,
    /// so later writes are no longer attributed to this capture.
    /// </summary>
    internal static void CommitAmbient(object? capture)
    {
        if (capture is AmbientSnapshot snapshot && ReferenceEquals(ActiveCapture.Value, snapshot))
        {
            ActiveCapture.Value = snapshot.Parent;
        }
    }

    /// <summary>
    /// Restore a snapshot captured by <see cref="CaptureAmbient"/> after a failed load or a
    /// finished CanonicalizeSave. The ambient binding is per-execution-context and is always
    /// rewound. The process fallback and the combined Cards gate are process-wide: they are
    /// rewound only while this operation's own write is still the latest publish; a newer latch
    /// publish from another thread has a different version and is deliberately left untouched.
    /// The gate is recomputed from the restored process-wide latch and the current switch values,
    /// so it stays consistent with the other content gates and a concurrent settings toggle is
    /// preserved. A null or foreign snapshot cannot be attributed, so it only clears this
    /// context's ambient binding and never overwrites a process-wide publish from another thread.
    /// </summary>
    internal static void RestoreAmbient(object? previous)
    {
        if (previous is not AmbientSnapshot snapshot)
        {
            Ambient.Value = null;
            return;
        }

        CommitAmbient(snapshot);
        Ambient.Value = snapshot.AmbientValue;
        lock (BindingGate)
        {
            long current = _processFallbackVersion;
            if (current == snapshot.LastOwnFallbackVersion
                && _processFallback != snapshot.ProcessFallbackValue)
            {
                Volatile.Write(ref _processFallback, snapshot.ProcessFallbackValue);
                _processFallbackVersion++;
            }
        }

        // Recompute the combined Cards gate from the restored latch and the current switches.
        // Taken after BindingGate is released: latch publishers take CardsGateSync first and
        // BindingGate second, so the two locks are never nested in reverse order here.
        Spire1Config.RestoreCardsGateFromRunLatch(
            snapshot.ProcessFallbackValue,
            snapshot.CardsLatchVersion,
            snapshot.LastOwnCardsLatchVersion);
    }

    /// <summary>Bind a value to one concrete RunState instance. The table holds the key weakly,
    /// so a discarded RunState can still be collected and no static strong reference leaks.</summary>
    internal static void BindToInstance(object instance, bool value)
    {
        if (instance is null)
        {
            throw new System.ArgumentNullException(nameof(instance));
        }
        lock (BindingGate)
        {
            InstanceBindings.AddOrUpdate(instance, new StrongBox<bool>(value));
        }
    }

    /// <summary>Read the value bound to one concrete RunState instance, or null when that
    /// instance has no binding (unknown context: callers must fail closed, never guess).</summary>
    internal static bool? TryReadInstanceBinding(object? instance)
    {
        if (instance is null)
        {
            return null;
        }
        lock (BindingGate)
        {
            return InstanceBindings.TryGetValue(instance, out StrongBox<bool>? box) ? box.Value : null;
        }
    }

    /// <summary>New run created: latch the global setting for that run and bind it to the
    /// concrete RunState instance so later save-time reads cannot pick up another run's value.</summary>
    public static void LatchForNewRun(RunState runState, bool registerContent)
    {
        Spire1Config.SetRunContentLatch(registerContent);
        BindToInstance(runState, registerContent);
    }

    /// <summary>Save loaded: adopt the value carried by that save (absent snapshot -> true) and
    /// bind it to the concrete RunState instance in the FromSerializable postfix. The latch write
    /// is marked as owned by the active lifecycle capture, so a failed load or a canonicalize can
    /// rewind exactly this write while it is still the latest publish.</summary>
    public static void RestoreFromSave(bool registeredInSave)
    {
        Spire1Config.SetRunContentLatchFromLoad(registeredInSave);
    }

    /// <summary>
    /// Leaving a run (menu / end / replay): clear the binding and return to the default true.
    /// Idempotent; never touches a save, because each run's decision travels with its own
    /// SerializableRun.Modifiers entry.
    /// </summary>
    public static void ResetForMenu()
    {
        Spire1Config.SetRunContentLatch(true);
    }
}
