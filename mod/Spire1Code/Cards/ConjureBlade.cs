using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Conjure Blade (Rare Skill, X-cost). Shuffle an Expunger into your draw pile whose attack repeats
/// X times (X+1 upgraded). Exhaust.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class ConjureBlade() : Spire1Card(-1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // The upgrade bonus is a DynamicVar rather than a private field so the generated Expunger and the card text
    // never disagree after an upgrade.
    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Bonus", 0)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var state = CombatState;
        if (state == null)
            return;
        int hits = ResolveEnergyXValue() + DynamicVars["Bonus"].IntValue;
        if (hits <= 0)
            return;
        var expunger = state.CreateCard<Expunger>(Owner);
        expunger.SetRepeats(hits);
        await CardPileCmd.AddGeneratedCardToCombat(expunger, PileType.Draw, Owner, CardPilePosition.Random);
    }

    protected override void OnUpgrade() => DynamicVars["Bonus"].UpgradeValueBy(1m);
}
