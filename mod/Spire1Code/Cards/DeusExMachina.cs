using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Deus Ex Machina (Rare Skill). Unplayable. When you draw this card, add 2 Miracles (3 upgraded)
/// to your hand and Exhaust it. Mirrors the base-game Void pattern (AfterCardDrawn on the card itself).
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class DeusExMachina() : Spire1Card(-2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(2)];

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this || CombatState == null)
            return;
        await Cmd.Wait(0.25f);
        await CardPileCmd.AddToCombatAndPreview<Miracle>(Owner.Creature, PileType.Hand, DynamicVars.Cards.IntValue, Owner);
        await CardCmd.Exhaust(choiceContext, this);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
