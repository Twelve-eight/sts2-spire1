using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Defect Uncommon Skill 遗传算法（jar 权威：cards/blue/GeneticAlgorithm.class；
/// 官方原文 eng "Gain !B! Block. Permanently increase this card's Block by !M!. NL Exhaust."
/// / zhs 获得 !B! 点格挡。每打出一次格挡永久 +!M!。消耗。）
/// 跨战斗成长链路：战斗实例 OnPlay -> 同步到 DeckVersion(牌库母本,[SavedProperty] 随存档序列化)
/// -> 下场战斗 PopulateCombatState 从母本 Clone(MemberwiseClone 含私有字段)。
/// </summary>
[Pool(typeof(DefectCardPool))]
public class GeneticAlgorithm : Spire1Card
{
    private const int BaseBlock = 1;
    private const int BaseIncrease = 2;

    /// <summary>DeckVersion 缺失警告的全进程一次性闩锁(有界日志).</summary>
    private static bool _deckVersionWarned;

    private int _extraGain;

    public GeneticAlgorithm() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>本场战斗实际提供的格挡(随永久成长增长).</summary>
    [SavedProperty]
    public int ExtraGain
    {
        get => _extraGain;
        set
        {
            AssertMutable();
            _extraGain = value;
            // 写回必须放在 setter 里,不能只放在 OnPlay:CardModel.FromSerializable 与
            // DeepCloneFields 都是先把 CanonicalVars 物化成 BaseBlock(=1),再逐个
            // Fill/拷贝 [SavedProperty].只在 OnPlay 刷新 BaseValue 会让"存读档后的
            // 首张牌"和"克隆后未出牌的首张牌"按 1 给格挡.
            // 引擎自带的 GeneticAlgorithm 同样把 DynamicVars.Block.BaseValue 的写入
            // 放在 [SavedProperty] CurrentBlock 的 setter 内(UpdateBlock -> CurrentBlock).
            DynamicVars.Block.BaseValue = CurrentBlock;
        }
    }

    // jar 权威(cards/blue/GeneticAlgorithm.class):
    //   构造器 misc=1 -> baseBlock=1;baseMagicNumber=2 -> magicNumber=2(成长量);
    //   upgrade() 仅 upgradeMagicNumber(1) -> 成长 2->3.基础格挡 1 升级后仍为 1.
    // 因此 Block 走 ExtraGain 累积(不随升级变化),Increase 才是升级通道.
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(CurrentBlock, ValueProp.Move),
        new IntVar("Increase", BaseIncrease),
    ];

    private int CurrentBlock => BaseBlock + ExtraGain;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        // 单点成长入口:自身与牌库母本走同一条路径,ExtraGain 的 setter 负责写回 Block.
        ApplyGrowth(DynamicVars["Increase"].IntValue);
        if (DeckVersion is GeneticAlgorithm master)
        {
            master.ApplyGrowth(DynamicVars["Increase"].IntValue);
        }
        else if (!_deckVersionWarned)
        {
            // 战斗内副本无 DeckVersion(如 Discovery 生成):成长不跨战斗.
            // 原实现每次出牌刷一条 Error;改为全进程一次,不掩盖问题也不刷屏.
            _deckVersionWarned = true;
            MainFile.Logger.Error("[Spire1] GA: DeckVersion missing/typed wrong - growth won't persist (logged once per process)");
        }
    }

    /// <summary>永久成长 +<paramref name="increase"/>;Block 的写回由 ExtraGain 的 setter 完成.</summary>
    private void ApplyGrowth(int increase) => ExtraGain += increase;

    protected override void OnUpgrade() => DynamicVars["Increase"].UpgradeValueBy(1m);

    protected override void AfterDowngraded()
    {
        // 降级时成长量回落 1,已累积的永久成长保留
        DynamicVars.Block.BaseValue = CurrentBlock;
    }
}
