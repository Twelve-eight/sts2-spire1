using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Sands of Time (Uncommon Attack). Retain; deal 20 damage (26 upgraded). Every time it is retained
/// at end of turn its cost drops by 1 for the rest of the combat.
/// Retention is observed through the card's own AfterFlush hook: CombatManager.FlushPlayerHand collects the retained
/// cards (.tmp/dllsrc/MegaCrit.Sts2.Core.Combat/CombatManager.cs:1797-1811) and Hook.AfterFlush
/// (.tmp/dllsrc/MegaCrit.Sts2.Core.Hooks/Hook.cs:572) delivers them to every card in a combat pile
/// (AbstractModel.cs:758). Combat-scoped cost reduction mirrors the shipped KinglyKick.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class SandsOfTime() : Spire1Card(4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(20, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play).Execute(choiceContext);

    public override Task AfterFlush(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyCollection<CardModel> flushedCards,
        IReadOnlyCollection<CardModel> retainedCards)
    {
        if (player == Owner && retainedCards.Contains(this))
            EnergyCost.AddThisCombat(-1, reduceOnly: true);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6m);
}
