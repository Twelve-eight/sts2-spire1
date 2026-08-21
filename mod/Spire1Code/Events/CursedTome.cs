using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 The City — Cursed Tome.
/// Reading the tome costs escalating HP (1, 2, 3, then 3 more to stop, or 10/15 to take the book).
/// StS1 constants: DMG_BOOK_OPEN=1, DMG_SECOND_PAGE=2, DMG_THIRD_PAGE=3, DMG_STOP_READING=3,
/// DMG_OBTAIN_BOOK=10 (15 at Ascension 15+).
///
/// Taking the book grants one of StS1's three tome relics — Necronomicon, Enchiridion or
/// Nilry's Codex — drawn from the ones the player does not already hold, with Circlet as the
/// fallback once all three are held. See <see cref="GrantRandomBook"/>.
/// </summary>
public class CursedTome : Spire1Event
{
    private const string _finalDmgKey = "FinalDmg";

    public override ActModel[] Acts => Act2;

    protected override string ShippedPortrait => "self_help_book";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar(_finalDmgKey, 10)];

    public override void CalculateVars()
    {
        // StS1: finalDmg = 15 at Ascension 15+, else 10.
        DynamicVars[_finalDmgKey].BaseValue = Owner.RunState.AscensionLevel >= 15 ? 15 : 10;
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            Option(Read),
            Option(Leave),
        ];
    }

    private async Task Read()
    {
        SetEventState(PageDescription("PAGE_1"), [Option(ContinuePage1, "PAGE_1").ThatDoesDamage(1)]);
        await Task.CompletedTask;
    }

    private async Task ContinuePage1()
    {
        await LoseHp(1);
        SetEventState(PageDescription("PAGE_2"), [Option(ContinuePage2, "PAGE_2").ThatDoesDamage(2)]);
    }

    private async Task ContinuePage2()
    {
        await LoseHp(2);
        SetEventState(PageDescription("PAGE_3"), [Option(ContinuePage3, "PAGE_3").ThatDoesDamage(3)]);
    }

    private async Task ContinuePage3()
    {
        await LoseHp(3);
        SetEventState(PageDescription("LAST_PAGE"),
        [
            Option(Take, "LAST_PAGE").ThatDoesDamage(DynamicVars[_finalDmgKey].BaseValue),
            Option(Stop, "LAST_PAGE").ThatDoesDamage(3),
        ]);
    }

    private async Task Take()
    {
        await LoseHp(DynamicVars[_finalDmgKey].BaseValue);
        await GrantRandomBook();
        SetEventFinished(PageDescription("OBTAIN"));
    }

    private async Task Stop()
    {
        await LoseHp(3);
        SetEventFinished(PageDescription("STOP"));
    }

    private Task Leave()
    {
        SetEventFinished(PageDescription("IGNORE"));
        return Task.CompletedTask;
    }

    /// <summary>
    /// StS1 <c>CursedTome.randomBook()</c>: build the list of tome relics the player does not already
    /// own, tested in the fixed order Necronomicon, Enchiridion, Nilry's Codex; if all three are held the
    /// list instead receives a single Circlet; then hand over
    /// <c>list.get(miscRng.random(list.size() - 1))</c>. StS1 delivers the pick by clearing the room's
    /// reward list, adding just that relic and opening the reward screen, so with a single entry it is a
    /// plain hand-over.
    /// </summary>
    private async Task GrantRandomBook()
    {
        // Three candidates at most, so the list is sized once instead of growing.
        List<RelicModel> books = new(3);
        if (Owner.GetRelic<Necronomicon>() == null)
        {
            books.Add(ModelDb.Relic<Necronomicon>());
        }
        if (Owner.GetRelic<Enchiridion>() == null)
        {
            books.Add(ModelDb.Relic<Enchiridion>());
        }
        if (Owner.GetRelic<NilrysCodex>() == null)
        {
            books.Add(ModelDb.Relic<NilrysCodex>());
        }
        if (books.Count == 0)
        {
            // StS1's "you already own every reward relic" filler. Not reimplemented: StS2 ships it
            // (MegaCrit.Sts2.Core.Models.Relics/Circlet.cs — RelicRarity.None, IsStackable), which is
            // exactly StS1's Circlet, so per the lean-code rule the shipped relic is granted.
            books.Add(ModelDb.Relic<Circlet>());
        }

        // StS1's Random.random(int i) is nextInt(i + 1) — an inclusive bound — so random(size - 1) is a
        // uniform index over the whole list: one random pick, never a shuffle. The event's own Rng stands
        // in for AbstractDungeon.miscRng, the RNG randomBook() draws from. The non-generic Obtain is used
        // because the pick is only known at runtime; RelicCmd.Obtain<T>(player) expands to exactly this
        // call (RelicCmd.cs:24).
        await RelicCmd.Obtain(books[Rng.NextInt(books.Count)].ToMutable(), Owner);
    }

    private async Task LoseHp(decimal amount)
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, amount,
            ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);
    }
}
