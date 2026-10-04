using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent - Cloak and Dagger (Common). Gain 6 Block, add 1 Shiv into your hand (2 Shivs upgraded).</summary>
[Pool(typeof(Spire1LegacyPool))]
public class CloakAndDagger() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(6, ValueProp.Move), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        // Cards content group gate (C18 r2, 2026-10-03): Shiv is granted directly by this Spire1 card, so the card pool filters cannot close the path; block gain above stays unchanged.
        // Fail closed before any card is constructed or added; the card is not granted.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] Cloak and Dagger Shiv grant skipped: cards content group is off");
            return;
        }

        await Shiv.CreateInHand(Owner, DynamicVars.Cards.IntValue, Owner.Creature.CombatState, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
