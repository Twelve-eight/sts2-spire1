using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Vault (Rare Skill, cost 3 / 2 upgraded). Take an extra turn after this one, then end your turn.
/// Exhaust.
/// The extra turn uses the shipped AmbergrisPower: it is the game's extra-turn power (ShouldTakeExtraTurn while its
/// counter is above 0, decrementing in AfterTakingExtraTurn), which CombatManager consults in its side-switch.
/// Energy is NOT refreshed manually; the real extra turn does that.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Vault() : Spire1Card(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await PowerCmd.Apply<AmbergrisPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
