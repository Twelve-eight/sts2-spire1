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

    /// <summary>Shipped StS2 cards identical to their StS1 Ironclad counterparts (A-group in
    /// .tmp/duplicate-cards-report.md). Required: ROOM_FULL_OF_CHEESE Gorge demands 8 distinct
    /// Commons from the character pool alone, and our own Ironclad commons number only 6.</summary>
    private static readonly System.Type[] IroncladReuse =
    [
        // Commons
        typeof(Sts2Cards.Anger),          // 0E, 6 dmg (+3), add a copy to discard
        typeof(Sts2Cards.Armaments),      // 1E, 5 block, upgrade a card in hand (+ all)
        typeof(Sts2Cards.BodySlam),       // 1E, dmg = current Block (+ cost 0)
        typeof(Sts2Cards.Havoc),          // 1E, play top card of draw pile, Exhaust (+ 0E)
        typeof(Sts2Cards.Headbutt),       // 1E, 9 dmg (+2), place discard card on draw top
        typeof(Sts2Cards.IronWave),       // 1E, 5 dmg & 5 block (+3 each)
        typeof(Sts2Cards.PommelStrike),   // 1E, 9 dmg (+2), draw 1 (+1)
        typeof(Sts2Cards.ShrugItOff),     // 1E, 8 block (+3), draw 1
        typeof(Sts2Cards.Thunderclap),    // 1E, 4 dmg (+3) & 1 Vulnerable to ALL
        typeof(Sts2Cards.TwinStrike),     // 1E, 5 dmg twice (+2 each)
    ];

    /// <summary>Shipped StS2 cards identical to their StS1 Silent counterparts (same A-group;
    /// same 8-Common contract — our own Silent commons also number only 6).</summary>
    private static readonly System.Type[] SilentReuse =
    [
        // Commons
        typeof(Sts2Cards.Backflip),       // 1E, 5 block (+3), draw 2
        // BladeDance EXCLUDED (re-verify 2026-08-24): shipped version self-exhausts
        // (CanonicalKeywords => [Exhaust]) while StS1's does not (jar: zero exhaust) —
        // B-group drift; our own Cards/BladeDance.cs serves in SilentCardPool instead.
        typeof(Sts2Cards.CloakAndDagger), // 1E, 6 block, add 1 Shiv (+1)
        typeof(Sts2Cards.DaggerSpray),    // 1E, 4 dmg to ALL, twice
        typeof(Sts2Cards.DaggerThrow),    // 1E, 9 dmg, draw 1, discard 1
        typeof(Sts2Cards.DeadlyPoison),   // 1E, apply 5 Poison (+2) — NO exhaust either side
        typeof(Sts2Cards.Deflect),        // 0E, 4 block (+3) — jar-arbitrated
        typeof(Sts2Cards.DodgeAndRoll),   // 1E, 4 block (+2), gain equal block next turn
        typeof(Sts2Cards.PiercingWail),   // 1E, enemies lose 6 Str this turn (+2), Exhaust
        typeof(Sts2Cards.Prepared),       // 0E, draw 1 discard 1 (+2/+2)
        typeof(Sts2Cards.Slice),          // 0E, 6 dmg (+3)
    ];

    /// <summary>
    /// Adds every reused shipped card to the matching custom pool. Call from MainFile.Initialize().
    /// </summary>
    public static void Register()
    {
        foreach (var cardType in DefectReuse) ModHelper.AddModelToPool(typeof(DefectCardPool), cardType);
        foreach (var cardType in IroncladReuse) ModHelper.AddModelToPool(typeof(Spire1CardPool), cardType);
        foreach (var cardType in SilentReuse) ModHelper.AddModelToPool(typeof(SilentCardPool), cardType);
    }
}
