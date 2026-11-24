using System;
using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// Projects the StS1 cost fields consumed by AllCostToHandAction and ScrapeFollowUpAction
/// from a single StS2 card cost object.
///
/// StS1 AllCostToHandAction reads the stored energy cost and freeToPlayOnce. StS2 exposes
/// neither field directly: the stored-cost equivalent is the base cost plus exact EndOfCombat
/// local modifiers, while a freeToPlayOnce equivalent is an exact WhenPlayed-only absolute
/// zero, non-reduce-only modifier. EndOfTurn|WhenPlayed is deliberately not accepted as freeOnce.
///
/// StS1 ScrapeFollowUpAction::update @52-84 reads a different pair: costForTurn and
/// freeToPlayOnce. costForTurn is projected from the engine's Local modifier set (the same
/// modifiers that produce the displayed turn cost), with X mapped back to the StS1 -1 sentinel
/// because S2 stores 0 as the X placeholder. The freeOnce marker is the same exact one above;
/// the effective All/Global cost is deliberately not substituted.
///
/// A negative base is the StS1 unplayable/X sentinel, not a zero cost: it is projected
/// exactly as the engine stores it (the engine itself returns a negative _base before
/// applying any local modifier), and only a non-negative base clamps at 0 the way an
/// StS1 cost reduction does.
/// </summary>
internal static class LegacyRecallCost
{
    private static readonly FieldInfo? LocalModifiersField = typeof(CardEnergyCost).GetField(
        "_localModifiers", BindingFlags.Instance | BindingFlags.NonPublic);

    internal static bool Matches(CardModel card)
    {
        if (!IsKnownCostSource(card))
            return HostMatches(card);

        return TryReadLegacyState(card, out int permanentCost, out _, out bool freeOnce)
            ? permanentCost == 0 || freeOnce
            : HostMatches(card);
    }

    internal static bool ShouldGrantSetupFreeOnce(CardModel card)
    {
        if (!IsKnownCostSource(card))
            return !card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.All) > 0;

        return TryReadLegacyState(card, out int permanentCost, out _, out bool freeOnce)
            ? permanentCost > 0 && !freeOnce
            : !card.EnergyCost.CostsX && card.EnergyCost.GetWithModifiers(CostModifiers.All) > 0;
    }

    /// <summary>
    /// StS1 ScrapeFollowUpAction::update @52-84 discards a drawn card exactly when
    /// `costForTurn != 0 &amp;&amp; !freeToPlayOnce`; the X sentinel (-1) therefore discards.
    /// Only the exact objects returned by the real draw are ever passed here.
    /// </summary>
    internal static bool DiscardsOnScrape(CardModel card)
    {
        if (!IsKnownCostSource(card))
            return HostScrapeMatches(card);

        return TryReadLegacyState(card, out _, out int forTurnCost, out bool freeOnce)
            ? forTurnCost != 0 && !freeOnce
            : HostScrapeMatches(card);
    }

    private static bool IsKnownCostSource(CardModel card)
        => card is Spire1Card || card.GetType().Assembly == typeof(CardModel).Assembly;

    private static bool HostMatches(CardModel card)
        => card.EnergyCost.GetWithModifiers(CostModifiers.All) == 0 && !card.EnergyCost.CostsX;

    // An untracked third-party card keeps the shipped filter (Models.Cards/Scrape.cs:32) because
    // its StS1 provenance is unknown; only the tracked projection may reinterpret its cost.
    private static bool HostScrapeMatches(CardModel card)
        => card.EnergyCost.GetWithModifiers(CostModifiers.All) != 0 || card.EnergyCost.CostsX;

    private static bool TryReadLegacyState(CardModel card, out int permanentCost, out int forTurnCost, out bool freeOnce)
    {
        permanentCost = 0;
        forTurnCost = 0;
        freeOnce = false;
        try
        {
            if (LocalModifiersField is null || LocalModifiersField.FieldType != typeof(List<LocalCostModifier>)
                || LocalModifiersField.GetValue(card.EnergyCost) is not List<LocalCostModifier> modifiers)
                return false;

            bool costsX = card.EnergyCost.CostsX;
            int unmodified = costsX ? -1 : card.EnergyCost.GetWithModifiers(CostModifiers.None);
            int projected = unmodified;
            foreach (LocalCostModifier modifier in modifiers)
            {
                // An X card keeps the StS1 -1 sentinel even though StS2 stores a zero
                // placeholder. A separate exact freeOnce marker may still qualify it.
                // The engine returns a negative _base before applying local modifiers,
                // so a negative stored cost keeps its sentinel value here as well;
                // clamping it would make unplayable cards look free to All For One.
                if (!costsX && unmodified >= 0 && modifier.Expiration == LocalCostModifierExpiration.EndOfCombat)
                {
                    if (modifier.Type != LocalCostType.Absolute && modifier.Type != LocalCostType.Relative)
                        return false;
                    projected = modifier.Modify(projected);
                }

                // Exact equality matters: SetThisTurnOrUntilPlayed is EndOfTurn|WhenPlayed
                // and represents costForTurn, not freeToPlayOnce.
                if (modifier.Expiration == LocalCostModifierExpiration.WhenPlayed
                    && modifier.Type == LocalCostType.Absolute && modifier.Amount == 0 && !modifier.IsReduceOnly)
                    freeOnce = true;
            }

            // StS1 clamps a real cost reduction at 0, so an over-reduced non-negative base
            // must not drift negative. Only the shipped sentinel stays negative; deleting
            // this clamp for every base would stop matching a genuinely free card.
            permanentCost = costsX ? -1 : (unmodified < 0 ? unmodified : Math.Max(0, projected));

            // ScrapeFollowUpAction reads costForTurn, which is the engine's Local projection
            // (all local modifiers, clamped at 0). GetWithModifiers returns the 0 placeholder
            // for X and the raw negative sentinel for an unplayable base, so X is mapped back
            // to StS1's -1 here; the negative sentinel already survives the call unchanged.
            forTurnCost = costsX ? -1 : card.EnergyCost.GetWithModifiers(CostModifiers.Local);
            return true;
        }
        catch (Exception)
        {
            // A future engine layout or an inaccessible private field must preserve the
            // host card's playable behavior. It must not make a mixed deck fail closed.
            permanentCost = 0;
            forTurnCost = 0;
            freeOnce = false;
            return false;
        }
    }
}
