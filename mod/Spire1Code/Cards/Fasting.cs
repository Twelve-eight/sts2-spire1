using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Fasting (Uncommon Power). Gain 3 Strength (4 upgraded), 3 Dexterity (4 upgraded), and gain
/// 1 less Energy at the start of each turn.
/// StS1 applies a generic "Energy Down" power for the drawback; StS2 ships that exact power as WasteAwayPower
/// (Debuff/Counter whose ModifyMaxEnergy returns amount - Amount). The per-turn refill is
/// PlayerCombatState.AddMaxEnergyToCurrent(), which reads MaxEnergy through Hook.ModifyMaxEnergy, so the shipped
/// power reduces exactly the turn-start energy and nothing else (Miracle and friends are unaffected), and it is a
/// Debuff in both games, so Artifact interacts identically.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Fasting() : Spire1Card(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<StrengthPower>(3),
        new PowerVar<DexterityPower>(3),
        new PowerVar<WasteAwayPower>(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<StrengthPower>(choiceContext, this);
        await CommonActions.ApplySelf<DexterityPower>(choiceContext, this);
        await CommonActions.ApplySelf<WasteAwayPower>(choiceContext, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Strength.UpgradeValueBy(1m);
        DynamicVars.Dexterity.UpgradeValueBy(1m);
    }
}
