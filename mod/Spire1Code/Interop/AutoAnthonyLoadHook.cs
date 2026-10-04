using System;
using System.Threading;
using ThreadingTimer = System.Threading.Timer;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;

namespace Spire1.Spire1Code.Interop;

/// <summary>
/// AutoAnthony bridge load-order fallback.
///
/// AssemblyLoad and System.Threading.Timer callbacks are notification sources only.
/// They never call Harmony or mutate bridge state directly: every Apply attempt is
/// serialized through ApplyGate/_applyInProgress and executes on the Godot main
/// thread, using the same Callable.CallDeferred path as the native smoke runner
/// when needed.
///
/// Thread identity: Godot.OS.GetThreadCallerId() == Godot.OS.GetMainThreadId().
/// NGame.IsMainThread() is deliberately not used: its first call can adopt a
/// non-main thread as "main" before NGame._EnterTree has recorded the real thread.
///
/// Re-entrancy: AutoAnthonyCompatBridge.Apply or a Harmony operation it performs
/// can synchronously raise AssemblyLoad on the same thread. Such a nested request
/// only sets _applyPending; the outer owner finishes the in-flight Apply and then
/// re-queues one deferred Apply turn. Apply is never re-entered recursively.
///
/// Retry liveness: retry sources are retained until Apply returns settled=true.
/// A missing or cancelled CallDeferred callback is detected by a generation
/// watchdog and resubmitted; late callbacks from older generations are ignored.
/// A terminal core reflection failure (incompatible AutoAnthony bytes) is
/// reported by AutoAnthonyCompatBridge.NeedsRetryWithoutAssemblyLoad as false:
/// the periodic source stops, while the AssemblyLoad hook stays installed for
/// late Watcher/AutoAnthonyWatcher observation.
/// </summary>
internal static class AutoAnthonyLoadHook
{
    private const int RetryIntervalMilliseconds = 1000;
    private const int DeferredApplyWatchdogMilliseconds = 2000;
    private static readonly object ApplyGate = new();
    private static readonly object RetryGate = new();
    private static readonly object AssemblyLoadGate = new();
    private static Harmony? _harmony;
    private static ThreadingTimer? _retryTimer;
    private static Thread? _retryFallbackThread;
    private static RetryFallbackState? _retryFallbackState;
    private static volatile bool _retrySourcesStopped;
    private static bool _deferredApplyQueued;
    private static long _deferredApplyGeneration;
    private static long _deferredApplyDeadlineTicks;
    private static bool _applyInProgress;
    private static bool _applyPending;
    private static bool _hooked;
    private static MainLoop? _mainLoop;
    private static volatile bool _shutdownRequested;
    private static bool _processExitHooked;

    private sealed class RetryFallbackState
    {
        internal volatile bool Stop;
    }

    internal static void TryApplyBridge(Harmony harmony)
    {
        HookProcessExit();
        lock (RetryGate)
        {
            _harmony = harmony;
        }

        RequestApply(harmony, "initializer");
    }

    private static Harmony? GetHarmony()
    {
        lock (RetryGate)
        {
            return _harmony;
        }
    }

    private static void HookProcessExit()
    {
        lock (RetryGate)
        {
            if (_processExitHooked)
            {
                return;
            }

            try
            {
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                _processExitHooked = true;
            }
            catch
            {
                // ProcessExit is only a shutdown backstop. The runtime-validity
                // guard still suppresses callbacks if the subscription fails.
            }
        }
    }

    private static void OnProcessExit(object? sender, EventArgs args)
    {
        _shutdownRequested = true;
        StopRetryTimer(permanent: true);

        lock (ApplyGate)
        {
            _deferredApplyQueued = false;
            _deferredApplyGeneration++;
            _applyPending = false;
        }

        lock (AssemblyLoadGate)
        {
            if (!_hooked)
            {
                return;
            }

            try
            {
                AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
            }
            catch
            {
                // Managed event cleanup is best effort during process exit.
            }
            _hooked = false;
        }
    }

