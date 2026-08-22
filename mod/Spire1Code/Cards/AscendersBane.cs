using MegaCrit.Sts2.Core.Models.CardPools;
using Spire1.Spire1Code.Character;
using MegaCrit.Sts2.Core.Entities.Cards;

using BaseLib.Utils;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Ironclad — Ascender's Bane (Curse). Unplayable. Ethereal. Cannot be removed (Eternal).
/// Mirror of the base-game AscendersBane: Eternal/Unplayable/Ethereal and excluded from all
/// random generation (only added by the Ascension modifier).
/// </summary>
[Pool(typeof(CurseCardPool))]
public class AscendersBane() : Spire1Curse()
{
    public override bool CanBeGeneratedByModifiers => false;

    public override bool CanBeGeneratedInCombat => false;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Eternal, CardKeyword.Unplayable, CardKeyword.Ethereal];
}
