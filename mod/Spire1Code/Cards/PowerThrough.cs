using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad - Power Through (Uncommon). Add 2 Wounds into your hand; gain 15 Block (20 upgraded).</summary>
public class PowerThrough() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(15, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        // Cards content group gate (C18, 2026-10-03): Wound is a Spire1 status card created directly by
        // AddToCombatAndPreview, which bypasses the pool filters that carry the cards gate. Fail closed
        // before any card is constructed; block gain stays unchanged.
        if (Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards))
        {
            await CardPileCmd.AddToCombatAndPreview<Wound>(Owner.Creature, PileType.Hand, 2, Owner);
        }
        else
        {
            MainFile.Logger.Warn("[Spire1] Power Through Wound grant skipped: cards content group is off");
        }
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5m);
}
