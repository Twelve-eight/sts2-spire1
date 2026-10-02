using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Forms;

namespace FormEffectsProbe;

internal static class TransactionScenarios
{
    private static readonly ResourceInfo ZeroResources = new()
    {
        EnergySpent = 0,
        EnergyValue = 0,
        StarsSpent = 0,
        StarValue = 0
    };
    public static void Register(ProbeSuite suite)
    {
        suite.Add("transaction.void_pending_spend_keeps_owner_reservation", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            object action = BeginManualAction(card);
            try
            {
                var gate = new TaskCompletionSource<(int, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
                Task<(int, int)> spend = ProductionPatchCalls.SpendResources(card, () => gate.Task);
                Check.Cost(power, card, true, "incomplete SpendResources keeps the owner reservation");
                Check.Cost(power, f.Card(), false, "a different card cannot borrow the reservation");
                gate.SetResult((3, 0));
                await spend;
                Check.Cost(power, card, true, "completed payment still awaits its matching play");

                CardPlay play = Fixture.Play(card);
                await PlayTransactionCard(f, power, card, play, isAutoPlay: false);
                Check.Cost(power, f.Card(), false, "completed manual play consumes the allowance");
            }
            finally
            {
                EndManualAction(action);
            }
        });

        suite.Add("transaction.void_nested_unowned_spend_does_not_end_outer", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel outer = f.Card();
            CardModel nested = f.Card();
            object action = BeginManualAction(outer);
            try
            {
                Task<(int, int)> spend = ProductionPatchCalls.SpendResources(outer, async () =>
                {
                    Check.Cost(power, outer, true, "outer card owns the reservation");
                    Task<(int, int)> nestedSpend = ProductionPatchCalls.SpendResources(nested, () =>
                        Task.FromResult((3, 0)));
                    await nestedSpend;

                    Check.Cost(power, nested, false, "nested different card cannot use the allowance");
                    CardPlay nestedPlay = Fixture.Play(nested);
                    await PlayTransactionCard(f, power, nested, nestedPlay, isAutoPlay: false);
                    Check.Cost(power, outer, true, "nested unowned play cannot release outer reservation");
                    return (3, 0);
                });
                await spend;

                Check.Cost(power, outer, true, "outer reservation survives nested completion");
                CardPlay outerPlay = Fixture.Play(outer);
                await PlayTransactionCard(f, power, outer, outerPlay, isAutoPlay: false);
                Check.Cost(power, f.Card(), false, "outer manual play consumes the allowance");
            }
            finally
            {
                EndManualAction(action);
            }
        });

