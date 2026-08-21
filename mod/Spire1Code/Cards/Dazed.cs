using Spire1.Spire1Code.Character;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using BaseLib.Utils;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Dazed (Status). Unplayable. Ethereal. Mirror of the base-game Dazed.</summary>
[Pool(typeof(Spire1LegacyPool))]
public class Dazed() : Spire1Card(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Unplayable];
}
