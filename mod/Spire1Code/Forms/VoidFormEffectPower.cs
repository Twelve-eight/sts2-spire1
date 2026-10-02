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
        public CardModel? consumedCard;
        public VoidFormPlayTransaction.SpendToken? consumedToken;
        public CardPlay? pendingFreePlay;
        public VoidFormPlayTransaction.SpendToken? pendingToken;
        public CardModel? reservedCard;
        public VoidFormPlayTransaction.SpendToken? reservedToken;
        public CardModel? blockedCard;
        public VoidFormPlayTransaction.SpendToken? blockedToken;
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

        // A different card cannot borrow this reservation. For the reserved card, a nested or
        // later SpendResources invocation must pay normally unless it is the exact invocation
        // that created the reservation. A post-payment cost query without an active invocation
        // remains free until the matching CardPlay consumes the allowance.
        if (data.reservedCard == null)
        {
            return false;
        }

        if (!ReferenceEquals(data.reservedCard, card))
        {
            return true;
        }

        return VoidFormPlayTransaction.HasCurrentSpendFor(card)
            && !VoidFormPlayTransaction.IsCurrentSpendFor(card, this);
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
    /// Try to bind one real SpendResources transaction to this power's allowance.
    /// The token is the generation identity used by every later cleanup path.
    /// </summary>
    internal bool TryReserveFor(CardModel card, VoidFormPlayTransaction.SpendToken token)
    {
        Data data = GetInternalData<Data>();
        if (card.Owner.Creature != Owner
            || data.consumed
            || data.pendingFreePlay != null
            || data.reservedToken != null
            || data.blockedCard != null)
        {
            return false;
        }

        data.reservedCard = card;
        data.reservedToken = token;
        return true;
    }

    internal bool IsReservationFor(CardModel card, VoidFormPlayTransaction.SpendToken token)
    {
        Data data = GetInternalData<Data>();
        return ReferenceEquals(data.reservedCard, card)
            && ReferenceEquals(data.reservedToken, token);
    }

    internal void ReleaseReservation(CardModel card, VoidFormPlayTransaction.SpendToken token)
    {
        Data data = GetInternalData<Data>();
        if (ReferenceEquals(data.reservedCard, card)
            && ReferenceEquals(data.reservedToken, token))
        {
            data.reservedCard = null;
            data.reservedToken = null;
        }
    }

    internal void BlockCard(CardModel card, VoidFormPlayTransaction.SpendToken? token)
    {
        Data data = GetInternalData<Data>();
        bool blockedSlotAvailable = data.blockedCard == null
            || (ReferenceEquals(data.blockedCard, card)
                && ReferenceEquals(data.blockedToken, token));
        if (!data.consumed
            && data.pendingFreePlay == null
            && blockedSlotAvailable
            && (data.reservedToken == null || ReferenceEquals(data.reservedToken, token)))
        {
            data.blockedCard = card;
            data.blockedToken = token;
            if (token != null)
            {
                VoidFormPlayTransaction.AdoptBlockedToken(this, token);
            }
        }
    }

    internal void ClearBlockedCard(CardModel card, VoidFormPlayTransaction.SpendToken? token)
    {
        Data data = GetInternalData<Data>();
        if (ReferenceEquals(data.blockedCard, card)
            && ((token == null && data.blockedToken == null)
                || ReferenceEquals(data.blockedToken, token)))
        {
            data.blockedCard = null;
            data.blockedToken = null;
        }
    }

    internal void ClearFailedPlay(
        CardModel card,
        VoidFormPlayTransaction.SpendToken? token,
        bool rollbackConsumed)
    {
        Data data = GetInternalData<Data>();
        bool sameToken(VoidFormPlayTransaction.SpendToken? candidate)
            => (token == null && candidate == null) || ReferenceEquals(candidate, token);

        if (ReferenceEquals(data.pendingFreePlay?.Card, card)
            && sameToken(data.pendingToken))
        {
            data.pendingFreePlay = null;
            data.pendingToken = null;
        }
        if (ReferenceEquals(data.reservedCard, card)
            && sameToken(data.reservedToken))
        {
            data.reservedCard = null;
            data.reservedToken = null;
        }
        if (ReferenceEquals(data.blockedCard, card)
            && sameToken(data.blockedToken))
        {
            data.blockedCard = null;
            data.blockedToken = null;
        }
        if (rollbackConsumed
            && ReferenceEquals(data.consumedCard, card)
            && sameToken(data.consumedToken))
        {
            data.consumedCard = null;
            data.consumedToken = null;
            data.consumed = false;
        }
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Player.Creature != Owner
            || cardPlay.IsAutoPlay
            || !cardPlay.IsFirstInSeries)
        {
            return Task.CompletedTask;
        }

        Data data = GetInternalData<Data>();
        if (data.consumed
            || data.pendingFreePlay != null
            || ReferenceEquals(data.blockedCard, cardPlay.Card))
        {
            return Task.CompletedTask;
        }

        VoidFormPlayTransaction.SpendToken? spend =
            VoidFormPlayTransaction.ClaimForBeforeCardPlayed(cardPlay, this);
        if (spend != null)
        {
            // The transaction patch has already proved that this CardPlay is running inside the
            // matching OnPlayWrapper and that the payment token owns this reservation.
            data.pendingFreePlay = cardPlay;
            data.pendingToken = spend;
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Data data = GetInternalData<Data>();
        if (ReferenceEquals(data.pendingFreePlay, cardPlay))
        {
            VoidFormPlayTransaction.SpendToken? token = data.pendingToken;
            data.pendingFreePlay = null;
            data.pendingToken = null;
            if (token != null)
            {
                ReleaseReservation(cardPlay.Card, token);
            }
            ClearBlockedCard(cardPlay.Card, token);
            data.consumedCard = cardPlay.Card;
            data.consumedToken = token;
            data.consumed = true;
        }

        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        // Reclaim payment tokens even when this owner is absent from a multiplayer extra-turn
        // participant list; only the allowance refresh itself is owner-turn scoped.
        VoidFormPlayTransaction.CancelOrphanTokensForOwner(Owner);
        VoidFormPlayTransaction.CancelForPower(this);
        Data data = GetInternalData<Data>();
        data.pendingFreePlay = null;
        data.pendingToken = null;
        data.reservedCard = null;
        data.reservedToken = null;
        data.blockedCard = null;
        data.blockedToken = null;
        if (!participants.Contains(Owner))
        {
            return Task.CompletedTask;
        }

        data.consumedCard = null;
        data.consumedToken = null;
        data.consumed = false;
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        VoidFormPlayTransaction.CancelOrphanTokensForOwner(oldOwner);
        VoidFormPlayTransaction.CancelForPower(this);
        Data data = GetInternalData<Data>();
        data.pendingFreePlay = null;
        data.pendingToken = null;
        data.reservedCard = null;
        data.reservedToken = null;
        data.blockedCard = null;
        data.blockedToken = null;
        data.consumedCard = null;
        data.consumedToken = null;
        data.consumed = false;
        return Task.CompletedTask;
    }
}
