using MegaCrit.Sts2.Core.Models.CardPools;
using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Slimed (Status). 1 cost, Exhaust. Playing it does NOTHING.
/// R6 (2026-09-06, docs/CODE-REVIEW-20260904.md): the shipped StS2 Slimed drifts — its
/// OnPlay draws 1 card (CardsVar(1) + CardPileCmd.Draw), while the jar's use() is an empty
/// method (javap-status/Slimed.txt L27-29: "public void use(...); Code: 0: return") and the
/// StS1 card text is just "Exhaust.". Our faithful class plays as a pure no-op.
/// NOTE for monster authors: this class name-shadows MegaCrit.Sts2.Core.Models.Cards.Slimed.
/// Monster files that import the engine namespace must use the fully-qualified engine name
/// (see CorruptHeart.cs which already does).
/// </summary>
[Pool(typeof(StatusCardPool))]
public class Slimed() : Spire1Card(1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // jar Slimed.use() is empty — playing it is a no-op (exhaust still applies via keyword).
}
