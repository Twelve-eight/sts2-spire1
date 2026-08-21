using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Spirit Shield (Rare Skill). Gain 3 Block (4 upgraded) for each card in your hand.
/// The per-card value is the card's BlockVar so the printed number tracks the upgrade; the total is computed at play
/// time (this card is already in the play pile, so it does not count itself, as in vanilla).
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class SpiritShield() : Spire1Card(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(3, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        int cardsInHand = PileType.Hand.GetPile(Owner).Cards.Count;
        if (cardsInHand <= 0)
            return;
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block.BaseValue * cardsInHand, DynamicVars.Block.Props, play);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(1m);
}
