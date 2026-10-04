using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using Spire1.Spire1Code.Forms;

namespace FormEffectsProbe;

internal static class VoidScenarios
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
        suite.Add("void.entry_after_does_not_spend_next_manual_allowance", async () =>
        {
            var f = new Fixture();
            CardPlay entry = Fixture.Play(f.Card());
            // The effect was absent for this play's Before hook and gained by its OnPlay.
            var power = f.Attach<VoidFormEffectPower>();
            await power.AfterCardPlayed(f.Context, entry);
            CardModel next = f.Card();
            Check.Cost(power, next, true, "after entry After only");
            await PlayManual(f, power, next, Fixture.Play(next));
            Check.Cost(power, f.Card(), false, "after next completed manual play");
            await power.AfterCardPlayed(f.Context, entry);
            Check.Cost(power, f.Card(), false, "late duplicate entry notification");
        });

        suite.Add("void.entry_replays_do_not_spend_same_card_can_later_spend", async () =>
        {
            var f = new Fixture();
            CardModel card = f.Card();
            var power = f.Attach<VoidFormEffectPower>();
            await power.AfterCardPlayed(f.Context, Fixture.Play(card, index: 0, count: 3));
            for (int index = 1; index < 3; index++)
                await f.Complete(power, Fixture.Play(card, index: index, count: 3));
            Check.Cost(power, card, true, "entry series remaining replays");
            // New manual series of exactly the same CardModel, not a permanently excluded card.
            await PlayManual(f, power, card, Fixture.Play(card));
            Check.Cost(power, f.Card(), false, "same CardModel later first play consumed");
        });

        suite.Add("void.own_turn_only_resets_allowance", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel firstCard = f.Card();
            await PlayManual(f, power, firstCard, Fixture.Play(firstCard));
            await f.StartSide(power, f.Other);
            Check.Cost(power, f.Card(), false, "other participant start");
            await f.StartSide(power, f.Owner, f.Other);
            Check.Cost(power, f.Card(), true, "owner next start");
            CardModel secondCard = f.Card();
            await PlayManual(f, power, secondCard, Fixture.Play(secondCard));
            Check.Cost(power, f.Card(), false, "once per owner turn");
        });

        suite.Add("void.autoplay_never_spends_manual_allowance", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card(pile: PileType.Play);
            for (int index = 0; index < 2; index++)
                await f.Complete(power, Fixture.Play(card, auto: true, index: index, count: 2));
            Check.Cost(power, f.Card(), true, "after automatic series");
            CardModel manualCard = f.Card();
            await PlayManual(f, power, manualCard, Fixture.Play(manualCard));
            Check.Cost(power, f.Card(), false, "later manual still consumes");
        });

        suite.Add("void.other_player_never_spends_owner_allowance", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            await f.Complete(power, Fixture.Play(f.Card(f.OtherPlayer)));
            Check.Cost(power, f.Card(), true, "foreign play ignored");
            Check.Cost(power, f.Card(f.OtherPlayer), false, "foreign costs unchanged");
        });

        suite.Add("void.cost_hooks_only_affect_owner_hand_or_play", () =>
        {
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            foreach (PileType pile in Enum.GetValues<PileType>())
                Check.Cost(power, f.Card(pile: pile), pile is PileType.Hand or PileType.Play, "pile " + pile);
            Check.Cost(power, f.Card(pile: null), false, "missing pile");
            return Task.CompletedTask;
        });

        suite.Add("void.pending_manual_reserves_allowance_during_nested_autoplay", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardPlay outer = Fixture.Play(f.Card(), count: 2);
            Check.Cost(power, outer.Card, true, "before first manual Before");
            object action = VoidFormPlayTransaction.ProbeBeginAction(outer.Card);
            try
            {
                await ProductionPatchCalls.SpendResources(
                    outer.Card,
                    () => Task.FromResult((3, 0)));

                outer.Card.ProbeOnPlayBody = async () =>
                {
                    // CardModel.OnPlayWrapper order: BeforeCardPlayed -> nested play -> AfterCardPlayed.
                    await power.BeforeCardPlayed(outer);
                    Check.Cost(power, f.Card(), false, "pending manual reserves allowance");
                    await f.Complete(power, Fixture.Play(f.Card(), auto: true));
                    Check.Cost(power, f.Card(), false, "nested play does not clear reservation");
                    await power.AfterCardPlayed(f.Context, outer);
                };
                try
                {
                    await outer.Card.OnPlayWrapper(
                        f.Context,
                        null,
                        isAutoPlay: false,
                        ZeroResources);
                }
                finally
                {
                    outer.Card.ProbeOnPlayBody = null;
                }
            }
            finally
            {
                VoidFormPlayTransaction.ProbeEndAction(action);
            }

            await f.Complete(power, Fixture.Play(outer.Card, index: 1, count: 2));
            Check.Cost(power, f.Card(), false, "manual series consumes only once");
        });

        suite.Add("void.player_snapshot_survives_card_owner_change", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            CardPlay play = Fixture.Play(card);
            object action = VoidFormPlayTransaction.ProbeBeginAction(card);
            try
            {
                await ProductionPatchCalls.SpendResources(
                    card,
                    () => Task.FromResult((3, 0)));
                card.ProbeOnPlayBody = async () =>
                {
                    await power.BeforeCardPlayed(play);
                    card.Owner = f.OtherPlayer;
                    await power.AfterCardPlayed(f.Context, play);
                    card.Owner = f.OwnerPlayer;
                };
                try
                {
                    await card.OnPlayWrapper(
                        f.Context,
                        null,
                        isAutoPlay: false,
                        ZeroResources);
                }
                finally
                {
                    card.ProbeOnPlayBody = null;
                    card.Owner = f.OwnerPlayer;
                }
            }
            finally
            {
                VoidFormPlayTransaction.ProbeEndAction(action);
            }

            Check.Cost(power, card, false, "CardPlay.Player remains original player");
        });

        suite.Add("void.same_card_reusable_after_turn_reset", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<VoidFormEffectPower>();
            CardModel card = f.Card();
            CardPlay first = Fixture.Play(card);
            await PlayManual(f, power, card, first);
            await power.AfterCardPlayed(f.Context, first);
            Check.Cost(power, card, false, "duplicate After does not refund");
            await f.StartSide(power, f.Owner);
            Check.Cost(power, card, true, "same card eligible next turn");
            await PlayManual(f, power, card, Fixture.Play(card));
            Check.Cost(power, card, false, "new CardPlay consumes refreshed allowance");
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
