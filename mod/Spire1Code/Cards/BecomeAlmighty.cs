using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

using BaseLib.Utils;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher token — Become Almighty. The Strength option of Wish's choose-one screen (vanilla StS1 ships the same
/// three option cards). Never enters a pile and never reaches reward generation (CardRarity.Token, no pool).
/// </summary>
[Pool(typeof(Spire1LegacyPool))]
public class BecomeAlmighty() : Spire1Card(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<StrengthPower>(3)];

    /// <summary>Matches the displayed Strength to the Wish that created this option.</summary>
    public void SetAmount(decimal amount)
    {
        AssertMutable();
        DynamicVars.Strength.BaseValue = amount;
    }
}
