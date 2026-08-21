using System;
using System.Collections.Generic;
using System.Reflection;
using BaseLib.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Features;

[HarmonyPatch]
public class PurgePatch
{
	[HarmonyPatch(typeof(CardModel))]
	private static class OldPurgePatch
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

		[HarmonyPrefix]
		private static bool GoAwayForever(CardModel __instance, ref PileType __result)
		{
			if (ShouldPurge(__instance))
			{
				__result = (PileType)0;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(CardModel))]
	private static class BetaPurgePatch
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

		[HarmonyPrefix]
		private static bool GoAwayForever(CardModel __instance, ref (PileType, CardPilePosition) __result)
		{
			if (ShouldPurge(__instance))
			{
				__result = ((PileType)0, (CardPilePosition)1);
				return false;
			}
			return true;
		}
	}

	public static bool ShouldPurge(CardModel c)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return c.Keywords.Contains(BaseLibKeywords.Purge);
	}
}
