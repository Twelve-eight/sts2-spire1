using Spire1.Spire1Code.Character;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Colorless — Dark Shackles (Common Skill). Enemy loses 9 Strength this turn, Exhaust (15 upgraded). 0 cost.
/// Uses the base game's DarkShacklesPower (a TemporaryStrengthPower with IsPositive=false): PowerCmd.Apply receives
/// the positive PowerVar amount and the power internally applies -X Strength, exactly like the base-game Dark Shackles card.
/// </summary>
[Pool(typeof(Spire1LegacyPool))]
public class DarkShackles() : Spire1Card(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<DarkShacklesPower>(9)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<StrengthPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => await CommonActions.Apply<DarkShacklesPower>(choiceContext, play.Target!, this);

    protected override void OnUpgrade() => DynamicVars.Power<DarkShacklesPower>().UpgradeValueBy(6m);
}
