using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Blasphemy (Rare Skill). Enter Divinity, then die at the start of your next turn. Exhaust.
/// Upgrade adds Retain. The death is carried by BlasphemyPower through CreatureCmd.Kill (never a direct HP write).
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Blasphemy() : Spire1Card(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await StanceCmd.Enter<DivinityPower>(choiceContext, Owner, this);
        await PowerCmd.Apply<BlasphemyPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}
