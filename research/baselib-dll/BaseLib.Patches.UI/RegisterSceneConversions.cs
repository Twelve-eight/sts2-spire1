using System;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.UI;

[HarmonyPatch(typeof(ModelDb), "Preload")]
internal class RegisterSceneConversions
{
	[HarmonyPostfix]
	private static void EnsureScenePathsRegistered()
	{
		foreach (Type registeredType in CustomContentDictionary.RegisteredTypes)
		{
			if (registeredType.IsAssignableTo(typeof(ISceneConversions)))
			{
				(ModelDb.GetById<AbstractModel>(ModelDb.GetId(registeredType)) as ISceneConversions)?.RegisterSceneConversions();
			}
		}
	}
}
