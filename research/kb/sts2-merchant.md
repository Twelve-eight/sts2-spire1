# StS2 Merchant Inventory - sts2-spire1 Knowledge Base

## Scope of This Volume
StS2 shop stock generation: MerchantInventory slot population and the ModifyMerchantCardPool hook chain. StS1 comparison anchors loot-rewards.md L04-L06/L14. Sources: engine decompile `research/engine-dllsrc/`. **Legend**: **High** = directly readable / **Medium** = inferred (noted).

## 1. Inventory Population

**M01 MerchantInventory construction** - Source `MegaCrit.Sts2.Core.Entities.Merchant/MerchantInventory.cs`: `MerchantInventory` (L13), `CreateForNormalMerchant` (L78), `PopulateCharacterCardEntries` (L98), `PopulateColorlessCardEntries` (L114), `PopulateRelicEntries` (L126), `PopulatePotionEntries` (L141), `AddRelicEntry` (L93), `UpdateEntries` (L147). Confidence: **High**

**M02 Card slots** - Source `PopulateCharacterCardEntries` (L98): 5 colored-card slots + on-sale roll via `PlayerRng.Shops`; `PopulateColorlessCardEntries` (L114). Confidence: **High**
Both consult `GetUnlockedCards(Player.UnlockState, Player.RunState.CardMultiplayerConstraint)` (L101, L116) - shop stock respects progression AND multiplayer constraints (ties to sts2-unlock-epoch.md U01/U02).

**M03 Relic slots** - Source `PopulateRelicEntries` (L126): 2 rolled relics + 1 Shop-tier; `AddRelicEntry` (L93). Confidence: **High**
StS1 comparison: StS1's third slot is likewise always SHOP tier (loot-rewards L14); StS2 keeps the 2+1 shape.

**M04 Potion slots** - Source `PopulatePotionEntries` (L141). Confidence: **High** (slot count read; per-slot pricing formula not itemized here - see open question 1).

## 2. ModifyMerchantCardPool Hook

**M05 Hook chain** - Source `Models/AbstractModel.cs` L1783 (virtual no-op `ModifyMerchantCardPool`); `Hooks/Hook.cs` L1803-1810 (dispatch over run-state listeners; companion `ModifyMerchantCardRarity` L1815); consumers `Factories/CardFactory.cs` L46 and L71 (applied before rarity/player-count filtering). Confidence: **High**
```
CardFactory (L46/L71) -> AbstractModel.ModifyMerchantCardPool (virtual, default no-op)
                        -> Hook dispatch (Hook.cs L1803-1810) over run-state listeners
                        -> then rarity + player-count filtering
```

**M06 Engine-side overriders** - only `Models.Modifiers/CharacterCards.cs` L35 (filters shop options to the buyer's character pool). No mod-side consumers (grep over `mod/` returns none). Confidence: **High**
This is the sanctioned extension point for altering shop stock; our mod does not currently use it (Spire1 shop needs are handled by StS1-parity pricing in our own code, not stock substitution).

## 3. Arbitration Case Table

| Scenario | Outcome | Basis |
|---|---|---|
| Porting StS1 shop pricing | StS2 pricing is slot-local in MerchantInventory; StS1's U(0.9,1.1)/U(0.95,1.05) x1.2/x0.5 multipliers (loot-rewards L14) have no StS2 equivalent in this layer | M02-M04 |
| Making mod cards purchasable | Register them in a pool the buyer's character can see; CharacterCards.cs L35 filters to the buyer's pool - cross-pool injection gets filtered out | M02/M06 |
| Altering shop stock composition | Override/patch ModifyMerchantCardPool via the Hook chain - it runs BEFORE rarity and player-count filtering | M05 |
| Multiplayer shop divergence | Stock consults CardMultiplayerConstraint per player - each player's unlock state shapes their own shop view | M02 |

## 4. Open Questions / Low-Confidence Items

1. Exact StS2 per-slot price formulas (base price tables, on-sale mechanics) not itemized; read MerchantInventory entries + StoreItem classes on demand. Confidence: **Medium**.
2. Whether relic/potion slots also pass through any Hook modification (only the card pool hook was traced). Confidence: **Medium**.
