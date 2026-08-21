using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 — Bird-Faced Urn (Rare). Whenever you play a Power card, heal 2 HP.</summary>
public class BirdFacedUrn : Spire1Relic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(2m)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Bird-Faced Urn",
            "#Whenever you play a Power card, heal !H! HP.",
            "This urn shows the crow god Mazaleth looking mischievous.");

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || !CombatManager.Instance.IsInProgress || cardPlay.Card.Type != CardType.Power)
            return;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }
}
