using MegaCrit.Sts2.Core.Models.CardPools;
using BaseLib.Utils;
using Spire1.Spire1Code.Character;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Wound (Status). Unplayable. Mirror of the base-game Wound (cost -1, Status rarity).</summary>
[Pool(typeof(StatusCardPool))]
public class Wound() : Spire1Card(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
}
