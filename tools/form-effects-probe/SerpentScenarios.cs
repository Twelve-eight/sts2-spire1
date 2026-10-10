using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.ValueProps;
using Forms.FormsCode;

namespace FormEffectsProbe;

internal static class SerpentScenarios
{
    public static void Register(ProbeSuite suite)
    {
        suite.Add("serpent.after_without_before_has_no_retroactive_hit", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<SerpentFormPower>();
            await power.AfterCardPlayed(f.Context, Fixture.Play(f.Card()));
            Check.Equal(0, Journal.DamageRequests.Count, "entry play must not hit");
            Check.Equal(0, f.Targets.NextItemCalls, "entry play must not touch RNG");
        });

        foreach (bool auto in new[] { false, true })
        foreach (int index in new[] { 0, 1, 2 })
        {
            bool isAuto = auto;
            int playIndex = index;
            suite.Add($"serpent.each_real_play_auto_{isAuto}_index_{playIndex}", async () =>
            {
                var f = new Fixture();
                var power = f.Attach<SerpentFormPower>();
                await f.Complete(power, Fixture.Play(f.Card(), auto: isAuto, index: playIndex, count: 3));
                AssertHit(f, 1);
            });
        }

        suite.Add("serpent.repeated_before_and_after_same_object_hit_once", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<SerpentFormPower>();
            CardPlay play = Fixture.Play(f.Card());
            await power.BeforeCardPlayed(play);
            await power.BeforeCardPlayed(play);
            await power.AfterCardPlayed(f.Context, play);
            await power.AfterCardPlayed(f.Context, play);
            AssertHit(f, 1);
        });

        suite.Add("serpent.new_play_objects_same_card_each_hit", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<SerpentFormPower>();
            CardModel card = f.Card();
            CardPlay first = Fixture.Play(card);
            CardPlay second = Fixture.Play(card);
            Check.NotSame(first, second, "separate actual CardPlay objects");
            await f.Complete(power, first);
            await f.Complete(power, second);
            await power.AfterCardPlayed(f.Context, first);
            AssertHit(f, 2);
        });

        suite.Add("serpent.inflight_damage_is_awaited_and_after_is_deduplicated", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<SerpentFormPower>();
            CardPlay play = Fixture.Play(f.Card());
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task? first = null, duplicate = null;
            Journal.DamagePause = _ => gate.Task;
            try
            {
                await power.BeforeCardPlayed(play);
                first = power.AfterCardPlayed(f.Context, play);
                Check.False(first.IsCompleted, "After must await damage command");
                duplicate = power.AfterCardPlayed(f.Context, play);
                Check.True(duplicate.IsCompleted, "duplicate After must not wait on another damage command");
                AssertHit(f, 1);
            }
            finally
            {
                Journal.DamagePause = null;
                gate.TrySetResult();
                await Task.WhenAll(first ?? Task.CompletedTask, duplicate ?? Task.CompletedTask);
            }
        });

        suite.Add("serpent.foreign_player_ignored", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<SerpentFormPower>();
            await f.Complete(power, Fixture.Play(f.Card(f.OtherPlayer)));
            Check.Equal(0, Journal.DamageRequests.Count, "other player");
            Check.Equal(0, f.Targets.NextItemCalls, "other player RNG");
        });

        suite.Add("serpent.player_snapshot_survives_card_owner_change", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<SerpentFormPower>();
            CardModel card = f.Card();
            CardPlay play = Fixture.Play(card);
            await power.BeforeCardPlayed(play);
            card.Owner = f.OtherPlayer;
            await power.AfterCardPlayed(f.Context, play);
            AssertHit(f, 1);
        });

        suite.Add("serpent.foreign_play_not_adopted_after_card_transfer", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<SerpentFormPower>();
            CardModel card = f.Card(f.OtherPlayer);
            CardPlay play = Fixture.Play(card);
            await power.BeforeCardPlayed(play);
            card.Owner = f.OwnerPlayer;
            await power.AfterCardPlayed(f.Context, play);
            Check.Equal(0, Journal.DamageRequests.Count, "original player remains foreign");
            Check.Equal(0, f.Targets.NextItemCalls, "transferred foreign card RNG");
        });

        suite.Add("serpent.empty_enemies_do_not_call_rng", async () =>
        {
            var f = new Fixture(enemyCount: 0, rng: Array.Empty<int>());
            var power = f.Attach<SerpentFormPower>();
            await f.Complete(power, Fixture.Play(f.Card()));
            Check.Equal(0, Journal.DamageRequests.Count, "no enemies");
            Check.Equal(0, f.Targets.NextItemCalls, "not even NextItem on an empty list");
            Check.Equal(0, f.Targets.Counter, "empty list leaves RNG counter");
        });

        suite.Add("serpent.fixed_source_rng_order_and_targets_repeat", async () =>
        {
            string[] first = await FixedSequence();
            string[] second = await FixedSequence();
            Check.Sequence(first, second, "identical scripted sources and event sequences");
        });

        foreach (string boundary in new[] { "ending", "over", "no_combat" })
        {
            string state = boundary;
            suite.Add("serpent.no_hit_" + state, async () =>
            {
                var f = new Fixture();
                var power = f.Attach<SerpentFormPower>();
                CardPlay play = Fixture.Play(f.Card());
                await power.BeforeCardPlayed(play);
                if (state == "ending") CombatManager.Instance.IsEnding = true;
                else if (state == "over") CombatManager.Instance.IsInProgress = false;
                else f.Owner.CombatState = null;
                await power.AfterCardPlayed(f.Context, play);
                Check.Equal(0, Journal.DamageRequests.Count, state + " damage calls");
                Check.Equal(0, f.Targets.NextItemCalls, state + " RNG calls");
            });
        }

        suite.Add("serpent.removed_source_live_listener_finishes_pending", async () =>
        {
            var f = new Fixture();
            var source = f.Attach<SerpentFormPower>();
            CardPlay play = Fixture.Play(f.Card());
            await source.BeforeCardPlayed(play);
            await PowerCmd.Remove(source);
            SerpentFormPower[] live = f.Owner.Powers.OfType<SerpentFormPower>().ToArray();
            Check.Equal(1, live.Length, "pending play requires one live completion bridge");
            Check.NotSame(source, live.Single(), "removed source is not the live listener");
            foreach (SerpentFormPower listener in live)
                await listener.AfterCardPlayed(f.Context, play);
            AssertHit(f, 1);
            Check.Equal(1, Journal.EnergyRequests.Count, "original removal pays once");
            Check.Equal(2m, Journal.EnergyRequests.Single().Amount, "original exit energy");
            Check.Equal(0, f.Owner.Powers.OfType<SerpentFormPower>().Count(), "completed bridge self-removes");
        });

        suite.Add("serpent.removed_source_and_bridge_duplicates_hit_once_and_clean_up", async () =>
        {
            var f = new Fixture();
            var source = f.Attach<SerpentFormPower>();
            CardPlay play = Fixture.Play(f.Card());
            await source.BeforeCardPlayed(play);
            await PowerCmd.Remove(source);
            SerpentFormPower bridge = f.Owner.Powers.OfType<SerpentFormPower>().Single();
            Check.NotSame(source, bridge, "live receiver is a completion bridge");
            foreach (SerpentFormPower listener in new[] { source, bridge, source, bridge })
                await listener.AfterCardPlayed(f.Context, play);
            AssertHit(f, 1);
            Check.Equal(1, Journal.EnergyRequests.Count, "duplicate callbacks never repay exit energy");
            Check.Equal(2m, Journal.EnergyRequests.Single().Amount, "one original exit payment");
            Check.Equal(0, f.Owner.Powers.OfType<SerpentFormPower>().Count(), "shared tracker drains and bridge self-removes");
        });
        suite.Add("serpent.exit_energy_exactly_two_once", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<SerpentFormPower>();
            await PowerCmd.Remove(power);
            await power.AfterRemoved(f.Owner);
            Check.Equal(1, Journal.EnergyRequests.Count, "one exit payment");
            Check.Equal(2m, Journal.EnergyRequests.Single().Amount, "exit energy");
            Check.Same(f.OwnerPlayer, Journal.EnergyRequests.Single().Player, "exit recipient");
        });

        suite.Add("serpent.public_grant_exit_energy_false_suppresses_payment", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<SerpentFormPower>();
            // No static member access: baseline without this new property must compile, then fail.
            PropertyInfo? property = typeof(SerpentFormPower).GetProperty("GrantExitEnergy", BindingFlags.Instance | BindingFlags.Public);
            Check.True(property?.PropertyType == typeof(bool) && property.GetMethod?.IsPublic == true && property.SetMethod?.IsPublic == true,
                "missing public bool GrantExitEnergy getter/setter");
            Check.Equal(true, (bool)property!.GetValue(power)!, "standalone default exit payment");
            property.SetValue(power, false);
            await PowerCmd.Remove(power);
            await power.AfterRemoved(f.Owner);
            Check.Equal(0, Journal.EnergyRequests.Count, "Watcher-owned exit must not double pay");
        });

        foreach (string boundary in new[] { "ending", "over", "dead", "no_combat", "no_player" })
        {
            string state = boundary;
            suite.Add("serpent.no_exit_energy_" + state, async () =>
            {
                var f = new Fixture();
                Creature owner = state == "no_player" ? f.Combat.Enemies[0] : f.Owner;
                var power = f.Attach<SerpentFormPower>(owner);
                if (state == "ending") CombatManager.Instance.IsEnding = true;
                if (state == "over") CombatManager.Instance.IsInProgress = false;
                if (state == "dead") owner.IsDead = true;
                if (state == "no_combat") owner.CombatState = null;
                await PowerCmd.Remove(power);
                Check.Equal(0, Journal.EnergyRequests.Count, state + " exit payment");
            });
        }
    }

    private static void AssertHit(Fixture f, int count)
    {
        Check.Equal(count, Journal.DamageRequests.Count, "damage command count");
        Check.Equal(count, f.Targets.Counter, "one shared RNG draw per hit");
        foreach (DamageRequest request in Journal.DamageRequests)
        {
            Check.Equal(3m, request.Amount, "damage per actual CardPlay");
            Check.Equal(ValueProp.Unpowered, request.Props, "unpowered damage");
            Check.Same(f.Owner, request.Dealer, "effect owner as dealer");
            Check.Same(null, request.CardSource, "no card damage source");
            Check.Same(null, request.CardPlay, "no attack replay source");
            Check.True(f.Combat.HittableEnemies.Contains(request.Target), "target came from hittable enemies");
        }
    }

    private static async Task<string[]> FixedSequence()
    {
        var f = new Fixture(rng: new[] { 2, 0, 1 });
        var power = f.Attach<SerpentFormPower>();
        int start = Journal.DamageRequests.Count;
        CardPlay first = Fixture.Play(f.Card());
        await f.Complete(power, first);
        await power.AfterCardPlayed(f.Context, first);
        Creature[] enemies = f.Combat.Enemies.ToArray();
        f.Combat.Enemies.Clear();
        await f.Complete(power, Fixture.Play(f.Card()));
        f.Combat.Enemies.AddRange(enemies);
        await f.Complete(power, Fixture.Play(f.Card(f.OtherPlayer)));
        await power.AfterCardPlayed(f.Context, Fixture.Play(f.Card()));
        await f.Complete(power, Fixture.Play(f.Card(), auto: true));
        await f.Complete(power, Fixture.Play(f.Card(), index: 2, count: 3));
        DamageRequest[] actual = Journal.DamageRequests.Skip(start).ToArray();
        Check.Sequence(new[] { "Enemy2", "Enemy0", "Enemy1" }, actual.Select(r => r.Target.Name), "scripted target identities");
        Check.True(actual.All(r => r.Amount == 3m && r.Props == ValueProp.Unpowered), "each scripted target takes one 3-point request");
        Check.Equal(3, f.Targets.NextItemCalls, "only real eligible plays select targets");
        Check.Equal(3, f.Targets.Counter, "shared stream consumption");
        Check.Sequence(new[]
        {
            "NextItem:[Enemy0,Enemy1,Enemy2]", "NextInt(0,3)=2",
            "NextItem:[Enemy0,Enemy1,Enemy2]", "NextInt(0,3)=0",
            "NextItem:[Enemy0,Enemy1,Enemy2]", "NextInt(0,3)=1"
        }, f.Targets.Trace, "exact RNG invocation order and input lists");
        return f.Targets.Trace.ToArray();
    }
}
