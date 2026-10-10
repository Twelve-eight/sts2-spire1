using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Forms.FormsCode;

namespace FormEffectsProbe;

internal static class DemonScenarios
{
    public static void Register(ProbeSuite suite)
    {
        foreach (var entry in new[] { (Round: 1, Total: 8), (Round: 2, Total: 10), (Round: 3, Total: 13) })
        {
            var sample = entry;
            suite.Add($"demon.entry_triangular_round_{sample.Round}_preserves_permanent", async () =>
            {
                var f = new Fixture(round: sample.Round);
                f.SeedStrength(7);
                var power = f.Attach<DemonFormPower>();
                await power.AfterApplied(f.Owner, null);
                Check.Equal(sample.Total, f.Strength, "entry plus existing permanent Strength");
                await PowerCmd.Remove(power);
                Check.Equal(7, f.Strength, "full exit preserves permanent Strength");
            });
        }

        suite.Add("demon.rounds_one_two_three_only_grant_incremental_deltas", async () =>
        {
            var f = new Fixture();
            f.SeedStrength(7);
            var power = f.Attach<DemonFormPower>();
            await power.AfterApplied(f.Owner, null);
            Check.Equal(8, f.Strength, "round 1 total");
            await f.StartOwnTurn(power, 2);
            Check.Equal(10, f.Strength, "round 2 total");
            f.Combat.RoundNumber = 3;
            await power.AfterPlayerTurnStart(f.Context, f.OtherPlayer);
            Check.Equal(10, f.Strength, "other player does not refresh");
            await f.StartOwnTurn(power, 3);
            await f.StartOwnTurn(power, 3);
            Check.Equal(13, f.Strength, "round 3 total and duplicate start");
            Check.Sequence(new[] { 1m, 2m, 3m }, StrengthRequests(), "scheduled deltas");
            await PowerCmd.Apply<StrengthPower>(f.Context, f.Owner, 5m, f.Owner, null);
            await PowerCmd.Remove(power);
            await power.AfterRemoved(f.Owner);
            Check.Equal(12, f.Strength, "later permanent grant and duplicate exit");
        });

        suite.Add("demon.duplicate_after_applied_does_not_reenter", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<DemonFormPower>();
            await power.AfterApplied(f.Owner, null);
            f.Combat.RoundNumber = 2;
            await power.AfterApplied(f.Owner, null);
            Check.Equal(1, f.Strength, "duplicate application is not another turn start");
            await f.StartOwnTurn(power, 2);
            Check.Equal(3, f.Strength, "real owner turn still advances");
        });

        suite.Add("demon.helmet_new_strength_tracks_actual_first_set_amount", async () =>
        {
            var f = new Fixture(round: 2);
            var helmet = new FirstPositiveStrengthDoubleRule(f.Owner);
            PowerHooks.Received.Add(helmet);
            var power = f.Attach<DemonFormPower>();
            await power.AfterApplied(f.Owner, null);
            Check.Equal(6, f.Strength, "round 2 requested 3 accepted 6");
            await f.StartOwnTurn(power, 3);
            Check.Equal(9, f.Strength, "round 3 adds only the scheduled 3");
            Check.True(helmet.Used, "helmet consumed in AfterModifying");
            Check.Equal(1, helmet.Modifications, "first positive grant only");
            await PowerCmd.Apply<StrengthPower>(f.Context, f.Owner, 4m, f.Owner, null);
            await PowerCmd.Remove(power);
            Check.Equal(4, f.Strength, "all actually granted form Strength removed, unrelated 4 remains");
        });

        suite.Add("demon.helmet_existing_strength_uses_actual_modify_amount", async () =>
        {
            var f = new Fixture();
            f.SeedStrength(7);
            var helmet = new FirstPositiveStrengthDoubleRule(f.Owner);
            PowerHooks.Received.Add(helmet);
            var power = f.Attach<DemonFormPower>();
            await power.AfterApplied(f.Owner, null);
            Check.Equal(9, f.Strength, "first positive 1 doubled");
            await f.StartOwnTurn(power, 2);
            Check.Equal(11, f.Strength, "second scheduled increment 2");
            await f.StartOwnTurn(power, 3);
            Check.Equal(14, f.Strength, "third scheduled increment 3");
            Check.Sequence(new[] { 1m, 2m, 3m }, StrengthRequests(), "schedule independent of accepted multiplier");
            await PowerCmd.Remove(power);
            Check.Equal(7, f.Strength, "no multiplied residual after exit");
        });