    private static void CaptureMainLoop()
    {
        if (_mainLoop != null)
        {
            return;
        }

        try
        {
            _mainLoop = Engine.GetMainLoop();
        }
        catch
        {
            // A missing main loop during very early initialization is not a
            // bridge failure; the first valid main-thread Apply captures it.
        }
    }

    private static bool ShouldStopNotifications()
    {
        if (_retrySourcesStopped || _shutdownRequested)
        {
            return true;
        }

        MainLoop? loop = _mainLoop;
        if (loop == null)
        {
            return false;
        }

        try
        {
            return !GodotObject.IsInstanceValid(loop);
        }
        catch
        {
            // Treat a disposed/invalid Godot wrapper as shutdown. Do not log
            // through a logger whose native owner may already be gone.
            return true;
        }
    }

    /// <summary>
    /// Godot main-thread identity. Both values are engine thread IDs, not managed
    /// thread IDs, so the check does not depend on NGame state.
    /// </summary>
    private static bool IsMainThread()
    {
        try
        {
            return Godot.OS.GetThreadCallerId() == Godot.OS.GetMainThreadId();
        }
        catch
        {
            _shutdownRequested = true;
            return false;
        }
    }

    private static bool RequestApply(Harmony harmony, string source)
    {
        if (ShouldStopNotifications())
        {
            return false;
        }

        if (IsMainThread())
        {
            return ApplyOnMainThread(harmony, source);
        }

        QueueDeferredApply(harmony, source);
        return false;
    }

    /// <summary>
    /// Submits one deferred Apply turn. Safe from any thread. An accepted but
    /// cancelled callback is recovered by the watchdog: once the deadline
    /// expires, a new generation is submitted and the old callback becomes a
    /// stale no-op. If the submission itself fails, the queued flag is cleared
    /// so the next timer tick can retry without waiting for the watchdog.
    /// </summary>
    private static void QueueDeferredApply(Harmony harmony, string source)
    {
        if (ShouldStopNotifications())
        {
            return;
        }

        long generation;
        lock (ApplyGate)
        {
            long now = System.Environment.TickCount64;
            if (_deferredApplyQueued && now < _deferredApplyDeadlineTicks)
            {
                // A live callback is already queued; coalesce this request.
                return;
            }

            // Either no callback is queued, or the previous accepted callback
            // is overdue. In the overdue case, bumping the generation makes
            // any late callback from the old submission a stale no-op.
            generation = ArmDeferredApplyLocked(now);
        }

        SubmitDeferredApply(generation, harmony, source);
        EnsureRetryWakeSourceIfNeeded();
    }

    private static long ArmDeferredApplyLocked(long now)
    {
        long generation = ++_deferredApplyGeneration;
        _deferredApplyQueued = true;
        _deferredApplyDeadlineTicks = now + DeferredApplyWatchdogMilliseconds;
        return generation;
    }

    private static void SubmitDeferredApply(long generation, Harmony harmony, string source)
    {
        try
        {
            // The caller only submits work. ExecuteDeferredApply owns the
            // serialized Apply boundary and runs on Godot's main thread.
            Callable.From(() => ExecuteDeferredApply(generation, harmony, source)).CallDeferred();
        }
        catch (Exception exception)
        {
            // CallDeferred rejected the submission. Clear only this generation
            // so a later tick or external request can submit again.
            lock (ApplyGate)
            {
                if (_deferredApplyGeneration == generation)
                {
                    _deferredApplyQueued = false;
                }
            }

            LogError($"[Spire1] AutoAnthony bridge: failed to queue {source} on Godot main thread: {exception.Message}");
        }
    }

    private static void ExecuteDeferredApply(long generation, Harmony harmony, string source)
    {
        if (ShouldStopNotifications())
        {
            ClearDeferredApplyState();
            return;
        }

        try
        {
            lock (ApplyGate)
            {
                if (!_deferredApplyQueued || generation != _deferredApplyGeneration)
                {
                    // A stale or cancelled callback (for example after a
                    // watchdog resubmission) must not run a second Apply.
                    return;
                }

                _deferredApplyQueued = false;
            }

            ApplyOnMainThread(harmony, source);
        }
        catch (Exception exception)
        {
            lock (ApplyGate)
            {
                if (_deferredApplyGeneration == generation)
                {
                    _deferredApplyQueued = false;
                }
            }

            LogError($"[Spire1] AutoAnthony bridge: deferred Apply callback failed during {source}: {exception.Message}");
            EnsureRetryWakeSourceIfNeeded();
        }
    }

