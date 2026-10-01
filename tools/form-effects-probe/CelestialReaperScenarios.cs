using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Forms;

namespace FormEffectsProbe;

internal static class CelestialReaperScenarios
{
    public static void Register(ProbeSuite suite)
    {
        foreach (var entry in new[] { (Round: 1, Amount: 3m), (Round: 3, Amount: 3m), (Round: 5, Amount: 5m) })
        {
            var sample = entry;
            suite.Add($"celestial.entry_round_{sample.Round}_pays_once", async () =>
            {
                var f = new Fixture(round: sample.Round);
                var power = f.Attach<CelestialFormPower>();
                await power.AfterApplied(f.Owner, null);
                AssertResources(f, sample.Amount);
                await power.AfterApplied(f.Owner, null);
                AssertResources(f, sample.Amount);
            });
        }

        foreach (string boundary in new[] { "no_player", "no_combat", "ending", "over" })
        {
            string state = boundary;
            suite.Add("celestial.no_resources_" + state, async () =>
            {
                var f = new Fixture(round: 5);
                Creature owner = state == "no_player" ? f.Combat.Enemies[0] : f.Owner;
                var power = f.Attach<CelestialFormPower>(owner);
                if (state == "no_combat") owner.CombatState = null;
                if (state == "ending") CombatManager.Instance.IsEnding = true;
                if (state == "over") CombatManager.Instance.IsInProgress = false;
                await power.AfterApplied(owner, null);
                Check.NoResources(state);
            });
        }

        suite.Add("celestial.inflight_entry_is_awaited_and_not_paid_twice", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<CelestialFormPower>();
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task? first = null, duplicate = null;
            Journal.EnergyPause = _ => gate.Task;
            try
            {
                first = power.AfterApplied(f.Owner, null);
                Check.False(first.IsCompleted, "entry awaits energy command");
                duplicate = power.AfterApplied(f.Owner, null);
                Check.True(duplicate.IsCompleted, "duplicate entry must not start another payment");
                Check.Equal(1, Journal.EnergyRequests.Count, "one in-flight energy request");
                Check.Equal(0, Journal.DrawRequests.Count, "draw follows awaited energy");
            }
            finally
            {
                Journal.EnergyPause = null;
                gate.TrySetResult();
                await Task.WhenAll(first ?? Task.CompletedTask, duplicate ?? Task.CompletedTask);
            }
            AssertResources(f, 3m);
        });

        foreach (string dealerKind in new[] { "owner", "owner_pet" })
        foreach (var split in new[] { (Block: 0, Hp: 5, Total: 5), (Block: 3, Hp: 2, Total: 5), (Block: 7, Hp: 0, Total: 7) })
        {
            string kind = dealerKind;
            var sample = split;
            suite.Add($"reaper.{kind}_blocked_{sample.Block}_unblocked_{sample.Hp}", async () =>
            {
                var f = new Fixture();
                var power = f.Attach<ReaperFormEffectPower>();
                Creature target = f.Combat.Enemies[0];
                Creature dealer = kind == "owner" ? f.Owner : f.OwnerPet;
                var result = new DamageResult(target, ValueProp.Move) { BlockedDamage = sample.Block, UnblockedDamage = sample.Hp };
                await power.AfterDamageGiven(f.Context, dealer, result, ValueProp.Move, target, null);
                AssertDoom(f, target, sample.Total);
            });
        }

        foreach (string dealerKind in new[] { "other", "other_pet", "enemy", "none" })
        {
            string kind = dealerKind;
            suite.Add("reaper.excludes_dealer_" + kind, async () =>
            {
                var f = new Fixture();
                var power = f.Attach<ReaperFormEffectPower>();
                Creature target = f.Combat.Enemies[0];
                Creature? dealer = kind switch
                {
                    "other" => f.Other,
                    "other_pet" => f.OtherPet,
                    "enemy" => f.Combat.Enemies[1],
                    _ => null
                };
                var result = new DamageResult(target, ValueProp.Move) { UnblockedDamage = 5 };
                await power.AfterDamageGiven(f.Context, dealer, result, ValueProp.Move, target, f.Card());
                Check.Equal(0, Journal.PowerRequests.Count, "foreign or missing dealer does not apply Doom");
            });
        }