        foreach (int permanent in new[] { 0, 7 })
        {
            int initial = permanent;
            suite.Add($"demon.rejected_grant_no_negative_drift_initial_{initial}", async () =>
            {
                var f = new Fixture(round: 3);
                f.SeedStrength(initial);
                var reject = new RejectPositiveStrengthRule(f.Owner);
                PowerHooks.Received.Add(reject);
                var power = f.Attach<DemonFormPower>();
                await power.AfterApplied(f.Owner, null);
                Check.Equal(1, reject.Rejections, "positive grant intercepted");
                Check.Equal(initial, f.Strength, "grant accepted zero");
                await PowerCmd.Remove(power);
                Check.Equal(initial, f.Strength, "zero accepted must not be withdrawn");
            });
        }

        suite.Add("demon.rejected_delta_not_retried_later_same_round", async () =>
        {
            var f = new Fixture();
            var reject = new RejectPositiveStrengthRule(f.Owner);
            PowerHooks.Received.Add(reject);
            var power = f.Attach<DemonFormPower>();
            await power.AfterApplied(f.Owner, null);
            reject.Enabled = false;
            await f.StartOwnTurn(power, 1);
            Check.Equal(0, f.Strength, "blocked round 1 is not retried");
            await f.StartOwnTurn(power, 2);
            Check.Equal(2, f.Strength, "only the round 2 scheduled delta accepted");
            Check.Sequence(new[] { 1m, 2m }, StrengthRequests(), "request ledger advances even when acceptance is zero");
            await PowerCmd.Remove(power);
            Check.Equal(0, f.Strength, "withdraw only accepted 2");
        });

        suite.Add("demon.helmet_then_rejection_leaves_no_strength_to_withdraw", async () =>
        {
            var f = new Fixture();
            var helmet = new FirstPositiveStrengthDoubleRule(f.Owner);
            PowerHooks.Received.Add(helmet);
            PowerHooks.Received.Add(new RejectPositiveStrengthRule(f.Owner));
            var power = f.Attach<DemonFormPower>();
            await power.AfterApplied(f.Owner, null);
            Check.True(helmet.Used, "modified-then-blocked still notifies helmet");
            Check.Equal(0, f.Strength, "later receiver canceled doubled grant");
            await PowerCmd.Remove(power);
            Check.Equal(0, f.Strength, "canceled multiplier is not a granted contribution");
        });

        foreach (int permanent in new[] { 0, 7 })
        {
            int initial = permanent;
            suite.Add($"demon.exit_is_not_a_new_blockable_debuff_initial_{initial}", async () =>
            {
                var f = new Fixture();
                f.SeedStrength(initial);
                var power = f.Attach<DemonFormPower>();
                await power.AfterApplied(f.Owner, null);
                var reject = new RejectNegativeStrengthRule(f.Owner);
                PowerHooks.Received.Add(reject);
                await PowerCmd.Remove(power);
                Check.Equal(initial, f.Strength, "cleanup bypasses negative-grant interception");
                Check.Equal(0, reject.Rejections, "cleanup did not enter received-amount hook");
            });
        }

