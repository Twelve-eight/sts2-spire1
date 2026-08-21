using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Cards;
using System.Linq;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Necronomicon (Event; one of the three mutually exclusive Cursed Tome rewards).
/// The first Attack played each turn that costs 2 or more is played twice.
/// </summary>
/// <remarks>
/// FLAG: one clause of StS1's Necronomicon is not reproduced, stated precisely at its implementation site
/// FLAG: below: StS1's freeToPlayOnce skip, which is invisible to the StS2 hook that decides replays.
/// FLAG: Nothing else about the relic is approximated.
/// </remarks>
public class Necronomicon : Spire1Relic
{
    private bool _usedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Event;

    /// <summary>StS1 <c>Necronomicon.COST_THRESHOLD = 2</c>.</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];

    // StS1 ships TWO descriptions for this relic: the pickup-screen text, which ends with "Upon pickup,
    // obtain a special #rCurse.", and the shorter text that onEquip() writes over it once the relic is
    // actually held ("The first #yAttack played each turn that costs #b2 or more is played twice."). A StS2
    // relic has exactly one description and it is always the equipped one, so the equipped form is the
    // faithful choice; the pickup sentence belongs to the event's reward preview, not to the relic.
    // StS1's #y/#b/#r colour codes are not StS2 markup and are stripped (DEVLOG.md:287).
    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Necronomicon",
            "#The first Attack played each turn that costs 2 or more is played twice.",
            "Only a fool would try and harness this evil power. At night your dreams are haunted by images of the book devouring your mind.");

    // StS1's onEquip() grants a Necronomicurse and onUnequip() removes it again. Both StS2 hooks exist
    // (RelicModel.AfterObtained() at RelicModel.cs:546, RelicModel.AfterRemoved() at RelicModel.cs:551),
    // and the curse itself now exists as mod/Spire1Code/Cards/Necronomicurse.cs, so the clause is
    // implemented rather than flagged. The curse's own two return paths are gated on still holding this
    // relic, exactly as StS1 gates them on hasRelic("Necronomicon") — which is what makes removing the
    // relic the intended escape.
    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await CardPileCmd.AddCurseToDeck<Necronomicurse>(Owner);
    }

    // StS1 onUnequip() removes every Necronomicurse from the master deck. Melting the relic must therefore
    // free the player; the curse stops resurrecting itself the moment GetRelic<Necronomicon>() returns null.
    public override async Task AfterRemoved()
    {
        await base.AfterRemoved();

        foreach (CardModel curse in PileType.Deck.GetPile(Owner).Cards.Where(c => c is Necronomicurse).ToList())
        {
            await CardPileCmd.RemoveFromDeck(curse);
        }
    }

    private bool UsedThisTurn
    {
        get => _usedThisTurn;
        set
        {
            AssertMutable();
            _usedThisTurn = value;
        }
    }

    // StS1 onUseCard(card, action) fires for CardType.ATTACK when
    //     (costForTurn >= 2 && !freeToPlayOnce) || (cost == -1 && energyOnUse >= 2)
    // and then replays the card once. StS2 expresses the replay as the
    // ModifyCardPlayCount / AfterModifyingCardPlayCount pair; ThrowingAxe.cs:38-57 is the shipped relic
    // using exactly that pair, and BurstPower.cs:25-28 is the shipped card-type filter.
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        if (UsedThisTurn)
            return playCount;

        // ThrowingAxe.cs:44 guards ownership the same way; ModifyCardPlayCount is a combat-wide hook
        // (dispatched over Hook.IterateCombatHookListeners, Hook.cs:1388), so in multiplayer it is also
        // invoked for cards played by other players.
        if (card.Owner != Owner)
            return playCount;

        if (card.Type != CardType.Attack)
            return playCount;

        // EnergyCost.GetResolved() (CardEnergyCost.cs:155-162) collapses BOTH of StS1's cost branches into
        // one test, which is why there is no X-cost special case here:
        //  * For a normal card it returns Max(0, GetWithModifiers(CostModifiers.All)) — the current cost
        //    including every modifier — matching StS1's `costForTurn`, so cost reductions count.
        //  * For an X-cost card it returns CapturedXValue, the energy actually spent, which is exactly
        //    StS1's `energyOnUse >= 2` branch for `cost == -1`.
        // GetWithModifiers(CostModifiers.All) must NOT be used on its own: it returns the raw _base early
        // when CostsX (CardEnergyCost.cs:105-108), i.e. 0 for every X-cost card (Canonical is forced to 0
        // for those, CardEnergyCost.cs:86), so X-cost Attacks would never qualify.
        // The value is live at this point. Manual play: PlayCardAction.cs:92 awaits SpendResources() — which
        // sets CapturedXValue in SpendEnergy (CardModel.cs:1824-1827) — before PlayCardAction.cs:103 calls
        // OnPlayWrapper, and OnPlayWrapper only reaches GeneratePlayCount at CardModel.cs:1887. Auto play:
        // CardCmd.cs:99-102 captures X before CardCmd.cs:130 calls OnPlayWrapper. In both paths the
        // when-played cost modifiers are not cleared until CardModel.cs:2007, well after the hook.
        // IntimidatingHelmet is the shipped consumer of GetResolved() for this same "cost of the card that
        // was just played" question (CardEnergyCost.cs:151).
        if (card.EnergyCost.GetResolved() < DynamicVars.Energy.IntValue)
            return playCount;

        // FLAG: KNOWN INEXACT — StS1 additionally requires !freeToPlayOnce, and this port cannot test it.
        // FLAG: ModifyCardPlayCount receives only (card, target, playCount) (AbstractModel.cs:1495); it gets
        // FLAG: no CardPlay and no ResourceInfo. The value that would answer the question,
        // FLAG: CardPlay.Resources.EnergySpent, only exists from BeforeCardPlayed onward, and that runs at
        // FLAG: CardModel.cs:1926 — AFTER GeneratePlayCount at CardModel.cs:1887 — so it is not reachable
        // FLAG: from here even indirectly.
        // FLAG: StS2's analogue of freeToPlayOnce is auto-play, which spends nothing yet leaves the cost
        // FLAG: untouched: CardCmd.AutoPlay never calls SpendResources and hardcodes EnergySpent = 0
        // FLAG: (CardCmd.cs:123-130), whereas manual play reports the real spend (PlayCardAction.cs:92-101).
        // FLAG: CONSEQUENCE: an auto-played, energy-free Attack costing 2 or more triggers Necronomicon in
        // FLAG: this port, where StS1 would skip it and save the charge for the next qualifying Attack.
        // FLAG: Cards whose cost was REDUCED to 0 are unaffected and stay faithful, because a reduction is a
        // FLAG: real cost modifier (SetToFreeThisTurn -> EnergyCost.SetThisTurnOrUntilPlayed(0),
        // FLAG: CardModel.cs:1267-1271) and so already drives GetResolved() below the threshold above.
        return playCount + 1;
    }

    // Only runs for models that actually changed the play count: Hook.ModifyCardPlayCount adds a model to
    // `modifyingModels` solely when its return value differs (Hook.cs:1390-1396), and that list is what
    // Hook.AfterModifyingCardPlayCount iterates (CardModel.cs:2032-2033). So this is a faithful one-shot
    // consumption of StS1's `activated` boolean.
    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        UsedThisTurn = true;
        Flash();
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    // StS1 re-arms `activated` in atTurnStart(). This is the per-turn reset — the one place Necronomicon
    // differs from ThrowingAxe, which latches for a whole combat instead (ThrowingAxe.cs:27-36).
    // Kunai.cs:71-78 is this repo's per-turn relic using the same hook and the same participant guard.
    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
            return Task.CompletedTask;

        UsedThisTurn = false;
        Status = RelicStatus.Active;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        UsedThisTurn = false;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }
}
