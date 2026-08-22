using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// Base class for all StS1 curse cards. Cost -1, CardType.Curse, CardRarity.Curse, TargetType.None.
/// Registered into the base-game shared <see cref="CurseCardPool"/> via the inherited [Pool] attribute,
/// so curses can be generated wherever the game generates curses. Curses never upgrade.
/// </summary>
[Pool(typeof(CurseCardPool))]
public abstract class Spire1Curse() : CustomCardModel(-1, CardType.Curse, CardRarity.Curse, TargetType.None)
{
    public override int MaxUpgradeLevel => 0;

    public override string CustomPortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigCardImagePath();
    public override string PortraitPath => $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".CardImagePath();
}
