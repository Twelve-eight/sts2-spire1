using MegaCrit.Sts2.Core.Commands;
using Forms.FormsCode;

namespace FormEffectsProbe;

internal static class CarrierScenarios
{
    public static void Register(ProbeSuite suite)
    {
        suite.Add("carrier.choice_preserves_entry_order_and_pays_once", async () =>
        {
            var f = new Fixture(round: 4);
            f.OwnerPlayer.PlayerCombatState.TurnNumber = 7;
            var choice = new ProbeChoiceContext();
            var entry = Prepare(f, choice);
            var sequence = new List<string>();
            Journal.EnergyPause = _ => { sequence.Add("energy"); return Task.CompletedTask; };
            Journal.DrawContinuation = async (context, player) =>
            {
                sequence.Add("draw-begin");
                await context.SignalPlayerChoiceBegun(player);
                await context.SignalPlayerChoiceEnded();
                sequence.Add("draw-end");
            };
            PowerHooks.AfterAmountChanged = request =>
            {
                if (request.Power is CelestialFormPower) sequence.Add("celestial-tail");
                if (ReferenceEquals(request.Power, entry.Carrier)) sequence.Add("carrier-tail");
                return Task.CompletedTask;
            };
            Task apply = Apply(f, entry, choice);
            await InspectPaused(apply, choice.Resume, async outcome =>
            {
                Check.False(apply.IsCompleted, "complete carrier Apply waits for the selected draw continuation");
                Check.Same(f.OwnerPlayer, choice.Chooser, "the carrier passes its owner's choice context");
                Check.Equal(4, entry.Carrier.EnteredRound, "carrier captures entry round");
                Check.Equal(7, entry.Carrier.EnteredTurnNumber, "carrier captures the per-player turn");
                await entry.Carrier.AfterApplied(f.Owner, null);
                Check.Equal(1, Journal.EnergyRequests.Count, "duplicate AfterApplied cannot grant again while paused");
                Check.Equal(1, Journal.DrawRequests.Count, "duplicate AfterApplied cannot draw again while paused");
                choice.Resume();
                Check.Same(null, await outcome, "entry completes without a fault");
                Check.Sequence(new[] { "energy", "draw-begin", "draw-end", "celestial-tail", "carrier-tail" }, sequence,
                    "entry resources finish before effect and carrier amount-change hooks");
                Check.Equal(4m, Journal.EnergyRequests.Single().Amount, "entry energy uses round");
                Check.Equal(4m, Journal.DrawRequests.Single().Amount, "entry draw uses round");
                AssertLiveEffects(f);
                await Leave(f, entry);
                Check.Sequence(Array.Empty<PowerModel>(), f.Owner.Powers, "normal exit removes exactly the fixture entry");
            });
        });

        suite.Add("carrier.late_draw_fault_cleans_current_effects", async () =>
        {
            var f = new Fixture();
            var choice = new ProbeChoiceContext();
            var entry = Prepare(f, choice);
            var fault = new InvalidOperationException("late draw fault");
            Journal.DrawContinuation = async (context, player) =>
            {
                await context.SignalPlayerChoiceBegun(player);
                throw fault;
            };
            Task apply = Apply(f, entry, choice);
            await InspectPaused(apply, choice.Resume, async outcome =>
            {
                PowerModel[] effects = Effects(f);
                Check.Equal(2, effects.Length, "both effects attached before draw pauses");
                choice.Resume();
                Check.Same(fault, await outcome, "original late failure remains the task failure");
                foreach (PowerModel effect in effects)
                    Check.False(f.Owner.Powers.Contains(effect), "failed entry removes its exact effect");
                Check.Sequence(new PowerModel[] { entry.Marker, entry.Carrier }, f.Owner.Powers,
                    "the narrow carrier layer does not pretend to execute bridge-level carrier cleanup");
                await Leave(f, entry);
            });
        });

        suite.Add("carrier.old_late_fault_cannot_remove_new_entry", async () =>
        {
            var f = new Fixture();
            var choice = new ProbeChoiceContext();
            var old = Prepare(f, choice);
            var fault = new InvalidOperationException("old entry continuation fault");
            Journal.DrawContinuation = async (context, player) =>
            {
                await context.SignalPlayerChoiceBegun(player);
                throw fault;
            };
            Task pending = Apply(f, old, choice);
            await InspectPaused(pending, choice.Resume, async outcome =>
            {
                PowerModel[] oldEffects = Effects(f);
                Check.False(pending.IsCompleted, "old entry is suspended");
                await Leave(f, old);
                Journal.DrawContinuation = null;
                var next = Prepare(f, f.Context);
                await Apply(f, next, f.Context);
                AssertLiveEffects(f);
                PowerModel[] survivors = f.Owner.Powers.ToArray();
                choice.Resume();
                Check.Same(fault, await outcome, "old task preserves its late failure");
                Check.Sequence(survivors, f.Owner.Powers, "late cleanup preserves every new entry instance");
                foreach (PowerModel effect in oldEffects)
                    Check.False(f.Owner.Powers.Contains(effect), "old effect never returns");
                await Leave(f, next);
            });
        });

        foreach (bool removeMarker in new[] { true, false })
        {
            bool markerBoundary = removeMarker;
            suite.Add("carrier.detached_before_after_removed_blocks_grant_marker_" + markerBoundary, async () =>
            {
                var f = new Fixture();
                var entry = Prepare(f, f.Context);
                var applyGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                var removeGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                PowerHooks.BeforeAmountChanged = request => request.Power is CelestialFormPower ? applyGate.Task : Task.CompletedTask;
                PowerModel detached = markerBoundary ? entry.Marker : entry.Carrier;
                PowerHooks.AfterRemoveInternal = power => ReferenceEquals(power, detached) ? removeGate.Task : Task.CompletedTask;
                Task apply = Apply(f, entry, f.Context);
                Task removal = PowerCmd.Remove(detached);
                await InspectPaused(Task.WhenAll(apply, removal), () => { applyGate.TrySetResult(); removeGate.TrySetResult(); }, async outcome =>
                {
                    Check.False(f.Owner.Powers.Contains(detached), "native RemoveInternal detached the exact entry part");
                    Check.False(removal.IsCompleted, "native AfterRemoved has not run yet");
                    applyGate.SetResult();
                    await apply;
                    if (!markerBoundary) await PowerCmd.Remove(entry.Marker);
                    removeGate.SetResult();
                    Check.Same(null, await outcome, "both native-shaped tasks settle");
                    Check.NoResources("detached entry cannot begin Celestial resources before AfterRemoved");
                    if (f.Owner.Powers.Contains(entry.Carrier)) await PowerCmd.Remove(entry.Carrier);
                });
            });
        }

        suite.Add("carrier.late_attachment_after_exit_is_removed_by_identity", async () =>
        {
            var f = new Fixture();
            var old = Prepare(f, f.Context);
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            PowerModel? late = null;
            PowerHooks.BeforeAmountChanged = request =>
            {
                if (request.Power is not CelestialFormPower || late != null) return Task.CompletedTask;
                late = request.Power;
                return gate.Task;
            };
            Task pending = Apply(f, old, f.Context);
            await InspectPaused(pending, () => gate.TrySetResult(), async outcome =>
            {
                Check.True(late != null && !f.Owner.Powers.Contains(late), "second effect is paused before attachment");
                await Leave(f, old);
                var next = Prepare(f, f.Context);
                await Apply(f, next, f.Context);
                AssertLiveEffects(f);
                PowerModel[] survivors = f.Owner.Powers.ToArray();
                gate.SetResult();
                Check.Same(null, await outcome, "cancelled old entry completes its exact-instance cleanup");
                Check.Sequence(survivors, f.Owner.Powers, "late old attachment cannot escape into the new entry");
                Check.True(Journal.Removals.Any(power => ReferenceEquals(power, late)), "late exact instance was removed");
                Check.Equal(1, Journal.EnergyRequests.Count, "only the live new entry may start an energy grant");
                Check.Equal(1, Journal.DrawRequests.Count, "the exited unstarted entry may not begin a new draw");
                await Leave(f, next);
            });
        });

        suite.Add("carrier.cleanup_fault_preserves_entry_and_cleanup_leaves", async () =>
        {
            var f = new Fixture();
            var entry = Prepare(f, f.Context);
            var entryFault = new InvalidOperationException("draw failure");
            var cleanupFault = new InvalidOperationException("first exact cleanup rejected");
            bool rejected = false;
            Journal.DrawContinuation = (_, _) => Task.FromException(entryFault);
            PowerHooks.BeforeRemove = power =>
            {
                if (power is CelestialFormPower && !rejected)
                {
                    rejected = true;
                    return Task.FromException(cleanupFault);
                }
                return Task.CompletedTask;
            };
            Exception failure = await Capture(Apply(f, entry, f.Context));
            var aggregate = failure as AggregateException;
            Check.True(aggregate != null, "dual failure is explicit, not masked by cleanup");
            Exception[] leaves = aggregate!.Flatten().InnerExceptions.ToArray();
            Check.Sequence(new Exception[] { entryFault, cleanupFault }, leaves, "both original failure identities survive");
            Check.Sequence(new PowerModel[] { entry.Marker, entry.Carrier }, f.Owner.Powers,
                "outer exact cleanup still removes both effects after one rejection");
            await Leave(f, entry);
        });

        suite.Add("carrier.clone_does_not_inherit_unconsumed_context_or_marker", async () =>
        {
            var f = new Fixture();
            var oldChoice = new ProbeChoiceContext();
            var original = Prepare(f, oldChoice);
            var clone = (EchoCelestialStancePower)original.Carrier.MutableClone();
            Check.Same(null, clone.NativeMarker, "clone has no old marker binding");
            Check.FreshInternalData(original.Carrier, clone);
            clone.BindNativeMarker(original.Marker);
            clone.ApplyInternal(f.Owner, 1m, silent: true);
            Exception error = await Capture(clone.AfterApplied(f.Owner, null));
            Check.True(error is InvalidOperationException && error.Message.Contains("player-choice context"),
                "clone must reject the original entry's unconsumed context");
            Check.NoResources("clone missing context");
            clone.BindEntryContext(f.Context);
            await clone.AfterApplied(f.Owner, null);
            Check.Equal(1, Journal.EnergyRequests.Count, "explicit new context permits exactly one entry");
            Check.Same(null, oldChoice.Chooser, "old unconsumed context was never used");
            await PowerCmd.Remove(original.Marker);
            await PowerCmd.Remove(clone);
        });
    }

