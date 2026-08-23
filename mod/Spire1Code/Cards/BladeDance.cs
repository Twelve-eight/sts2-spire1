using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Silent — Blade Dance (Common). Add 3 Shivs into your hand (4 upgraded).
/// ACTIVE in SilentCardPool: the shipped StS2 BladeDance self-exhausts (CanonicalKeywords
/// => [Exhaust], re-verified 2026-08-24 vs desktop-1.0.jar which has no exhaust) — a B-group
/// drift, so per SharedCardReuse rules our vanilla-faithful class serves instead.</summary>
[Pool(typeof(SilentCardPool))]
public class BladeDance() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.None)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await Shiv.CreateInHand(Owner, DynamicVars.Cards.IntValue, Owner.Creature.CombatState, Owner);

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
