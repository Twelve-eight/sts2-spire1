using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Watcher — Pray (Uncommon Skill). Gain 3 Mantra (4 upgraded) and shuffle an Insight into your draw pile.
/// </summary>
[Pool(typeof(WatcherCardPool))]
public class Pray() : Spire1Card(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<MantraPower>(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await StanceCmd.GainMantra(choiceContext, Owner, DynamicVars.Power<MantraPower>().BaseValue, this);
        await CardPileCmd.AddToCombatAndPreview<Insight>(
            Owner.Creature,
            PileType.Draw,
            1,
            Owner,
            CardPilePosition.Random);
    }

    protected override void OnUpgrade() => DynamicVars.Power<MantraPower>().UpgradeValueBy(1m);
}
