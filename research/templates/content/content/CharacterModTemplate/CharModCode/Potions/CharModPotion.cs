using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CharMod.CharModCode.Character;
using CharMod.CharModCode.Extensions;

namespace CharMod.CharModCode.Potions;

[Pool(typeof(CharModPotionPool))]
public abstract class CharModPotion : CustomPotionModel
{
	public override string? CustomPackedImagePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
	public override string? CustomPackedOutlinePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}