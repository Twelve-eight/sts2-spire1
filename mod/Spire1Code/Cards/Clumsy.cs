using MegaCrit.Sts2.Core.Models.CardPools;
using Spire1.Spire1Code.Character;
using MegaCrit.Sts2.Core.Entities.Cards;

using BaseLib.Utils;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Ironclad — Clumsy (Curse). Unplayable. Ethereal. Mirror of the base-game Clumsy.</summary>
[Pool(typeof(CurseCardPool))]
public class Clumsy() : Spire1Curse()
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal];
}
