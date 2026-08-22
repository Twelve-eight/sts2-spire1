using MegaCrit.Sts2.Core.Models.CardPools;
using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Ironclad — Shame (Curse). Unplayable. At the end of your turn, gain 1 Frail.
/// Mirror of the base-game Shame: applies at turn end and sets SkipNextDurationTick so the
/// Frail is not ticked down immediately and lasts through the enemy's turn.
/// </summary>
[Pool(typeof(CurseCardPool))]
public class Shame() : Spire1Curse()
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FrailPower>(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<FrailPower>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        bool alreadyHasFrail = Owner.Creature.HasPower<FrailPower>();
        var power = await CommonActions.Apply<FrailPower>(choiceContext, Owner.Creature, this);
        if (power != null && !alreadyHasFrail)
        {
            power.SkipNextDurationTick = true;
        }
    }
}
