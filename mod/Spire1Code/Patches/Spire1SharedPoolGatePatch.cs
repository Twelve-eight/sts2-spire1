using System.Reflection;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Cards gate for the SHARED colorless pool (user request 2026-09-13): the 11
/// StS1 colorless cards are registered into the ENGINE's ColorlessCardPool via
/// [Pool(typeof(ColorlessCardPool))], so base-game shops/rewards offered them
/// even with the card injection toggle off (the toggle existed but had no
/// consumer). This postfix removes them from every GetUnlockedCards result
/// while EnableSts1Cards is off. Other Spire1 cards (character pools) and all
/// vanilla cards are untouched; composes with Perfect's gate on the same
/// method (independent filters).
/// </summary>
[HarmonyPatch(typeof(CardPoolModel), nameof(CardPoolModel.GetUnlockedCards))]
internal static class Spire1SharedPoolGatePatch
{
    private static readonly HashSet<Type> SharedColorlessCardTypes = Build();

    private static HashSet<Type> Build()
    {
        var set = new HashSet<Type>();
        foreach (var t in typeof(Spire1Card).Assembly.GetTypes())
        {
            if (!t.IsSubclassOf(typeof(Spire1Card)))
            {
                continue;
            }
            var pool = t.GetCustomAttribute<PoolAttribute>();
            if (pool?.PoolType == typeof(ColorlessCardPool))
            {
                set.Add(t);
            }
        }
        return set;
    }

    private static void Postfix(ref IEnumerable<CardModel> __result)
    {
        if (Spire1Config.CardsEnabled)
        {
            return;
        }
        __result = __result.Where(c => !SharedColorlessCardTypes.Contains(c.GetType()));
    }
}
