using MegaCrit.Sts2.Core.Models.CardPools;
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
        typeof(Sts2Cards.Reboot),
        typeof(Sts2Cards.AllForOne),
        typeof(Sts2Cards.Barrage),
        typeof(Sts2Cards.Chill),
        typeof(Sts2Cards.Claw),
        typeof(Sts2Cards.CreativeAi),
        typeof(Sts2Cards.Darkness),
        typeof(Sts2Cards.Ftl),
        typeof(Sts2Cards.MultiCast),
        typeof(Sts2Cards.Tempest),          // 0E, shuffle everything back, draw 4 (+2), Exhaust
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
        typeof(Sts2Cards.TwinStrike),
        typeof(Sts2Cards.Barricade),
        typeof(Sts2Cards.BattleTrance),
        typeof(Sts2Cards.Bloodletting),
        typeof(Sts2Cards.Bludgeon),
        typeof(Sts2Cards.BurningPact),
        typeof(Sts2Cards.DarkEmbrace),
        typeof(Sts2Cards.Feed),
        typeof(Sts2Cards.FeelNoPain),
        typeof(Sts2Cards.FiendFire),
        typeof(Sts2Cards.FlameBarrier),
        typeof(Sts2Cards.Impervious),
        typeof(Sts2Cards.InfernalBlade),
        typeof(Sts2Cards.Inflame),
        typeof(Sts2Cards.Rage),
        typeof(Sts2Cards.Rupture),
        typeof(Sts2Cards.SecondWind),
        typeof(Sts2Cards.Shockwave),
        typeof(Sts2Cards.SwordBoomerang),
        typeof(Sts2Cards.TrueGrit),
        typeof(Sts2Cards.Uppercut),
        typeof(Sts2Cards.Whirlwind),     // 1E, 5 dmg twice (+2 each)
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
        typeof(Sts2Cards.Slice),
        typeof(Sts2Cards.Accuracy),
        typeof(Sts2Cards.Acrobatics),
        typeof(Sts2Cards.Adrenaline),
        typeof(Sts2Cards.Afterimage),
        typeof(Sts2Cards.Alchemize),
        typeof(Sts2Cards.Backstab),
        typeof(Sts2Cards.Blur),
        typeof(Sts2Cards.BouncingFlask),
        typeof(Sts2Cards.BulletTime),
        typeof(Sts2Cards.Burst),
        typeof(Sts2Cards.CalculatedGamble),
        typeof(Sts2Cards.Dash),
        typeof(Sts2Cards.Envenom),
        typeof(Sts2Cards.EscapePlan),
        typeof(Sts2Cards.Finisher),
        typeof(Sts2Cards.Flechettes),
        typeof(Sts2Cards.Footwork),
        typeof(Sts2Cards.InfiniteBlades),
        typeof(Sts2Cards.LegSweep),
        typeof(Sts2Cards.Malaise),
        typeof(Sts2Cards.Nightmare),
        typeof(Sts2Cards.NoxiousFumes),
        typeof(Sts2Cards.Predator),
        typeof(Sts2Cards.StormOfSteel),
        typeof(Sts2Cards.ToolsOfTheTrade),          // 0E, 6 dmg (+3)
    ];

    // (2026-08-26) PureSts1Adds 已删除：3a0de3d 起 pure 分支改走 AddOwnImplementations 全稀有度注入，
    // 本数组再无引用，属死代码。非 pure 分支的 Ironclad/Defect 孪生注入曾在其重构中被误删
    // （ROOM_FULL_OF_CHEESE 崩溃因此回归），本提交一并恢复。


    // (2026-08-26) ColorlessReuse 已删除：DarkShackles 本就在官方 ColorlessCardPool.GenerateAllCards
    // 出货，重复注入使 ConcatModelsFromMods（盲拼接无去重）后的无色奖励候选权重翻倍。

    /// <summary>
    /// Adds every reused shipped card to the matching custom pool. Call from MainFile.Initialize().
    /// PureSts1Pools=true 时跳过全部二代官方卡注入，改为注入自有 StS1 实现类。
    /// </summary>
    public static void Register()
    {
        if (Config.Spire1Config.PureSts1Pools)
        {
            // 纯一代池：角色池 = 自研实现类（一代卡面），覆盖全部稀有度。
            // 同名实现类以 [Pool(Spire1LegacyPool)] 退役，这里动态加入角色池；
            // 官方二代卡（无自研孪生的）在 pure 模式下不注入——缺失由 RewardClampPatch 钳制兜底。
            // 历史教训（2026-08-25）：此前 pure 分支只注入 Common（10 张），稀有度带宽=0，
            // 摇中 Uncommon/Rare 时候选为空，DingyRug 把无色池并入后奖励全部无色。
            AddOwnImplementations(typeof(Spire1CardPool), IroncladReuse);
            AddOwnImplementations(typeof(SilentCardPool), SilentReuse);
            AddOwnImplementations(typeof(DefectCardPool), DefectReuse);
            return;
        }
        foreach (var cardType in IroncladReuse) ModHelper.AddModelToPool(typeof(Spire1CardPool), cardType);
        foreach (var cardType in DefectReuse) ModHelper.AddModelToPool(typeof(DefectCardPool), cardType);
        foreach (var cardType in SilentReuse) ModHelper.AddModelToPool(typeof(SilentCardPool), cardType);

        LogPoolCensus("ColorlessCardPool", typeof(ColorlessCardPool));
        LogPoolCensus("Spire1CardPool", typeof(Spire1CardPool));
        LogPoolCensus("SilentCardPool", typeof(SilentCardPool));
        LogPoolCensus("DefectCardPool", typeof(DefectCardPool));
    }

    /// <summary>终态直证：打印三池最终成员的稀有度分布，用于核对复用注入是否生效。</summary>
    private static void LogPoolCensus(string name, System.Type poolType)
    {
        try
        {
            var pool = (MegaCrit.Sts2.Core.Models.CardPoolModel)typeof(MegaCrit.Sts2.Core.Models.ModelDb)
                .GetMethod("CardPool", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                ?.MakeGenericMethod(poolType)
                ?.Invoke(null, null);
            if (pool == null) { MainFile.Logger.Error($"[Spire1] PoolCensus {name}: CardPool<T> returned null"); return; }
            var hist = new System.Collections.Generic.SortedDictionary<string, int>();
            int total = 0;
            foreach (var c in pool.AllCards)
            {
                hist[c.Rarity.ToString()] = hist.GetValueOrDefault(c.Rarity.ToString()) + 1;
                total++;
            }
            var parts = new List<string>();
            foreach (var kv in hist) parts.Add(kv.Key + "=" + kv.Value);
            MainFile.Logger.Info($"[Spire1] PoolCensus {name}: total={total} ({string.Join(", ", parts)})");
        }
        catch (System.Exception e)
        {
            MainFile.Logger.Error($"[Spire1] PoolCensus {name} failed: {e.Message}");
        }
    }

    /// <summary>pure 模式：对每个官方孪生条目，若存在同名自研实现类则注入角色池。</summary>
    private static void AddOwnImplementations(System.Type pool, System.Type[] twins)
    {
        foreach (var twin in twins)
        {
            var own = ResolveOwnImplementation(twin);
            if (own != null)
            {
                ModHelper.AddModelToPool(pool, own);
            }
        }
    }

    private static System.Type? ResolveOwnImplementation(System.Type twin)
    {
        // 官方类 Sts2Cards.X → 自研类 Spire1.Spire1Code.Cards.X
        var name = twin.Name;
        var own = typeof(SharedCardReuse).Assembly.GetType("Spire1.Spire1Code.Cards." + name);
        return own != null && own.BaseType?.Name == "Spire1Card" ? own : null;
    }
}
