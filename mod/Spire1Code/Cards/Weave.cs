using BaseLib.Hooks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Weave (Uncommon Attack). Deal 4 damage (6 upgraded); whenever you Scry, this returns from the
/// discard pile to your hand.
/// The trigger is BaseLib's IAfterScryed (research/BaseLib-StS2/Hooks/IAfterScryed.cs:17), dispatched to every card
/// in a combat pile via HookUtils.Dispatch (research/BaseLib-StS2/Utils/HookUtils.cs:48) from
/// BaseLibHooks.AfterScryed (Hooks/BaseLibHooks.cs:46). Like StS1, a copy that the same scry just discarded is
/// already in the discard pile when this runs, so it comes straight back.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Weave() : Spire1Card(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy), IAfterScryed
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(4, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.CardAttack(this, play).Execute(choiceContext);

    public async Task AfterScryed(
        PlayerChoiceContext ctx,
        Player player,
        int scryAmount,
        int discardAmount,
        List<CardModel> seen,
        List<CardModel> discarded)
    {
        if (player == Owner && Pile?.Type == PileType.Discard)
            await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
