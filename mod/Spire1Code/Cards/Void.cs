using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Ironclad — Void (Status). Unplayable. Ethereal. When drawn, lose 1 Energy.
/// Mirror of the base-game Void (AfterCardDrawn + PlayerCmd.LoseEnergy).
/// </summary>
public class Void() : Spire1Card(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal];

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card == this)
        {
            await Cmd.Wait(0.25f);
            await PlayerCmd.LoseEnergy(DynamicVars.Energy.IntValue, Owner);
        }
    }
}
