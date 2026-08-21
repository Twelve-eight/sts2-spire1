using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// Shared implementation of the StS1 slime split. Bytecode rule (AcidSlime_L.takeTurn /
/// SpikeSlime_L.takeTurn): the parent removes itself and two children of the next smaller
/// size spawn at its position, each constructed with the parent's <c>currentHealth</c>.
/// Only the L slimes split in vanilla — M and S never do.
/// </summary>
public static class SlimeSplit
{
    /// <summary>True when the parent has dropped to half HP or below (vanilla damage() check:
    /// <c>currentHealth &lt;= maxHealth / 2f</c>, not while dying, once per combat).</summary>
    public static bool ShouldSplit(Creature creature) =>
        creature is { IsDead: false } c && c.CurrentHp <= c.MaxHp / 2f;

    /// <summary>
    /// Spawns <paramref name="count"/> children of type <typeparamref name="T"/>, each with the
    /// parent's current HP, then removes the parent. Children are added first so the enemy side
    /// is never momentarily empty (which would end the combat).
    /// </summary>
    public static async Task SplitInto<T>(Spire1Monster parent, int count) where T : Spire1Monster, ISlimeSplitSpawn
    {
        var state = parent.CombatState;
        int hp = parent.Creature.CurrentHp;

        var spawned = new List<Creature>();
        for (int i = 0; i < count; i++)
        {
            // ModelDb.Monster<T>() returns the canonical instance (IsMutable == false);
            // CreatureCmd.Add asserts mutability. ToMutable() MemberwiseClones a mutable
            // copy — set SpawnHp AFTER cloning so the preset survives on the copy.
            var child = (T)ModelDb.Monster<T>().ToMutable();
            child.SpawnHp = hp;
            spawned.Add(await CreatureCmd.Add(child, state, parent.Creature.Side));
        }

        await CreatureCmd.Kill(parent.Creature);
    }
}
