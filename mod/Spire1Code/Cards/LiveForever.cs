using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher token — Live Forever. The Plated Armor option of Wish's choose-one screen (vanilla StS1 ships the same
/// three option cards). Never enters a pile and never reaches reward generation (CardRarity.Token, no pool).
/// </summary>
public class LiveForever() : Spire1Card(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PlatingPower>(6)];

    /// <summary>Matches the displayed Plated Armor to the Wish that created this option.</summary>
    public void SetAmount(decimal amount)
    {
        AssertMutable();
        DynamicVars["PlatingPower"].BaseValue = amount;
    }
}
