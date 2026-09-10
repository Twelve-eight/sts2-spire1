# StS2 HP-loss pipeline: phases and famous interactions (KB: engine-facts supplement)

Question that spawned this entry (user quiz 2026-09-10): 发条靴 (The Boot, THE_BOOT) vs
无实体 (Intangible) - which wins?

## Answer

**发条靴 wins for attack damage: the 1 becomes 5.**

## Evidence chain (all from decompiled engine, v0.111)

1. `CreatureCmd.Damage` (CreatureCmd.cs:288-305) computes unblocked damage, then runs
   `Hook.ModifyHpLost(..., HpLossHookPhase.BeforeOsty)` and again with `AfterOsty`.
2. `Hook.ModifyHpLost` (Hook.cs:1726) runs, per phase, ALL non-Late hooks first, then ALL
   Late hooks (`ModifyHpLostBeforeOsty` loop, then `ModifyHpLostBeforeOstyLate` loop;
   same pattern for AfterOsty).
3. `IntangiblePower.ModifyHpLostAfterOsty` (IntangiblePower.cs): target==owner,
   amount>=1 -> cap to 1. NON-Late phase.
4. `TheBoot.ModifyHpLostAfterOstyLate` (TheBoot.cs): dealer==owner (outgoing damage),
   `props.IsPoweredAttack()`, amount>=1 and < DamageMinimum(5) -> raise to 5. LATE phase.

So the chain for a 10-damage unblocked attack against an Intangible target, attacker
holding The Boot: 10 -> (Intangible, AfterOsty) -> 1 -> (TheBoot, AfterOstyLate) -> 5.

The Boot description matches: "当你造成小于 {DamageThreshold}(4) 点未被格挡的攻击伤害
时,将伤害提升为 {DamageMinimum}(5)". Note the zhs loc says DamageThreshold 4 but the code
only checks `amount < DamageMinimum` (threshold var is display-only in this build).

## Scope limits

- Intangible caps ALL incoming HP loss (any source, any amount>=1 -> 1); The Boot only
  raises outgoing POWERED ATTACK damage. Non-attack damage vs Intangible stays 1 (no boot).
- `IntangiblePower.ModifyDamageCap` (cap 1) is preview-only duplication; authoritative
  value is the HpLost chain.
- Listener order WITHIN a phase comes from `runState.IterateHookListeners` (stable but
  model-order dependent); across phases, Late always after non-Late.

## Sibling modifiers on the same pipeline (for future KB questions)

| Modifier | Hook | Effect |
|---|---|---|
| IntangiblePower (无实体) | AfterOsty | incoming >=1 -> 1 |
| TheBoot (发条靴) | AfterOstyLate | outgoing powered-attack <5 -> 5 |
| TungstenRod (钨棒) | AfterOsty | incoming -1 (min 0) |
| BufferPower (缓冲) | AfterOstyLate | incoming -> 0 (consumes stack) |
| SlipperyPower (油滑?) | AfterOsty | (see source) |
| BeatingRemnant | (relic) | see source |

Late-phase beats non-Late when both touch the same number: final word = last Late hook
in listener order that changes the value.

## KB gap this exposed

research/kb had ZERO coverage of Intangible and the HP-loss hook pipeline before this
question (grep: no 'Intangible', no 'Clockwork'/'TheBoot' hits). The pipeline knowledge
lived only in the decompile. Lesson: damage/HP-loss modifier phases (BeforeOsty/AfterOsty
x base/Late) belong in kb/engine-facts.md - this file is that entry.

## Supplement: Doom (灾厄, DOOM_POWER) vs pipeline modifiers - no interaction

User quiz 2026-09-10 (follow-up): 发条靴/无实体优先级之后第二问 - 灾厄 vs 无实体?

**Verdict: 灾厄 completely bypasses 无实体 (and every HP-loss modifier). Doom kills
outright; there is no priority contest because Doom never enters the damage pipeline.**

Evidence (DoomPower.cs, engine v0.111):

1. Doom triggers at side-turn end: `BeforeSideTurnEnd` (enemy side) / `AfterSideTurnEnd`
   (player side) -> `ShouldDoomTrigger` -> `IsOwnerDoomed()` = `Owner.CurrentHp <= Amount`.
2. Kill executes via `DoomKill` -> `CreatureCmd.Kill(creature)` - a DIRECT kill command,
   not `CreatureCmd.Damage`. Zero HP-loss hooks run.
3. `IntangiblePower`'s full method surface: `ModifyHpLostAfterOsty`,
   `AfterModifyingHpLostAfterOsty`, `ModifyDamageCap`, `AfterModifyingDamageAmount`,
   `AfterSideTurnEnd` (self-decrement). It has NO death/kill hooks - it only shapes
   numbers that pass through Damage/HpLost, and Doom passes no numbers.

Corollary: the same bypass applies to TungstenRod, BufferPower, TheBoot, and any other
modifier living in the HP-loss/damage pipeline. Only `CreatureCmd.Kill(force:false)`
death-prevention (Fairy-in-a-Bottle-style `ShouldDie` effects) can interpose.

Timing note: on the enemy side, Doom fires in the Before phase of side-turn-end, before
Intangible's own AfterSideTurnEnd decrement - but even stacks remaining are irrelevant
since the kill is direct.