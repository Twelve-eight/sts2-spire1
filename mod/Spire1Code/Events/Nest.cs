using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — The Nest.
/// Join the cult: lose 6 HP and receive a Ritual Dagger. Or smash and grab the donation box
/// for 99 gold (50 at Ascension 15+).
///
/// Ritual Dagger is a mod card (SPIRE1-RITUAL_DAGGER).
/// StS1 constants: HP_LOSS = 6, goldGain = 99 (50 at Ascension 15+).
/// </summary>
public class Nest : Spire1Event
{
    private const string _goldGainKey = "GoldGain";

    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "byrdoNis_nest";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar(_goldGainKey, 99)];

    public override void CalculateVars()
    {
        DynamicVars[_goldGainKey].BaseValue = Owner.RunState.AscensionLevel >= 15 ? 50 : 99;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return [Option(Continue)];
    }

    private Task Continue()
    {
        SetEventState(PageDescription("CHOICE"),
        [
            Option(StayInLine, "CHOICE").ThatDoesDamage(6),
            Option(SmashAndGrab, "CHOICE"),
        ]);
        return Task.CompletedTask;
    }

    private async Task StayInLine()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, 6,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);
        await CardPileCmd.Add(Owner.RunState.CreateCard<RitualDagger>(Owner), PileType.Deck);
        SetEventFinished(PageDescription("ACCEPT"));
    }

    private async Task SmashAndGrab()
    {
        await PlayerCmd.GainGold(DynamicVars[_goldGainKey].BaseValue, Owner);
        SetEventFinished(PageDescription("EXIT"));
    }
}
