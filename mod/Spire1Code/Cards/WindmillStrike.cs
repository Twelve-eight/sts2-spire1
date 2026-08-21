using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Windmill Strike (Uncommon Attack). Retain; deal 7 damage (10 upgraded). Every time it is retained
/// at end of turn its damage grows by 4 (5 upgraded) for the rest of the combat.
/// Retention is observed through the card's own AfterFlush hook: CombatManager.FlushPlayerHand collects the retained
/// cards (.tmp/dllsrc/MegaCrit.Sts2.Core.Combat/CombatManager.cs:1797-1811) and Hook.AfterFlush
/// (.tmp/dllsrc/MegaCrit.Sts2.Core.Hooks/Hook.cs:572) delivers them to every card in a combat pile
/// (AbstractModel.cs:758).
/// The growth lives in card DynamicVars so the calc lambda stays STATIC (see Rampage / GlassKnife); it is cleared at
/// combat start because the effect is only "this combat".
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class WindmillStrike() : Spire1Card(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("WindmillInc", 4),
        new IntVar("WindmillBonus", 0),
        ..CustomCardModel.MakeCalculatedDamage(7,
            static (card, target) => card.DynamicVars["WindmillBonus"].BaseValue)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play).Execute(choiceContext);

    public override Task AfterFlush(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyCollection<CardModel> flushedCards,
        IReadOnlyCollection<CardModel> retainedCards)
    {
        if (player == Owner && retainedCards.Contains(this))
            DynamicVars["WindmillBonus"].BaseValue += DynamicVars["WindmillInc"].BaseValue;
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        DynamicVars["WindmillBonus"].BaseValue = 0m;
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        DynamicVars["CalculationBase"].UpgradeValueBy(3m);
        DynamicVars["WindmillInc"].UpgradeValueBy(1m);
    }
}
