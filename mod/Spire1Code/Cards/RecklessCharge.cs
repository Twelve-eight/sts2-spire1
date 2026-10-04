using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Reckless Charge (Uncommon). Deal 7 damage; shuffle a Dazed into your draw pile (10 upgraded).</summary>
public class RecklessCharge() : Spire1Card(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(7, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play).Execute(choiceContext);
        // Cards content group gate (C18, 2026-10-03): Dazed is a Spire1 status card created directly by
        // AddToCombatAndPreview, which bypasses the pool filters that carry the cards gate. Fail closed
        // before any card is constructed; the attack itself stays unchanged.
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            MainFile.Logger.Warn("[Spire1] Reckless Charge Dazed grant skipped: cards content group is off");
            return;
        }
        await CardPileCmd.AddToCombatAndPreview<Dazed>(Owner.Creature, PileType.Draw, 1, Owner, CardPilePosition.Random);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
