using MegaCrit.Sts2.Core.Entities.Cards;
using Forms.FormsCode;

namespace FormEffectsProbe;

internal static class EchoScenarios
{
    public static void Register(ProbeSuite suite)
    {
        foreach (int existing in new[] { 1, 2, 4 })
        {
            int initial = existing;
            suite.Add($"echo.preserves_existing_count_{initial}_and_adds_one", () =>
            {
                var f = new Fixture();
                var power = f.Attach<EchoFormEffectPower>();
                CardModel card = f.Card();
                Check.Equal(initial + 1, power.ModifyCardPlayCount(card, null, initial), "existing replay sources retained");
                Check.Equal(initial + 1, power.ModifyCardPlayCount(card, null, initial), "query alone does not consume");
                return Task.CompletedTask;
            });
        }

        suite.Add("echo.after_modifying_consumes_only_once", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<EchoFormEffectPower>();
            CardModel card = f.Card();
            Check.Equal(4, power.ModifyCardPlayCount(card, null, 3), "next actual request");
            await power.AfterModifyingCardPlayCount(card);
            Check.Equal(3, power.ModifyCardPlayCount(f.Card(), null, 3), "consumed effect keeps existing count");
            await power.AfterModifyingCardPlayCount(card);
            Check.Equal(1, power.ModifyCardPlayCount(card, null, 1), "duplicate callback never refreshes");
        });

        suite.Add("echo.other_player_request_or_notification_does_not_consume", async () =>
        {
            var f = new Fixture();
            var power = f.Attach<EchoFormEffectPower>();
            CardModel foreign = f.Card(f.OtherPlayer);
            Check.Equal(3, power.ModifyCardPlayCount(foreign, null, 3), "foreign replay count");
            await power.AfterModifyingCardPlayCount(foreign);
            Check.Equal(2, power.ModifyCardPlayCount(f.Card(), null, 1), "owner opportunity remains");
        });

        foreach (int invalid in new[] { 0, -1, int.MaxValue })
        {
            int initial = invalid;
            suite.Add($"echo.nonpositive_or_overflow_request_{initial}_is_unchanged", async () =>
            {
                var f = new Fixture();
                var power = f.Attach<EchoFormEffectPower>();
                Check.Equal(initial, await GenerateFromSnapshot(new[] { power }, f.Card(), initial), "invalid request not modified");
                Check.Equal(2, power.ModifyCardPlayCount(f.Card(), null, 1), "invalid request does not consume next legal one");
            });
        }

        suite.Add("echo.entry_onplay_occurs_after_replay_count_was_generated", async () =>
        {
            var f = new Fixture();
            var installed = new List<PowerModel>();
            CardModel entry = f.Card();
            // CardModel.cs:1887,1926,1933,1965,2029-2034.
            // This narrow driver preserves the documented order; it is not the native scheduler.
            int entryCount = await GenerateFromSnapshot(installed, entry, 1);
            var power = f.Attach<EchoFormEffectPower>(); // Enter during OnPlay, after generation/Before.
            installed.Add(power);
            await power.AfterCardPlayed(f.Context, Fixture.Play(entry, count: entryCount));
            Check.Equal(1, entryCount, "entry series is not regenerated after gaining Echo");
            Check.Equal(4, await GenerateFromSnapshot(installed, f.Card(), 3), "next legal series gets one extra");
            Check.Equal(3, await GenerateFromSnapshot(installed, f.Card(), 3), "only that notified modifier is consumed");
        });
    }

    // Only notify effects that actually changed this request, using the pre-OnPlay listener set.
    // The six form implementations themselves are never copied into this driver.
    private static async Task<int> GenerateFromSnapshot(IEnumerable<PowerModel> effects, CardModel card, int playCount)
    {
        var modifiers = new List<PowerModel>();
        foreach (PowerModel effect in effects.ToArray())
        {
            int next = effect.ModifyCardPlayCount(card, null, playCount);
            if (next != playCount) modifiers.Add(effect);
            playCount = next;
        }
        foreach (PowerModel effect in modifiers) await effect.AfterModifyingCardPlayCount(card);
        return playCount;
    }
}