    private static void AssertLiveEffects(Fixture f)
    {
        Check.Equal(1, f.Owner.Powers.OfType<EchoFormEffectPower>().Count(), "live entry owns one Echo");
        Check.Equal(1, f.Owner.Powers.OfType<CelestialFormPower>().Count(), "live entry owns one Celestial");
    }

    private static async Task InspectPaused(Task operation, Action resume, Func<Task<Exception?>, Task> inspect)
    {
        Task<Exception?> outcome = Observe(operation);
        try { await inspect(outcome); }
        finally
        {
            resume();
            // Even an assertion failure must release and observe the scenario's suspended task.
            await outcome.WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    private static async Task<Exception?> Observe(Task operation)
    {
        try { await operation; return null; }
        catch (Exception error) { return error; }
    }

    private static (ProbeDivinityMarker Marker, EchoCelestialStancePower Carrier) Prepare(Fixture f, PlayerChoiceContext context)
    {
        var marker = f.Attach<ProbeDivinityMarker>();
        var carrier = (EchoCelestialStancePower)ModelDb.Power<EchoCelestialStancePower>().ToMutable();
        carrier.BindNativeMarker(marker);
        carrier.BindEntryContext(context);
        return (marker, carrier);
    }

    private static Task Apply(Fixture f, (ProbeDivinityMarker Marker, EchoCelestialStancePower Carrier) entry, PlayerChoiceContext context)
        => PowerCmd.Apply(context, entry.Carrier, f.Owner, 1m, f.Owner, null);

    private static async Task Leave(Fixture f, (ProbeDivinityMarker Marker, EchoCelestialStancePower Carrier) entry)
    {
        // Native marker removal is supplied explicitly; no imitation of native Watcher Exit.
        await PowerCmd.Remove(entry.Marker);
        await PowerCmd.Remove(entry.Carrier);
    }

    private static PowerModel[] Effects(Fixture f)
        => f.Owner.Powers.Where(power => power is EchoFormEffectPower or CelestialFormPower).ToArray();

    private static async Task<Exception> Capture(Task operation)
    {
        try { await operation; }
        catch (Exception error) { return error; }
        throw new ProbeFailure("Expected the operation to fail, but it completed successfully.");
    }
}
