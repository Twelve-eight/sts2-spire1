using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

using BaseLib.Utils;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher token — Fame and Fortune. The Gold option of Wish's choose-one screen (vanilla StS1 ships the same
/// three option cards). Never enters a pile and never reaches reward generation (CardRarity.Token, no pool).
/// </summary>
[Pool(typeof(Spire1LegacyPool))]
public class FameAndFortune() : Spire1Card(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new GoldVar(25)];

    /// <summary>Matches the displayed Gold to the Wish that created this option.</summary>
    public void SetAmount(decimal amount)
    {
        AssertMutable();
        DynamicVars.Gold.BaseValue = amount;
    }
}
