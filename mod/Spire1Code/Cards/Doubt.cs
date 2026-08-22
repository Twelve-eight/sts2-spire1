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
/// StS1 Ironclad — Doubt (Curse). Unplayable. At the end of your turn, gain 1 Weak.
/// Mirror of the base-game Doubt: applies at turn end and sets SkipNextDurationTick so the
/// Weak is not ticked down immediately and lasts through the enemy's turn.
/// </summary>
[Pool(typeof(CurseCardPool))]
public class Doubt() : Spire1Curse()
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<WeakPower>(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<WeakPower>()];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        bool alreadyHasWeak = Owner.Creature.HasPower<WeakPower>();
        var power = await CommonActions.Apply<WeakPower>(choiceContext, Owner.Creature, this);
        if (power != null && !alreadyHasWeak)
        {
            power.SkipNextDurationTick = true;
        }
    }
}