        foreach (ValueProp flags in new[] { ValueProp.Unpowered, ValueProp.Move | ValueProp.Unpowered, (ValueProp)0 })
        {
            ValueProp props = flags;
            suite.Add($"reaper.excludes_nonpowered_props_{(int)props}", async () =>
            {
                var f = new Fixture();
                var power = f.Attach<ReaperFormEffectPower>();
                Creature target = f.Combat.Enemies[0];
                var result = new DamageResult(target, props) { BlockedDamage = 2, UnblockedDamage = 3 };
                await power.AfterDamageGiven(f.Context, f.Owner, result, props, target, f.Card(type: CardType.Attack));
                Check.Equal(0, Journal.PowerRequests.Count, "an Attack CardModel cannot override ValueProp gating");
            });
        }

        suite.Add("reaper.zero_total_damage_does_not_apply_doom", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<ReaperFormEffectPower>();
            Creature target = f.Combat.Enemies[0];
            await power.AfterDamageGiven(f.Context, f.Owner, new DamageResult(target, ValueProp.Move), ValueProp.Move, target, null);
            Check.Equal(0, Journal.PowerRequests.Count, "TotalDamage must be positive");
        });

        suite.Add("reaper.powered_damage_does_not_require_attack_card_source", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<ReaperFormEffectPower>();
            Creature target = f.Combat.Enemies[0];
            ValueProp props = ValueProp.Move | ValueProp.Unblockable;
            var result = new DamageResult(target, props) { UnblockedDamage = 4 };
            await power.AfterDamageGiven(f.Context, f.Owner, result, props, target, f.Card(type: CardType.Skill));
            AssertDoom(f, target, 4);
        });

        suite.Add("reaper.multi_hit_applies_each_targets_total_damage", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<ReaperFormEffectPower>();
            Creature first = f.Combat.Enemies[0], second = f.Combat.Enemies[1];
            await power.AfterDamageGiven(f.Context, f.Owner, new DamageResult(first, ValueProp.Move) { BlockedDamage = 2, UnblockedDamage = 1 }, ValueProp.Move, first, null);
            await power.AfterDamageGiven(f.Context, f.OwnerPet, new DamageResult(second, ValueProp.Move) { UnblockedDamage = 4 }, ValueProp.Move, second, null);
            Check.Equal(2, Journal.PowerRequests.Count, "one Doom command for every completed hit");
            Check.Sequence(new[] { 3m, 4m }, Journal.PowerRequests.Select(r => r.Amount), "per-hit totals");
            Check.Sequence(new[] { first, second }, Journal.PowerRequests.Select(r => r.Target), "per-hit targets");
            Check.Equal(3, first.GetPowerAmount<DoomPower>(), "first target accepted Doom");
            Check.Equal(4, second.GetPowerAmount<DoomPower>(), "second target accepted Doom");
        });
    }

    private static void AssertResources(Fixture f, decimal amount)
    {
        Check.Equal(1, Journal.EnergyRequests.Count, "entry energy call count");
        Check.Equal(1, Journal.DrawRequests.Count, "entry draw call count");
        Check.Equal(amount, Journal.EnergyRequests.Single().Amount, "entry energy amount");
        Check.Equal(amount, Journal.DrawRequests.Single().Amount, "entry draw amount");
        Check.Same(f.OwnerPlayer, Journal.EnergyRequests.Single().Player, "energy recipient");
        Check.Same(f.OwnerPlayer, Journal.DrawRequests.Single().Player, "draw recipient");
    }

    private static void AssertDoom(Fixture f, Creature target, int expected)
    {
        Check.Equal(1, Journal.PowerRequests.Count, "Doom request count");
        PowerRequest request = Journal.PowerRequests.Single();
        Check.True(request.Power is DoomPower, "actual command applies DoomPower");
        Check.Same(target, request.Target, "Doom target");
        Check.Equal((decimal)expected, request.Amount, "blocked plus unblocked damage");
        Check.Same(f.Owner, request.Applier, "form owner applies Doom, including pet attacks");
        Check.Same(null, request.CardSource, "Doom has no copied card source");
        Check.Equal(expected, target.GetPowerAmount<DoomPower>(), "stored Doom amount");
    }
}
