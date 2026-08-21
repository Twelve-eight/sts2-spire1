using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Colorless — Bandage Up (Common Skill). Heal 4 HP, Exhaust (6 HP upgraded). 0 cost.</summary>
[Pool(typeof(ColorlessCardPool))]
public class BandageUp() : Spire1Card(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(4)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);

    protected override void OnUpgrade() => DynamicVars.Heal.UpgradeValueBy(2m);
}
