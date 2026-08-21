using MegaCrit.Sts2.Core.Entities.Cards;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Injury (Curse). Unplayable. No effect. Mirror of the base-game Injury.</summary>
public class Injury() : Spire1Curse()
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
}
