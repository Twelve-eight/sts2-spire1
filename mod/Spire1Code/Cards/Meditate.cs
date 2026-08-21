using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Meditate (Uncommon Skill). Put 1 card (2 upgraded) from your discard pile into your hand and
/// Retain it, enter Calm, then end your turn.
/// The retain is a single-turn retain (CardCmd.ApplySingleTurnRetain), which is what CombatManager.FlushPlayerHand
/// reads through CardModel.ShouldRetainThisTurn when it discards the hand.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Meditate() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int count = DynamicVars.Cards.IntValue;
        if (count > 0 && PileType.Discard.GetPile(Owner).Cards.Count > 0)
        {
            var prefs = new CardSelectorPrefs(SelectionScreenPrompt, count);
            List<CardModel> picked =
                (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Discard, null)).ToList();
            foreach (CardModel card in picked)
            {
                await CardPileCmd.Add(card, PileType.Hand);
                CardCmd.ApplySingleTurnRetain(card);
            }
        }

        await StanceCmd.Enter<CalmPower>(choiceContext, Owner, this);
        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
