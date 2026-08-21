using BaseLib.Abstracts;
using BaseLib.Utils;
using Spire1.Spire1Code.Character;
using Spire1.Spire1Code.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// Base class for all Ironclad (Spire1) cards. Carries the pool tag so concrete cards need no [Pool].
/// M1: every card uses the shipped placeholder art (card.png); real per-card art is a later wave.
/// </summary>
[Pool(typeof(Spire1CardPool))]
public abstract class Spire1Card(int cost, CardType type, CardRarity rarity, TargetType target)
    : CustomCardModel(cost, type, rarity, target)
{
    public override string CustomPortraitPath => "card.png".BigCardImagePath();
    public override string PortraitPath => "card.png".CardImagePath();
}
