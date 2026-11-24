using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - True Grit (Common). Gain 7 Block, exhaust a random hand card (9 Block, choose card upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class TrueGrit() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(7, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);

        if (IsUpgraded)
        {
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
            var picked = (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Hand, null)).FirstOrDefault();
            if (picked != null)
                await CardCmd.Exhaust(choiceContext, picked);
        }
        else
        {
            var cards = PileType.Hand.GetPile(Owner).Cards;
            // StS1 ExhaustAction.update @12-128 consumes no RNG when at most one card remains.
            var random = cards.Count == 0 ? null : cards[cards.Count == 1
                ? 0
                : Owner.RunState.Rng.CombatCardSelection.NextInt(0, cards.Count)];
            if (random != null)
                await CardCmd.Exhaust(choiceContext, random);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}
