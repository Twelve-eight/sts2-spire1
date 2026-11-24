using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 PerfectedStrike.countCards() projection: hand + draw pile + discard pile, predicate
/// hasTag(CardTags.STRIKE) (.tmp/audit/javap/PerfectedStrike.txt countCards @0-138, isStrike @0-7).
/// <para>
/// This deliberately does not use PlayerCombatState.AllCards (five piles - Exhaust and Play are
/// included there) and does not read a captured probe snapshot. The three legacy piles are read
/// when the card's CalculatedDamageVar is evaluated for the actual CardPlay.
/// </para>
/// </summary>
internal static class LegacyStrikeCount
{
    internal static int Count(CardModel card, CardPlay? actualPlay)
    {
        Player owner = card.Owner;
        PlayerCombatState? state = owner.PlayerCombatState;
        if (state is null)
            return 0;

        int count = CountPile(state.Hand, owner)
                  + CountPile(state.DrawPile, owner)
                  + CountPile(state.DiscardPile, owner);

        // StS1 useCard (AbstractPlayer.javap-asc @4242-4311) calls calculateCardDamage (@16) while
        // the card is still in hand, before hand.removeCard (@109). StS2 has already moved the
        // played card to the Play pile by the time the damage var is calculated, so project that
        // exact object back into the counted domain. Only the first non-auto play is projected:
        // the card was in hand and this is the same CardPlay being executed. Draw-top auto-plays,
        // CreateDupe copies and later same-object Replay plays are outside the verified domain.
        if (actualPlay is not null
            && ReferenceEquals(actualPlay.Card, card)
            && ReferenceEquals(actualPlay.Player, owner)
            && !actualPlay.IsAutoPlay
            && actualPlay.PlayIndex == 0
            && card.Pile?.Type == PileType.Play
            && IsStrike(card))
        {
            count++;
        }

        return count;
    }

    private static bool IsStrike(CardModel card)
    {
        foreach (CardTag tag in card.Tags)
        {
            if (tag == CardTag.Strike)
                return true;
        }
        return false;
    }

    private static int CountPile(CardPile pile, Player owner)
    {
        int count = 0;
        foreach (CardModel candidate in pile.Cards)
        {
            if (!ReferenceEquals(candidate.Owner, owner))
                continue;
            if (IsStrike(candidate))
                count++;
        }
        return count;
    }
}
