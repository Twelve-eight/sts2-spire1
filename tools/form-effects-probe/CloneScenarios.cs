using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Forms.FormsCode;

namespace FormEffectsProbe;

// Each case uses MemberwiseClone followed by production InitInternalData dispatch.
// This is a narrow lifecycle check, not a save/load or native engine clone conformance test.
internal static class CloneScenarios
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
        suite.Add("clone.void_consumed_state_is_not_shared", async () =>
        {
            var f = new Fixture();
            var original = f.Attach<VoidFormEffectPower>();
            CardModel originalCard = f.Card();
            await PlayManual(f, original, originalCard, Fixture.Play(originalCard));
            var clone = f.CloneAndAttach(original, f.Other);
            Check.FreshInternalData(original, clone);
            Check.Cost(original, f.Card(), false, "original remains consumed");
            CardModel cloneCard = f.Card(f.OtherPlayer);
            Check.Cost(clone, cloneCard, true, "clone starts unconsumed");
            await PlayManual(f, clone, cloneCard, Fixture.Play(cloneCard));
            await f.StartSide(original, f.Owner);
            Check.Cost(original, f.Card(), true, "original resets independently");
            Check.Cost(clone, cloneCard, false, "original reset cannot reset clone");
        });

        suite.Add("clone.void_pending_play_is_not_shared", async () =>
        {
            var f = new Fixture();
            var original = f.Attach<VoidFormEffectPower>();
            CardModel originalCard = f.Card();
            CardPlay play = Fixture.Play(originalCard);
            object action = VoidFormPlayTransaction.ProbeBeginAction(originalCard);
            try
            {
                await ProductionPatchCalls.SpendResources(
                    originalCard,
                    () => Task.FromResult((3, 0)));
                var clone = f.CloneAndAttach(original, f.Other);
                Check.FreshInternalData(original, clone);
                originalCard.ProbeOnPlayBody = async () =>
                {
                    await original.BeforeCardPlayed(play);
                    await clone.AfterCardPlayed(f.Context, play);
                    Check.Cost(clone, f.Card(f.OtherPlayer), true, "clone did not see the original Before");
                    await original.AfterCardPlayed(f.Context, play);
                    Check.Cost(original, originalCard, false, "original completed its own pending play");
                };
                try
                {
                    await originalCard.OnPlayWrapper(
                        f.Context,
                        null,
                        isAutoPlay: false,
                        ZeroResources);
                }
                finally
                {
                    originalCard.ProbeOnPlayBody = null;
                }
            }
            finally
            {
                VoidFormPlayTransaction.ProbeEndAction(action);
            }
        });

        suite.Add("clone.serpent_started_play_set_is_not_shared", async () =>
        {
            var f = new Fixture();
            var original = f.Attach<SerpentFormPower>();
            CardPlay first = Fixture.Play(f.Card());
            await original.BeforeCardPlayed(first);
            var clone = f.CloneAndAttach(original);
            Check.FreshInternalData(original, clone);
            await clone.AfterCardPlayed(f.Context, first);
            Check.Equal(0, Journal.DamageRequests.Count, "clone cannot consume original started set");
            await original.AfterCardPlayed(f.Context, first);
            Check.Equal(1, Journal.DamageRequests.Count, "original started set survived clone notification");
            CardPlay second = Fixture.Play(first.Card);
            await clone.BeforeCardPlayed(second);
            await original.AfterCardPlayed(f.Context, second);
            Check.Equal(1, Journal.DamageRequests.Count, "original cannot consume clone started set");
            await clone.AfterCardPlayed(f.Context, second);
            Check.Equal(2, Journal.DamageRequests.Count, "clone's own play still triggers");
        });

        suite.Add("clone.demon_strength_ledgers_are_independent", async () =>
        {
            var f = new Fixture(round: 2);
            f.SeedStrength(5);
            f.SeedStrength(2, f.Other);
            var original = f.Attach<DemonFormPower>();
            await original.AfterApplied(f.Owner, null);
            Check.Equal(8, f.Strength, "original granted 3");
            var clone = f.CloneAndAttach(original, f.Other);
            Check.FreshInternalData(original, clone);
            await clone.AfterApplied(f.Other, null);
            Check.Equal(5, f.Other.GetPowerAmount<StrengthPower>(), "clone starts a fresh 3-point ledger");
            await PowerCmd.Remove(clone);
            Check.Equal(2, f.Other.GetPowerAmount<StrengthPower>(), "clone removes only its own contribution");
            Check.Equal(8, f.Strength, "clone exit did not clear original ledger");
            await PowerCmd.Remove(original);
            Check.Equal(5, f.Strength, "original still removes its own contribution");
        });

        suite.Add("clone.echo_consumption_is_not_shared", async () =>
        {
            var f = new Fixture();
            var original = f.Attach<EchoFormEffectPower>();
            CardModel card = f.Card();
            Check.Equal(2, original.ModifyCardPlayCount(card, null, 1), "original opportunity");
            await original.AfterModifyingCardPlayCount(card);
            var clone = f.CloneAndAttach(original);
            Check.FreshInternalData(original, clone);
            Check.Equal(1, original.ModifyCardPlayCount(card, null, 1), "original remains consumed");
            Check.Equal(2, clone.ModifyCardPlayCount(card, null, 1), "clone has fresh opportunity");
        });

        suite.Add("clone.celestial_entry_payment_state_is_not_shared", async () =>
        {
            var f = new Fixture();
            var original = f.Attach<CelestialFormPower>();
            await original.AfterApplied(f.Owner, null);
            var clone = f.CloneAndAttach(original);
            Check.FreshInternalData(original, clone);
            await clone.AfterApplied(f.Owner, null);
            await original.AfterApplied(f.Owner, null);
            Check.Sequence(new[] { 3m, 3m }, Journal.EnergyRequests.Select(r => r.Amount), "one energy request per independent instance");
            Check.Sequence(new[] { 3m, 3m }, Journal.DrawRequests.Select(r => r.Amount), "one draw request per independent instance");
        });

        suite.Add("clone.reaper_stateless_effect_rebinds_owner", async () =>
        {
            var f = new Fixture();
            var original = f.Attach<ReaperFormEffectPower>();
            var clone = f.CloneAndAttach(original, f.Other);
            Check.FreshInternalData(original, clone);
            Creature first = f.Combat.Enemies[0], second = f.Combat.Enemies[1];
            var firstHit = new DamageResult(first, ValueProp.Move) { UnblockedDamage = 3 };
            var secondHit = new DamageResult(second, ValueProp.Move) { BlockedDamage = 4 };
            await original.AfterDamageGiven(f.Context, f.Owner, firstHit, ValueProp.Move, first, null);
            await clone.AfterDamageGiven(f.Context, f.Owner, firstHit, ValueProp.Move, first, null);
            await clone.AfterDamageGiven(f.Context, f.Other, secondHit, ValueProp.Move, second, null);
            await original.AfterDamageGiven(f.Context, f.Other, secondHit, ValueProp.Move, second, null);
            Check.Equal(2, Journal.PowerRequests.Count, "stateless clones honor their rebound owner");
            Check.Sequence(new[] { f.Owner, f.Other }, Journal.PowerRequests.Select(r => r.Applier!), "independent Doom appliers");
            Check.Sequence(new[] { 3m, 4m }, Journal.PowerRequests.Select(r => r.Amount), "independent target totals");
        });
    }

    private static async Task PlayManual(
        Fixture fixture,
        VoidFormEffectPower power,
        CardModel card,
        CardPlay play)
    {
        object action = VoidFormPlayTransaction.ProbeBeginAction(card);
        try
        {
            await ProductionPatchCalls.SpendResources(
                card,
                () => Task.FromResult((3, 0)));

            card.ProbeOnPlayBody = async () =>
            {
                await power.BeforeCardPlayed(play);
                await power.AfterCardPlayed(fixture.Context, play);
            };
            try
            {
                await card.OnPlayWrapper(
                    fixture.Context,
                    null,
                    isAutoPlay: false,
                    ZeroResources);
            }
            finally
            {
                card.ProbeOnPlayBody = null;
            }
        }
        finally
        {
            VoidFormPlayTransaction.ProbeEndAction(action);
        }
    }
}
