using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Sanctity (Uncommon Skill). Gain 6 Block (9 upgraded); if the last card played this combat was a
/// Skill, draw 2 cards. Same "last card played" lookup as the mod's SashWhip / CrushJoints.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Sanctity() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new BlockVar(6, ValueProp.Move), new CardsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        var lastPlay = CombatManager.Instance.History.CardPlaysFinished
            .LastOrDefault(entry => entry.CardPlay.Player == Owner && entry.CardPlay != play);
        if (lastPlay?.CardPlay.Card.Type == CardType.Skill)
            await CommonActions.Draw(this, choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);
}
