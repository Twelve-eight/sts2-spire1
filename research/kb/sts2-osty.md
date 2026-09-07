# StS2 Osty / Pet System (Necrobinder Pets) - sts2-spire1 Knowledge Base

## Scope of This Volume
The Necrobinder's pet system: Osty summoning, the DieForYou damage redirect, pet lifecycle and death linkage. Sources: engine decompile `research/engine-dllsrc/` (C#); every claim cites file + line. **Legend**: **High** = directly readable in decompile / **Medium** = inferred (noted).

## 1. Data Model and Ownership

**O01 Pet ownership chain** - Source `Entities.Creatures/Creature.cs`: `PetOwner` (L181), `IsPet` (L200), `Pets` (L208); `Entities.Players/Player.cs`: `Osty` (L132), `IsOstyAlive` (L137), `IsOstyMissing` (L142). Confidence: **High**
```
Player.Osty -> Creature (the pet instance)
Creature.PetOwner -> Creature (back-reference; the owning player's creature)
Creature.IsPet -> bool marker; Creature.Pets -> pet collection on any creature
```

**O02 Summon pipeline** - Source `Commands/OstyCmd.cs`: `Summon` (L35), idempotent re-summon guard (L47-51), `DieForYouPower` applied to Osty (L74). Confidence: **High**
Re-summoning while an Osty is alive is idempotent (guards at L47-51). The pet carries `DieForYouPower` from birth (L74) - the redirect is a power on the PET, not on the owner.

## 2. The Damage Redirect

**O03 DieForYou redirect hook** - Source `Models.Powers/DieForYouPower.cs`: `ModifyUnblockedDamageTarget` (L15); damage pipeline dispatch `Commands/CreatureCmd.cs` L286-293 (BeforeOsty hooks -> `Hook.ModifyUnblockedDamageTarget` at L290 -> AfterOsty hooks); `Hooks/Hook.cs` L2057-2065. Confidence: **High**
```
DieForYouPower.ModifyUnblockedDamageTarget (on Osty):
  attacks whose target is Owner.PetOwner.Creature (the player) are re-targeted to Osty
```
Dispatch order in CreatureCmd (L286-293): BeforeOsty hook sweep -> the redirect hook itself -> AfterOsty hook sweep. The Hook dispatch at Hook.cs L2057-2065 is a DIRECT call (bypasses the combat-ending guard that wraps many other hook families) - the redirect still evaluates while combat is ending.

**O04 Redirect scope** - the redirect applies to UNBLOCKED damage targeting the owner (hook name: ModifyUnblockedDamageTarget). Blocked damage, non-attack HP loss, and effects that do not go through the unblocked-damage gate are NOT redirected. Confidence: **High** (hook name + call site) / **Medium** (exhaustive non-attack enumeration not done).

## 3. Death Linkage

**O05 Pet death linkage** - Source `Entities.Players/PlayerCombatState.cs`: `AddPetInternal` (L234), `pet.Died += OnPetDied` subscription (L243), `OnPetDied` (L281), `GetPet<T>()` (L255). Confidence: **High**
Pet death is an event the owner's combat state subscribes to at add time; `OnPetDied` (L281) is the single reaction point (re-permit re-summon etc.).

**O06 Owner death kills pets** - Source `Commands/CreatureCmd.cs` L581-584. Confidence: **High**
When the owner's creature dies, the engine kills Osty at CreatureCmd L581-584 (no orphan pets outliving their owner in combat).

**O07 Power/pet removal interplay** - Source `DieForYouPower.cs`: `ShouldCreatureBeRemovedFromCombatAfterDeath` (L40), `ShouldPowerBeRemovedAfterOwnerDeath` (L49). Confidence: **High**
These two virtual answers govern the cleanup ordering when the pet carrying the power dies vs when the power's owner dies - porting any owner-pet power must answer both questions explicitly.

## 4. Osty as a Monster Model

**O08 Osty monster model** - Source `Models/Monsters/Osty.cs` (L13). Osty is registered as a monster model (HP/behavior live there), summoned into the player's side via OstyCmd. Confidence: **High** (registration) / **Medium** (its monster AI specifics are outside this volume's scope).

## 5. Arbitration Case Table

| Scenario | Outcome | Basis |
|---|---|---|
| Porting StS1 "blocks for you" companions (e.g. decoy-style effects) | Model as a pet + DieForYouPower subclass answering O07's two virtuals; do NOT try to patch the damage pipeline directly - the BeforeOsty/redirect/AfterOsty slots exist for exactly this | O03 |
| Pet surviving owner death | Engine forbids it (owner death kills Osty first) | O06 |
| Third-party characters gaining pets | AddPetInternal + the Died subscription is the only sanctioned entry; direct list mutation bypasses the event linkage | O05 |
| Redirect and combat-ending | The hook family bypasses the combat-ending guard; redirects still resolve during the death window | O03 |

## 6. Open Questions / Low-Confidence Items

1. Osty monster AI specifics (Models/Monsters/Osty.cs body) not itemized here; read on demand when porting pet behaviors. Confidence: **Medium**.
2. Whether multiple pets per creature are supported in practice (`Pets` collection exists; only Osty observed as consumer). Confidence: **Medium**.