    /// <summary>
    /// Serialized Apply boundary. Only the Godot main thread may reach the
    /// bridge. A nested request from the in-flight Apply is merged into
    /// _applyPending and drained after the current call has fully returned;
    /// it is never executed recursively on the same stack.
    /// </summary>
    private static bool ApplyOnMainThread(Harmony harmony, string source)
    {
        if (ShouldStopNotifications())
        {
            return false;
        }

        if (!IsMainThread())
        {
            // Fail closed. No bridge state or Harmony operation may run here.
            // This path is fail-closed. It only keeps a periodic source when
            // the bridge state says progress is possible without another load.
            LogError($"[Spire1] AutoAnthony bridge: refusing non-main-thread Apply from {source}; retry wake source retained.");
            EnsureRetryWakeSourceIfNeeded();
            return false;
        }

        CaptureMainLoop();
        if (ShouldStopNotifications())
        {
            return false;
        }

        bool coalesced = false;
        lock (ApplyGate)
        {
            if (_applyInProgress)
            {
                // Synchronous AssemblyLoad (or another re-entrant caller) while
                // Apply is in progress. Merge it into the owner's next turn.
                _applyPending = true;
                return false;
            }

            if (_deferredApplyQueued)
            {
                // A deferred turn is already queued and will run on the main
                // thread; it observes the same idempotent bridge state, so this
                // request is coalesced instead of starting a second Apply.
                coalesced = true;
            }
            else
            {
                _applyInProgress = true;
            }
        }

        if (coalesced)
        {
            // Keep a wake source for the queued callback. This call is outside
            // ApplyGate so the two gates are never held at the same time.
            EnsureRetryWakeSourceIfNeeded();
            return false;
        }

        bool settled = false;
        bool requeue = false;
        try
        {
            settled = ExecuteApplyCore(harmony, source);
        }
        catch (Exception exception)
        {
            // ExecuteApplyCore is expected to contain its own failures; this
            // catch keeps a future retry alive if its bookkeeping ever throws.
            LogError($"[Spire1] AutoAnthony bridge: unexpected failure during {source}: {exception.Message}");
            settled = false;
            EnsureRetryWakeSourceIfNeeded();
        }
        finally
        {
            lock (ApplyGate)
            {
                _applyInProgress = false;
                requeue = _applyPending && !settled;
                _applyPending = false;
            }
        }

        if (requeue)
        {
            // A capability assembly was loaded while Apply was running and the
            // bridge is not settled yet. Re-run the idempotent Apply on a later
            // main-thread turn, after the previous call has fully returned.
            QueueDeferredApply(harmony, source + " (queued re-entry)");
        }

        return settled;
    }

