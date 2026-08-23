using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class BiasedCognition() : Spire1Card(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<FocusPower>(4),
        // ApplySelf<BiasedCognitionPower> 按名 "BiasedCognitionPower" 查 DynamicVars，
        // 缺注册会 KeyNotFoundException 打断出牌（P1SMOKE3-r2 F10 实测）。1=每回合失去的集中数。
        new PowerVar<Spire1.Spire1Code.Powers.BiasedCognitionPower>(1),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.ApplySelf<FocusPower>(choiceContext, this);
        await CommonActions.ApplySelf<Spire1.Spire1Code.Powers.BiasedCognitionPower>(choiceContext, this);
    }

    protected override void OnUpgrade() => DynamicVars.Power<FocusPower>().UpgradeValueBy(1m);
}
