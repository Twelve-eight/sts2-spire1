using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Potions;

[Pool(typeof(Spire1PotionPool))]
public abstract class Spire1Potion : CustomPotionModel
{
	public override string? CustomPackedImagePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionImagePath();
	public override string? CustomPackedOutlinePath =>
		$"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PotionOutlineImagePath();
}