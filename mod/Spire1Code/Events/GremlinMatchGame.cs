using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — Match and Keep! The gremlin's memory minigame: 12 face-down cards (6 identical
/// pairs) and 5 attempts; every pair you match is added to your deck. The card set is exactly the
/// StS1 one — a random Rare, Uncommon and Common from your card pool, a random curse (two curses at
/// Ascension 15+ instead of the colorless card), a random colorless Uncommon (below A15 only) and a
/// card from your starting deck, each duplicated — and the outcome (matched pairs kept, 5 attempts,
/// non-matches waste an attempt) is reproduced.
///
/// PRESENTATION DEVIATION (FLAGGED): StS2 has no face-down memory-minigame UI, so the game is played
/// through the shipped card-selection grid with all cards face-up instead of the StS1 flip-and-match
/// board. The player picks two cards per attempt; two copies of the same card are a match.
/// </summary>
public class GremlinMatchGame : Spire1Event
{
    private const int _attempts = 5;

    // NOT readonly: AbstractModel.MutableClone uses MemberwiseClone, which shallow-copies this
    // reference, so the per-player mutable clone would otherwise share the canonical event's list
    // and the board would keep growing on every visit. DeepCloneFields below re-seeds it.
    private List<CardModel> _cards = [];

    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _cards = [];
    }

    private int _attemptsLeft = _attempts;

    protected override string ShippedPortrait => "this_or_that";

    public override void CalculateVars()
    {
        // Build the exact StS1 card set (see class doc) and duplicate it into pairs.
        List<CardModel> unlocked = Owner.Character.CardPool.GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint).ToList();
        List<CardModel> curses = ModelDb.CardPool<CurseCardPool>().GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint).ToList();
        List<CardModel> colorless = ModelDb.CardPool<ColorlessCardPool>().GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint).ToList();

        List<CardModel> distinct = [];
        AddPick(distinct, unlocked, c => c.Rarity == CardRarity.Rare);
        AddPick(distinct, unlocked, c => c.Rarity == CardRarity.Uncommon);
        AddPick(distinct, unlocked, c => c.Rarity == CardRarity.Common);
        if (Owner.RunState.AscensionLevel >= 15)
        {
            AddPick(distinct, curses);
            AddPick(distinct, curses);
        }
        else
        {
            AddPick(distinct, colorless, c => c.Rarity == CardRarity.Uncommon);
            AddPick(distinct, curses);
        }
        AddPick(distinct, Owner.Character.StartingDeck);

        foreach (CardModel card in distinct)
        {
            // Two mutable display copies per pair. They are not registered in the run state; they
            // only exist for the selection screens. The kept copy is cloned into the deck on a match.
            _cards.Add(MakeDisplayCopy(card));
            _cards.Add(MakeDisplayCopy(card));
        }
        Rng.Shuffle(_cards);
    }

    private void AddPick(List<CardModel> into, IEnumerable<CardModel> from, Func<CardModel, bool>? filter = null)
    {
        CardModel? pick = filter == null ? Rng.NextItem(from) : Rng.NextItem(from.Where(filter));
        if (pick != null)
        {
            into.Add(pick);
        }
    }

    private CardModel MakeDisplayCopy(CardModel canonical)
    {
        CardModel copy = canonical.ToMutable();
        copy.Owner = Owner;
        return copy;
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
        SetEventState(PageDescription("RULES"), [Option(Play, "RULES")]);
        return Task.CompletedTask;
    }

    private async Task Play()
    {
        // One attempt per two picked cards, exactly like one pair-flip in StS1: every attempt
        // (matched or not) consumes one of the five tries.
        while (_attemptsLeft > 0 && _cards.Count >= 2)
        {
            LocString prompt = new("events", "SPIRE1-GREMLIN_MATCH_GAME.selectionScreenPrompt");
            CardSelectorPrefs prefs = new(prompt, 2, 2);
            prefs.Prompt.Add("Attempts", _attemptsLeft);
            List<CardModel> picked = (await CardSelectCmd.FromSimpleGrid(new BlockingPlayerChoiceContext(), _cards, Owner, prefs)).ToList();
            if (picked.Count < 2)
            {
                break;
            }
            if (picked[0].Id == picked[1].Id)
            {
                // Match! Both leave the table, one copy joins the deck.
                _cards.Remove(picked[0]);
                _cards.Remove(picked[1]);
                CardModel kept = Owner.RunState.CloneCard(picked[0]);
                await CardPileCmd.Add(kept, PileType.Deck);
            }
            _attemptsLeft--;
        }
        SetEventFinished(PageDescription("COMPLETE"));
    }
}
