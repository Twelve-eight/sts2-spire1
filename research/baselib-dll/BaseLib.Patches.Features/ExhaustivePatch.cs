using System;
using System.Collections.Generic;
using System.Reflection;
using BaseLib.Cards.Variables;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Features;

[HarmonyPatch]
public static class ExhaustivePatch
{
	[HarmonyPatch(typeof(CardModel))]
	private static class OldExhaustivePatch
	{
		private static MethodInfo? TargetMethod = AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileTypeForCardPlay", (Type[])null, (Type[])null) ?? AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileType", (Type[])null, (Type[])null);

		private static IEnumerable<MethodBase> TargetMethods()
		{
			if (TargetMethod != null)
			{
				yield return TargetMethod;
			}
		}

		private static bool Prepare()
		{
			return TargetMethod != null;
		}

		[HarmonyPostfix]
		private static void ExhaustForExhaustive(CardModel __instance, ref PileType __result)
		{
			if (ShouldExhaustForExhaustive(__instance))
			{
				__result = (PileType)4;
			}
		}
	}

	[HarmonyPatch(typeof(CardModel))]
	private static class BetaExhaustivePatch
	{
		private static MethodInfo? TargetMethod = AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileTypeAndPositionForCardPlay", (Type[])null, (Type[])null);

		private static IEnumerable<MethodBase> TargetMethods()
		{
			if (TargetMethod != null)
			{
				yield return TargetMethod;
			}
		}

		private static bool Prepare()
		{
			return TargetMethod != null;
		}

		[HarmonyPostfix]
		private static void ExhaustForExhaustive(CardModel __instance, ref (PileType, CardPilePosition) __result)
		{
			if (ShouldExhaustForExhaustive(__instance))
			{
				__result = ((PileType)4, (CardPilePosition)1);
			}
		}
	}

	private static bool ShouldExhaustForExhaustive(CardModel card)
	{
		return GetExhaustive(card) == 1;
	}

	public static int GetExhaustive(CardModel card)
	{
		DynamicVar val = default(DynamicVar);
		int baseExhaustive = (card.DynamicVars.TryGetValue("Exhaustive", ref val) ? val.IntValue : 0);
		return ExhaustiveVar.ExhaustiveCount(card, baseExhaustive);
	}
}
