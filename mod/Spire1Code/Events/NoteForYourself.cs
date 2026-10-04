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

using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 shrine - A Note For Yourself. Receive a card (in StS1, the card stored in a previous run via
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
        // Cards content group gate (C18 r2, 2026-10-03): this event grants IronWave directly via RunState.CreateCard, bypassing the card pool filters; the event still completes so the page never soft-locks.
        // Fail closed before any card is constructed or added; the card is not granted.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] Note For Yourself card grant skipped: cards content group is off");
            // End the event so a stale page cannot be retried indefinitely; nothing is granted or stored.
            SetEventFinished(PageDescription("DONE"));
            return;
        }

        // FLAGGED: in StS1 the received card and its upgrade count are read from the player's
        // persistent prefs (NOTE_CARD / NOTE_UPGRADE) and the stored card is written back, so the
        // card you store now is the one you receive in future runs. No such save system exists in
        // this port, so the event always starts from the StS1 defaults (Iron Wave, unupgraded) and
        // the stored card is not persisted.
        CardModel received = Owner.RunState.CreateCard<MegaCrit.Sts2.Core.Models.Cards.IronWave>(Owner);
        await CardPileCmd.Add(received, PileType.Deck);
        List<CardModel> stored = (await CardSelectCmd.FromDeckForRemoval(Owner,
            new CardSelectorPrefs(new LocString("events", "SPIRE1-NOTE_FOR_YOURSELF.selectionScreenPrompt"), 1)))
            .Where(card => card.IsRemovable).ToList();
        await CardPileCmd.RemoveFromDeck(stored);
        SetEventFinished(PageDescription("DONE"));
    }

    private Task Ignore()
    {
        SetEventFinished(PageDescription("DONE"));
        return Task.CompletedTask;
    }
}
