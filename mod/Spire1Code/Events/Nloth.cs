using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — N'loth. The entire event is a relic swap: offer one of two randomly chosen relics
/// you own and receive N'loth's Gift (or a Circlet if you already own N'loth's Gift).
///
/// FLAG: the blocker is N'loth's Gift alone — Circlet is available (StS2 ships it, and it is the
/// engine's own fallback relic). N'loth's Gift ("triple the chance of finding Rare cards from combat
/// rewards") has NO expressible implementation: the rarity roll lives in CardFactory.RollForRarity,
/// the probabilities are consts/statics in MegaCrit.Sts2.Core.Odds/CardRarityOdds.cs, the only
/// mutable input is the per-player pity offset that Roll then overwrites, and
/// ModifyCardRewardCreationOptions can swap pools/filters/flags/odds-preset/rng but never a
/// probability multiplier. BaseLib adds no rarity hook either. So both offer options stay withheld.
/// </summary>
public class Nloth : Spire1Event
{
    protected override string ShippedPortrait => "welcome_to_wongos";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // FLAGGED: StS1 offers "[Offer: <relic name>] Lose this relic. Obtain a special relic." for
        // two random owned relics, granting NlothsGift (or Circlet when NlothsGift is already owned).
        // NlothsGift is not implementable (see the class doc), so the offers are not shown.
        return
        [
            Option(Leave)
        ];
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
