using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 — Ornamental Fan (Uncommon). Every time you play 3 Attacks in a single turn, gain 4 Block.</summary>
public class OrnamentalFan : Spire1Relic
{
    private bool _isActivating;

    private int _attacksPlayedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount =>
        IsActivating ? DynamicVars.Cards.IntValue : AttacksPlayedThisTurn % DynamicVars.Cards.IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3), new BlockVar(4m, ValueProp.Unpowered)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Ornamental Fan",
            "#Every time you play 3 Attacks in a single turn, gain !B! Block.",
            "The fan seems to extend and harden as blood is spilled.");

    private bool IsActivating
    {
        get => _isActivating;
        set
        {
            _isActivating = value;
            UpdateDisplay();
        }
    }

    private int AttacksPlayedThisTurn
    {
        get => _attacksPlayedThisTurn;
        set
        {
            _attacksPlayedThisTurn = value;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (IsActivating || !CombatManager.Instance.IsInProgress)
        {
            Status = RelicStatus.Normal;
        }
        else
        {
            int threshold = DynamicVars.Cards.IntValue;
            Status = AttacksPlayedThisTurn % threshold == threshold - 1 ? RelicStatus.Active : RelicStatus.Normal;
        }
        InvokeDisplayAmountChanged();
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
            return Task.CompletedTask;
        AttacksPlayedThisTurn = 0;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || !CombatManager.Instance.IsInProgress || cardPlay.Card.Type != CardType.Attack)
            return;
        AttacksPlayedThisTurn++;
        if (AttacksPlayedThisTurn % DynamicVars.Cards.IntValue == 0)
        {
            TaskHelper.RunSafely(DoActivateVisuals());
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
        }
    }

    private async Task DoActivateVisuals()
    {
        IsActivating = true;
        Flash();
        await Cmd.Wait(1f);
        IsActivating = false;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        IsActivating = false;
        return Task.CompletedTask;
    }
}
