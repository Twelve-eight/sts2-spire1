using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Config;
using Spire1.Spire1Code.Patches;

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
    /// <summary>
    /// Curse rewards and curse modifiers query this property after reading CurseCardPool.
    /// Keep the model registered for save compatibility, but fail closed at that consumer when
    /// the Spire1 cards group is disabled.
    /// </summary>
    public override bool CanBeGeneratedByModifiers =>
        !Spire1PowersGate.ContentUnavailableActive
        && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards);

    /// <summary>
    /// Keep curse transformation and other combat-factory paths closed with the same group gate.
    /// </summary>
    public override bool CanBeGeneratedInCombat =>
        !Spire1PowersGate.ContentUnavailableActive
        && Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Cards);
}
