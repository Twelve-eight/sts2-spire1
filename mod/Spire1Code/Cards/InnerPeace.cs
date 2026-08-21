using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>StS1 Watcher — Inner Peace (Uncommon Skill). If you are in Calm, draw 3 cards (4 upgraded); otherwise enter Calm.</summary>
[Pool(typeof(WatcherCardPool))]
public class InnerPeace() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        if (StanceCmd.IsIn<CalmPower>(Owner))
        {
            await CommonActions.Draw(this, choiceContext);
            return;
        }

        await StanceCmd.Enter<CalmPower>(choiceContext, Owner, this);
    }

    protected override void OnUpgrade() => DynamicVars.Cards.UpgradeValueBy(1m);
}
