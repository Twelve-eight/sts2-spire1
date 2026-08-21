using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Relics;
// This mod ships its own Doubt curse (Spire1.Spire1Code.Cards.Doubt) and StS2 ships one too, so
// importing MegaCrit.Sts2.Core.Models.Cards wholesale would make `Doubt` ambiguous. Alias the single
// shipped card this event reuses instead.
using Normality = MegaCrit.Sts2.Core.Models.Cards.Normality;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 Beyond event — Mind Bloom (<c>com.megacrit.cardcrawl.events.beyond.MindBloom</c>).
/// <para>
/// Three of StS1's four options are ported, in StS1's own order. "[I am Awake]" upgrades every
/// upgradable card in the deck and grants <see cref="MarkOfTheBloom"/>. The third slot is then split
/// by StS1's <c>floorNum % 50 &lt;= 40</c> test: "[I am Rich]" gains 999 gold and adds 2 Normality
/// curses (the curse StS2 already ships), otherwise "[I am Healthy]" heals to full HP and adds a
/// Doubt curse.
/// </para>
/// <para>
/// FLAG: "[I am War]" is omitted — see <see cref="GenerateInitialOptions"/> for the encounter it
/// picks and why it is blocked.
/// </para>
/// </summary>
public class MindBloom : Spire1Event
{
    // StS1 constants, all read off MindBloom.buttonEffect: the Rich branch pushes sipush 999 into
    // both logMetric and gainGold, constructs `new Normality()` exactly twice, and the Rich/Healthy
    // split is `floorNum % 50 <= 40` (bipush 50 / irem / bipush 40 / if_icmpgt).
    private const int _richGold = 999;
    private const int _normalityCount = 2;
    private const int _floorCycle = 50;
    private const int _richFloorMax = 40;

    protected override string ShippedPortrait => "aroma_of_chaos";

    public override ActModel[] Acts => Act3;

    // StS1 evaluates this test twice — once in the constructor to choose the third option's text and
    // again in buttonEffect to choose its effect — but floorNum cannot change while the event is
    // open, so a single test at option-generation time is equivalent.
    private bool IsRichFloor => Owner.RunState.TotalFloor % _floorCycle <= _richFloorMax;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // StS1 intro order: OPTIONS[0] "[I am War]", OPTIONS[3] "[I am Awake]", then OPTIONS[1]
        // "[I am Rich]" or OPTIONS[2] "[I am Healthy]" selected by IsRichFloor. StS1 attaches a card
        // preview to that third option (setDialogOption(text, CardLibrary.getCopy("Normality"/"Doubt"))),
        // which is what the hover tips below reproduce; "[I am Awake]" gets no preview in StS1.
        //
        // FLAG: "[I am War]" is omitted. StS1 builds the list ["The Guardian", "Hexaghost",
        // "Slime Boss"], shuffles it with `new Random(miscRng.randomLong())` and fights element 0 —
        // i.e. a uniformly random one of the three Act-1 bosses — after clearing the room rewards and
        // replacing them with 50 gold (25 at Ascension 13+) plus a RARE relic reward. This is blocked
        // on unported StS1 monster encounters, NOT on a missing StS2 API: MonsterHelper.getEncounter
        // has no counterpart here because no MonsterGroup/EncounterModel exists for The Guardian,
        // Hexaghost or Slime Boss in this mod.
        return
        [
            Option(IAmAwake),
            IsRichFloor
                ? Option(IAmRich, HoverTipFactory.FromCardWithCardHoverTips<Normality>())
                : Option(IAmHealthy, HoverTipFactory.FromCardWithCardHoverTips<Doubt>()),
        ];
    }

    private async Task IAmAwake()
    {
        // StS1: iterate the master deck and upgrade every card whose canUpgrade() is true (the first
        // 20 additionally play a ShowCardBriefly + UpgradeShine VFX, which CardPreviewStyle.EventLayout
        // covers here), then spawnRelicAndObtain(RelicLibrary.getRelic("Mark of the Bloom")).
        //
        // There is no Circlet fallback in this event: AbstractRoom.spawnRelicAndObtain only
        // special-cases a relic whose own id IS "Circlet" (it bumps the owned Circlet's counter and
        // flashes it); for any other relic it spawns and obtains unconditionally. So StS1 hands over
        // Mark of the Bloom every time, and this branch does the same.
        var upgradable = Owner.Deck.Cards.Where(c => c.IsUpgradable).ToList();
        CardCmd.Upgrade(upgradable, CardPreviewStyle.EventLayout);
        await RelicCmd.Obtain<MarkOfTheBloom>(Owner);
        SetEventFinished(PageDescription("AWAKE"));
    }

    private async Task IAmRich()
    {
        // StS1: gainGold(999), then two separately constructed `new Normality()` instances shown
        // simultaneously (two ShowCardAndObtainEffect at 0.6*WIDTH and 0.3*WIDTH). AddCursesToDeck
        // calls RunState.CreateCard once per element, so passing the model twice produces two distinct
        // cards and one combined preview — the faithful match for StS1's pair of effects.
        //
        // Normality is not reimplemented: StS2 ships an identical one
        // (MegaCrit.Sts2.Core.Models.Cards.Normality — cost -1 Curse, Unplayable, MaxUpgradeLevel 0,
        // the "cannot play more than 3 cards per turn" lock enforced through ShouldPlay) and it is
        // already registered in CurseCardPool, so per the lean-code rule we grant the shipped card.
        await PlayerCmd.GainGold(_richGold, Owner);
        await CardPileCmd.AddCursesToDeck(
            Enumerable.Repeat(ModelDb.Card<Normality>(), _normalityCount), Owner);
        SetEventFinished(PageDescription("RICH"));
    }

    private async Task IAmHealthy()
    {
        // StS1: player.heal(player.maxHealth) — a heal whose amount is the full max HP, which
        // SetCurrentHpInternal clamps, so it always ends at full HP — then obtain one Doubt curse.
        await CreatureCmd.Heal(Owner.Creature, Owner.Creature.MaxHp);
        await CardPileCmd.AddCurseToDeck<Doubt>(Owner);
        SetEventFinished(PageDescription("HEALTHY"));
    }
}
