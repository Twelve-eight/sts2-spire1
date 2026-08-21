using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Omniscience (Rare Skill, cost 4 / 3 upgraded). Choose a card in your draw pile, play it twice and
/// Exhaust it. Exhaust.
/// The two plays are free auto-plays of stat-equivalent copies (CardModel.CreateCloneForPlayer + CardCmd.AutoPlay,
/// the shipped Imitation Learning pattern); the chosen original is then Exhausted, exactly like vanilla's
/// OmniscienceAction. AutoPlay with a null target randomizes enemy targeting.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Omniscience() : Spire1Card(4, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        CardModel? chosen = (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Draw)).FirstOrDefault();
        if (chosen == null)
            return;

        for (int i = 0; i < 2; i++)
        {
            var copy = chosen.CreateCloneForPlayer(Owner);
            await CardCmd.AutoPlay(choiceContext, copy, null);
        }

        await CardCmd.Exhaust(choiceContext, chosen);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
