using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// Marks a slime model that can be created by a split (or that participates in one).
/// <see cref="SpawnHp"/> is set by <see cref="SlimeSplit.SplitInto{T}"/> right before the
/// model is added to combat, so the child spawns with the parent's current HP — the exact
/// rule from StS1 bytecode (<c>new AcidSlime_M(x, y, 0, currentHealth)</c>).
/// </summary>
public interface ISlimeSplitSpawn
{
    /// <summary>Preset initial HP for a split-spawned slime; null when spawned normally.</summary>
    int? SpawnHp { get; set; }
}
