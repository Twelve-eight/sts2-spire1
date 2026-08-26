using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Armaments (Common). Gain 5 Block; upgrade a card in your hand (all cards upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Armaments() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.None)
{
    private bool _all;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(5, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        if (_all)
        {
            foreach (var c in PileType.Hand.GetPile(Owner).Cards.Where(c => c.IsUpgradable).ToList())
                CardCmd.Upgrade(c);
        }
        else
        {
            var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
            var picked = (await CommonActions.SelectCards(this, prefs, choiceContext, PileType.Hand, c => c.IsUpgradable)).FirstOrDefault();
            if (picked != null)
                CardCmd.Upgrade(picked);
        }
    }

    protected override void OnUpgrade()
    {
        // (2026-08-26 reverify fix) StS1's upgrade ONLY changes "a card" → "all cards";
        // Block stays 5 (cards-red.json upgraded_description_diff has no !B! delta, and the
        // shipped StS2 Armaments also keeps BlockVar(5)). The 5→8 added in 3a0de3d was a
        // misremembered "fix" and is reverted.
        _all = true;
    }
}