        suite.Add("demon.zero_aggregate_recreates_only_unrelated_negative_strength", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<DemonFormPower>();
            await power.AfterApplied(f.Owner, null);
            await PowerCmd.ModifyAmount(f.Context, f.Owner.GetPower<StrengthPower>()!, -1m, f.Owner, null);
            Check.Same(null, f.Owner.GetPower<StrengthPower>(), "zero aggregate removed by engine command");
            var reject = new RejectNegativeStrengthRule(f.Owner);
            PowerHooks.Received.Add(reject);
            int before = Journal.PowerRequests.Count;
            await PowerCmd.Remove(power);
            Check.Equal(-1, f.Strength, "form +1 removed, unrelated -1 retained");
            Check.Equal(before, Journal.PowerRequests.Count, "exact ApplyInternal cleanup is not a new grant");
            Check.Equal(0, reject.Rejections, "negative grant hook cannot block exact restoration");
        });

        suite.Add("demon.purged_nonzero_aggregate_is_not_withdrawn_twice", async () =>
        {
            var f = new Fixture();
            f.SeedStrength(7);
            var power = f.Attach<DemonFormPower>();
            await power.AfterApplied(f.Owner, null);
            await PowerCmd.Remove(f.Owner.GetPower<StrengthPower>());
            await PowerCmd.Apply<StrengthPower>(f.Context, f.Owner, 4m, f.Owner, null);
            await PowerCmd.Remove(power);
            Check.Equal(4, f.Strength, "separate replacement Strength not charged for purged contribution");
        });

        suite.Add("demon.accepted_strength_respects_engine_storage_clamp", async () =>
        {
            var f = new Fixture(round: 3);
            f.SeedStrength(999999998);
            var power = f.Attach<DemonFormPower>();
            await power.AfterApplied(f.Owner, null);
            Check.Equal(999999999, f.Strength, "stored amount clamped, only +1 accepted");
            await PowerCmd.Remove(power);
            Check.Equal(999999998, f.Strength, "rollback uses clamped actual delta not requested 6");
        });

        foreach (int permanent in new[] { 0, 7 })
        {
            int initial = permanent;
            suite.Add($"demon.reactive_permanent_bonus_not_charged_to_form_initial_{initial}", async () =>
            {
                var f = new Fixture(round: 2);
                f.SeedStrength(initial);
                var power = f.Attach<DemonFormPower>();
                PowerHooks.AfterAmountChanged = async request =>
                {
                    if (request.Power is not StrengthPower || request.Target != f.Owner) return;
                    PowerHooks.AfterAmountChanged = null;
                    await PowerCmd.Apply<StrengthPower>(f.Context, f.Owner, 4m, f.Owner, null);
                };
                await power.AfterApplied(f.Owner, null);
                Check.Equal(initial + 7, f.Strength, "form +3 and separate reactive +4");
                await PowerCmd.Remove(power);
                Check.Equal(initial + 4, f.Strength, "initial storage observation excludes reactive bonus");
            });
        }

        suite.Add("demon.removal_inside_grant_hook_finishes_exact_cleanup", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<DemonFormPower>();
            PowerHooks.AfterAmountChanged = async request =>
            {
                if (request.Power is not StrengthPower || request.Target != f.Owner) return;
                PowerHooks.AfterAmountChanged = null;
                await PowerCmd.Remove(power);
            };
            await power.AfterApplied(f.Owner, null);
            Check.False(f.Owner.Powers.Contains(power), "form removed during awaited grant");
            Check.Equal(0, f.Strength, "inflight accepted grant cleaned after removal");
        });

        RegisterDamage(suite);
    }

    private static IEnumerable<decimal> StrengthRequests()
        => Journal.PowerRequests.Where(r => r.Power is StrengthPower).Select(r => r.Amount);

    private static void RegisterDamage(ProbeSuite suite)
    {
        foreach (int round in new[] { 1, 2, 3 })
        foreach (ValueProp props in new[] { ValueProp.Move, ValueProp.Unpowered, ValueProp.Move | ValueProp.Unpowered, (ValueProp)0 })
        {
            int n = round;
            ValueProp damageProps = props;
            suite.Add($"demon.enemy_damage_round_{n}_props_{(int)damageProps}", () =>
            {
                var f = new Fixture(round: n);
                var power = f.Attach<DemonFormPower>();
                decimal actual = power.ModifyDamageAdditive(f.Owner, 8m, damageProps, f.Combat.Enemies[0], null, null);
                // Direct additive hook. It returns a delta, not the final 8+n damage amount.
                Check.Equal((decimal)n, actual, "enemy damage adds current round even when Unpowered");
                return Task.CompletedTask;
            });
        }

        foreach (string boundary in new[] { "self", "ally", "own_pet", "no_dealer", "other_target", "no_target" })
        {
            string source = boundary;
            suite.Add("demon.no_extra_damage_" + source, () =>
            {
                var f = new Fixture(round: 3);
                var power = f.Attach<DemonFormPower>();
                Creature? dealer = source switch
                {
                    "self" => f.Owner,
                    "ally" => f.Other,
                    "own_pet" => f.OwnerPet,
                    "no_dealer" => null,
                    _ => f.Combat.Enemies[0]
                };
                Creature? target = source == "other_target" ? f.Other : source == "no_target" ? null : f.Owner;
                Check.Equal(0m, power.ModifyDamageAdditive(target, 8m, ValueProp.Move, dealer, null, null), source + " must not add damage");
                return Task.CompletedTask;
            });
        }
    }
}
