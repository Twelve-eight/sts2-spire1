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
            var gate = new TaskCompletionSource<(int, int)>(TaskCreationOptions.RunContinuationsAsynchronously);

            Task<(int, int)> spend = ProductionPatchCalls.SpendResources(card, () => gate.Task);
            Check.Cost(power, card, true, "incomplete SpendResources keeps the owner reservation");
            Check.Cost(power, f.Card(), false, "a different card cannot borrow the reservation");
            gate.SetResult((3, 0));
            await spend;
            Check.Cost(power, card, true, "completed payment still awaits its matching play");

            CardPlay play = Fixture.Play(card);
            await power.BeforeCardPlayed(play);
            await card.OnPlayWrapper(f.Context, null, isAutoPlay: false, ZeroResources);
            await power.AfterCardPlayed(f.Context, play);
            Check.Cost(power, f.Card(), false, "completed manual play consumes the allowance");
        });

        suite.Add("transaction.void_nested_unowned_spend_does_not_end_outer", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel outer = f.Card();
            CardModel nested = f.Card();

            Task<(int, int)> spend = ProductionPatchCalls.SpendResources(outer, async () =>
            {
                Check.Cost(power, outer, true, "outer card owns the reservation");
                Task<(int, int)> nestedSpend = ProductionPatchCalls.SpendResources(nested, () =>
                    Task.FromResult((3, 0)));
                await nestedSpend;

                Check.Cost(power, nested, false, "nested different card cannot use the allowance");
                CardPlay nestedPlay = Fixture.Play(nested);
                await power.BeforeCardPlayed(nestedPlay);
                await nested.OnPlayWrapper(f.Context, null, isAutoPlay: false, ZeroResources);
                await power.AfterCardPlayed(f.Context, nestedPlay);
                Check.Cost(power, outer, true, "nested unowned play cannot release outer reservation");
                return (3, 0);
            });
            await spend;

            Check.Cost(power, outer, true, "outer reservation survives nested completion");
            CardPlay outerPlay = Fixture.Play(outer);
            await power.BeforeCardPlayed(outerPlay);
            await outer.OnPlayWrapper(f.Context, null, isAutoPlay: false, ZeroResources);
            await power.AfterCardPlayed(f.Context, outerPlay);
            Check.Cost(power, f.Card(), false, "outer manual play consumes the allowance");
        });

        suite.Add("transaction.void_failed_spend_releases_owner_reservation", () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();

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
        });

        suite.Add("transaction.void_cancelled_spend_releases_owner_reservation", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
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
        });

        suite.Add("transaction.void_auto_play_does_not_consume_allowance", async () =>
        {
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            CardPlay play = Fixture.Play(card, auto: true);

            await power.BeforeCardPlayed(play);
            await card.OnPlayWrapper(f.Context, null, isAutoPlay: true, ZeroResources);
            await power.AfterCardPlayed(f.Context, play);

            Check.Cost(power, f.Card(), true, "auto play does not consume the manual allowance");
        });

        suite.Add("transaction.void_payment_without_power_then_power_entry_does_not_consume_new_allowance", async () =>
        {
            Check.True(ProductionPatchCalls.HasVoidPatch, "linked Void transaction patch is present");
            Check.True(ProductionPatchCalls.HasOnPlayPatch, "linked OnPlay transaction patch is present");
            var f = new Fixture();
            CardModel card = f.Card();
            var gate = new TaskCompletionSource<(int, int)>(TaskCreationOptions.RunContinuationsAsynchronously);

            // The real payment starts before the Void power exists.
            Task<(int, int)> spend = ProductionPatchCalls.SpendResources(card, () => gate.Task);
            var power = f.Attach<VoidFormEffectPower>();
            gate.SetResult((3, 0));
            await spend;

            // The managed token has no matching reservation. OnPlay must block this card, not
            // consume the Void power that was gained while payment was awaiting.
            CardPlay play = Fixture.Play(card);
            await power.BeforeCardPlayed(play);
            await card.OnPlayWrapper(f.Context, null, isAutoPlay: false, ZeroResources);
            await power.AfterCardPlayed(f.Context, play);
            Check.Cost(power, f.Card(), true, "power gained during payment remains available to a later card");
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
}
