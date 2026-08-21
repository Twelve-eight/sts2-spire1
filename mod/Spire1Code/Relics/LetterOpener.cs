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

/// <summary>StS1 — Letter Opener (Uncommon). Every time you play 3 Skills in a single turn, deal 5 damage to ALL enemies.</summary>
public class LetterOpener : Spire1Relic
{
    private bool _isActivating;

    private int _skillsPlayedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => CombatManager.Instance.IsInProgress;

    public override int DisplayAmount =>
        IsActivating ? DynamicVars.Cards.IntValue : SkillsPlayedThisTurn % DynamicVars.Cards.IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3), new DamageVar(5m, ValueProp.Unpowered)];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Letter Opener",
            "#Every time you play 3 Skills in a single turn, deal !D! damage to ALL enemies.",
            "Unnaturally sharp.");

    private bool IsActivating
    {
        get => _isActivating;
        set
        {
            _isActivating = value;
            UpdateDisplay();
        }
    }

    private int SkillsPlayedThisTurn
    {
        get => _skillsPlayedThisTurn;
        set
        {
            _skillsPlayedThisTurn = value;
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        if (IsActivating)
        {
            Status = RelicStatus.Normal;
        }
        else
        {
            int threshold = DynamicVars.Cards.IntValue;
            Status = SkillsPlayedThisTurn % threshold == threshold - 1 ? RelicStatus.Active : RelicStatus.Normal;
        }
        InvokeDisplayAmountChanged();
    }

    public override Task BeforeCombatStart()
    {
        SkillsPlayedThisTurn = 0;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
            return Task.CompletedTask;
        if (Owner.PlayerCombatState.TurnNumber == 1)
            return Task.CompletedTask;
        SkillsPlayedThisTurn = 0;
        Status = RelicStatus.Normal;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner || !CombatManager.Instance.IsInProgress || cardPlay.Card.Type != CardType.Skill)
            return;
        SkillsPlayedThisTurn++;
        if (SkillsPlayedThisTurn % DynamicVars.Cards.IntValue == 0)
        {
            TaskHelper.RunSafely(DoActivateVisuals());
            await CreatureCmd.Damage(choiceContext, Owner.Creature.CombatState.HittableEnemies, DynamicVars.Damage, Owner.Creature);
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
        Status = RelicStatus.Normal;
        IsActivating = false;
        return Task.CompletedTask;
    }
}
