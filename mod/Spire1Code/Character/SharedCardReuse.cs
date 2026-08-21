using MegaCrit.Sts2.Core.Modding;
using Sts2Cards = MegaCrit.Sts2.Core.Models.Cards;

namespace Spire1.Spire1Code.Character;

/// <summary>
/// LEAN-CODE RULE (see DEVELOP.md 7a): when a StS1 card's mechanic AND numbers are identical to the
/// card StS2 already ships under the same name, we do NOT define a duplicate class. Instead the
/// shipped card is added to our character's pool through <see cref="ModHelper.AddModelToPool"/>,
/// which resolves the canonical instance via ModelDb and concatenates it into the pool
/// (MegaCrit.Sts2.Core.Modding.ModHelper.ConcatModelsFromMods).
///
/// This MUST run before the game finishes initializing: ModHelper freezes each pool's modded content
/// the first time the pool is generated and then throws on further additions.
///
/// Every entry below was verified field by field (cost, base values, upgrade deltas, keywords, target
/// and effect) against the decompiled shipped card. Cards whose shipped version differs in any field
/// keep our own vanilla-faithful class instead and are absent from these lists.
/// </summary>
internal static class SharedCardReuse
{
    /// <summary>Shipped StS2 cards that are identical to their StS1 Defect counterparts.</summary>
    private static readonly System.Type[] DefectReuse =
    [
        // Commons
        typeof(Sts2Cards.BallLightning),   // 1E, 7 dmg (+3), channel 1 Lightning
        typeof(Sts2Cards.BeamCell),        // 0E, 3 dmg (+1), 1 Vulnerable (+1)
        typeof(Sts2Cards.ColdSnap),        // 1E, 6 dmg (+3), channel 1 Frost
        typeof(Sts2Cards.CompileDriver),   // 1E, 7 dmg (+3), draw per unique orb
        typeof(Sts2Cards.Coolheaded),      // 1E, channel 1 Frost, draw 1 (+1)
        typeof(Sts2Cards.GoForTheEyes),    // 0E, 3 dmg (+1), Weak if attacking (+1)
        typeof(Sts2Cards.Hologram),        // 1E, 3 Block (+2), return a discarded card, Exhaust
        typeof(Sts2Cards.Leap),            // 1E, 9 Block (+3)
        typeof(Sts2Cards.SweepingBeam),    // 1E, 6 dmg (+3) AoE, draw 1
        typeof(Sts2Cards.Turbo),           // 0E, 2 Energy (+1), add a Void to discard
        // Uncommons
        typeof(Sts2Cards.BootSequence),    // 0E, 10 Block (+3), Innate + Exhaust
        typeof(Sts2Cards.Capacitor),       // 1E, +2 orb slots (+1)
        typeof(Sts2Cards.Chaos),           // 1E, channel 1 random orb (+1)
        typeof(Sts2Cards.DoubleEnergy),    // 1E->0E, double current Energy, Exhaust
        typeof(Sts2Cards.Equilibrium),     // 2E, 13 Block (+3), retain hand
        typeof(Sts2Cards.Loop),            // 1E, trigger next orb passive at turn start (+1)
        typeof(Sts2Cards.Overclock),       // 0E, draw 2 (+1), add a Burn to discard
        typeof(Sts2Cards.Scrape),          // 1E, 7 dmg (+3), draw 4 (+1), discard non-zero-cost draws
        typeof(Sts2Cards.Skim),            // 1E, draw 3 (+1)
        typeof(Sts2Cards.WhiteNoise),      // 1E->0E, add a random Power costing 0 this turn, Exhaust
        // Rares
        typeof(Sts2Cards.Buffer),          // 2E, 1 Buffer (+1)
        typeof(Sts2Cards.EchoForm),        // 3E, Ethereal (removed on upgrade), first card played twice
        typeof(Sts2Cards.MachineLearning), // 1E, draw 1 extra each turn, Innate on upgrade
        typeof(Sts2Cards.MeteorStrike),    // 5E, 24 dmg (+6), channel 3 Plasma
        typeof(Sts2Cards.Rainbow),         // 2E, channel Lightning + Frost + Dark, Exhaust
        typeof(Sts2Cards.Reboot),          // 0E, shuffle everything back, draw 4 (+2), Exhaust
    ];

    /// <summary>
    /// Adds every reused shipped card to the matching custom pool. Call from MainFile.Initialize().
    /// </summary>
    public static void Register()
    {
        foreach (var cardType in DefectReuse)
        {
            ModHelper.AddModelToPool(typeof(DefectCardPool), cardType);
        }
    }
}
