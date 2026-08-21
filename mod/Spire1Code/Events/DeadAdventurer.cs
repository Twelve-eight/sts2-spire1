using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 — Dead Adventurer. A multi-stage search over a shuffled GOLD / NOTHING / RELIC queue; each
/// successful search raises the chance (starting 25%, +25% per search) that the next search wakes a
/// monster. Searching three times exhausts the corpse.
/// </summary>
public class DeadAdventurer : Spire1Event
{
    private enum RewardKind
    {
        Gold,
        Nothing,
        Relic,
    }

    private const int _goldReward = 30;

    private const int _encounterChanceStart = 25;

    private const int _a15EncounterChanceStart = 35;

    private const int _encounterChanceRamp = 25;

    private const int _maxSearches = 3;

    private static readonly string[] _enemyFlavors =
    [
        "the armor and face appear to be scoured by flames.",
        "it looks as though he's been gouged and trampled by a horned beast.",
        "he looks to have been eviscerated and chopped by giant claws.",
    ];

    // NOT readonly: AbstractModel.MutableClone uses MemberwiseClone, which shallow-copies this
    // reference, so the per-player mutable clone would otherwise share (and keep appending to) the
    // canonical event's list. DeepCloneFields below gives every clone its own list.
    private List<RewardKind> _rewards = [];

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _rewards = [];
    }

    private int _encounterChance = _encounterChanceStart;

    private int _numRewards;

    public override ActModel[] Acts => Act1;

    protected override string ShippedPortrait => "field_of_man_sized_holes";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Chance", _encounterChanceStart),
        new StringVar("Flavor"),
    ];

    public override void CalculateVars()
    {
        // StS1 shuffles [GOLD, NOTHING, RELIC] with a Random seeded from miscRng and rolls the corpse
        // type (0 = "3 Sentries", 1 = "Gremlin Nob", 2 = "Lagavulin Event").
        ((StringVar)DynamicVars["Flavor"]).StringValue = _enemyFlavors[Rng.NextInt(0, _enemyFlavors.Length)];
        _rewards.Clear();
        _rewards.Add(RewardKind.Gold);
        _rewards.Add(RewardKind.Nothing);
        _rewards.Add(RewardKind.Relic);
        Rng.Shuffle(_rewards);
        // StS1: encounterChance starts at 35 at Ascension 15+, else 25.
        _encounterChance = Owner.RunState.AscensionLevel >= 15 ? _a15EncounterChanceStart : _encounterChanceStart;
        DynamicVars["Chance"].BaseValue = _encounterChance;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Search),
            Option(Leave),
        ];
    }

    private async Task Search()
    {
        await SearchForLoot();
    }

    private async Task Continue()
    {
        await SearchForLoot();
    }

    private async Task SearchForLoot()
    {
        if (Rng.NextInt(0, 100) < _encounterChance)
        {
            // FLAGGED: in StS1 this starts an elite fight (3 Sentries / Gremlin Nob / Lagavulin) that
            // adds 25-35 gold to the rewards and ends the event. StS1 encounters are not ported yet,
            // so the ambush page only offers an escape instead of the forced fight.
            SetEventState(PageDescription("FIGHT"), [Option(Leave, "FIGHT")]);
            return;
        }

        switch (_rewards[0])
        {
            case RewardKind.Gold:
                _rewards.RemoveAt(0);
                _numRewards++;
                await PlayerCmd.GainGold(_goldReward, Owner);
                ShowNextPage("GOLD");
                break;
            case RewardKind.Nothing:
                _rewards.RemoveAt(0);
                _numRewards++;
                ShowNextPage("NOTHING");
                break;
            case RewardKind.Relic:
                _rewards.RemoveAt(0);
                _numRewards++;
                var relic = RelicFactory.PullNextRelicFromFront(Owner).ToMutable();
                await RelicCmd.Obtain(relic, Owner);
                ShowNextPage("RELIC");
                break;
        }
    }

    private void ShowNextPage(string pageKey)
    {
        if (_numRewards >= _maxSearches)
        {
            SetEventFinished(PageDescription("SUCCESS"));
            return;
        }
        _encounterChance += _encounterChanceRamp;
        DynamicVars["Chance"].BaseValue = _encounterChance;
        SetEventState(PageDescription(pageKey),
        [
            Option(Continue, pageKey),
            Option(Leave, pageKey),
        ]);
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("ESCAPE"));
    }
}