    /// <summary>
    /// Actual bridge call plus retry-source bookkeeping. Runs only after the
    /// main-thread check in ApplyOnMainThread; the check is repeated here as a
    /// defense-in-depth guard at the exact bridge boundary.
    /// </summary>
    private static bool ExecuteApplyCore(Harmony harmony, string source)
    {
        if (ShouldStopNotifications())
        {
            return false;
        }

        if (!IsMainThread())
        {
            LogError($"[Spire1] AutoAnthony bridge: refusing non-main-thread Apply from {source}; retry wake source retained.");
            EnsureRetryWakeSourceIfNeeded();
            return false;
        }

        bool settled;
        try
        {
            settled = AutoAnthonyCompatBridge.Apply(harmony);
        }
        catch (Exception exception)
        {
            LogError($"[Spire1] AutoAnthony bridge failed during {source}: {exception.Message}");
            settled = false;
        }

        try
        {
            if (settled)
            {
                // Only a genuinely settled core + optional capability may
                // remove retry sources. The periodic source itself is stopped
                // after this bookkeeping succeeds, outside this try/catch, so
                // a failure here can never leave settled=false with the timer
                // already stopped. Clearing the queued turn prevents a late
                // deferred callback from keeping the queue latched after the
                // bridge no longer needs retries.
                UnhookAssemblyLoad();
                ClearDeferredApplyState();
            }
            else
            {
                // Keep AssemblyLoad as the cheap late-capability source. A
                // periodic source is reserved for core/registration/unpatch
                // states that can make progress without another assembly load.
                // The terminal incompatible-core latch reports no such progress,
                // so only the AssemblyLoad hook remains for optional addons.
                HookAssemblyLoad();
                if (NeedsPeriodicRetry())
                {
                    EnsureRetryWakeSource();
                }
                else
                {
                    // Do not leave a timer alive for the ordinary
                    // "AutoAnthony present, optional mods absent" state.
                    // This stop is non-terminal: a later AssemblyLoad event
                    // may re-enable the source if its new state needs it.
                    StopRetryTimer(permanent: false);
                }
            }
        }
        catch (Exception exception)
        {
            // A failure while updating the retry sources must not leave the
            // bridge without a wake source; fail closed and retain the timer.
            LogError($"[Spire1] AutoAnthony bridge: retry-source update failed during {source}: {exception.Message}");
            EnsureRetryWakeSourceIfNeeded();
            settled = false;
        }

        if (settled)
        {
            // StopRetryTimer is reached only from this settled success path.
            // It is never called when Apply returned false, nor when the
            // retry-source bookkeeping above failed and set settled=false.
            StopRetryTimer(permanent: true);
        }

        return settled;
    }

    private static void ClearDeferredApplyState()
    {
        lock (ApplyGate)
        {
            _deferredApplyQueued = false;
            _deferredApplyGeneration++;
        }
    }

    private static void HookAssemblyLoad()
    {
        if (ShouldStopNotifications())
        {
            return;
        }

        Exception? subscriptionFailure = null;
        lock (AssemblyLoadGate)
        {
            // ProcessExit and this method share the gate. The second shutdown
            // check closes the gap where ProcessExit could mark shutdown after
            // the fast check but before the event subscription.
            if (_hooked || ShouldStopNotifications())
            {
                return;
            }

            try
            {
                AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
                // Only a successful subscription may mark the hook installed.
                _hooked = true;
            }
            catch (Exception exception)
            {
                // Undo a partial subscription if possible, then keep the
                // independent retry source reachable so a later non-settled
                // attempt can retry the subscription. Retry bookkeeping stays
                // outside AssemblyLoadGate to avoid lock inversion at shutdown.
                try
                {
                    AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
                }
                catch
                {
                    // Best-effort cleanup only.
                }

                _hooked = false;
                subscriptionFailure = exception;
            }
        }

        if (subscriptionFailure != null)
        {
            EnsureRetryWakeSourceIfNeeded();
            LogError($"[Spire1] AutoAnthony bridge: AssemblyLoad subscription failed: {subscriptionFailure.Message}");
        }
    }

    private static void UnhookAssemblyLoad()
    {
        Exception? unsubscriptionFailure = null;
        lock (AssemblyLoadGate)
        {
            if (!_hooked)
            {
                return;
            }

            try
            {
                AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
                _hooked = false;
            }
            catch (Exception exception)
            {
                // Settled path only. If removal fails, keep _hooked=true so a
                // later call cannot double-subscribe. The remaining handler is
                // notification-only and Apply is idempotent.
                unsubscriptionFailure = exception;
            }
        }

        if (unsubscriptionFailure != null)
        {
            LogError($"[Spire1] AutoAnthony bridge: AssemblyLoad unsubscription failed: {unsubscriptionFailure.Message}");
        }
    }

    private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        if (ShouldStopNotifications())
        {
            return;
        }

