using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// DustyTome (the Darv ancient's card reward) draws a random Ancient-rarity card from
/// <c>player.Character.CardPool</c>. Our placeholder characters use custom pools whose only
/// members are SPIRE1-* cards — none Ancient — so the vanilla method rolls
/// <c>NextItem(empty)</c>, gets null, and dereferences <c>.Id</c>: NRE, event never opens.
/// (Log: NRE at DustyTome.SetupForPlayer ← Darv.GenerateInitialOptions.)
/// StS1 has no "Ancient" rarity; until legacy ancients are ported as real cards, fall back to
/// the base-game pool of the character we are standing in for (PlaceholderID), which always
/// has Ancient cards. Prefix must REPLACE the vanilla body when falling back: the NRE happens
/// inside the original method, so a postfix would never run.
/// BaseLib's own DustyTomePatch prefix runs first for ITomeCard characters and returns false;
/// our patch then sees the call too — guard by checking the pool ourselves either way.
/// </summary>
[HarmonyPatch(typeof(DustyTome), nameof(DustyTome.SetupForPlayer))]
internal static class DustyTomeAncientFallbackPatch
{
    [HarmonyPrefix]
    private static bool FallbackToNativePool(DustyTome __instance, Player player)
    {
        var placeholder = player.Character as PlaceholderCharacterModel;
        List<CardModel> ancient = FilterAncient(player.Character.CardPool, player);
        if (ancient.Count == 0 && placeholder == null) return true;
        if (ancient.Count > 0)
        {
            return true; // pool has Ancients — let the vanilla roll run
        }

        CardPoolModel? native = placeholder == null ? null : NativePoolFor(placeholder.PlaceholderID);
        if (native == null)
        {
            return true; // nothing better to do; keep vanilla behavior
        }

        List<CardModel> fallback = FilterAncient(native, player);
        if (fallback.Count == 0)
        {
            return true; // native pool unexpectedly empty too — keep vanilla behavior
        }

        __instance.AncientCard = player.PlayerRng.Rewards.NextItem(fallback.Select(c => c.Id));
        return false; // replaced: vanilla would NRE on the empty mod pool
    }

    private static List<CardModel> FilterAncient(CardPoolModel pool, Player player) =>
        pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == CardRarity.Ancient && !ArchaicTooth.TranscendenceCards.Contains(c))
            .ToList();

    private static CardPoolModel? NativePoolFor(string? placeholderId) => placeholderId switch
    {
        "ironclad" => ModelDb.CardPool<IroncladCardPool>(),
        "silent" => ModelDb.CardPool<SilentCardPool>(),
        "defect" => ModelDb.CardPool<DefectCardPool>(),
        "regent" => ModelDb.CardPool<RegentCardPool>(), // Watcher stands in for the Regent
        _ => null,
    };
}
