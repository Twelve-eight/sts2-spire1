using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Forms;

/// <summary>
/// One free manual card series on entry and at each of the owner's turn starts.
/// The allowance is reserved by the real SpendResources transaction and is consumed only by the
/// matching manual CardPlay. Auto-play never consumes it.
/// </summary>
public sealed class VoidFormEffectPower : CustomPowerModel
{
    private sealed class Data
    {
        public bool consumed;
        public CardPlay? pendingFreePlay;
        public CardModel? reservedCard;
        public CardModel? blockedCard;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override bool IsVisibleInternal => false;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Void Form",
            "#The next card you manually play this turn is *free*. Refreshes at the start of your turn.",
            "One free manual card each turn.");

    // PowerModel.DeepCloneFields creates fresh internal data, including all card identities.
    protected override object InitInternalData() => new Data();

    private bool ShouldSkip(CardModel card)
    {
        Data data = GetInternalData<Data>();
        if (card.Owner.Creature != Owner
            || (card.Pile?.Type != PileType.Hand && card.Pile?.Type != PileType.Play)
            || data.consumed
            || data.pendingFreePlay != null
            || ReferenceEquals(data.blockedCard, card))
        {
            return true;
        }

        // While a spend is in flight the allowance belongs to that one card. A different card
        // queried during the same window - a nested play, or a cost preview for another card -
        // must not read it as free.
        return data.reservedCard != null && !ReferenceEquals(data.reservedCard, card);
    }

    public override bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (ShouldSkip(card))
        {
            return false;
        }

        modifiedCost = 0m;
        return true;
    }

    public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (ShouldSkip(card))
        {
            return false;
        }

        modifiedCost = 0m;
        return true;
    }

    /// <summary>
    /// True when this power may still hand out its allowance to <paramref name="card"/>.
    /// Called at the synchronous entry of that card's real resource spend.
    /// </summary>
    internal bool CanReserveFor(CardModel card)
    {
        Data data = GetInternalData<Data>();
        return card.Owner.Creature == Owner
            && !data.consumed
            && data.pendingFreePlay == null
            && data.reservedCard == null
            && data.blockedCard == null;
    }

    internal void ReserveFor(CardModel card)
    {
        GetInternalData<Data>().reservedCard = card;
    }

    internal bool IsReservationFor(CardModel card)
        => ReferenceEquals(GetInternalData<Data>().reservedCard, card);

    internal void ReleaseReservation(CardModel card)
    {
        Data data = GetInternalData<Data>();
        if (ReferenceEquals(data.reservedCard, card))
        {
            data.reservedCard = null;
        }
    }

    internal void BlockCard(CardModel card)
    {
        Data data = GetInternalData<Data>();
        if (!data.consumed && data.pendingFreePlay == null && data.reservedCard == null)
        {
            data.blockedCard = card;
        }
    }

    internal void ClearBlockedCard(CardModel card)
    {
        Data data = GetInternalData<Data>();
        if (ReferenceEquals(data.blockedCard, card))
        {
            data.blockedCard = null;
        }
    }

    internal bool HasPendingFor(CardModel card)
        => ReferenceEquals(GetInternalData<Data>().pendingFreePlay?.Card, card);

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        Data data = GetInternalData<Data>();
        bool managedBySpend = VoidFormPlayTransaction.IsManaged(cardPlay.Card);
        bool ownsReservation = ReferenceEquals(data.reservedCard, cardPlay.Card);
        if (cardPlay.Player.Creature == Owner
            && !cardPlay.IsAutoPlay
            && cardPlay.IsFirstInSeries
            && !data.consumed
            && data.pendingFreePlay == null
            && !ReferenceEquals(data.blockedCard, cardPlay.Card)
            && (ownsReservation || !managedBySpend))
        {
            // Resources have already been spent. Keep the exact CardPlay identity so a nested play
            // cannot consume the allowance and a duplicate callback cannot consume it twice.
            data.pendingFreePlay = cardPlay;
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Data data = GetInternalData<Data>();
        if (ReferenceEquals(data.pendingFreePlay, cardPlay))
        {
            data.pendingFreePlay = null;
            data.reservedCard = null;
            data.blockedCard = null;
            data.consumed = true;
        }

        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner))
        {
            Data data = GetInternalData<Data>();
            data.pendingFreePlay = null;
            data.reservedCard = null;
            data.blockedCard = null;
            data.consumed = false;
        }

        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        Data data = GetInternalData<Data>();
        data.pendingFreePlay = null;
        data.reservedCard = null;
        data.blockedCard = null;
        return Task.CompletedTask;
    }
}
