using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
// Explicit alias: MegaCrit.Sts2.Core.Models.Cards is imported for Writhe, so an unqualified `Madness`
// would be ambiguous the moment StS2 ever ships one. Bind it to our card.
using Madness = Spire1.Spire1Code.Cards.Madness;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 Beyond event — Winding Halls. "Embrace Madness" loses 12.5% max HP and grants 2 Madness cards;
/// "Focus" heals 25% max HP and adds a Writhe curse (Writhe is the card StS2 already ships, reused);
/// "Retrace Your Steps" loses 5% max HP. The Ascension 15+ variants (18% HP loss,
/// 20% heal) are not applied because StS2's ascension levels do not map 1:1 onto StS1's ladder.
/// </summary>
public class WindingHalls : Spire1Event
{
    /// <summary>StS1 queues exactly two ShowCardAndObtainEffect(new Madness()) calls.</summary>
    private const int _madnessCount = 2;

    protected override string ShippedPortrait => "jungle_maze_adventure";

    public override ActModel[] Acts => Act3;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HpLoss", 0m),
        new DynamicVar("HealAmt", 0m),
        new DynamicVar("MaxHpLoss", 0m),
    ];

    public override void CalculateVars()
    {
        // StS1: hpAmt = round(maxHp * 0.125), healAmt = round(maxHp * 0.25), maxHPAmt = round(maxHp * 0.05).
        DynamicVars["HpLoss"].BaseValue = Math.Round(Owner.Creature.MaxHp * 0.125m, MidpointRounding.AwayFromZero);
        DynamicVars["HealAmt"].BaseValue = Math.Round(Owner.Creature.MaxHp * 0.25m, MidpointRounding.AwayFromZero);
        DynamicVars["MaxHpLoss"].BaseValue = Math.Round(Owner.Creature.MaxHp * 0.05m, MidpointRounding.AwayFromZero);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // StS1 INTRO: "..." only.
        return [Option(Proceed)];
    }

    private async Task Proceed()
    {
        SetEventState(PageDescription("CHOICE"), new List<EventOption>
        {
            Option(EmbraceMadness, "CHOICE").ThatDoesDamage(DynamicVars["HpLoss"].BaseValue),
            Option(Focus, "CHOICE"),
            Option(RetraceYourSteps, "CHOICE"),
        });
    }

    private async Task EmbraceMadness()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["HpLoss"].BaseValue, ValueProp.Unblockable | ValueProp.Unpowered, null, null);

        // StS1 queues two independent `new ShowCardAndObtainEffect(new Madness(), x, y)` calls — the ±350*xScale
        // offsets are only the two on-screen card positions, so the mechanical effect is exactly 2 Madness into
        // the master deck. Madness is a Skill, so CardPileCmd.AddCursesToDeck cannot be used: it throws
        // ArgumentException for any non-Curse (CardPileCmd.cs:1262-1265). The deck-add primitive it wraps is used
        // instead, matching Necronomicurse.cs:100.
        List<CardPileAddResult> added = new(_madnessCount);
        for (int i = 0; i < _madnessCount; i++)
        {
            added.Add(await CardPileCmd.Add(Owner.RunState.CreateCard<Madness>(Owner), PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd(added, 2f);

        SetEventFinished(PageDescription("MADNESS"));
    }

    private async Task Focus()
    {
        // Writhe is not reimplemented: StS2 ships an identical one (cost -1 Curse, Innate + Unplayable,
        // MaxUpgradeLevel 0) already registered in CurseCardPool, so the shipped card is granted.
        await CreatureCmd.Heal(Owner.Creature, DynamicVars["HealAmt"].BaseValue);
        await CardPileCmd.AddCurseToDeck<Writhe>(Owner);
        SetEventFinished(PageDescription("FOCUS"));
    }

    private async Task RetraceYourSteps()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["MaxHpLoss"].BaseValue, isFromCard: false);
        SetEventFinished(PageDescription("RETRACE"));
    }
}
