using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Splash (StS2 Defect rare) offers attacks from OTHER characters' pools. Vanilla code
/// removes only the holder's own pool OBJECT from the candidate list, which is insufficient
/// for our ported characters: e.g. a SPIRE1-DEFECT run legitimately draws shipped StS2
/// Defect attacks (SharedCardReuse), so those models "belong to gen-1 self" even though the
/// vanilla Defect pool object is a different instance still present in the candidate list.
///
/// Fix per user directive: candidate set = ALL characters' attacks MINUS the holder's own
/// card SET (by id), instead of excluding pool objects. For vanilla holders this is a no-op
/// (their models appear in no other character pool).
/// Replaces OnPlay wholesale (prefix returns false) because the candidate enumeration is
/// inline; the mock-card test branch is preserved verbatim.
/// </summary>
[HarmonyPatch(typeof(Splash))]
[HarmonyPatch("OnPlay")]
internal static class SplashOwnSetSubtractPatch
{
    static bool Prefix(Splash __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
    {
        __result = RunAsync(__instance, choiceContext, cardPlay);
        return false; // skip original
    }

    private static async Task RunAsync(Splash splash, PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var mock = Traverse.Create(splash).Field("_mockGeneratedCard")?.GetValue<CardModel>();
        CardModel? chosen;
        if (mock == null)
        {
            var player = splash.Owner;
            List<CardPoolModel> pools = player.UnlockState.CharacterCardPools.ToList();
            if (pools.Count > 1)
            {
                pools.Remove(player.Character.CardPool);
            }

            HashSet<string> ownIds = new(
                player.Character.CardPool
                    .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                    .Select(c => c.Id.Entry));

            IEnumerable<CardModel> cards = pools
                .SelectMany(p => p.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
                .Where(c => c.Type == CardType.Attack && !ownIds.Contains(c.Id.Entry));

            List<CardModel> offers = CardFactory
                .GetDistinctForCombat(player, cards, 3, player.RunState.Rng.CombatCardGeneration)
                .ToList();
            if (splash.IsUpgraded)
            {
                foreach (var offer in offers)
                {
                    CardCmd.Upgrade(offer);
                }
            }
            chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, offers, splash.Owner, canSkip: true);
        }
        else
        {
            chosen = mock;
            if (splash.IsUpgraded)
            {
                CardCmd.Upgrade(chosen);
            }
        }
        if (chosen != null)
        {
            chosen.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, splash.Owner);
        }
    }
}
