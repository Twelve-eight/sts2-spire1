using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(WatcherCardPool))]
public class Evaluate() : Spire1Card(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        // Evaluate+ 洗入的是升级版 Insight+（StS1 cards.json UPGRADE_DESCRIPTION "Shuffle an *Insight+ ..."）。
        // AddToCombatAndPreview 内部走 CombatState.CreateCard，不继承升级态（CombatState.cs#CreateCard 只 ToMutable+AfterCreated），
        // 故按引擎同型卡（Begone/PrimalForce/Compact）的 create->CardCmd.Upgrade->入堆 链路手写。
        // Insight 不能升费用/加关键字差异，CardCmd.Upgrade 会调 Insight.OnUpgrade 使 CardsVar 2->3。
        var insight = CombatState.CreateCard<Insight>(Owner);
        if (IsUpgraded)
        {
            CardCmd.Upgrade(insight);
        }
        await CardPileCmd.AddGeneratedCardToCombat(insight, PileType.Draw, Owner);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);
}
