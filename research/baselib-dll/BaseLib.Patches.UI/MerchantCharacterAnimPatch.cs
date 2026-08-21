using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace BaseLib.Patches.UI;

[HarmonyPatch(typeof(NMerchantCharacter), "PlayAnimation")]
internal class MerchantCharacterAnimPatch
{
	[HarmonyPrefix]
	public static bool SkipAnimIfNotSpine(NMerchantCharacter __instance, string anim, bool loop)
	{
		return !CustomAnimation.PlayCustomAnimation((Node)(object)__instance, GetAnimNames(anim));
	}

	private static string[] GetAnimNames(string animName)
	{
		if (!(animName == "relaxed_loop"))
		{
			if (animName == "die")
			{
				return new string[2] { "Die", animName };
			}
			return new string[1] { animName };
		}
		return new string[3] { "idle", "Idle", animName };
	}
}