        suite.Add("transaction.void_failed_spend_releases_owner_reservation", () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            object action = BeginManualAction(card);
            try
            {
                try
                {
                    ProductionPatchCalls.SpendResources(card, () =>
                        throw new InvalidOperationException("synthetic spend failure"));
                }
                catch (InvalidOperationException)
                {
                    // The linked finalizer must preserve the original failure and release the token.
                }

                Check.Cost(power, f.Card(), true, "failed spend leaves the allowance available");
                return Task.CompletedTask;
            }
            finally
            {
                EndManualAction(action);
            }
        });

        suite.Add("transaction.void_cancelled_spend_releases_owner_reservation", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            object action = BeginManualAction(card);
            try
            {
                var gate = new TaskCompletionSource<(int, int)>(TaskCreationOptions.RunContinuationsAsynchronously);
                Task<(int, int)> spend = ProductionPatchCalls.SpendResources(card, () => gate.Task);
                gate.SetCanceled();
                try
                {
                    await spend;
                }
                catch (OperationCanceledException)
                {
                    // ObserveSpendCompletion must release the reservation on cancellation.
                }

                Check.Cost(power, f.Card(), true, "cancelled spend leaves the allowance available");
            }
            finally
            {
                EndManualAction(action);
            }
        });

        suite.Add("transaction.void_auto_play_does_not_consume_allowance", async () =>
        {
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            CardPlay play = Fixture.Play(card, auto: true);
            await PlayTransactionCard(f, power, card, play, isAutoPlay: true);

            Check.Cost(power, f.Card(), true, "auto play does not consume the manual allowance");
        });

        suite.Add("transaction.void_payment_without_power_then_power_entry_does_not_consume_new_allowance", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            CardModel card = f.Card();
            object action = BeginManualAction(card);
            try
            {
                var gate = new TaskCompletionSource<(int, int)>(TaskCreationOptions.RunContinuationsAsynchronously);

                // The real payment starts before the Void power exists.
                Task<(int, int)> spend = ProductionPatchCalls.SpendResources(card, () => gate.Task);
                var power = f.Attach<VoidFormEffectPower>();
                gate.SetResult((3, 0));
                await spend;

                // The exact action handoff exists, but the token has no matching reservation.
                // OnPlay must block this card, not consume the Void power gained while payment awaited.
                CardPlay play = Fixture.Play(card);
                await PlayTransactionCard(f, power, card, play, isAutoPlay: false);
                Check.Cost(power, card, true, "payment-in-flight block is released after an unowned wrapper");
            }
            finally
            {
                EndManualAction(action);
            }
        });

        suite.Add("transaction.void_late_power_block_token_is_reclaimed", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            CardModel card = f.Card();
            object action = BeginManualAction(card);
            try
            {
                var paymentGate = new TaskCompletionSource<(int, int)>(TaskCreationOptions.RunContinuationsAsynchronously);

                // Payment begins before Void exists; the power enters before payment completion.
                Task<(int, int)> spend = ProductionPatchCalls.SpendResources(card, () => paymentGate.Task);
                var power = f.Attach<VoidFormEffectPower>();
                paymentGate.SetResult((3, 0));
                await spend;

                // BeginPlay blocks this token on the newly entered power. Keep the wrapper open while
                // an owner-absent side start must reclaim the token through the power bucket.
                var wrapperGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                card.ProbeOnPlayBody = () => wrapperGate.Task;
                Task wrapper = card.OnPlayWrapper(f.Context, null, isAutoPlay: false, ZeroResources);
                await f.StartSide(power, f.Other);
                wrapperGate.SetResult(true);
                await wrapper;
                card.ProbeOnPlayBody = null;

                // A later same-card generation must remain uniquely claimable.
                object nextAction = BeginManualAction(card);
                try
                {
                    Task<(int, int)> nextSpend = ProductionPatchCalls.SpendResources(
                        card,
                        () => Task.FromResult((3, 0)));
                    await nextSpend;
                    await PlayTransactionCard(f, power, card, Fixture.Play(card), isAutoPlay: false);
                }
                finally
                {
                    EndManualAction(nextAction);
                }
                Check.Cost(power, f.Card(), false, "late power blocked token was reclaimed before next generation");
            }
            finally
            {
                card.ProbeOnPlayBody = null;
                EndManualAction(action);
            }
        });

        suite.Add("transaction.void_payment_without_wrapper_side_cleanup_reclaims_orphan", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            var f = new Fixture();
            CardModel card = f.Card();

            // No action or wrapper is started: this deliberately leaves one completed, owner-only
            // token in the transaction registry, matching payment success followed by action cancel.
            Task<(int, int)> spend = ProductionPatchCalls.SpendResources(
                card,
                () => Task.FromResult((3, 0)));
            await spend;
            var power = f.Attach<VoidFormEffectPower>();
            await f.StartSide(power, f.Other);

            object action = BeginManualAction(card);
            try
            {
                Task<(int, int)> nextSpend = ProductionPatchCalls.SpendResources(
                    card,
                    () => Task.FromResult((3, 0)));
                await nextSpend;
                await PlayTransactionCard(f, power, card, Fixture.Play(card), isAutoPlay: false);
            }
            finally
            {
                EndManualAction(action);
            }
            Check.Cost(power, f.Card(), false, "side cleanup reclaims payment-without-wrapper orphan");
        });

        suite.Add("transaction.void_payment_without_wrapper_removal_cleanup_reclaims_orphan", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            var f = new Fixture();
            CardModel card = f.Card();

            // Payment succeeds before the power exists and the action never reaches its wrapper.
            // AfterRemoved is the only cleanup boundary exercised in this scenario.
            Task<(int, int)> spend = ProductionPatchCalls.SpendResources(
                card,
                () => Task.FromResult((3, 0)));
            await spend;
            var power = f.Attach<VoidFormEffectPower>();
            await power.AfterRemoved(f.Owner);

            object action = BeginManualAction(card);
            try
            {
                Task<(int, int)> nextSpend = ProductionPatchCalls.SpendResources(
                    card,
                    () => Task.FromResult((3, 0)));
                await nextSpend;
                await PlayTransactionCard(f, power, card, Fixture.Play(card), isAutoPlay: false);
            }
            finally
            {
                EndManualAction(action);
            }
            Check.Cost(power, f.Card(), false, "removal cleanup reclaims payment-without-wrapper orphan");
        });
        suite.Add("transaction.void_native_cancel_releases_unstarted_payment", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            object action = BeginNativeAction(card);
            object? nextAction = null;
            try
            {
                // Payment succeeds inside the native action, but the wrapper is never started.
                Task<(int, int)> spend = ProductionPatchCalls.SpendResources(
                    card,
                    () => Task.FromResult((3, 0)));
                await spend;
                Check.Cost(power, card, true, "native cancel sees the reserved allowance before cleanup");

                // CancelAction may be followed by ExecuteAction completion because GameAction.Cancel
                // does not stop the execution task. Both cleanup entry points must therefore be safe
                // when invoked repeatedly and must not depend on an OperationCanceledException.
                VoidFormPlayTransaction.CancelNativeActionForPatch(action);
                VoidFormPlayTransaction.CancelNativeActionForPatch(action);
                await VoidFormPlayTransaction.ObserveNativeActionCompletionForPatch(Task.CompletedTask, action);
                Check.Cost(power, card, true, "native cancel releases token and reservation");

                // A fresh generation must get the allowance, reach the real wrapper order, and consume
                // it exactly once. Cleanup of the old cancelled action must not roll this consumption back.
                nextAction = BeginNativeAction(card);
                Task<(int, int)> nextSpend = ProductionPatchCalls.SpendResources(
                    card,
                    () => Task.FromResult((3, 0)));
                await nextSpend;
                await PlayTransactionCard(f, power, card, Fixture.Play(card), isAutoPlay: false);
                Check.Cost(power, f.Card(), false, "next generation consumes after native cancel cleanup");

                await VoidFormPlayTransaction.ObserveNativeActionCompletionForPatch(Task.CompletedTask, action);
                VoidFormPlayTransaction.CancelNativeActionForPatch(action);
                Check.Cost(power, f.Card(), false, "late old-action cleanup does not roll back AfterCardPlayed");
            }
            finally
            {
                if (nextAction != null)
                {
                    VoidFormPlayTransaction.CancelNativeActionForPatch(nextAction);
                }
                VoidFormPlayTransaction.CancelNativeActionForPatch(action);
            }
        });

        suite.Add("transaction.void_native_completion_releases_unstarted_payment", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            object action = BeginNativeAction(card);
            object? nextAction = null;
            try
            {
                // Payment succeeds inside the native action, but no OnPlayWrapper callback occurs.
                Task<(int, int)> spend = ProductionPatchCalls.SpendResources(
                    card,
                    () => Task.FromResult((3, 0)));
                await spend;
                Check.Cost(power, card, true, "native completion sees the reserved allowance before cleanup");

                // A successful ExecuteAction completion is the normal non-exception cleanup boundary.
                // Calling it twice models completion observed after an earlier cancel/race and proves
                // that cleanup is idempotent rather than exception-driven.
                await VoidFormPlayTransaction.ObserveNativeActionCompletionForPatch(Task.CompletedTask, action);
                await VoidFormPlayTransaction.ObserveNativeActionCompletionForPatch(Task.CompletedTask, action);
                Check.Cost(power, card, true, "native completion releases token and reservation");

                nextAction = BeginNativeAction(card);
                Task<(int, int)> nextSpend = ProductionPatchCalls.SpendResources(
                    card,
                    () => Task.FromResult((3, 0)));
                await nextSpend;
                await PlayTransactionCard(f, power, card, Fixture.Play(card), isAutoPlay: false);
                Check.Cost(power, f.Card(), false, "next generation consumes after native completion cleanup");

                await VoidFormPlayTransaction.ObserveNativeActionCompletionForPatch(Task.CompletedTask, action);
                Check.Cost(power, f.Card(), false, "late old-action completion does not roll back AfterCardPlayed");
            }
            finally
            {
                if (nextAction != null)
                {
                    VoidFormPlayTransaction.CancelNativeActionForPatch(nextAction);
                }
                VoidFormPlayTransaction.CancelNativeActionForPatch(action);
            }
        });\n\n        suite.Add("transaction.void_no_token_manual_wrapper_does_not_consume_allowance", async () =>
        {
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            CardPlay play = Fixture.Play(card);

            await PlayTransactionCard(f, power, card, play, isAutoPlay: false);

            Check.Cost(power, f.Card(), true, "manual wrapper without payment token cannot consume allowance");
        });

        suite.Add("transaction.void_stale_only_manual_wrapper_does_not_consume_and_next_generation_works", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();

            // One completed payment is intentionally not followed by an action or wrapper. The
            // next wrapper has no payment handoff and must not guess this stale generation.
            Task<(int, int)> staleSpend = ProductionPatchCalls.SpendResources(
                card,
                () => Task.FromResult((3, 0)));
            await staleSpend;
            await PlayTransactionCard(f, power, card, Fixture.Play(card), isAutoPlay: false);
            Check.Cost(power, f.Card(), true, "stale-only manual wrapper cannot consume allowance");

            // A fresh payment/action handoff must not be blocked by the stale generation.
            object action = BeginManualAction(card);
            try
            {
                Task<(int, int)> nextSpend = ProductionPatchCalls.SpendResources(
                    card,
                    () => Task.FromResult((3, 0)));
                await nextSpend;
                await PlayTransactionCard(f, power, card, Fixture.Play(card), isAutoPlay: false);
            }
            finally
            {
                EndManualAction(action);
            }
            Check.Cost(power, f.Card(), false, "new generation remains claimable after stale cleanup");
        });

        suite.Add("transaction.void_wrapper_skip_after_cleans_pending_without_consuming", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            var play = Fixture.Play(card);
            object action = BeginManualAction(card);
            try
            {
                Task<(int, int)> spend = ProductionPatchCalls.SpendResources(
                    card,
                    () => Task.FromResult((3, 0)));
                await spend;

                card.ProbeOnPlayBody = () => power.BeforeCardPlayed(play);
                try
                {
                    // The wrapper completes without the engine AfterCardPlayed callback, matching
                    // owner-death or combat-ending early returns in CardModel.OnPlayWrapper.
                    await card.OnPlayWrapper(f.Context, null, isAutoPlay: false, ZeroResources);
                }
                finally
                {
                    card.ProbeOnPlayBody = null;
                }

                Check.Cost(power, f.Card(), true, "skipped AfterCardPlayed clears pending without consuming allowance");
            }
            finally
            {
                EndManualAction(action);
            }
        });

        suite.Add("transaction.void_after_success_tail_fault_keeps_consumed", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            var play = Fixture.Play(card);
            object action = BeginManualAction(card);
            try
            {
                Task<(int, int)> spend = ProductionPatchCalls.SpendResources(
                    card,
                    () => Task.FromResult((3, 0)));
                await spend;

                card.ProbeOnPlayBody = async () =>
                {
                    await power.BeforeCardPlayed(play);
                    await power.AfterCardPlayed(f.Context, play);
                    throw new InvalidOperationException("synthetic wrapper tail fault");
                };
                try
                {
                    await card.OnPlayWrapper(f.Context, null, isAutoPlay: false, ZeroResources);
                    throw new ProbeFailure("tail fault unexpectedly completed");
                }
                catch (InvalidOperationException)
                {
                    // The successful AfterCardPlayed callback must not be rolled back by observer abort.
                }
                finally
                {
                    card.ProbeOnPlayBody = null;
                }

                Check.Cost(power, f.Card(), false, "tail fault preserves the consumed allowance");
            }
            finally
            {
                EndManualAction(action);
            }
        });

        suite.Add("transaction.demon_real_set_amount_delta_round_one", async () =>
        {
            Check.True(ProductionPatchCalls.HasDemonPatch, "linked Demon transaction patch is present");
            var f = new Fixture(round: 1);
            f.SeedStrength(10);
            var power = f.Attach<DemonFormPower>();

            await power.AfterApplied(f.Owner, null);
            Check.Equal(11, f.Strength, "Demon grants the actual round-one delta");
            Check.Equal(1, Journal.HistoryRequests.Count, "the grant records one PowerReceived boundary");

            await PowerCmd.Remove(power);
            Check.Equal(10, f.Strength, "Demon removal withdraws only its accepted delta");
        });

        suite.Add("transaction.demon_write_after_storage_event_throw_keeps_actual_delta", async () =>
        {
            Check.True(ProductionPatchCalls.HasDemonPatch, "linked Demon transaction patch is present");
            var f = new Fixture(round: 1);
            f.SeedStrength(10);
            var power = f.Attach<DemonFormPower>();
            StrengthPower strength = f.Owner.GetPower<StrengthPower>()!;
            Action handler = () => throw new InvalidOperationException("synthetic post-write event failure");
            strength.DisplayAmountChanged += handler;

            try
            {
                await power.AfterApplied(f.Owner, null);
                throw new ProbeFailure("Demon grant unexpectedly completed");
            }
            catch (InvalidOperationException)
            {
                // The write crossed the storage boundary before the synchronous event failed.
            }
            finally
            {
                strength.DisplayAmountChanged -= handler;
            }

            Check.Equal(11, f.Strength, "post-write exception preserves the stored strength");
            await PowerCmd.Remove(power);
            Check.Equal(10, f.Strength, "exit removes the accepted delta after post-write failure");
        });

        suite.Add("transaction.demon_write_before_storage_exception_records_nothing", async () =>
        {
            Check.True(ProductionPatchCalls.HasDemonPatch, "linked Demon transaction patch is present");
            var f = new Fixture(round: 1);
            f.SeedStrength(10);
            var power = f.Attach<DemonFormPower>();
            PowerHooks.BeforeAmountChanged = _ =>
                Task.FromException(new InvalidOperationException("synthetic pre-write failure"));

            try
            {
                await power.AfterApplied(f.Owner, null);
                throw new ProbeFailure("Demon grant unexpectedly completed");
            }
            catch (InvalidOperationException)
            {
                // The failure happened before CombatHistory and SetAmount storage.
            }
            finally
            {
                PowerHooks.BeforeAmountChanged = null;
            }

            Check.Equal(10, f.Strength, "pre-write exception does not change strength");
            Check.Equal(0, Journal.HistoryRequests.Count, "pre-write exception records no PowerReceived boundary");
            await PowerCmd.Remove(power);
            Check.Equal(10, f.Strength, "exit has no unaccepted delta to remove");
        });

        suite.Add("transaction.demon_post_write_reentry_preserves_unrelated_strength", async () =>
        {
            Check.True(ProductionPatchCalls.HasDemonPatch, "linked Demon transaction patch is present");
            var f = new Fixture(round: 1);
            f.SeedStrength(10);
            var power = f.Attach<DemonFormPower>();
            StrengthPower strength = f.Owner.GetPower<StrengthPower>()!;
            bool reentered = false;
            Action handler = () =>
            {
                if (reentered) return;
                reentered = true;
                strength.SetAmount(strength.Amount + 5, silent: true);
                throw new InvalidOperationException("synthetic post-write reentry failure");
            };
            strength.DisplayAmountChanged += handler;

            try
            {
                await power.AfterApplied(f.Owner, null);
                throw new ProbeFailure("Demon grant unexpectedly completed");
            }
            catch (InvalidOperationException)
            {
                // The nested write is unrelated and the outer write already crossed _amount = amount.
            }
            finally
            {
                strength.DisplayAmountChanged -= handler;
            }

            Check.Equal(16, f.Strength, "post-write unrelated reentry remains stored");
            await PowerCmd.Remove(power);
            Check.Equal(15, f.Strength, "exit removes only the outer Demon delta");
        });
    }

    private static object BeginManualAction(CardModel card)
        => VoidFormPlayTransaction.ProbeBeginAction(card);

    private static object BeginNativeAction(CardModel card)
        => VoidFormPlayTransaction.ProbeBeginNativeAction(card);

    private static void EndManualAction(object action)
        => VoidFormPlayTransaction.ProbeEndAction(action);
    private static async Task PlayTransactionCard(
        Fixture fixture,
        VoidFormEffectPower power,
        CardModel card,
        CardPlay play,
        bool isAutoPlay)
    {
        card.ProbeOnPlayBody = async () =>
        {
            // CardModel.OnPlayWrapper order: BeforeCardPlayed -> card body -> AfterCardPlayed.
            await power.BeforeCardPlayed(play);
            await Task.CompletedTask;
            await power.AfterCardPlayed(fixture.Context, play);
        };
        try
        {
            await card.OnPlayWrapper(fixture.Context, null, isAutoPlay, ZeroResources);
        }
        finally
        {
            card.ProbeOnPlayBody = null;
        }
    }
}
