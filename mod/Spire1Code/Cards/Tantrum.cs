using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Tantrum (Uncommon Attack). Deal 3 damage 3 times (4 times upgraded), enter Wrath, then shuffle
/// this card back into your draw pile instead of discarding it (StS1 flag shuffleBackIntoDrawPile).
/// The post-play destination is redirected exactly like the shipped ReboundPower does it, keeping the base result
/// (dupes / exhaust) untouched.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Tantrum() : Spire1Card(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // Hit count is a RepeatVar so the !Repeat! token shows the upgraded value.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new DamageVar(3, ValueProp.Move), new RepeatVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardAttack(this, play, hitCount: DynamicVars.Repeat.IntValue).Execute(choiceContext);
        await StanceCmd.Enter<WrathPower>(choiceContext, Owner, this);
    }

    protected override CardLocation GetResultLocationForCardPlay()
    {
        CardLocation location = base.GetResultLocationForCardPlay();
        if (location.pileType != PileType.Discard)
            return location;
        location.pileType = PileType.Draw;
        location.position = CardPilePosition.Random;
        return location;
    }

    protected override void OnUpgrade() => DynamicVars.Repeat.UpgradeValueBy(1m);
}
