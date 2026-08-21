using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine — A Note For Yourself. Receive a card (in StS1, the card stored in a previous run via
/// the NOTE_CARD / NOTE_UPGRADE prefs; this mod has no cross-run save system, so the in-run half is
/// implemented with the vanilla defaults: Iron Wave, unupgraded) and store a card of your choice,
/// which is removed from your deck. Cross-run persistence is FLAGGED as not implemented.
/// </summary>
public class NoteForYourself : Spire1Event
{
    protected override string ShippedPortrait => "round_tea_party";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("Card")];

    public override void CalculateVars()
    {
        ((StringVar)DynamicVars["Card"]).StringValue = ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.IronWave>().Title;
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
        SetEventState(PageDescription("CHOOSE"),
        [
            Option(TakeAndGive, "CHOOSE"),
            Option(Ignore, "CHOOSE")
        ]);
        return Task.CompletedTask;
    }

    private async Task TakeAndGive()
    {
        // FLAGGED: in StS1 the received card and its upgrade count are read from the player's
        // persistent prefs (NOTE_CARD / NOTE_UPGRADE) and the stored card is written back, so the
        // card you store now is the one you receive in future runs. No such save system exists in
        // this port, so the event always starts from the StS1 defaults (Iron Wave, unupgraded) and
        // the stored card is not persisted.
        CardModel received = Owner.RunState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.IronWave>(Owner);
        await CardPileCmd.Add(received, PileType.Deck);
        List<CardModel> stored = (await CardSelectCmd.FromDeckGeneric(Owner,
            new CardSelectorPrefs(new LocString("events", "SPIRE1-NOTE_FOR_YOURSELF.selectionScreenPrompt"), 1))).ToList();
        await CardPileCmd.RemoveFromDeck(stored);
        SetEventFinished(PageDescription("DONE"));
    }

    private Task Ignore()
    {
        SetEventFinished(PageDescription("DONE"));
        return Task.CompletedTask;
    }
}
