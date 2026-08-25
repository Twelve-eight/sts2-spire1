using System.Collections.Generic;
using System.Threading.Tasks;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — N'loth. The entire event is a relic swap: offer one of two randomly chosen relics
/// you own and receive N'loth's Gift (or a Circlet if you already own N'loth's Gift).
///
/// FLAG UPDATE (2026-08-25): N'loth's Gift IS implementable after all. The rarity roll is
/// `Roll(type) = f(GetBaseOdds(type,Rare)+pity, ...)` (dllsrc CardRarityOdds.cs L71/L96-110).
/// A Harmony postfix on GetBaseOdds — ×3 when rarity==Rare, type==CombatReward, and the rolling
/// player owns N'loth's Gift — triples the rare band while the vanilla pity progression
/// (L74/L78) keeps running untouched. Remaining work: resolve the odds-instance→player link
/// for per-owner gating in multiplayer, then un-withhold both offer options.
///
/// FLAG (still open): Circlet fallback is available (StS2 ships it); N'loth's Gift art/assets
/// need porting alongside the odds hook.
/// </summary>
public class Nloth : Spire1Event
{
    protected override string ShippedPortrait => "welcome_to_wongos";

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // FLAGGED: StS1 offers "[Offer: <relic name>] Lose this relic. Obtain a special relic." for
        // Offers withheld pending the GetBaseOdds ×3 odds hook (see class doc FLAG UPDATE).
        // StS1 form: "[Offer: <relic name>] Lose this relic. Obtain a special relic." ×2 random
        // owned relics → NlothsGift (or Circlet if already owned).
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
