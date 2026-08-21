using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Wish (Rare Skill). Choose one: gain 6 Plated Armor (8 upgraded), 3 Strength (4 upgraded) or 25 Gold
/// (30 upgraded). Exhaust.
/// The three-way choice is the game's choose-a-card screen (CardSelectCmd.FromChooseACardScreen, which accepts up to
/// three cards and cannot be skipped here), driven by the same three option cards vanilla StS1 uses. The options are
/// display only: this card owns the numbers and applies the effect. Our own class is required because StS2's shipped
/// Wish is a different Ancient-rarity card.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Wish() : Spire1Card(3, CardType.Skill, CardRarity.Rare, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PlatingPower>(6),
        new PowerVar<StrengthPower>(3),
        new GoldVar(25),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var state = CombatState;
        if (state == null)
            return;

        var platingOption = state.CreateCard<LiveForever>(Owner);
        platingOption.SetAmount(DynamicVars["PlatingPower"].BaseValue);
        var strengthOption = state.CreateCard<BecomeAlmighty>(Owner);
        strengthOption.SetAmount(DynamicVars.Strength.BaseValue);
        var goldOption = state.CreateCard<FameAndFortune>(Owner);
        goldOption.SetAmount(DynamicVars.Gold.BaseValue);

        List<CardModel> options = [platingOption, strengthOption, goldOption];
        CardModel? chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, options, Owner);

        if (chosen is LiveForever)
            await CommonActions.ApplySelf<PlatingPower>(choiceContext, this);
        else if (chosen is BecomeAlmighty)
            await CommonActions.ApplySelf<StrengthPower>(choiceContext, this);
        else if (chosen is FameAndFortune)
            await PlayerCmd.GainGold(DynamicVars.Gold.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PlatingPower"].UpgradeValueBy(2m);
        DynamicVars.Strength.UpgradeValueBy(1m);
        DynamicVars.Gold.UpgradeValueBy(5m);
    }
}