        try
        {
            string? assemblyName = args.LoadedAssembly.GetName().Name;
            if (!IsCapabilityAssembly(assemblyName))
            {
                return;
            }

            Harmony? harmony = GetHarmony();
            if (harmony != null)
            {
                RequestApply(harmony, "AssemblyLoad:" + assemblyName);
            }
        }
        catch (Exception exception)
        {
            // AssemblyLoad is raised inside the loader; a notification must
            // never propagate an exception back into the loading thread.
            // Rebuild the independent retry source only when the bridge state
            // still needs a periodic retry. Shutdown invalidates this path.
            EnsureRetryWakeSourceIfNeeded();
            LogError($"[Spire1] AutoAnthony bridge: AssemblyLoad notification failed: {exception.Message}");
        }
    }

    /// <summary>
    /// Ensures a retry wake source exists. This wrapper never throws: it is
    /// called from notification callbacks and fail-closed paths that must not
    /// propagate an exception into the loader, ThreadPool, or initializer.
    /// </summary>
    private static void EnsureRetryWakeSource()
    {
        if (ShouldStopNotifications())
        {
            return;
        }

        try
        {
            EnsureRetryWakeSourceCore();
        }
        catch (Exception exception)
        {
            LogError($"[Spire1] AutoAnthony bridge: failed to ensure retry wake source: {exception.Message}");
        }
    }

    /// <summary>
    /// Creates a retry wake source without inspecting bridge state. Safe to
    /// call from any thread and from fail-closed paths where the main thread
    /// cannot be reached. The primary source is a 1-second ThreadingTimer; if
    /// its construction fails, an independent 1-second fallback thread is
    /// started instead. Both only submit notification work.
    /// </summary>
    private static void EnsureRetryWakeSourceCore()
    {
        if (ShouldStopNotifications())
        {
            return;
        }

        lock (RetryGate)
        {
            if (_retrySourcesStopped || _shutdownRequested)
            {
                // StopRetryTimer is only reached from the settled success
                // path. Once stopped, sources stay stopped; a late callback
                // cannot re-arm them.
                return;
            }

            if (_retryTimer != null)
            {
                return;
            }

            if (_retryFallbackThread != null && _retryFallbackThread.IsAlive)
            {
                return;
            }

            // A previous fallback thread that is no longer alive must not
            // block a new source attempt.
            _retryFallbackThread = null;
            _retryFallbackState = null;

            try
            {
                _retryTimer = new ThreadingTimer(
                    static _ => OnRetryTimer(),
                    null,
                    RetryIntervalMilliseconds,
                    RetryIntervalMilliseconds);
                return;
            }
            catch (Exception exception)
            {
                LogError($"[Spire1] AutoAnthony bridge: failed to create retry timer: {exception.Message}");
            }

            try
            {
                RetryFallbackState state = new();
                Thread thread = new Thread(() => RetryFallbackLoop(state))
                {
                    IsBackground = true,
                    Name = "Spire1.AutoAnthony.RetryFallback"
                };
                _retryFallbackState = state;
                _retryFallbackThread = thread;
                thread.Start();
            }
            catch (Exception exception)
            {
                _retryFallbackState = null;
                _retryFallbackThread = null;
                // Both independent wake sources failed. The remaining
                // autonomous source is a future capability AssemblyLoad (if
                // that subscription succeeded); otherwise only a later
                // external TryApplyBridge call can retry. This boundary is
                // explicit and is not presented as covered.
                LogError($"[Spire1] AutoAnthony bridge: retry timer and fallback thread both failed; no autonomous periodic wake source remains: {exception.Message}");
            }
        }
    }

    private static void RetryFallbackLoop(RetryFallbackState state)
    {
        while (!state.Stop)
        {
            try
            {
                Thread.Sleep(RetryIntervalMilliseconds);
            }
            catch (ThreadInterruptedException)
            {
                return;
            }
            catch (Exception exception)
            {
                // Do not spin if the platform sleep primitive fails.
                LogError($"[Spire1] AutoAnthony bridge: retry fallback sleep failed: {exception.Message}");
                return;
            }

            if (state.Stop || ShouldStopNotifications())
            {
                return;
            }

            try
            {
                OnRetryTimer();
            }
            catch (Exception exception)
            {
                // OnRetryTimer is designed not to throw; if it ever does,
                // keep the bounded 1-second loop alive instead of spinning.
                LogError($"[Spire1] AutoAnthony bridge: retry fallback notification failed: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// Removes every periodic retry source. Called only from the settled
    /// success path in ExecuteApplyCore. Sets a suppression flag under
    /// RetryGate so an in-flight notification cannot re-arm a source after
    /// this call returns.
    /// </summary>
    private static void StopRetryTimer(bool permanent)
    {
        ThreadingTimer? timer;
        RetryFallbackState? fallback;
        lock (RetryGate)
        {
            if (permanent)
            {
                _retrySourcesStopped = true;
                _harmony = null;
            }
            timer = _retryTimer;
            _retryTimer = null;
            fallback = _retryFallbackState;
            _retryFallbackState = null;
            _retryFallbackThread = null;
        }

        if (fallback != null)
        {
            fallback.Stop = true;
        }

        try
        {
            timer?.Dispose();
        }
        catch (Exception exception)
        {
            // Disposal failure must not turn the settled result into a retry
            // loop; the timer callback is notification-only.
            LogError($"[Spire1] AutoAnthony bridge: retry timer disposal failed: {exception.Message}");
        }
    }

    private static void OnRetryTimer()
    {
        if (ShouldStopNotifications())
        {
            StopRetryTimer(permanent: true);
            return;
        }

        try
        {
            Harmony? harmony = GetHarmony();
            if (harmony == null)
            {
                return;
            }

            // Watchdog: an accepted CallDeferred callback that was cancelled
            // or dropped leaves _deferredApplyQueued=true. After the deadline,
            // submit a new generation; the old callback is ignored.
            WatchDeferredApply(harmony);

            // This callback is a ThreadPool/fallback notification only.
            // RequestApply submits the actual Apply through
            // Callable.CallDeferred when it is not on the main thread.
            RequestApply(harmony, "retry timer");
        }
        catch (Exception exception)
        {
            // A timer callback must never throw into the ThreadPool or the
            // fallback loop. During Godot teardown, do not touch the logger or
            // create another source after the native owner is disposed.
            if (ShouldStopNotifications())
            {
                StopRetryTimer(permanent: true);
                return;
            }
            LogError($"[Spire1] AutoAnthony bridge: retry timer notification failed: {exception.Message}");
            EnsureRetryWakeSourceIfNeeded();
        }
    }

    private static void WatchDeferredApply(Harmony harmony)
    {
        if (ShouldStopNotifications())
        {
            return;
        }

        long generation;
        lock (ApplyGate)
        {
            if (!_deferredApplyQueued)
            {
                return;
            }

            long now = System.Environment.TickCount64;
            if (now < _deferredApplyDeadlineTicks)
            {
                return;
            }

            generation = ArmDeferredApplyLocked(now);
        }

        SubmitDeferredApply(generation, harmony, "deferred watchdog");
    }

    private static bool IsCapabilityAssembly(string? assemblyName)
        => assemblyName == "AutoAnthony"
           || assemblyName == "Watcher"
           || assemblyName == "AutoAnthonyWatcher";

    /// <summary>Consults the bridge state. Returns false for the terminal
    /// incompatible-core latch and for the ordinary "AutoAnthony present,
    /// optional mods absent" state; both keep only the AssemblyLoad hook.</summary>
    private static bool NeedsPeriodicRetry()
    {
        if (ShouldStopNotifications())
        {
            return false;
        }

        try
        {
            return AutoAnthonyCompatBridge.NeedsRetryWithoutAssemblyLoad;
        }
        catch (Exception exception)
        {
            LogError($"[Spire1] AutoAnthony bridge: retry-state probe failed: {exception.Message}");
            return !ShouldStopNotifications();
        }
    }

    private static void EnsureRetryWakeSourceIfNeeded()
    {
        if (NeedsPeriodicRetry())
        {
            EnsureRetryWakeSource();
        }
    }

    private static void LogError(string message)
    {
        if (ShouldStopNotifications())
        {
            return;
        }

        try
        {
            MainFile.Logger.Error(message);
        }
        catch
        {
            // Logging must never become a liveness dependency.
        }
    }
}