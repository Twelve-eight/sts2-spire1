using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Ironclad? no — Defect Uncommon Skill 遗传算法 (Genetic Algo):
/// "Gain 1(2) Dexterity. Permanently increase this card's Dexterity gain by 1."
/// 跨战斗成长链路：战斗实例 OnPlay → 同步到 DeckVersion(牌库母本,[SavedProperty] 随存档序列化)
/// → 下场战斗 PopulateCombatState 从母本 Clone(MemberwiseClone 含私有字段)。
/// </summary>
public class GeneticAlgorithm : Spire1Card
{
    private const int BaseGain = 1;
    private const int BaseGainUpgraded = 2;

    private int _extraGain;

    public GeneticAlgorithm() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>本场战斗实际提供的敏捷值（随永久成长增长）。</summary>
    [SavedProperty]
    public int ExtraGain
    {
        get => _extraGain;
        set
        {
            AssertMutable();
            _extraGain = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(CurrentGain, ValueProp.Move),
        new IntVar("Increase", 1),
    ];

    private int CurrentGain => (IsUpgraded ? BaseGainUpgraded : BaseGain) + ExtraGain;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        MainFile.Logger.Info($"[Spire1] GA play: extra={ExtraGain} gain={CurrentGain} deck={(DeckVersion != null ? "ok" : "null")}");
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        int inc = DynamicVars["Increase"].IntValue;
        ExtraGain += inc;
        DynamicVars.Block.BaseValue = CurrentGain;
        if (DeckVersion is GeneticAlgorithm master)
        {
            master.ExtraGain += inc;
            MainFile.Logger.Info($"[Spire1] GA master buffed -> extra={master.ExtraGain}");
        }
        else
        {
            MainFile.Logger.Error("[Spire1] GA: DeckVersion missing/typed wrong — growth won't persist");
        }
    }

    protected override void AfterDowngraded()
    {
        // 降级时基值回落，保留永久成长
        DynamicVars.Block.BaseValue = CurrentGain;
    }
}
