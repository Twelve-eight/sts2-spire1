using MegaCrit.Sts2.Core.Models.CardPools;
using BaseLib.Utils;
using Spire1.Spire1Code.Character;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Injury (Curse). Unplayable. No effect. Mirror of the base-game Injury.</summary>
[Pool(typeof(CurseCardPool))]
public class Injury() : Spire1Curse()
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];
}
