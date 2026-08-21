using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

[Pool(typeof(DefectCardPool))]
public class GeneticAlgorithm : Spire1Card
{
    private int _currentBlock = 2;
    private int _increasedBlock;

    public GeneticAlgorithm() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    [SavedProperty]
    public int CurrentBlock
    {
        get => _currentBlock;
        set
        {
            AssertMutable();
            _currentBlock = value;
            DynamicVars.Block.BaseValue = value;
        }
    }

    [SavedProperty]
    public int IncreasedBlock
    {
        get => _increasedBlock;
        set
        {
            AssertMutable();
            _increasedBlock = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(CurrentBlock, ValueProp.Move),
        new IntVar("Increase", 2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        await CommonActions.CardBlock(this, DynamicVars.Block, play);
        int increase = DynamicVars["Increase"].IntValue;
        BuffFromPlay(increase);
        (DeckVersion as GeneticAlgorithm)?.BuffFromPlay(increase);
    }

    protected override void OnUpgrade() => DynamicVars["Increase"].UpgradeValueBy(1);

    protected override void AfterDowngraded() => UpdateBlock();

    private void BuffFromPlay(int increase)
    {
        IncreasedBlock += increase;
        UpdateBlock();
    }

    private void UpdateBlock() => CurrentBlock = 2 + IncreasedBlock;
}
