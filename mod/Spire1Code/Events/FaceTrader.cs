using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — Face Trader (<c>com.megacrit.cardcrawl.events.shrines.FaceTrader</c>).
/// "[Touch]" loses max(1, MaxHp / 10) HP and gains 75 Gold (50 at Ascension 15+).
/// "[Trade]" costs NOTHING and grants one random face relic — verified from the bytecode, where the
/// Trade branch calls only getRandomFace() then spawnRelicAndObtain, with no damage() and no gainGold()
/// (those appear solely in the Touch branch).
/// </summary>
public class FaceTrader : Spire1Event
{
    private const int _goldReward = 75;

    private const int _a15GoldReward = 50;

    protected override string ShippedPortrait => "relic_trader";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new GoldVar(_goldReward), new DamageVar("FaceDamage", 0m, ValueProp.Unblockable | ValueProp.Unpowered)];

    public override void CalculateVars()
    {
        // StS1: damage = max(1, maxHealth / 10); gold = 75 (50 at A15+).
        DynamicVars.Gold.BaseValue = Owner.RunState.AscensionLevel >= 15 ? _a15GoldReward : _goldReward;
        DynamicVars["FaceDamage"].BaseValue = Math.Max(1, Owner.Creature.MaxHp / 10);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Continue)
        ];
    }

    private Task Continue()
    {
        SetEventState(PageDescription("MAIN"),
        [
            Option(Touch, "MAIN"),
            Option(Trade, "MAIN"),
            Option(Leave, "MAIN")
        ]);
        return Task.CompletedTask;
    }

    /// <summary>
    /// StS1 getRandomFace(): collect the five face relics the player does not already own, in source order
    /// (CultistMask, FaceOfCleric, GremlinMask, NlothsMask, SsserpentHead); append Circlet ONLY when that
    /// list comes out empty; shuffle with <c>new Random(miscRng.randomLong())</c> and return element 0.
    /// A shuffle-then-take-first over the candidate list is a uniform draw, so <c>Rng.NextInt(count)</c>
    /// reproduces the distribution exactly — the same mapping <see cref="CursedTome.GrantRandomBook"/>
    /// already uses for the tome roll, with the event's Rng standing in for AbstractDungeon.miscRng.
    /// The advertised "50%: Good Face. 50%: Bad Face." is flavour text: the real roll is uniform over
    /// whichever faces remain unowned (two are upsides, two are downsides, and Cultist Headpiece has no
    /// mechanical effect at all).
    /// </summary>
    private async Task Trade()
    {
        List<RelicModel> faces = new(5);
        if (Owner.GetRelic<CultistMask>() == null)
        {
            faces.Add(ModelDb.Relic<CultistMask>());
        }
        if (Owner.GetRelic<FaceOfCleric>() == null)
        {
            faces.Add(ModelDb.Relic<FaceOfCleric>());
        }
        if (Owner.GetRelic<GremlinMask>() == null)
        {
            faces.Add(ModelDb.Relic<GremlinMask>());
        }
        if (Owner.GetRelic<NlothsMask>() == null)
        {
            faces.Add(ModelDb.Relic<NlothsMask>());
        }
        if (Owner.GetRelic<SsserpentHead>() == null)
        {
            faces.Add(ModelDb.Relic<SsserpentHead>());
        }
        if (faces.Count == 0)
        {
            // StS1 appends Circlet only when every face is already held; StS2 ships Circlet as its
            // stackable RelicRarity.None consolation relic, so it is reused rather than reimplemented.
            faces.Add(ModelDb.Relic<Circlet>());
        }

        await RelicCmd.Obtain(faces[Rng.NextInt(faces.Count)].ToMutable(), Owner);
        SetEventFinished(PageDescription("TRADE"));
    }

    private async Task Touch()
    {
        await PlayerCmd.GainGold(DynamicVars.Gold.IntValue, Owner);
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["FaceDamage"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);
        SetEventFinished(PageDescription("TOUCH"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("LEAVE"));
        return Task.CompletedTask;
    }
}
