using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Relics;

/// <summary>StS1 — Pen Nib (Uncommon). Every 10th Attack you play deals double damage. Counter persists between turns and combats.</summary>
public class PenNib : Spire1Relic
{
    private const int _attacksThreshold = 10;

    private bool _isActivating;

    private int _attacksPlayed;

    private CardModel? _attackToDouble;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShowCounter => true;

    public override int DisplayAmount =>
        IsActivating ? _attacksThreshold : AttacksPlayed % _attacksThreshold;

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "StS1 - Pen Nib",
            "#Every 10th Attack you play deals double damage.",
            "Holding the nib, you can see everyone ever slain by a previous owner of the pen. A violent history.");

    private bool IsActivating
    {
        get => _isActivating;
        set
        {
            _isActivating = value;
            UpdateDisplay();
        }
    }

    [SavedProperty]
    public int AttacksPlayed
    {
        get => _attacksPlayed;
        private set
        {
            _attacksPlayed = value % _attacksThreshold;
            UpdateDisplay();
        }
    }

    private CardModel? AttackToDouble
    {
        get => _attackToDouble;
        set => _attackToDouble = value;
    }

    private void UpdateDisplay()
    {
        if (IsActivating)
        {
            Status = RelicStatus.Normal;
        }
        else
        {
            Status = AttacksPlayed == _attacksThreshold - 1 ? RelicStatus.Active : RelicStatus.Normal;
        }
        InvokeDisplayAmountChanged();
    }

    private void NotifyAttackPlayed()
    {
        AttacksPlayed++;
        if (AttacksPlayed == 0)
        {
            TaskHelper.RunSafely(DoActivateVisuals());
        }
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (!props.IsPoweredAttack())
            return 1m;
        if (cardSource == null)
            return 1m;
        if (dealer != Owner.Creature && dealer != Owner.Osty)
            return 1m;
        if (AttackToDouble == null)
        {
            CardPile? pile = cardSource.Pile;
            if ((pile == null || pile.Type != PileType.Play) && AttacksPlayed == _attacksThreshold - 1)
                return 2m;
            return 1m;
        }
        if (cardSource == AttackToDouble)
            return 2m;
        return 1m;
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;
        if (cardPlay.Card.Owner != Owner)
            return Task.CompletedTask;
        NotifyAttackPlayed();
        if (AttacksPlayed == 0)
        {
            AttackToDouble = cardPlay.Card;
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (AttackToDouble == null)
            return Task.CompletedTask;
        if (cardPlay.Card != AttackToDouble)
            return Task.CompletedTask;
        AttackToDouble = null;
        return Task.CompletedTask;
    }

    private async Task DoActivateVisuals()
    {
        IsActivating = true;
        Flash();
        await Cmd.Wait(1f);
        IsActivating = false;
    }
}
