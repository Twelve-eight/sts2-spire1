using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Acts;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — The Joust.
/// Bet 50 gold on the murderer (70%: win 100 gold) or on the knight (30%: win 250 gold).
/// The joust itself is flavor — no combat is started.
/// StS1 constants: WIN_OWNER = 250, WIN_MURDERER = 100, BET_AMT = 50, ownerWins = randomBoolean(0.3f).
/// </summary>
public class TheJoust : Spire1Event
{
    private const int _betAmount = 50;

    private const int _murdererReward = 100;

    private const int _ownerReward = 250;

    private bool _betForOwner;

    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "battleworn_dummy";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Continue)];
    }

    private Task Continue()
    {
        SetEventState(PageDescription("EXPLANATION"),
        [
            Option(BetOnMurderer, "EXPLANATION"),
            Option(BetOnOwner, "EXPLANATION"),
        ]);
        return Task.CompletedTask;
    }

    private async Task BetOnMurderer()
    {
        _betForOwner = false;
        await PlayerCmd.LoseGold(_betAmount, Owner, GoldLossType.Spent);
        SetEventState(PageDescription("BET_AGAINST"), [Option(Watch, "BET_AGAINST")]);
    }

    private async Task BetOnOwner()
    {
        _betForOwner = true;
        await PlayerCmd.LoseGold(_betAmount, Owner, GoldLossType.Spent);
        SetEventState(PageDescription("BET_FOR"), [Option(Watch, "BET_FOR")]);
    }

    private Task Watch()
    {
        // StS1: the joust animation plays here, then the result is shown.
        SetEventState(PageDescription("COMBAT"), [Option(Resolve, "COMBAT")]);
        return Task.CompletedTask;
    }

    private async Task Resolve()
    {
        // StS1: ownerWins = Random.randomBoolean(0.3f).
        bool ownerWins = Rng.NextFloat() < 0.3f;
        if (ownerWins)
        {
            // "The nemesis was slain." + bet outcome.
            if (_betForOwner)
            {
                await PlayerCmd.GainGold(_ownerReward, Owner);
                SetEventFinished(PageDescription("NEMESIS_SLAIN_BET_WON"));
            }
            else
            {
                SetEventFinished(PageDescription("NEMESIS_SLAIN_BET_LOST"));
            }
        }
        else if (_betForOwner)
        {
            // "The owner died." + bet outcome.
            SetEventFinished(PageDescription("OWNER_DIED_BET_LOST"));
        }
        else
        {
            await PlayerCmd.GainGold(_murdererReward, Owner);
            SetEventFinished(PageDescription("OWNER_DIED_BET_WON"));
        }
    }
}
