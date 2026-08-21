# RitsuLib 0.5.13 — API assessment for StS2 game version 0.111.0

Salvaged from `history://RitsuLibApiScout` (1h04m run; died at "I have complete evidence. Writing the artifact.") by transcription. Nothing re-derived: every claim below is what the source transcript established.

Library under test: **RitsuLib 0.5.13** (`STS2-RitsuLib`, MIT, ~1325 public types, github.com/BAKAOLC/STS2-RitsuLib), installed at `G:/steam/steamapps/workshop/content/2868840/3747602295/` with per-game-version variants under `lib/<game-version>/`, including an exact `0.111.0` build with XML documentation (`lib/0.111.0/STS2-RitsuLib.xml`). Our project targets game 0.111.0 exactly.

## 1. Per-gap verdict table

| Project gap | Verdict | Exact member / evidence |
|---|---|---|
| Custom monsters / encounters | Helps but **NOT needed** — BaseLib already covers it | `ModMonsterTemplate`, `ModEncounterTemplate`, `ModContentRegistry.RegisterMonster<T>()`, `RegisterActEncounter<TAct,TEnc>()` / `RegisterGlobalEncounter<TEnc>()`, `IModEncounterActValidity.IsValidForAct(ActModel)`; RitsuLib's genuine additions matter only for our own StS1 PNG art: `VisualCueSet` / `VisualFrameSequence`, `Backends.CueAnimationBackend(Node, Sprite2D, VisualCueSet)`, `ModAnimStateMachine` + `ModAnimStateMachineBuilder`, `IModNonSpineAnimationStateMachineFactory`, `IModCreatureCombatAnimationStateMachineFactory.TryCreateCombatAnimationStateMachine(Godot.Node)` |
| Character visuals | YES on paper, **UNPROVEN in the field** | `ModContentRegistry.RegisterCharacterAssetReplacement(string characterId, CharacterAssetProfile)` + `RegisterGlobalCharacterAssetReplacement(CharacterAssetProfile)`; `CharacterAssetProfile`; `ModCharacterTemplate<TCardPool,TRelicPool,TPotionPool>`; precedence over BaseLib is `[INFERENCE]`; no shipped consumer validates it |
| Card-reward rarity odds (`N'loth's Gift`) | Flat **NO** | 0 hits for `CardRarityOdds\|RollForRarity\|PlayerOdds\|CardRarity.Roll` in the XML; only rarity-ish member `Models.Capabilities.ICardPropertyContributor.GetCardRarity(CardModel)` (reports, does not roll); `Combat.Rewards` is custom reward types + linked sets, not odds |
| Card-play resource info at play-count time (`Necronomicon`'s `freeToPlayOnce`) | **YES, the cleanest win** | `static class STS2RitsuLib.Cards.FreePlay.FreePlayBindingRegistry`: `Register(string, Func<CardPlay,bool>)`, `IsFreeForPlay(CardPlay)`, `Resolve(CardPlay) -> FreePlayResolution`, `IsCardFreeForUpcomingPlay(CardModel)`; also `Cards.ICardOnPlayHookListener.BeforeCardOnPlay(BeforeCardOnPlayContext)`; ordering vs `GeneratePlayCount` is `[UNVERIFIED]` |
| Act sequencing | PARTIAL; act sequencing is the prize | `ModContentRegistry.RegisterActEnterForce<TAct>(int slotIndex, int priority, Func<ActEnterResolveContext,bool> eligible)`; `ActEnterResolveContext{RunManager, RunState, int EnteringActIndex, Rng, UnlockState, bool IsMultiplayer}`; `ModActTemplate` + `RegisterAct<T>()` (needs `IModActRandomListPolicy` to join vanilla list) |
| Character select | NO purpose-built hook | Only `IModCharacterVanillaSelectionPolicy{HideFromVanillaCharacterSelect, AllowInVanillaRandomCharacterSelect, HideInCardLibraryCompendium}` + asset paths; generic tools: `Scaffolding.Godot.NodeAttachments.ModNodeAttachmentRegistry.RegisterReadyChild<TParent,TNode>(...)`, `Screens.ModScreenService.Open/Close/Toggle(ICapstoneScreen)`, `Ui.Windows.RitsuFloatingWindow`; proof it is patchable: RitsuLib's own `Scaffolding.Characters.Patches.NCharacterButtonStripScroller` |
| Run setup | YES — lobby staging | `RunSavedDataLobbyScope<T>.GetOrCreate/Set/Modify(StartRunLobby[, Player])`, `RunSavedDataLobby.NotifyStagingChanged/TryPushContribution`, `RunSavedDataLobbyStagingEvent{Lobby,IsMultiplayer,IsHost,Reason}` |
| Multiplayer state | YES | `RitsuLibManagedNetActions.Register<T>/Request<T>`, `RitsuNetMessageTailExtensions.RegisterBytes/Write/Read`, `Networking.Sidecar.*` (58 public types) |
| Relic icons | YES | `ExternalAssetOverrideRegistry.RegisterRelicIconPathProvider/RegisterRelicIconTextureProvider/RegisterRelicBigIconTextureProvider(string key, Func<RelicModel,...>)` + `RuntimeAssetRefreshCoordinator.RequestRelicsWhere(Predicate<RelicModel>)`, model-agnostic so it should cover BaseLib relics |

Section detail follows. Verdicts and citations preserved from the source transcript.

### 1.1 Custom monsters / encounters — helps, but **not needed**

Registration side (RitsuLib), all confirmed public in the binary:

- `abstract class STS2RitsuLib.Scaffolding.Content.ModMonsterTemplate : MegaCrit.Sts2.Core.Models.MonsterModel, IModMonsterAssetOverrides, IModCreatureVisualsFactory, IModMonsterCreatureVisualsFactory, IModCreatureAnimatorFactory, IModCreatureCombatAnimationStateMachineFactory, IModNonSpineAnimationStateMachineFactory`
- `void ModContentRegistry.RegisterMonster<TMonster>()` — XML: *"Registers a mod monster model for identity tracking, dynamic injection, and inclusion in the patched `ModelDb.Monsters` list."*
- `abstract class ModEncounterTemplate : EncounterModel, IModEncounterAssetOverrides, IModEncounterCombatSceneFactory, IModEncounterActValidity`
- `void ModContentRegistry.RegisterActEncounter<TAct,TEncounter>()`; `void RegisterGlobalEncounter<TEncounter>()` — XML: *"appended to every act's GenerateAllEncounters result, after vanilla and act-scoped mod encounters."*
- `bool IModEncounterActValidity.IsValidForAct(ActModel)` — XML: *"A value of false excludes it from GenerateAllEncounters, including normal, elite, and boss encounter pools."*
- `ModEncounterTemplate`: `family virtual Godot.Control TryCreateEncounterCombatScene()`, `family virtual BackgroundAssets BuildProgrammaticCombatBackground(ActModel, Rng)`, `family bool UseActCombatBackground` (default true), `UseProgrammaticCombatBackground`, `SuppliesEncounterCombatSceneFromFactory`, `EncounterAssetProfile AssetProfile`, `CustomEncounterScenePath`, `CustomBackgroundScenePath`, `CustomBackgroundLayersDirectoryPath`, `CustomBossNodePath`, `IEnumerable<string> CustomExtraAssetPaths`, `CustomMapNodeAssetPaths`, `CustomRunHistoryIconPath`, `CustomRunHistoryIconOutlinePath`
- `record MonsterAssetProfile(string VisualsScenePath)` + `.Empty`
- `static class ModMonsterMoveStateMachines`: `SingleMoveLoop(MoveState)`, `Cycle(MoveState[])`, `Cycle(IReadOnlyList<MoveState>)`, `HeadThenRepeatTail(MoveState head, MoveState tail)` (XML: *"matching patterns such as Track → Hounds → Hounds"*), `RandomEntry(string, Action<RandomBranchState>, IReadOnlyList<MonsterState>)`, `ConditionalEntry(string, Action<ConditionalBranchState>, IReadOnlyList<MonsterState>)` (XML: *"for patterns such as Toadpole's initial branch"*)

The scout's own verdict on this gap: **BaseLib already covers the whole path** — `Abstracts/CustomMonsterModel.cs:21` `CustomVisualPath`, `:39` `CreateCustomVisuals()`, `:53` `SetupCustomAnimationStates(MegaSprite)`, `:74` `static CreatureAnimator SetupAnimationState(...)`, with Harmony patches at `:115` (`MonsterModel.CreateVisuals`), `:128` (`MonsterModel.VisualsPath` getter), `:141` (`MonsterModel.GenerateAnimator`), `:155/:169/:183` (Attack/Cast/Death SFX getters). BaseLib even has a non-Spine route already: `Utils/CustomAnimation.cs:11-19` resolves an `AnimationTree` → `AnimationPlayer` → `AnimatedSprite2D` handler under the visuals root, driven by `Patches/UI/CustomAnimationPatch.cs:40,56,77` which hooks death, revive and `NCreature.SetAnimationTrigger`.

So if the plan is to reuse the 121 shipped StS2 monster scenes via `CustomVisualPath`, **BaseLib alone is sufficient and RitsuLib adds nothing you need.** What RitsuLib genuinely adds only matters if we ship our own StS1 PNG art — see §3.1. `ModMonsterMoveStateMachines` is pure convenience over the vanilla `MonsterMoveStateMachine` API — nice for StS1 fidelity, not an unblocker.

### 1.2 Real (non-placeholder) character visuals — **yes on paper, unproven in practice**

The member that matters most, because it needs no base-class change:

- `void ModContentRegistry.RegisterCharacterAssetReplacement(string characterId, CharacterAssetProfile)` — XML: *"Registers asset replacements for a character ID. Non-null fields from later registrations take precedence."*
- `void ModContentRegistry.RegisterGlobalCharacterAssetReplacement(CharacterAssetProfile)` — XML: *"Registers this mod's asset replacements for all characters. Character-specific replacements take precedence."*
- Key normalisation: `static string ModContentRegistry.NormalizeCharacterAssetEntryKey(string)` — trim + invariant uppercase. Builder form `ModContentPackBuilder.CharacterAssetReplacement(string, CharacterAssetProfile)`; entry type `CharacterAssetReplacementRegistrationEntry`.
- Vanilla character IDs are exposed as `ModContentRegistry.VanillaCharacterIds.{Ironclad, Silent, Defect, Regent, Necrobinder}`.

Because it is keyed by an **ID string**, not by a RitsuLib-derived model type, this is the low-cost path: keep `BaseLib.Abstracts.PlaceholderCharacterModel`, and override only the slots we actually have StS1 art for, leaving the rest resolving to the shipped StS2 character.

`CharacterAssetProfile` is a record grouping roughly 30 path slots: `Scenes` = `CharacterSceneAssetSet { VisualsPath, EnergyCounterPath, MerchantAnimPath, RestSiteAnimPath }`; `Ui` = `CharacterUiAssetSet { IconTexturePath, IconOutlineTexturePath, IconPath, CharacterSelectBgPath, CharacterSelectIconPath, CharacterSelectLockedIconPath, CharacterSelectTransitionPath, MapMarkerPath }`; `Vfx` = `CharacterVfxAssetSet { TrailPath, CharacterTrailStyle TrailStyle }` (10 nullable trail knobs: outer/inner modulate+width, big/little spark colours, primary/secondary sprite modulate+scale); `Spine` = `{ CombatSkeletonDataPath }`; `Audio` = `{ CharacterSelectSfx, CharacterTransitionSfx, AttackSfx, CastSfx, DeathSfx }`; `Multiplayer` = `{ ArmPointingTexturePath, ArmRockTexturePath, ArmPaperTexturePath, ArmScissorsTexturePath }`; plus `VisualCues`, `WorldProceduralVisuals`, and `VanillaRelic/Potion/CardVisualOverrides[]`. Composition helpers: `CharacterAssetProfiles.FromCharacterId(string)`, `Resolve(profile, placeholderId)`, `Merge(a,b)`, `FillMissingFrom(profile, fallback)`, `WithPlaceholder(profile, characterId)`, and canned `Ironclad()/Silent()/Defect()/Regent()/Necrobinder()`.

Full base class if we ever migrate: `abstract class ModCharacterTemplate<TCardPool,TRelicPool,TPotionPool> : CharacterModel, IModCharacterAssetOverrides, IModCreatureVisualsFactory, IModCharacterCreatureVisualsFactory, IModCreatureAnimatorFactory, IModCharacterCreatureAnimatorFactory, IModCreatureCombatAnimationStateMachineFactory, IModNonSpineAnimationStateMachineFactory, IModCharacterMerchantAnimationStateMachineFactory, IModCharacterRestSiteAnimationStateMachineFactory, ...` with `string PlaceholderCharacterId`, `family CharacterAssetProfile ResolvedAssetProfile`, 24 `Custom*Path` overrides, and `family virtual ModAnimStateMachine SetupCustomCombatAnimationStateMachine/SetupCustomNonSpineAnimationStateMachine/SetupCustomMerchantAnimationStateMachine/SetupCustomRestSiteAnimationStateMachine(Godot.Node, CharacterModel)`.

Non-Spine world visuals: `ModAnimStateMachines.StandardCue / StandardMerchantCue / StandardRestSiteCue(Godot.Node, CharacterModel, string idle, string? dead, bool deadLoop, string? hit, bool hitLoop, string? attack, bool attackLoop, string? cast, bool castLoop, string? relaxed, bool relaxedLoop, VisualCueSet)`. XML says these *"mirror baselib's `CustomCharacterModel.SetupAnimationState` shape"*, and `ModAnimStateMachines.Standard(MegaSprite, ...)` *"produces a vanilla CreatureAnimator so callers can return it directly from `CharacterModel.GenerateAnimator`; this is the closest drop-in replacement for the baselib helper."* Also `ModCreatureVisualPlayback.TryPlayCue/TryPlayOnVisualRoot/TryPlayFromCreatureAnimatorTrigger` and `ModWorldSceneVisualNodeFactory.TryInstantiateMerchantCharacter(CharacterModel)` / `TryCreateRestSiteCharacter(Player, int)`.

Two honest caveats:

1. That a RitsuLib character-asset replacement actually **wins over** a BaseLib `CustomCharacterModel` subclass's own path getters is `[INFERENCE]`. The evidence is that 27 internal RitsuLib patch classes carry `[HarmonyAfter("BaseLib")]` — decoded from the `CustomAttribute` table: `Scaffolding.Characters.Patches.{CharacterVisualsPathPatch, CharacterEnergyCounterPathPatch, CharacterMerchantAnimPathPatch, CharacterRestSiteAnimPathPatch, CharacterIconPathPatch, CharacterIconTexturePathPatch, CharacterIconOutlineTexturePathPatch, CharacterSelectBgPathPatch, CharacterSelectIconPathPatch, CharacterSelectLockedIconPathPatch, CharacterSelectTransitionPathPatch, CharacterMapMarkerPathPatch, CharacterTrailPathPatch, CharacterAttackSfxPatch, CharacterCastSfxPatch, CharacterDeathSfxPatch, CharacterArmPointing/Rock/Paper/ScissorsTexturePathPatch, CardLibraryCompendiumPatch}`, `Scaffolding.Content.Patches.{MonsterVisualsPathPatch (also `[HarmonyPriority(0)]`), ImageHelperAncientModRunHistoryIconPathPatch, ImageHelperModEncounterRunHistoryIconPathPatch}`, `Settings.Patches.{MainMenuModSettingsButtonPatch, ModSettingsSubmenuPatch, SettingsScreenModSettingsButtonPatch}`. The *target member* of each patch is declared in code (via `ModPatchTarget`), not in the attribute, so e.g. "patches `CharacterModel.VisualsPath`" could not be confirmed — marked `[UNVERIFIED]`, though the naming is 1:1 with BaseLib's own patch set. This needs a smoke test before committing.
2. **No shipped mod uses this surface.** See §4.

### 1.3 Card-reward rarity odds (`N'loth's Gift`) — **NO**

Flat no, and checked hard for it:

- `grep -c "CardRarityOdds\|RollForRarity\|PlayerOdds\|CardRarity.Roll"` over `lib/0.111.0/STS2-RitsuLib.xml` → **0**.
- Every rarity-adjacent member in the whole XML: `ModPlaceholderRelicTemplate.Rarity`, `ModPlaceholderPotionTemplate.Rarity`, `ModBadgeTemplate.Rarity(SerializableRun, SerializablePlayer)`, `ModCardPileSortOption.Rarity`, `RitsuDebugCardCatalog.MethodName.GetRarityOrder`, and `Models.Capabilities.ICardPropertyContributor.GetCardRarity(CardModel) -> CardRarity?`. That last one changes what rarity a *card model reports*; it does not touch the reward roll, its probabilities, or the pity offset.
- `Combat.Rewards` is about custom reward **types** and linked sets, not odds: `ModRewardRegistry.For(string)/RegisterOwned(string, ModRewardFactory)/RegisterOwned<TPayload>(...)/Register(RewardType, ModRewardFactory)/GetRewardType(string)`, `abstract class ModCustomReward : Reward, IModSerializableReward`, `LinkedRewardSets.Create(IEnumerable<Reward>, Player, LinkedRewardSelectionMode)/Configure/GetSelectionMode`, `enum LinkedRewardSelectionMode { ChooseOne, TakeAll }`, `ModRewardSerialization.CreateSerializable(...)`.

The only thing on offer is a Harmony harness we drive ourselves — `Patching.Core.ModPatcher`, `Patching.Models.PatchTarget.Method/Getter/Setter/Constructor/AsyncMethod/EnumeratorMethod`, `Patching.Builders.DynamicPatchBuilder.Add(MethodBase, HarmonyMethod prefix, postfix, transpiler, finalizer, bool isCritical, string, string)`, `Utils.HarmonyIl.HarmonyIlRewriter.From(IEnumerable<CodeInstruction>).RedirectCalls(...)/.ReplaceEach(...)`, `HarmonyIl.Call(MethodInfo)/IsCallTo(...)`, `Patching.PrivateAccess.Field/DeclaredField`. BaseLib already bundles Harmony (every `Abstracts/*.cs` does `using HarmonyLib;`). So this is ergonomics, not capability. **Do not adopt RitsuLib for `N'loth's Gift`.**

### 1.4 Card-play resource info at play-count time (`Necronomicon` `freeToPlayOnce`) — **YES, the cleanest win**

`static class STS2RitsuLib.Cards.FreePlay.FreePlayBindingRegistry` — XML: *"Provides an extensible registry for determining whether a card play is free."*

```
static void Register(string id, Func<CardPlay,bool> detector)
static bool IsFreeForPlay(CardPlay)
static FreePlayResolution Resolve(CardPlay)
static bool IsCardFreeForUpcomingPlay(CardModel)
static void MarkCardFreeNextPlay(CardModel)
static void MarkCardFreeThisTurn(CardModel)
static void MarkCardFreeThisCombat(CardModel)
static void MarkCurrentPlayFree(CardPlay)
static bool ClearCardFreeThisTurn(CardModel)
static bool ClearCardFreeAfterPlayed(CardModel)
record FreePlayResolution(bool IsAutoPlayNoSpend, bool IsCardBindingFree, bool IsRegisteredDetectorFree) { bool IsFree { get; } }
static void CardModelFreePlayExtensions.SetToFreeForRestOfTurn(CardModel)
```

XML on `Register`: *"Registers an additional free-play detector. The detector should return true when mod-defined rules consider the specified CardPlay free."* On `IsCardFreeForUpcomingPlay`: *"Returns whether the card is marked free before a CardPlay exists, without consuming a next-play charge."*

Critically, RitsuLib already observes the vanilla mechanism rather than only its own: internal `Cards.FreePlay.Patches.CardModelSetToFreeThisTurnBindingPatch` — XML: *"Records game-level `SetToFree` calls in FreePlayBindingRegistry."*

It also supplies the pre-play hook that core lacked: `Cards.ICardOnPlayHookListener.BeforeCardOnPlay(BeforeCardOnPlayContext) -> Task<bool>` and `AfterCardOnPlay(AfterCardOnPlayContext) -> Task`, where `BeforeCardOnPlayContext = { ICombatState CombatState; PlayerChoiceContext ChoiceContext; CardPlay CardPlay }` and the After variant adds `bool OriginalOnPlayRan`. Registered via `CardOnPlayHook.RegisterGlobalListener(...)` or `RitsuLibFramework.RegisterCardOnPlayHookListener(...)`; returning `false` from Before suppresses the original `OnPlay`.

One caveat: `Necronomicon` needs the free-ness at `GeneratePlayCount` time, and the scout never traced RitsuLib's call ordering against `GeneratePlayCount`. `IsCardFreeForUpcomingPlay` is documented for exactly that position, but treat the ordering as `[UNVERIFIED]`. What is certain is that the data is reachable through a public API.

### 1.5 Act sequencing / character select / run setup / multiplayer state — **partial; act sequencing is the real prize**

Act slot replacement, the single most valuable member found for the act-sequencing gap:

```
void ModContentRegistry.RegisterActEnterForce<TAct>(int slotIndex, int priority, Func<ActEnterResolveContext,bool> eligible)
```
XML: *"Registers a rule that replaces slotIndex with TAct when eligible. Higher priority wins, with earlier registration breaking ties."*

```
struct ActEnterResolveContext { RunManager RunManager; RunState RunState; int EnteringActIndex; Rng Rng; UnlockState UnlockState; bool IsMultiplayer }
```
so the predicate can read run state and multiplayer-ness to decide "this is an StS1 dungeon run". Alternatives: `RegisterActEnterUniformPool(int)` + `RegisterActEnterUniformPoolCandidate<TAct>(int, Func<...,bool>)`; `RegisterActEnterWeightedPool(int)` + `RegisterActEnterWeightedPoolCandidate<TAct>(int, Func<...,bool>, Func<...,double>)` + `RegisterActEnterWeightedPoolBaseline(int, Func<...,double>)`; `enum ActEnterPoolModeKind { Uniform, Weighted }`; `static bool ModContentRegistry.HasAnyActEnterRegistration`. All mirrored on `ModContentPackBuilder`.

Acts: `abstract class ModActTemplate : ActModel, IModActAssetOverrides, IModActRandomListPolicy`, registered by `RegisterAct<TAct>()` — XML: *"This does not add it to the vanilla randomized act list; implement IModActRandomListPolicy to opt in"* (`bool AllowInRandomActList`). `record ActAssetProfile(BackgroundScenePath, RestSiteBackgroundPath, MapTopBgPath, MapMidBgPath, MapBotBgPath, ChestSpineResourcePath, BackgroundLayersDirectoryPath)`.

Run setup — how a lobby-time dungeon choice reaches the run, including co-op:

```
class RunSavedDataLobbyScope<T> { T GetOrCreate(StartRunLobby); bool TryGet(StartRunLobby, out T); void Set(...); bool Remove(...); T Modify(StartRunLobby, Action<T>) }
class PlayerRunSavedDataLobbyScope<T> { ... (StartRunLobby, ulong netId | Player) overloads }
static class RunSavedDataLobby { void NotifyStagingChanged(StartRunLobby); bool TryPushContribution(StartRunLobby) }
class RunSavedDataLobbyStagingEvent { StartRunLobby Lobby; bool IsMultiplayer; bool IsHost; RunSavedDataLobbyStagingReason Reason }
```
XML on the event: *"Notifies mods that start-run lobby staging data can be read or changed before it is committed to the run."* Plus `RunSavedDataStore`, `RunSavedData<T>`, `RunSavedDataOptions`, `RunSavedDataWritePolicy`, `RunSavedDataPreparingEvent`.

Multiplayer state for anything of our own:
- `RitsuLibManagedNetActions.Register<T>(RitsuLibManagedNetActionDescriptor<T>) -> ulong` / `Request<T>(RunManager, descriptor, T, ulong?)`, `const int MaxPayloadBytes`; descriptor = `{ ModuleId, ActionKey, Func<T,byte[]> Serialize, Func<ReadOnlySpan<byte>,T> Deserialize, Func<RitsuLibManagedNetActionContext<T>,Task> Execute, GameActionType ActionType }`.
- `RitsuNetMessageTailExtensions.RegisterBytes<TMessage>(string, int, Func<TMessage,byte[]>, Action<int,ReadOnlyMemory<byte>>)` + `Write<TMessage>(PacketWriter, TMessage)` / `Read<TMessage>(PacketReader)` — appends mod bytes to a vanilla net message tail.
- `Networking.Sidecar.*`, 58 public types: `RitsuLibSidecarBus.RegisterHandler(ulong, Action<RitsuLibSidecarDispatchContext>)`, `WaitForNextAsync(ulong, TimeSpan, Func<...,bool>, bool, CancellationToken)`, `RitsuLibSidecar.CreateEnvelope/CreateEnvelopeCompressed/CreateEnvelopeWithDelivery`, `RitsuLibSidecarConfigSyncService.RegisterTopic/PublishHostState/TopicChanged`, `RitsuLibSidecarSessionManager.HandshakeCompleted`, `IRitsuLibSidecarCapabilityValidationRoute`, chunked streaming.
- Deterministic per-run RNG isolated from vanilla streams: `RitsuLibFramework.GetModRunRng(RunState, string, string)` / `GetModRunRng(Player, ...)` / `GetModPlayerRng(Player, ...)`, backed by `RunRngs.ModRunRngRegistry` and persisted through `ModRunRngState`/`ModRunRngSnapshot`.

**Character select: no purpose-built hook.** The only character-select surface is asset paths (`CharacterSelectBgPath`, `CharacterSelectIconPath`, `CharacterSelectLockedIconPath`, `CharacterSelectTransitionPath`, and `CharacterAssetPathHelper.GetCharacterSelectBackgroundPath/GetCharacterSelectIconPath/GetCharacterSelectLockedIconPath`) plus `IModCharacterVanillaSelectionPolicy { HideFromVanillaCharacterSelect; AllowInVanillaRandomCharacterSelect; HideInCardLibraryCompendium }`. For a dungeon-selector control the nearest general tools are `Scaffolding.Godot.NodeAttachments.ModNodeAttachmentRegistry.RegisterReadyChild<TParent,TNode>(string localId, Func<TParent,TNode> factory, Action<TParent,TNode> setup, NodeAttachmentOptions)` (plus `RegisterReadyChildFromScene`/`FromConvertedScene`, and a rich `NodeAttachmentOptions { Name, Order, UniqueNameInOwner, IncludeDerivedParentTypes, DuplicatePolicy, AddMode, AttachParentSelector, SetupTiming, ChildIndex, InsertBeforeName, InsertAfterName, QueueFreeReplacedNode }`), `Screens.ModScreenService.Open/Close/Toggle(ICapstoneScreen)`, `Ui.Windows.RitsuFloatingWindow`, `TopBar.ModTopBarButtonRegistry`. Proof the screen is patchable at all: RitsuLib itself ships `Scaffolding.Characters.Patches.NCharacterButtonStripScroller`, a scroller it grafts onto the character button strip.

Also useful for the run-setup gap: 96 lifecycle event structs, subscribable via `RitsuLibFramework.SubscribeLifecycle<TEvent>(Action<TEvent>, bool)` / `SubscribeLifecycleOnce<TEvent>(...)` / `SubscribeLifecycle(ILifecycleObserver, bool)`. The relevant ones: `ActEnteringEvent { RunManager, int TargetActIndex, bool DoTransition }`, `ActEnteredEvent { IRunState, int CurrentActIndex }`, `MapGeneratedEvent { IRunState, ActMap Map, int ActIndex }`, `RunStartedEvent`/`RunLoadedEvent { RunState, bool IsMultiplayer, bool IsDaily }`, `RunEndedEvent { SerializableRun Run, bool IsVictory, bool IsAbandoned }`, `RoomEntering/Entered/ExitedEvent`, `MainMenuReadyEvent`, `GameReadyEvent { NGame Game }`, `ContentRegistrationClosedEvent { string Reason }`. Events implementing `IReplayableFrameworkLifecycleEvent` are replayed to late subscribers.

### 1.6 Two gaps outside the batch list that were also verified

**Relic art (the single placeholder icon) — YES.** `static class Scaffolding.Content.Patches.ExternalAssetOverrideRegistry`:
```
static void RegisterRelicIconPathProvider(string key, Func<RelicModel,string>)
static void RegisterRelicIconOutlinePathProvider(string key, Func<RelicModel,string>)
static void RegisterRelicIconTextureProvider(string key, Func<RelicModel,Godot.Texture2D>)
static void RegisterRelicIconOutlineTextureProvider(string key, Func<RelicModel,Godot.Texture2D>)
static void RegisterRelicBigIconTextureProvider(string key, Func<RelicModel,Godot.Texture2D>)
static bool Unregister(string key)
static void Clear()
```
Model-agnostic `Func<RelicModel,…>`, so it should cover BaseLib-declared relics and replace the `mod/Spire1Code/Extensions/StringExtensions.cs:49-65` fallback with real per-relic art. Live refresh: `RuntimeAssetRefreshCoordinator.RequestRelicsWhere(Predicate<RelicModel>)` / `Request(RuntimeAssetRefreshScope)`. The same registry has 40 provider methods covering powers, potions, orbs, acts, events, encounters, ancients, afflictions, enchantments and modifiers; sibling registries `ExternalCardMaterialOverrideRegistry` and `ExternalBadgeIconOverrideRegistry`.

**`MutagenicStrength`'s "lose 3 Strength at end of turn" icon — NO new capability.** RitsuLib has `abstract class Combat.Powers.ModTemporaryPowerTemplate : ModPowerTemplate, ITemporaryPower` (`LastForXExtraTurns`, `UntilEndOfOtherSideTurn`, `IsPositive`, `RemainingExtraTurnCycles`, `IgnoreNextInstance()`, `AfterSideTurnEnd(...)`, `SignedAmount(decimal)`, `const string ExtraTurnCyclesVarName`) and a neat `ModTemporaryAppliedPowerTemplate<TOriginModel,TPower>`. But BaseLib already ships the equivalent: `Abstracts/CustomTemporaryPowerModel.cs:24` `public abstract class CustomTemporaryPowerModel : CustomPowerModel, ITemporaryPower, IBetaCompatTempPower, IAddDumbVariablesToPowerDescription` with `InternallyAppliedPower`, `OriginModel`, `UntilEndOfOtherSideTurn`, `LastForXExtraTurns`. Use BaseLib.

Face relics and `Madness`: RitsuLib is irrelevant; the blocker is jar data extraction.

## 2. Namespace / type index and provenance

### 2.1 Which facts came from `STS2-RitsuLib.xml` versus from the binary

**From the binary** (authoritative for visibility, staticness, return types, generic arity, base types, interfaces):
- Assembly identity `STS2-RitsuLib 0.5.13.0`; **not strong-named** (CLI header flags `0x1`, no strong-name signature) → version-agnostic simple-name binding.
- 5 713 `STS2RitsuLib.*` TypeDefs; 1 606 public or nested-public; **1 325 public, non-nested, non-compiler-generated types across 92 namespaces**.
- 36 assembly references, notably `GodotSharp 4.5.1.0`, `sts2 0.1.0.0`, `0Harmony 2.4.2.0`, `SmartFormat 3.0.0.0`, `System.Text.Json`, `System.Net.Http`, `System.Net.Sockets`, `System.IO.Hashing`, `System.Reflection.Emit*`, `System.IO.Compression.Brotli`.
- The `[HarmonyAfter("BaseLib")]` / `[HarmonyPriority(0)]` evidence in §1 — decoded fixed-args from `CustomAttribute` blobs.

**From `lib/0.111.0/STS2-RitsuLib.xml`** — every quoted description elsewhere in this document. It is 5 680 997 bytes with 12 233 `<member>` entries, 12 136 of them under `STS2RitsuLib.*`, and it is bilingual (`<para xml:lang="en">` and `zh-CN` side by side). The remaining 97 members are `System.Text.RegularExpressions.Generated.*` source-generator noise.

An important trap: **the XML documents internal members too.** `RitsuLibEmbeddedPngResourceLoader` appears fully documented but the binary shows it is `NotPublic`, extends `Godot.ResourceFormatLoader`, and its `EnsureRegistered()` is `internal` — it maps `res://STS2-RitsuLib/<name>.png` onto RitsuLib's own embedded `STS2RitsuLib.Assets.<name>.png` resources and is not a general mechanism a consumer can register into. Similarly `STS2RitsuLib.Graphics` is documented but has zero public types. Every visibility claim in this document comes from the binary, not the XML.

The extraction method: `ilspycmd` was unavailable, so the scout wrote a self-contained ECMA-335 metadata reader — PE headers → CLI header → metadata root → `#~`/`#Strings`/`#Blob` streams → `TypeDef`, `MethodDef`, `Field`, `Property`, `PropertyMap`, `Event`, `EventMap`, `MethodSemantics`, `NestedClass`, `GenericParam`, `InterfaceImpl`, `AssemblyRef`, `CustomAttribute`, with full blob signature decoding. See §7 for the reusable dumps.

### 2.2 The dumped index (verbatim from `G:/omp works/sts2-spire1/.tmp/ritsu/nsindex.md`)

Format: `**namespace** — Type ·kind; ...` (`·class`, `·static class`, `·abstract class`, `·interface`, `·struct`, `·enum`, `·delegate`).

**STS2RitsuLib** — ActEnteredEvent ·struct; ActEnteringEvent ·struct; AttackEndedEvent ·struct; AttackStartingEvent ·struct; BeforeFlushEvent ·struct; BlockBrokenEvent ·struct; BlockClearedEvent ·struct; BlockGainedEvent ·struct; BlockGainingEvent ·struct; CardAutoPlayingEvent ·struct; CardDiscardedEvent ·struct; CardDrawnEvent ·struct; CardEnteredCombatEvent ·struct; CardExhaustedEvent ·struct; CardGeneratedForCombatEvent ·struct; CardMovedBetweenPilesEvent ·struct; CardPlayedEvent ·struct; CardPlayingEvent ·struct; CardRemovingEvent ·struct; CardsFlushedEvent ·struct; CombatEndedEvent ·struct; CombatStartingEvent ·struct; CombatVictoryEvent ·struct; Const ·static class; ContentRegistrationClosedEvent ·struct; CreatureAddedToCombatEvent ·struct; CreatureDiedEvent ·struct; CreatureDyingEvent ·struct; CurrentHpChangedEvent ·struct; DeferredInitializationCompletedEvent ·struct; DeferredInitializationStartingEvent ·struct; EnergyGainedEvent ·struct; EnergyResetEvent ·struct; EnergySpentEvent ·struct; EpochObtainedEvent ·struct; EpochRevealedEvent ·struct; EssentialInitializationCompletedEvent ·struct; EssentialInitializationStartingEvent ·struct; ExtraTurnTakenEvent ·struct; FrameworkInitializedEvent ·struct; FrameworkInitializingEvent ·struct; GameOverScreenCreatedEvent ·struct; GameReadyEvent ·struct; GameTreeEnteredEvent ·struct; GoldGainedEvent ·struct; GoldLostEvent ·struct; HandDrawingEvent ·struct; HandEmptiedEvent ·struct; HoverTipHelper ·static class; IFrameworkLifecycleEvent ·interface; ILifecycleObserver ·interface; IReplayableFrameworkLifecycleEvent ·interface; ItemPurchasedEvent ·struct; MainMenuReadyEvent ·struct; MapGeneratedEvent ·struct; ModDataRuntimeInterop ·static class; ModelIdsInitializedEvent ·struct; ModelIdsInitializingEvent ·struct; ModelPreloadingCompletedEvent ·struct; ModelPreloadingStartingEvent ·struct; ModelRegistryInitializedEvent ·struct; ModelRegistryInitializingEvent ·struct; PlayerTurnStartedEvent ·struct; PotionDiscardedEvent ·struct; PotionProcuredEvent ·struct; PotionUsedEvent ·struct; PotionUsingEvent ·struct; ProfileDeletedEvent ·struct; ProfileDeletingEvent ·struct; ProfileIdInitializedEvent ·struct; ProfileServicesInitializedEvent ·struct; ProfileServicesInitializingEvent ·struct; ProfileSwitchedEvent ·struct; ProfileSwitchingEvent ·struct; ProgressSavedEvent ·struct; ProgressSavingEvent ·struct; RelicObtainedEvent ·struct; RelicRemovedEvent ·struct; RestSiteHealedEvent ·struct; RestSiteSmithedEvent ·struct; RewardTakenEvent ·struct; RewardsScreenContinuingEvent ·struct; RitsuLibFramework ·static class; RoomEnteredEvent ·struct; RoomEnteringEvent ·struct; RoomExitedEvent ·struct; RunEndedEvent ·struct; RunLoadedEvent ·struct; RunSavedEvent ·struct; RunSavingEvent ·struct; RunStartedEvent ·struct; ShuffledEvent ·struct; SideTurnEndedEvent ·struct; SideTurnEndingEvent ·struct; SideTurnStartedEvent ·struct; SideTurnStartingEvent ·struct; StarsGainedEvent ·struct; StarsSpentEvent ·struct; SummonedEvent ·struct; TelemetryStartupSnapshotReadyEvent ·struct; UnlockIncrementedEvent ·struct

**STS2RitsuLib.Audio** — AudioAdaptiveMusicDirector ·class; AudioAdaptiveMusicHandle ·class; AudioAdaptiveMusicPlan ·class; AudioAdaptivePlans ·static class; AudioChannelMode ·enum; AudioChannelRegistry ·class; AudioEventHandle ·class; AudioFileHandle ·class; AudioHandleBase ·abstract class; AudioLifecycleRegistry ·class; AudioLifecycleScope ·enum; AudioLoopHandle ·class; AudioMusicHandle ·class; AudioParameterSet ·class; AudioPlayResult ·class; AudioPlayStatus ·enum; AudioPlaybackOptions ·class; AudioRoutingOptions ·class; AudioScopeToken ·class; AudioSnapshotHandle ·class; AudioSource ·abstract class; AudioVanillaBridge ·static class; FmodEventPath ·struct; FmodParameterMap ·static class; FmodPathRoundRobinPool ·class; FmodPlaybackThrottle ·static class; FmodStudioBusAccess ·static class; FmodStudioDeferredBankRegistration ·static class; FmodStudioDirectOneShots ·static class; FmodStudioEventInstances ·static class; FmodStudioLoadBankMode ·enum; FmodStudioMixerGlobals ·static class; FmodStudioRouting ·static class; FmodStudioServer ·static class; FmodStudioSnapshots ·static class; FmodStudioStreamingFiles ·static class; GameAudioService ·class; GameFmod ·static class; GameFmodAudioService ·class; IAudioHandle ·interface; IFmodLoopPlayback ·interface; IFmodMixerVolumes ·interface; IFmodMusicPlayback ·interface; IFmodOneShotPlayback ·interface; IGameAudio ·interface; IGameFmodAudio ·interface; ResourceSoundFileSource ·class; SnapshotSource ·class; SoundFileSource ·class; StreamingMusicSource ·class; StreamingResourceMusicSource ·class; Sts2SfxAlignedFmod ·static class; StudioEventSource ·class; StudioGuidSource ·class; VirtualFmodEventDefinition ·class; VirtualFmodEventKind ·enum; VirtualFmodEventRegistry ·static class; VirtualFmodVariantSelection ·enum

**STS2RitsuLib.CardPiles** — CardPileRegistrationEntry ·class; IModCardPileFlightContext ·interface; IModCardPileHandler ·interface; ModCardPile ·class; ModCardPileAnchor ·struct; ModCardPileAnchorKind ·enum; ModCardPileDefinition ·class; ModCardPileExtensions ·static class; ModCardPileExtraHandSpec ·class; ModCardPileFlightStartContext ·class; ModCardPileFlightTargetContext ·class; ModCardPileHoverTipFactory ·static class; ModCardPileHoverTipPlacement ·enum; ModCardPileHoverTipViewport ·static class; ModCardPileOpenContext ·class; ModCardPilePlayerSaveState ·class; ModCardPileRegistry ·class; ModCardPileScope ·enum; ModCardPileSortOption ·enum; ModCardPileSpec ·class; ModCardPileUiStyle ·enum; ModCardPileViewSpec ·class; ModCardPileViewStyleContext ·class; ModCardPileVisibilityContext ·class; ModExtraHandCardContext ·class; ModExtraHandCardTransform ·struct; ModExtraHandLayoutDirection ·enum

**STS2RitsuLib.CardPiles.Nodes** — NModCardPileButton ·class; NModExtraHand ·class; NModTopBarPileButton ·class

**STS2RitsuLib.CardTags** — CardTagRegistrationEntry ·class; ModCardTagDefinition ·class; ModCardTagExtensions ·static class; ModCardTagRegistry ·class

**STS2RitsuLib.CardTags.Serialization** — CardTagHashSetJsonConverter ·class; CardTagJsonConverter ·class

**STS2RitsuLib.Cards** — AfterCardOnPlayContext ·struct; BeforeCardOnPlayContext ·struct; CardOnPlayHook ·static class; CardTypeTextHook ·static class; ICardOnPlayHookListener ·interface

**STS2RitsuLib.Cards.DynamicVars** — ComputedDynamicVar ·class; ComputedDynamicVarContext ·class; ComputedDynamicVarFactory ·delegate; ComputedEnergyVar ·class; ComputedPowerVar<T> ·class; ComputedStarsVar ·class; DynamicVarExtensions ·static class; DynamicVarTooltipRegistry ·static class; IComputedDynamicVar ·interface; ModCardVars ·static class

**STS2RitsuLib.Cards.FreePlay** — CardModelFreePlayExtensions ·static class; FreePlayBindingRegistry ·static class; FreePlayResolution ·class

**STS2RitsuLib.Cards.Transforms** — ModCardTransformContext ·struct; ModCardTransformRegistry ·class

**STS2RitsuLib.Combat.AttackHits** — AttackHitContext ·class; AttackHitHook ·static class; IAttackHitHookListener ·interface

**STS2RitsuLib.Combat.CardTargeting** — AttackCommandTargetingExtensions ·static class; CardModelTargetingExtensions ·static class; CustomTargetContext ·class; CustomTargetType ·static class; PotionModelTargetingExtensions ·static class

**STS2RitsuLib.Combat.HandSize** — IMaxHandSizeModifier ·interface; MaxHandSizeCalculator ·static class

**STS2RitsuLib.Combat.Healing** — HealContext ·class; HealHook ·static class; IHealHookListener ·interface

**STS2RitsuLib.Combat.HealthBars** — HealthBarForecastContext ·struct; HealthBarForecastGrowthDirection ·enum; HealthBarForecastLaneBuilder ·class; HealthBarForecastLeftOriginLayout ·enum; HealthBarForecastOrder ·static class; HealthBarForecastRegistry ·static class; HealthBarForecastSegment ·struct; HealthBarForecastSequenceBuilder ·class; HealthBarForecasts ·static class; HealthBarVisualGraftContext ·struct; HealthBarVisualGraftMetrics ·struct; HealthBarVisualGraftRegistry ·static class; IHealthBarForecastSource ·interface; IHealthBarVisualGraftSource ·interface

**STS2RitsuLib.Combat.PlayerResources** — IPlayerResourceHookListener ·interface; PlayerResourceGainContext ·struct; PlayerResourceHook ·static class; PlayerResourceKind ·enum

**STS2RitsuLib.Combat.Powers** — ModTemporaryAppliedPowerTemplate<TOriginModel,TPower> ·abstract class; ModTemporaryPowerTemplate ·abstract class

**STS2RitsuLib.Combat.Rewards** — IModSerializableReward ·interface; LinkedRewardSelectionMode ·enum; LinkedRewardSets ·static class; ModCustomReward ·abstract class; ModRewardDefinition ·class; ModRewardRegistry ·class; ModRewardSerialization ·static class

**STS2RitsuLib.Combat.SecondaryResources** — ICardSecondaryResourceCostContributor ·interface; ICardSecondaryResourceUseContributor ·interface; ISecondaryResourceHookListener ·interface; ModSecondaryResourceRegistry ·class; NSecondaryResourceCardCostUi ·class; NSecondaryResourceCounter ·class; NSecondaryResourceCounterRow ·class; NSecondaryResourceIcon ·class; SecondaryResourceCardCostColor ·enum; SecondaryResourceCardCostContext ·struct; SecondaryResourceCardCostHelper ·static class; SecondaryResourceCardCostUiStyle ·class; SecondaryResourceCardExtensions ·static class; SecondaryResourceCardUiContext<TParent,TNode> ·struct; SecondaryResourceCardUiLayout ·static class; SecondaryResourceCardVisibilityContext ·struct; SecondaryResourceChangeContext ·struct; SecondaryResourceChangeReason ·enum; SecondaryResourceChangedEntry ·class; SecondaryResourceChangedEvent ·class; SecondaryResourceCmd ·static class; SecondaryResourceCombatUiChangeContext<TParent,TNode> ·struct; SecondaryResourceCombatUiChangedHandler<TParent,TNode> ·delegate; SecondaryResourceCombatUiContext<TParent,TNode> ·struct; SecondaryResourceCombatUiVisibilityPredicate ·delegate; SecondaryResourceCombatVisibilityContext ·struct; SecondaryResourceConsoleCmd ·class; SecondaryResourceContext ·struct; SecondaryResourceCost ·class; SecondaryResourceCostContext ·struct; SecondaryResourceCostDuration ·enum; SecondaryResourceCostSet ·class; SecondaryResourceCounterEnergyCounterLikeParticlesEffect ·class; SecondaryResourceCounterGainEffect ·abstract class; SecondaryResourceCounterGainEffects ·static class; SecondaryResourceCounterGainFeedback ·class; SecondaryResourceCounterIconBrightnessFlashEffect ·class; SecondaryResourceCounterSceneBurstEffect ·class; SecondaryResourceCounterStarCounterLikeBurstEffect ·class; SecondaryResourceCounterStyle ·class; SecondaryResourceDefinition ·class; SecondaryResourceHistory ·static class; SecondaryResourceHistoryEntry ·abstract class; SecondaryResourceHook ·static class; SecondaryResourceHoverTipBinder ·class; SecondaryResourceHoverTipFactory ·static class; SecondaryResourceHoverTipPlacementContext ·struct; SecondaryResourceHoverTipRequest ·struct; SecondaryResourceHoverTipStyle ·class; SecondaryResourceIconStyle ·class; SecondaryResourceIconsFormatter ·class; SecondaryResourceInsufficientPayment ·class; SecondaryResourceInsufficientPaymentContext ·struct; SecondaryResourceInsufficientPaymentMode ·enum; SecondaryResourceLocStringSource ·class; SecondaryResourceMaxContext ·struct; SecondaryResourceModelHookRegistry ·static class; SecondaryResourceMultiplayerPlayerStateUiContext<TNode> ·struct; SecondaryResourcePaymentLine ·class; SecondaryResourcePaymentPlan ·class; SecondaryResourcePaymentResolver ·static class; SecondaryResourcePersistence ·static class; SecondaryResourcePersistencePolicy ·enum; SecondaryResourcePlayExtensions ·static class; SecondaryResourcePlayLedger ·class; SecondaryResourcePlayLedgerLine ·class; SecondaryResourcePlayLedgerRuntime ·static class; SecondaryResourcePlayUse ·class; SecondaryResourcePlayUseSet ·class; SecondaryResourceResetEntry ·class; SecondaryResourceRunSaveState ·class; SecondaryResourceShortfallContext ·struct; SecondaryResourceShortfallPaymentHandler ·delegate; SecondaryResourceShortfallResolution ·class; SecondaryResourceShortfallResolutionContext ·struct; SecondaryResourceShortfallResolver ·delegate; SecondaryResourceSpendContext ·struct; SecondaryResourceSpentEntry ·class; SecondaryResourceState ·class; SecondaryResourceStateStore ·static class; SecondaryResourceText ·static class; SecondaryResourceTurnStartPolicy ·enum; SecondaryResourceUiRuntime ·static class; SecondaryResourceUseKind ·enum; SecondaryResourceVar ·class; SecondaryResourceVars ·static class; SecondaryResourceVisibility ·static class; SecondaryResourceXContext ·struct

**STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels** — ExtraIconAmountLabelCorner ·enum; ExtraIconAmountLabelSlot ·struct; ExtraIconAmountLabelSpec ·struct; ExtraIconAmountLabelTextMode ·enum; ExtraIconRichTextLabelSlot ·struct; IIntentExtraCornerAmountLabelSpecsProvider ·interface; IIntentExtraCornerAmountLabelsChangeSource ·interface; IIntentExtraCornerAmountLabelsProvider ·interface; IPowerExtraIconAmountLabelSpecsProvider ·interface; IPowerExtraIconAmountLabelsChangeSource ·interface; IPowerExtraIconAmountLabelsProvider ·interface; IRelicExtraIconAmountLabelSpecsProvider ·interface; IRelicExtraIconAmountLabelsChangeSource ·interface; IRelicExtraIconAmountLabelsProvider ·interface

**STS2RitsuLib.Compat** — RitsuModInfo ·class; RitsuModLoadState ·enum; RitsuModManager ·static class; RitsuModSource ·enum

**STS2RitsuLib.Content** — ActEnterPoolModeKind ·enum; ActEnterResolveContext ·struct; CardLibraryCompendiumFilterInsertRelation ·enum; CardLibraryCompendiumPlacementDefaults ·static class; CardLibraryCompendiumPlacementRule ·class; CardLibraryCompendiumSharedPoolFilterRegistration ·class; CardLibraryCompendiumVanillaFilterNames ·static class; ContentRegistrationState ·enum; ContentSourceDescriptor ·struct; ContentSourceResolver ·static class; IContentSourceSupplier ·interface; ModContentRegistry ·class; ModelPublicEntryOptions ·struct; PlaceholderCardDescriptor ·struct; PlaceholderPotionDescriptor ·struct; PlaceholderRelicDescriptor ·struct

**STS2RitsuLib.Data** — ModDataStore ·class; ModDataStoreCache<T> ·class

**STS2RitsuLib.Data.Models** — RitsuLibSettings ·class

**STS2RitsuLib.Diagnostics.CardExport** — CardPngExportCaptureMode ·enum; CardPngExportRequest ·struct; CardPngExporter ·static class

**STS2RitsuLib.Diagnostics.Commands** — OpenLogViewerConsoleCmd ·class; RitsuLibConsoleCmd ·class

**STS2RitsuLib.Diagnostics.CompendiumExport** — CompendiumDetailPngExporter ·static class; CompendiumPngExportRequest ·struct

**STS2RitsuLib.Diagnostics.DebugTools** — RitsuDebugToolsPageContext ·class; RitsuDebugToolsPageDefinition ·class; RitsuDebugToolsPageRegistry ·static class

**STS2RitsuLib.Diagnostics.DevConsole** — DevConsoleAutocomplete ·static class; DevConsoleAutocompleteBinding ·class; DevConsoleAutocompleteContext ·class; DevConsoleAutocompleteContextPredicates ·static class; DevConsoleAutocompleteDisplay ·static class; DevConsoleAutocompleteEnhancements ·enum; DevConsoleAutocompleteEnhancer ·static class; DevConsoleAutocompleteMatchExtensions ·static class; DevConsoleAutocompleteOwnedIdMatch ·static class; DevConsoleAutocompleteRegistry ·static class; DevConsolePileNameAutocompleteCatalog ·static class; DevConsoleSecondaryResourceAutocompleteCatalog ·static class

**STS2RitsuLib.Interactions.RightClick** — IModRightClickHandler ·interface; IModRightClickableCard ·interface; IModRightClickableModel ·interface; IModRightClickableOrb ·interface; IModRightClickablePotion ·interface; IModRightClickablePower ·interface; IModRightClickableRelic ·interface; ModRightClickBindingId ·struct; ModRightClickContext ·struct; ModRightClickExecutionContext ·struct; ModRightClickModelKind ·enum; ModRightClickRegistry ·static class; ModRightClickSource ·enum; ModRightClickTrigger ·struct

**STS2RitsuLib.Interop** — AssemblyInteropAttribute ·class; IModTypeDiscoveryContributor ·interface; InteropAnyParamAttribute ·class; InteropClassWrapper ·abstract class; InteropTargetAttribute ·class; JsonDomChannelDelegates ·class; KeyedJsonDomTransport ·static class; KeyedJsonPathRouting ·class; ModInteropAttribute ·class; ModInteropTypeDiscoveryContributor ·class; ModTypeDiscoveryHub ·static class; ReflectionInteropConvention ·class; ReflectionStaticChannel ·class; ReflectionStaticChannelBinder ·static class

**STS2RitsuLib.Interop.AutoRegistration** — ActScopedRegistrationAttributeBase ·abstract class; AttributeAutoRegistrationTypeDiscoveryContributor ·class; AutoRegistrationAttribute ·abstract class; AutoTimelineSlotAfterColumnAttribute ·class; AutoTimelineSlotAfterEpochColumnAttribute ·class; AutoTimelineSlotAttribute ·class; AutoTimelineSlotBeforeColumnAttribute ·class; AutoTimelineSlotBeforeEpochColumnAttribute ·class; AutoTimelineSlotInColumnAttribute ·class; AutoTimelineSlotInEpochColumnAttribute ·class; CharacterEpochRegistrationAttributeBase ·abstract class; CharacterStarterRegistrationAttributeBase ·abstract class; ContentRegistrationAttribute ·abstract class; KeywordRegistrationAttributeBase ·abstract class; ModelPublicEntryRegistrationAttributeBase ·abstract class; RegisterAchievementAttribute ·class; RegisterActAncientAttribute ·class; RegisterActAttribute ·class; RegisterActEncounterAttribute ·class; RegisterActEventAttribute ·class; RegisterAfflictionAttribute ·class; RegisterArchaicToothTranscendenceAttribute ·class; RegisterBadModifierAttribute ·class; RegisterCardAttribute ·class; RegisterCharacterAttribute ·class; RegisterCharacterStarterCardAttribute ·class; RegisterCharacterStarterPotionAttribute ·class; RegisterCharacterStarterRelicAttribute ·class; RegisterDefaultModelCapabilityAttribute ·class; RegisterDustyTomeCardAttribute ·class; RegisterEnchantmentAttribute ·class; RegisterEpochAttribute ·class; RegisterEpochCardsAttribute ·class; RegisterEpochRelicsFromPoolAttribute ·class; RegisterGlobalEncounterAttribute ·class; RegisterGoodModifierAttribute ·class; RegisterModelCapabilityAttribute ·class; RegisterMonsterAttribute ·class; RegisterMutuallyExclusiveModifierGroupAttribute ·class; RegisterNodeAttachmentAttribute ·class; RegisterNodeAttachmentAttributeBase ·abstract class; RegisterNodeAttachmentFromConvertedSceneAttribute ·class; RegisterNodeAttachmentFromSceneAttribute ·class; RegisterOrbAttribute ·class; RegisterOwnedCardKeywordAttribute ·class; RegisterOwnedCardPileAttribute ·class; RegisterOwnedCardTagAttribute ·class; RegisterOwnedKeywordAttribute ·class; RegisterOwnedTopBarButtonAttribute ·class; RegisterPotionAttribute ·class; RegisterPowerAttribute ·class; RegisterRelicAttribute ·class; RegisterSharedAncientAttribute ·class; RegisterSharedCardPoolAttribute ·class; RegisterSharedEventAttribute ·class; RegisterSharedPotionPoolAttribute ·class; RegisterSharedRelicPoolAttribute ·class; RegisterSingletonAttribute ·class; RegisterSmartFormatSourceAttribute ·class; RegisterSmartFormatterAttribute ·class; RegisterStoryAttribute ·class; RegisterStoryEpochAttribute ·class; RegisterTouchOfOrobasRefinementAttribute ·class; RegisterTrashHeapCardAttribute ·class; RegisterTrashHeapRelicAttribute ·class; RequireAllCardsInPoolAttribute ·class; RequireEpochAttribute ·class; RevealAscensionAfterEpochAttribute ·class; RitsuLibOwnedByAttribute ·class; UnlockCharacterAfterRunAsAttribute ·class; UnlockEpochAfterAscensionOneWinAttribute ·class; UnlockEpochAfterAscensionWinAttribute ·class; UnlockEpochAfterBossVictoriesAttribute ·class; UnlockEpochAfterEliteVictoriesAttribute ·class; UnlockEpochAfterRunAsAttribute ·class; UnlockEpochAfterWinAsAttribute ·class

**STS2RitsuLib.Keywords** — KeywordRegistrationEntry ·class; KeywordRegistrationState ·enum; ModKeywordCardDescriptionPlacement ·enum; ModKeywordDefinition ·class; ModKeywordExtensions ·static class; ModKeywordRegistry ·class

**STS2RitsuLib.Localization** — AncientDialogueLocalization ·static class; I18NLocTableBridge ·static class

**STS2RitsuLib.Localization.SmartFormat** — ModSmartFormatExtensionDefinition ·class; ModSmartFormatExtensionRegistry ·class; SmartFormatExtensionInjector ·static class; SmartFormatExtensionKind ·enum

**STS2RitsuLib.Models** — HookedSingletonModel ·abstract class; ModelCloneContext ·struct; ModelCloneRegistry ·class; ModelLocStringSource ·class; ModelTitleExtensions ·static class

**STS2RitsuLib.Models.Capabilities** — AfflictionCapability ·abstract class; ApplyModelCapabilityOptions ·struct; CapabilityCardModel ·abstract class; CardCapability ·abstract class; CardDescriptionContext ·struct; CardDescriptionFragment ·struct; CardDescriptionFragmentPlacement ·enum; CardOverlayContext ·struct; CardOverlayContribution ·class; CardPlayCapability ·abstract class; CardTitleContext ·struct; CardTitleFragment ·struct; CardTitleFragmentPlacement ·enum; CharacterCapability ·abstract class; EnchantmentCapability ·abstract class; ICardDescriptionContributor ·interface; ICardEnergyCostContributor ·interface; ICardGlowContributor ·interface; ICardHoverTipContributor ·interface; ICardOverlayAssetPathContributor ·interface; ICardOverlayContributor ·interface; ICardPlayResultContributor ·interface; ICardPlayStateContributor ·interface; ICardPropertyContributor ·interface; ICardStarCostContributor ·interface; ICardTitleContributor ·interface; ICardTransformCarryOverCapability ·interface; ICardTypeTextModifier ·interface; ICustomTypeTextCard ·interface; IModelAssetPathContributor ·interface; IModelAssetPathContributor<TModel> ·interface; IModelCapability ·interface; IModelCapability<TModel> ·interface; IModelCapabilityCloneHandler ·interface; IModelCapabilityCloneNotification ·interface; IModelCapabilityHookListener ·interface; IModelCapabilityJsonState ·interface; IModelCapabilityMergeHandler ·interface; IModelCapabilitySource ·interface; IModelDynamicVarContributor ·interface; IModelHoverTipContributor ·interface; IModelHoverTipContributor<TModel> ·interface; IModelRightClickCapability ·interface; IOrbHoverTipDescriptionContributor ·interface; IOrbValueDisplayContributor ·interface; MissingModelCapabilityAnchorPolicy ·enum; ModelAssetPathContext ·struct; ModelAssetPathScope ·enum; ModelCapabilities ·static class; ModelCapability ·abstract class; ModelCapability<TModel> ·abstract class; ModelCapabilityConflictLogMode ·enum; ModelCapabilityDiagnostics ·static class; ModelCapabilityDynamicVarNames ·static class; ModelCapabilityExtensions ·static class; ModelCapabilityList ·class; ModelCapabilityRegistry ·static class; ModelCapabilitySaveDocument ·class; ModelCapabilitySaveEntry ·class; ModelCapabilitySet ·class; ModelHookListener<TListener> ·struct; ModelHookListenerDispatcher ·static class; ModelRightClickCapabilityRunMode ·enum; ModelSavedData<TTarget,TPayload> ·class; ModelSavedDataClonePolicy ·enum; ModelSavedDataOptions ·class; ModelSavedDataStore ·class; ModelSavedDataWritePolicy ·enum; MonsterCapability ·abstract class; OneShotCardPlayCapability ·abstract class; OrbAfterTurnStartTriggerContext ·struct; OrbBeforeTurnEndTriggerContext ·struct; OrbCapability ·abstract class; OrbEvokeContext ·struct; OrbHoverTipDescriptionContext ·struct; OrbHoverTipDescriptionFragment ·struct; OrbHoverTipDescriptionFragmentPlacement ·enum; OrbPassiveTriggerContext ·struct; OrbValueDisplayContext ·struct; OrbValueDisplayState ·struct; OwnerHookCapability<TModel> ·abstract class; PotionCapability ·abstract class; PowerCapability ·abstract class; RelicCapability ·abstract class; StatefulModelCapability<TModel,TState> ·abstract class; StatefulModelCapability<TState> ·abstract class; TurnLimitedCapability<TModel> ·abstract class; UnknownModelCapabilityPolicy ·enum; UntilCombatEndCapability<TModel> ·abstract class

**STS2RitsuLib.Models.Identity** — ModModelIdentity ·struct; ModModelIdentityToken ·struct

**STS2RitsuLib.Networking.ManagedActions** — RitsuLibManagedGameAction ·class; RitsuLibManagedNetAction ·abstract class; RitsuLibManagedNetActionContext<T> ·struct; RitsuLibManagedNetActionDescriptor<T> ·class; RitsuLibManagedNetActions ·static class

**STS2RitsuLib.Networking.MessageExtensions** — RitsuNetMessageTailExtensions ·static class

**STS2RitsuLib.Networking.Sidecar** — IRitsuLibSidecarCapabilityValidationRoute ·interface; IRitsuLibSidecarMessageCodec<T> ·interface; IRitsuLibSidecarSyncProcessor<T> ·interface; RitsuLibSidecar ·static class; RitsuLibSidecarBus ·static class; RitsuLibSidecarChunkBinary ·static class; RitsuLibSidecarChunkReceiveProgress ·struct; RitsuLibSidecarChunkStream ·static class; RitsuLibSidecarChunkStreamSendProgress ·struct; RitsuLibSidecarChunkTransferNotifications ·static class; RitsuLibSidecarConfigSyncService ·static class; RitsuLibSidecarConnectionExchange ·static class; RitsuLibSidecarConnectionSession ·static class; RitsuLibSidecarControlOpcodes ·static class; RitsuLibSidecarDeliverySemantics ·enum; RitsuLibSidecarDispatchContext ·struct; RitsuLibSidecarEnvelope ·static class; RitsuLibSidecarEvents ·static class; RitsuLibSidecarGodotMainLoopScheduling ·static class; RitsuLibSidecarHandshakeBinary ·static class; RitsuLibSidecarHeaderExtension ·static class; RitsuLibSidecarHighLevelSend ·static class; RitsuLibSidecarJsonSerializer<T> ·class; RitsuLibSidecarMessageBinding ·static class; RitsuLibSidecarMessageDescriptor<T> ·class; RitsuLibSidecarNetDiagnosticsOptions ·static class; RitsuLibSidecarNetworkMapping ·static class; RitsuLibSidecarNetworkingLifecycle ·static class; RitsuLibSidecarOpcodes ·static class; RitsuLibSidecarPayloadCompression ·enum; RitsuLibSidecarPeerFeatures ·enum; RitsuLibSidecarPeerReachability ·enum; RitsuLibSidecarProtocol ·static class; RitsuLibSidecarRequestCorrelation ·static class; RitsuLibSidecarRequestReply ·static class; RitsuLibSidecarRequiredCapabilities ·static class; RitsuLibSidecarRequiredCapabilityPolicy ·enum; RitsuLibSidecarResourcePolicy ·static class; RitsuLibSidecarSend ·static class; RitsuLibSidecarSessionManager ·static class; RitsuLibSidecarSyncBroadcastScope ·enum; RitsuLibSidecarSyncFailurePolicy ·enum; RitsuLibSidecarSyncMessageContext<T> ·struct; RitsuLibSidecarSyncMessageDescriptor<T> ·class; RitsuLibSidecarSyncMessages ·static class; RitsuLibSidecarTrafficCounters ·static class; RitsuLibSidecarTypedDispatchContext<T> ·struct; RitsuLibSidecarTypedMessageRegistry ·static class; RitsuLibSidecarWire ·static class; RitsuLibSidecarWireFlags ·enum; SidecarConfigTopicChangedEvent ·struct; SidecarHandshakeCompletedEvent ·struct; SidecarPeerReachabilityChangedEvent ·struct; SidecarRequiredCapabilityCheckCompletedEvent ·struct; SidecarRequiredCapabilityMiss ·struct; SidecarSessionBoundEvent ·struct; SidecarSessionUnboundEvent ·struct; SidecarTypedMessageReceivedEvent ·struct

**STS2RitsuLib.Patching** — PrivateAccess ·static class

**STS2RitsuLib.Patching.Builders** — DynamicPatchBuilder ·class

**STS2RitsuLib.Patching.Core** — ModPatcher ·class; ModPatcherExtensions ·static class; PatchLog ·static class; PatchTargetMethodResolver ·static class

**STS2RitsuLib.Patching.Models** — DynamicPatchInfo ·class; IModPatches ·interface; IPatchMethod ·interface; ModPatchInfo ·class; ModPatchResult ·class; ModPatchTarget ·class; PatchTarget ·static class

**STS2RitsuLib.Patching.Rules** — ModPatchRule ·class; PatchRuleBuilder ·class

**STS2RitsuLib.Relics.Visibility** — IModRelicVisibility ·interface; ModRelicVisibilityRegistry ·static class

**STS2RitsuLib.RunData** — PlayerRunSavedData<T> ·class; PlayerRunSavedDataLobbyScope<T> ·class; RunSavedData<T> ·class; RunSavedDataLobby ·static class; RunSavedDataLobbyScope<T> ·class; RunSavedDataLobbyStagingEvent ·class; RunSavedDataLobbyStagingReason ·enum; RunSavedDataOptions ·class; RunSavedDataPreparingEvent ·class; RunSavedDataStore ·class; RunSavedDataWritePolicy ·enum

**STS2RitsuLib.RunRngs** — ModRunRngRegistry ·static class; ModRunRngSnapshot ·class; ModRunRngState ·class

**STS2RitsuLib.RuntimeInput** — IRuntimeHotkeyHandle ·interface; RitsuSteamInputActionRegistry ·static class; RuntimeHotkeyOptions ·class; RuntimeHotkeyRegistrationDetails ·class; RuntimeHotkeyRegistrationInfo ·class; RuntimeHotkeyService ·static class; RuntimeHotkeyText ·abstract class

**STS2RitsuLib.Saves** — PreservedProgressRecords ·class

**STS2RitsuLib.Saves.RawProgress** — CloudReadBackStatus ·enum; IRawProgressCommitBridge ·interface; ProgressGeneration ·class; RawProgressBridge ·static class; RawProgressBridgeDescriptor ·class; RawProgressBridgeFeature ·enum; RawProgressCommitOutcome ·enum; RawProgressCommitRequest ·class; RawProgressCommitResult ·class; RawProgressReadOutcome ·enum; RawProgressReadResult ·class; RawProgressRecoveryDiscardOutcome ·enum; RawProgressRecoveryDiscardResult ·class; RawProgressRecoveryReadOutcome ·enum; RawProgressRecoveryReadResult ·class; RawProgressRecoveryRecord ·class; RawProgressRecoveryRequest ·class; RawProgressRecoveryStage ·enum; RawProgressSnapshot ·class

**STS2RitsuLib.Scaffolding.Ancients.Options** — ModAncientOptionRegistry ·static class; ModAncientOptionRule ·class

**STS2RitsuLib.Scaffolding.Cards.HandGlow** — CardModelHandGlowExtensions ·static class; ModCardHandGlowCombine ·static class; ModCardHandGlowPredicates ·static class; ModCardHandGlowRegistry ·static class; ModCardHandGlowRules ·struct

**STS2RitsuLib.Scaffolding.Cards.HandOutline** — ModCardHandOutlineRegistry ·static class; ModCardHandOutlineRules ·struct; ModCardHandOutlineRules<TCard> ·struct; ModCardHandOutlineSwitchRule ·struct; ModCardHandOutlineSwitchRule<TCard> ·struct

**STS2RitsuLib.Scaffolding.Characters** — CharacterAssetPathHelper ·static class; CharacterAssetProfile ·class; CharacterAssetProfiles ·static class; CharacterAudioAssetSet ·class; CharacterCombatExtensions ·static class; CharacterMultiplayerAssetSet ·class; CharacterOwnedVanillaRelicModelId ·static class; CharacterSceneAssetSet ·class; CharacterSpineAssetSet ·class; CharacterTrailStyle ·class; CharacterUiAssetSet ·class; CharacterVanillaCardVisualOverride ·class; CharacterVanillaPotionVisualOverride ·class; CharacterVanillaRelicVisualOverride ·class; CharacterVfxAssetSet ·class; IModCharacterAssetOverrides ·interface; IModCharacterCardLibraryCompendiumPlacement ·interface; IModCharacterEpochTimelineRequirement ·interface; IModCharacterUnlockPrerequisite ·interface; IModCharacterVanillaSelectionPolicy ·interface; IModColorfulPhilosophersCardPool ·interface; ModCharacterTemplate<TCardPool,TRelicPool,TPotionPool> ·abstract class; StartingDeckEntry ·struct

**STS2RitsuLib.Scaffolding.Characters.Visuals** — ModCreatureVisualPlayback ·static class; ModWorldSceneVisualNodeFactory ·static class

**STS2RitsuLib.Scaffolding.Characters.Visuals.Definition** — CharacterMerchantWorldDefinition ·class; CharacterRestSiteWorldDefinition ·class; CharacterWorldProceduralVisualSet ·class; CharacterWorldProceduralVisualSetBuilder ·class; ModCharacterWorldSceneVisuals ·static class

**STS2RitsuLib.Scaffolding.Combat** — CombatTurnPhaseExtensions ·static class

**STS2RitsuLib.Scaffolding.Content** — AchievementRegistrationEntry<TAchievement> ·class; ActAncientRegistrationEntry<TAct,TAncient> ·class; ActAssetProfile ·class; ActEncounterRegistrationEntry<TAct,TEncounter> ·class; ActEventRegistrationEntry<TAct,TEvent> ·class; ActRegistrationEntry<TAct> ·class; AfflictionAssetProfile ·class; AfflictionRegistrationEntry<TAffliction> ·class; AncientEventPresentationAssetProfile ·class; AncientEventStageProceduralVisualSet ·class; AncientEventStageProceduralVisualSetBuilder ·class; AncientOptionRegistrationEntry<TAncient> ·class; ArchaicToothTranscendenceByIdRegistrationEntry ·class; ArchaicToothTranscendenceRegistrationEntry<TStarterCard,TAncientCard> ·class; BadModifierRegistrationEntry<TModifier> ·class; BindCardUnlockEpochPackEntry<TEpoch> ·class; BindRelicUnlockEpochPackEntry<TEpoch> ·class; CardAssetProfile ·class; CardHandGlowRegistrationEntry<TCard> ·class; CardHandOutlineRegistrationEntry<TCard> ·class; CardPoolAssetProfile ·class; CardPoolAssetProfiles ·static class; CardPoolDeckViewStyle ·class; CardPoolDeckViewStyleContext ·class; CardRegistrationEntry<TPool,TCard> ·class; CardVisualStyle ·enum; CharacterAssetReplacementRegistrationEntry ·class; CharacterRegistrationEntry<TCharacter> ·class; CharacterStarterCardRegistrationEntry<TCharacter,TCard> ·class; CharacterStarterPotionRegistrationEntry<TCharacter,TPotion> ·class; CharacterStarterRelicRegistrationEntry<TCharacter,TRelic> ·class; CombatBackgroundAssetsFactory ·static class; ContentAssetProfiles ·static class; DustyTomeCardByIdRegistrationEntry ·class; DustyTomeCardRegistrationEntry<TCharacter,TAncientCard> ·class; EnchantmentAssetProfile ·class; EnchantmentRegistrationEntry<TEnchantment> ·class; EncounterAssetProfile ·class; EpochAssetProfile ·class; EpochPackEntry<TEpoch> ·class; EpochSlotBuilder<TEpoch> ·class; EventAssetProfile ·class; GlobalEncounterRegistrationEntry<TEncounter> ·class; GoodModifierRegistrationEntry<TModifier> ·class; HealthBarForecastRegistrationEntry<TSource> ·class; IContentRegistrationEntry ·interface; IModActRandomListPolicy ·interface; IModAncientActValidity ·interface; IModCharacterCreatureAnimatorFactory ·interface; IModCharacterCreatureVisualsFactory ·interface; IModCharacterMerchantAnimationStateMachineFactory ·interface; IModCharacterRestSiteAnimationStateMachineFactory ·interface; IModContentPackEntry ·interface; IModCreatureAnimatorFactory ·interface; IModCreatureCombatAnimationStateMachineFactory ·interface; IModCreatureVisualsFactory ·interface; IModEncounterActValidity ·interface; IModEncounterCombatSceneFactory ·interface; IModEventBackgroundPackedSceneFactory ·interface; IModEventLayoutPackedSceneFactory ·interface; IModEventVfxFactory ·interface; IModMonsterCreatureVisualsFactory ·interface; IModNonSpineAnimationStateMachineFactory ·interface; IModOrbRandomPoolPolicy ·interface; IModOrbSpriteFactory ·interface; IModOrbValueDisplayPolicy ·interface; IModPotionAssetOverrides ·interface; IModRestSiteOptionAssetOverrides ·interface; IModRestSiteOptionCustomTitle ·interface; ModActTemplate ·abstract class; ModAfflictionTemplate ·abstract class; ModAncientActValidityFilter ·static class; ModAncientEventTemplate ·abstract class; ModAncientStageVisuals ·static class; ModBadgeTemplate ·abstract class; ModCardTemplate ·abstract class; ModContentPackBuilder ·class; ModContentPackContext ·struct; ModEnchantmentTemplate ·abstract class; ModEncounterActValidityFilter ·static class; ModEncounterTemplate ·abstract class; ModEventTemplate ·abstract class; ModModifierTemplate ·abstract class; ModMonsterTemplate ·abstract class; ModOrbTemplate ·abstract class; ModOrbValueDisplayMode ·enum; ModPlaceholderCardTemplate ·abstract class; ModPlaceholderPotionTemplate ·abstract class; ModPlaceholderRelicTemplate ·abstract class; ModPotionTemplate ·abstract class; ModPowerTemplate ·abstract class; ModRelicTemplate ·abstract class; ModRestSiteOptionTemplate ·abstract class; ModifierAssetProfile ·class; MonsterAssetProfile ·class; MonsterRegistrationEntry<TMonster> ·class; OrbAssetProfile ·class; OrbRegistrationEntry<TOrb> ·class; PlaceholderCardFromOptionsRegistrationEntry<TPool> ·class; PlaceholderCardRegistrationEntry<TPool> ·class; PlaceholderPotionFromOptionsRegistrationEntry<TPool> ·class; PlaceholderPotionRegistrationEntry<TPool> ·class; PlaceholderRelicFromOptionsRegistrationEntry<TPool> ·class; PlaceholderRelicRegistrationEntry<TPool> ·class; PotionAssetProfile ·class; PotionRegistrationEntry<TPool,TPotion> ·class; PowerAssetProfile ·class; PowerRegistrationEntry<TPower> ·class; RelicAssetProfile ·class; RelicRegistrationEntry<TPool,TRelic> ·class; RequireEpochPackEntry<TModel,TEpoch> ·class; RestSiteOptionAssetProfile ·class; RevealAscensionAfterEpochPackEntry<TCharacter,TEpoch> ·class; RuntimeAssetReloadExtensions ·static class; SharedAncientRegistrationEntry<TAncient> ·class; SharedCardPoolRegistrationEntry<TPool> ·class; SharedEventRegistrationEntry<TEvent> ·class; SharedPotionPoolRegistrationEntry<TPool> ·class; SharedRelicPoolRegistrationEntry<TPool> ·class; SingletonRegistrationEntry<TSingleton> ·class; StoryEpochPackEntry<TStory,TEpoch> ·class; StoryPackEntry<TStory> ·class; TimelineColumnBuilder<TStory> ·class; TimelineColumnPackEntry<TStory> ·class; TouchOfOrobasRefinementByIdRegistrationEntry ·class; TouchOfOrobasRefinementRegistrationEntry<TStarterRelic,TUpgradedRelic> ·class; TrashHeapCardRegistrationEntry<TCard> ·class; TrashHeapRelicRegistrationEntry<TRelic> ·class; TypeListCardPoolModel ·abstract class; TypeListPotionPoolModel ·abstract class; TypeListRelicPoolModel ·abstract class; UnlockCharacterAfterRunAsPackEntry<TCharacter,TEpoch> ·class; UnlockEpochAfterAscensionOneWinPackEntry<TCharacter,TEpoch> ·class; UnlockEpochAfterAscensionWinPackEntry<TCharacter,TEpoch> ·class; UnlockEpochAfterBossVictoriesPackEntry<TCharacter,TEpoch> ·class; UnlockEpochAfterEliteVictoriesPackEntry<TCharacter,TEpoch> ·class; UnlockEpochAfterRunAsPackEntry<TCharacter,TEpoch> ·class; UnlockEpochAfterRunCountPackEntry<TEpoch> ·class; UnlockEpochAfterWinAsPackEntry<TCharacter,TEpoch> ·class

**STS2RitsuLib.Scaffolding.Content.Patches** — CardPoolDeckViewStyleRegistry ·static class; ExternalAssetOverrideRegistry ·static class; ExternalBadgeIconOverrideRegistry ·static class; ExternalCardMaterialOverrideRegistry ·static class; IModActAssetOverrides ·interface; IModAfflictionAssetOverrides ·interface; IModAncientEventAssetOverrides ·interface; IModBigEnergyIconPool ·interface; IModCardAncientBannerMaterialOverride ·interface; IModCardAncientBorderMaterialOverride ·interface; IModCardAncientTextBgMaterialOverride ·interface; IModCardAssetOverrides ·interface; IModCardBannerMaterialOverride ·interface; IModCardEnergyIconMaterialOverride ·interface; IModCardFrameMaterialOverride ·interface; IModCardPoolAssetOverrides ·interface; IModCardPoolDeckViewStyle ·interface; IModCardPoolFrameMaterial ·interface; IModCardPortraitBorderMaterialOverride ·interface; IModCardPortraitMaterialOverride ·interface; IModEnchantmentAssetOverrides ·interface; IModEncounterAssetOverrides ·interface; IModEpochAssetOverrides ·interface; IModEventAssetOverrides ·interface; IModModifierAssetOverrides ·interface; IModMonsterAssetOverrides ·interface; IModOrbAssetOverrides ·interface; IModPowerAssetOverrides ·interface; IModRelicAssetOverrides ·interface; IModTextEnergyIconPool ·interface; RuntimeAssetRefreshCoordinator ·static class; RuntimeAssetRefreshScope ·enum

**STS2RitsuLib.Scaffolding.Content.Visuals** — AncientStageProceduralRootFactory ·static class

**STS2RitsuLib.Scaffolding.Godot** — IRitsuGodotNodeFactory<TNode> ·interface; RitsuGodotNodeExtensions ·static class; RitsuGodotNodeFactories ·static class; RitsuGodotPackedSceneHelper ·static class; RitsuGodotTreeCompat ·static class

**STS2RitsuLib.Scaffolding.Godot.NodeAttachments** — INodeAttachmentFactory ·interface; INodeAttachmentSetup ·interface; ModNodeAttachmentRegistry ·class; NodeAttachmentAddMode ·enum; NodeAttachmentDefinition ·class; NodeAttachmentDuplicatePolicy ·enum; NodeAttachmentOptions ·class; NodeAttachmentSetupTiming ·enum

**STS2RitsuLib.Scaffolding.MonsterMoves** — ModMonsterMoveStateMachines ·static class

**STS2RitsuLib.Scaffolding.Visuals** — ModVisualCues ·static class

**STS2RitsuLib.Scaffolding.Visuals.Definition** — VisualCueSet ·class; VisualCueSetBuilder ·class; VisualFrame ·struct; VisualFrameSequence ·class; VisualFrameSequenceBuilder ·class; VisualNodeStyle ·class

**STS2RitsuLib.Scaffolding.Visuals.StateMachine** — CompositeBackendFactory ·static class; IAnimationBackend ·interface; IAnimationTimingProvider ·interface; ModAnimState ·class; ModAnimStateMachine ·class; ModAnimStateMachineBuilder ·class; ModAnimStateMachines ·static class

**STS2RitsuLib.Scaffolding.Visuals.StateMachine.Backends** — AnimatedSprite2DBackend ·class; AnimationTreeStateMachineBackend ·class; CompositeAnimationBackend ·class; CueAnimationBackend ·class; FormSwitchingAnimationBackend ·class; GodotAnimationPlayerBackend ·class; SpineAnimationBackend ·class

**STS2RitsuLib.Screens** — ModScreenService ·static class

**STS2RitsuLib.Search** — IRitsuSearchExpansionProvider ·interface; RitsuSearch ·static class; RitsuSearchExpansion ·class; RitsuSearchExpansionContext ·class; RitsuSearchExpansionKind ·enum; RitsuSearchExpansionRegistration ·class; RitsuSearchExpansionRegistry ·static class; RitsuSearchText ·class

**STS2RitsuLib.Settings** — ButtonModSettingsEntryDefinition ·class; ChoiceModSettingsEntryDefinition<TValue> ·class; ColorModSettingsEntryDefinition ·class; CustomModSettingsEntryDefinition ·class; DefaultModSettingsValueBinding<TValue> ·class; FloatSliderModSettingsEntryDefinition ·class; HeaderModSettingsEntryDefinition ·class; HostContextButtonModSettingsEntryDefinition ·class; IDefaultModSettingsValueBinding<TValue> ·interface; IModSettingsBinding ·interface; IModSettingsBindingSemantics ·interface; IModSettingsUiActionHost ·interface; IModSettingsValueBinding<TValue> ·interface; IStructuredModSettingsValueAdapter<TValue> ·interface; IStructuredModSettingsValueBinding<TValue> ·interface; ITransientModSettingsBinding ·interface; ImageModSettingsEntryDefinition ·class; InMemoryModSettingsValueBinding<TValue> ·class; InfoCardModSettingsEntryDefinition ·class; InputBindingModSettingsEntryDefinition ·class; IntSliderModSettingsEntryDefinition ·class; KeyBindingModSettingsEntryDefinition ·class; ListModSettingsEntryDefinition<TItem> ·class; ModConfigMirrorRegistrationOptions ·class; ModSettingsBindingAttribute ·class; ModSettingsBindingWriteEvents ·static class; ModSettingsBindings ·static class; ModSettingsButtonAttribute ·class; ModSettingsButtonTone ·enum; ModSettingsCallbackValueBinding<T> ·class; ModSettingsChoiceAttribute ·class; ModSettingsChoiceControl<TValue> ·class; ModSettingsChoiceOption<TValue> ·struct; ModSettingsChoicePresentation ·enum; ModSettingsChromeBindingSnapshot ·class; ModSettingsClipboardAccess ·static class; ModSettingsClipboardEnvelopeView ·class; ModSettingsClipboardOperations ·static class; ModSettingsClipboardScope ·enum; ModSettingsColorAttribute ·class; ModSettingsColorControl ·class; ModSettingsCopyActionEventArgs ·class; ModSettingsCustomEntryAttribute ·class; ModSettingsDoubleFromIntBinding ·class; ModSettingsDropdownChoiceControl<TValue> ·class; ModSettingsEntryDefinition ·abstract class; ModSettingsFloatSliderControl ·class; ModSettingsGameSettingsEntryLine ·static class; ModSettingsGamepadCompatibleButton ·class; ModSettingsHeaderAttribute ·class; ModSettingsHostSurface ·enum; ModSettingsHostSurfaceResolver ·static class; ModSettingsImageAttribute ·class; ModSettingsInfoCardAttribute ·class; ModSettingsIntFromDoubleBinding ·class; ModSettingsIntSliderAttribute ·class; ModSettingsKeyBindingAttribute ·class; ModSettingsKeyBindingControl ·class; ModSettingsLabelDescriptionTextAttribute ·abstract class; ModSettingsListItemContext<TItem> ·class; ModSettingsLocation ·class; ModSettingsMenuAction ·class; ModSettingsMenuCapabilities ·enum; ModSettingsMiniButton ·class; ModSettingsMultiKeyBindingControl ·class; ModSettingsMultilineStringAttribute ·class; ModSettingsNavigator ·static class; ModSettingsOpenOptions ·class; ModSettingsOpenResult ·class; ModSettingsOrderedEntryAttribute ·abstract class; ModSettingsPage ·class; ModSettingsPageAttribute ·class; ModSettingsPageBuilder ·class; ModSettingsPageCopyEventArgs ·class; ModSettingsPageDataClipboardPayload ·class; ModSettingsPagePasteEventArgs ·class; ModSettingsPageUiContext ·class; ModSettingsParagraphAttribute ·class; ModSettingsPasteFailureReason ·enum; ModSettingsPasteValidationContext ·class; ModSettingsPasteVerdict ·enum; ModSettingsReflectionBindingSource ·enum; ModSettingsRegistry ·static class; ModSettingsRuntimeHotkeySummaryAttribute ·class; ModSettingsRuntimeReflectionInteropMirror ·static class; ModSettingsSection ·class; ModSettingsSectionAttribute ·class; ModSettingsSectionBuilder ·class; ModSettingsSectionCopyEventArgs ·class; ModSettingsSectionDataClipboardPayload ·class; ModSettingsSectionPasteEventArgs ·class; ModSettingsSectionUiContext ·class; ModSettingsSidebarButton ·class; ModSettingsSidebarItemKind ·enum; ModSettingsSliderAttribute ·class; ModSettingsStandardActionIds ·static class; ModSettingsStringAttribute ·class; ModSettingsStringLineControl ·class; ModSettingsStringMultilineControl ·class; ModSettingsStructuredData ·static class; ModSettingsSubpageAttribute ·class; ModSettingsText ·abstract class; ModSettingsTextButton ·class; ModSettingsTitleDescriptionTextAttribute ·abstract class; ModSettingsToggleAttribute ·class; ModSettingsToggleControl ·class; ModSettingsTryPasteApplier<TValue> ·delegate; ModSettingsUiActionRegistry ·static class; ModSettingsUiChromeClipboard ·static class; ModSettingsUiControlTheming ·static class; ModSettingsUiFactory ·static class; ModSettingsUiPresentation ·static class; ModSettingsUiResources ·static class; ModSettingsValueBinding<TModel,TValue> ·class; ModSettingsValueSemantics ·enum; MultiKeyBindingModSettingsEntryDefinition ·class; MultilineStringModSettingsEntryDefinition ·class; ParagraphModSettingsEntryDefinition ·class; ProjectedModSettingsValueBinding<TSource,TValue> ·class; RitsuModSettingsSubmenu ·class; RuntimeHotkeySummaryModSettingsEntryDefinition ·class; SliderModSettingsEntryDefinition ·class; StringFieldModSettingsEntryDefinition ·abstract class; StringModSettingsEntryDefinition ·class; StructuredModSettingsValueBinding<TValue> ·class; SubpageModSettingsEntryDefinition ·class; ToggleModSettingsEntryDefinition ·class

**STS2RitsuLib.Telemetry** — DisabledTelemetryAdapter ·class; HttpJsonTelemetryAdapter ·class; ITelemetryAdapter ·interface; ITelemetryClient ·interface; ITelemetryContributionProvider ·interface; PostHogTelemetryAdapter ·class; TelemetryApi ·static class; TelemetryApplicant ·class; TelemetryConsentState ·enum; TelemetryContributionContext ·class; TelemetryContributionDefinition ·class; TelemetryContributionVisibility ·enum; TelemetryDataCategory ·enum; TelemetryEnvelope ·class; TelemetryRegistry ·static class; TelemetryRequest ·class; TelemetrySchemas ·static class; TelemetrySendResult ·struct

**STS2RitsuLib.Timeline** — ModEpochGatedContentRegistry ·static class; ModStoryEpochBindings ·static class; ModTimelineEraIconRegistry ·static class; ModTimelineLayoutRegistry ·static class; ModTimelineRegistry ·class

**STS2RitsuLib.Timeline.Scaffolding** — CardUnlockEpochTemplate ·abstract class; CharacterUnlockEpochTemplate<TCharacter> ·abstract class; ModEpochTemplate ·abstract class; ModStoryTemplate ·abstract class; PackDeclaredCardUnlockEpochTemplate ·abstract class; PackDeclaredRelicUnlockEpochTemplate ·abstract class; PotionUnlockEpochTemplate ·abstract class; RelicUnlockEpochTemplate ·abstract class

**STS2RitsuLib.TopBar** — IModTopBarButtonHandler ·interface; ModTopBarButtonContext ·class; ModTopBarButtonDefinition ·class; ModTopBarButtonHoverTipFactory ·static class; ModTopBarButtonRegistry ·class; ModTopBarButtonSpec ·class; ModTopBarLayout ·static class; TopBarButtonRegistrationEntry ·class

**STS2RitsuLib.Ui.Catalog** — RitsuCatalogBrowser ·class; RitsuCatalogBrowserOptions ·class; RitsuCatalogDetailPresentation ·enum; RitsuCatalogFilter ·class; RitsuCatalogFilterOption ·class; RitsuCatalogItem ·class; RitsuCatalogItemAction ·class; RitsuCatalogItemActionTone ·enum; RitsuCatalogPresentation ·enum; RitsuCatalogSelectionChangedEventArgs ·class

**STS2RitsuLib.Ui.RichTextEffects** — ModRichTextEffectRegistration ·class; ModRichTextEffectRegistry ·static class; ModRichTextTag ·static class; ModRichTextTagParameter ·struct

**STS2RitsuLib.Ui.Shell** — RitsuShellChromeStyles ·static class; RitsuShellPanelStyles ·static class; RitsuShellThemePaths ·static class; RitsuShellTooltipTheme ·static class

**STS2RitsuLib.Ui.Shell.Theme** — BgBorder ·class; BorderWidthMetrics ·class; ChoiceCenterTokens ·class; ChoiceMetrics ·class; ChromeMenuTokens ·class; CollapsibleTokens ·class; ColorRowMetrics ·class; ColorTokens ·class; ComponentTokens ·class; DragHandleTokens ·class; DropdownTokens ·class; EntryMetrics ·class; EntrySurfaceTokens ·class; FontSizeMetrics ·class; FontTokens ·class; FramedSurfaceTokens ·class; InsetSurfaceTokens ·class; KeybindingMetrics ·class; ListEditorTokens ·class; ListItemTokens ·class; ListShellTokens ·class; MetricTokens ·class; OverlayMetrics ·class; OverlayPanelTokens ·class; PageToolbarTrayTokens ·class; PillTokens ·class; RadiusMetrics ·class; RitsuShellTheme ·class; RitsuShellThemeCatalog ·static class; RitsuShellThemeDocument ·class; RitsuShellThemeModRegistration ·class; RitsuShellThemeRuntime ·static class; ShadowTokens ·class; SidebarBtnTokens ·class; SidebarCardTokens ·class; SidebarMetrics ·class; SidebarRailTokens ·class; SliderMetrics ·class; SliderTokens ·class; StepperTokens ·class; StringEntryMetrics ·class; StringValidationTokens ·class; SurfaceTokens ·class; TextButtonTokens ·class; TextButtonToneTokens ·class; TextTokens ·class; ToggleTokens ·class

**STS2RitsuLib.Ui.Toast** — RitsuToastAnchor ·enum; RitsuToastAnimationPreset ·enum; RitsuToastHandle ·class; RitsuToastLevel ·enum; RitsuToastRequest ·class; RitsuToastService ·static class

**STS2RitsuLib.Ui.Windows** — RitsuFloatingWindow ·class; RitsuFloatingWindowGeometry ·struct; RitsuFloatingWindowOptions ·class

**STS2RitsuLib.Unlocks** — CountedEpochUnlockRule ·class; EliteEpochUnlockRule ·class; ModUnlockRegistry ·class; PostRunEpochUnlockRule ·class; PostRunUnlockContext ·class

**STS2RitsuLib.Updates** — ModUpdateCheckLocalizedText ·class; ModUpdateCheckManifest ·class; ModUpdateCheckOptions ·class; ModUpdateCheckResult ·class; ModUpdateCheckStatus ·enum; ModUpdateChecker ·static class

**STS2RitsuLib.Utils** — AttachedState<TKey,TValue> ·class; CreatureHpDisplayExtensions ·static class; DynamicEnumValueDefinition<TEnum> ·class; DynamicEnumValueMinter<TEnum> ·class; DynamicEnumValueRegistry<TEnum> ·static class; FileOperations ·static class; GodotResourcePath ·static class; I18N ·class; IWeightedValue ·interface; MaterialUtils ·static class; ModDynamicEnumValueRegistry<TEnum> ·class; RitsuAnsiText ·static class; RitsuTextSegment ·class; SavedAttachedState<TKey,TValue> ·class; WeightedList<T> ·class

**STS2RitsuLib.Utils.HarmonyIl** — HarmonyAsyncAwaitSite ·class; HarmonyAsyncAwaitSites ·class; HarmonyAsyncIl ·static class; HarmonyAsyncTaskBridge ·static class; HarmonyIl ·static class; HarmonyIlBasicBlock ·class; HarmonyIlCallPath ·class; HarmonyIlContext ·class; HarmonyIlControlFlowDiagnostic ·class; HarmonyIlControlFlowGraph ·class; HarmonyIlEffectAnalysisOptions ·class; HarmonyIlEffectAnalysisResult ·class; HarmonyIlEffectAnalyzer ·static class; HarmonyIlEffectCallSite ·class; HarmonyIlEffectDiagnostic ·class; HarmonyIlEffectDiagnosticSeverity ·enum; HarmonyIlEffectMethodSlice ·class; HarmonyIlEffectSink ·class; HarmonyIlFlowEdge ·class; HarmonyIlFlowEdgeKind ·enum; HarmonyIlInspectionExtensions ·static class; HarmonyIlLocalRef ·struct; HarmonyIlMatch ·struct; HarmonyIlMatches ·class; HarmonyIlMethodBody ·class; HarmonyIlPattern ·class; HarmonyIlPayloadTranspiler ·static class; HarmonyIlPayloadTranspilerHandle ·class; HarmonyIlReturnInsertionMode ·enum; HarmonyIlRewriteReport ·struct; HarmonyIlRewriter ·class; HarmonyIlValidation ·static class; HarmonyIlValidationIssue ·struct; HarmonyIlValidationReport ·class

**STS2RitsuLib.Utils.Json** — JsonCanonicalizer ·static class; JsonIJsonValidator ·static class; JsonMergePatch ·static class; JsonPatch ·static class; JsonPatchException ·class; JsonPatchOperation ·class; JsonPointer ·static class

**STS2RitsuLib.Utils.Persistence** — DataLifecycleState ·enum; DataReadyLifecycle ·static class; PersistentDataEntry<T> ·class; ProfileDataChangedEvent ·struct; ProfileDataInvalidatedEvent ·struct; ProfileDataReadyEvent ·struct; ProfileManager ·class; SaveScope ·enum

**STS2RitsuLib.Utils.Persistence.Context** — StorageContext ·class; StorageContextKey<TValue> ·class; StorageContextKeys ·static class

**STS2RitsuLib.Utils.Persistence.Interop** — InteropMigrationAdapter ·class; ModDataInteropJsonDocument ·class

**STS2RitsuLib.Utils.Persistence.Migration** — IMigration ·interface; MigrationManager ·class; MigrationResult<T> ·class; ModDataMigrationConfig ·class; ModDataVersion ·static class

**STS2RitsuLib.Utils.Speculation** — SpeculativeDiagnostic ·class; SpeculativeDiagnosticSeverity ·enum; SpeculativeEffect ·class; SpeculativeExecutionBudget ·class; SpeculativeExecutionSession ·class

*(End of nsindex.md dump — all 92 namespaces, 1 325 public non-nested types.)*

### 2.3 Grouped reading of the index

The 92 public namespaces, grouped by what they are for: content authoring (`Scaffolding.Content` 139 types, `Scaffolding.Characters`, `Scaffolding.Visuals[.Definition|.StateMachine|.StateMachine.Backends]`, `Scaffolding.MonsterMoves`, `Scaffolding.Ancients.Options`, `Scaffolding.Cards.HandGlow|HandOutline`, `Scaffolding.Godot[.NodeAttachments]`, `Content`, `Keywords`, `CardTags`, `CardPiles`, `Timeline[.Scaffolding]`, `Unlocks`); runtime hooks (`Cards[.FreePlay|.DynamicVars|.Transforms]`, `Combat.AttackHits|CardTargeting|HandSize|Healing|HealthBars|PlayerResources|Powers|Rewards|SecondaryResources|Ui.ExtraCornerAmountLabels`, `Models[.Capabilities|.Identity]`, `Relics.Visibility`, `Interactions.RightClick`, `RunRngs`, root lifecycle events); persistence (`Data`, `RunData`, `Saves[.RawProgress]`, `Utils.Persistence[.Context|.Interop|.Migration]`); patching (`Patching[.Builders|.Core|.Models|.Rules]`, `Utils.HarmonyIl` 34 types, `Utils.Speculation`); networking (`Networking.ManagedActions|MessageExtensions|Sidecar`); presentation (`Settings` 127 types, `Ui.Catalog|Overlay|RichTextEffects|Shell[.Theme]|Toast|Windows`, `TopBar`, `Audio` 58 types, `Screens`, `RuntimeInput`, `Search`, `Localization[.SmartFormat]`); and infrastructure (`Interop[.AutoRegistration]` 90 types, `Compat`, `Telemetry`, `Updates`, `Diagnostics.*`, `Utils[.Json|.FileOperations]`).

The façade is `static class STS2RitsuLib.RitsuLibFramework` — about 120 static members: `Initialize()`, `CreateLogger`, `CreatePatcher`, `ApplyRequiredPatcher`, `EnsureGodotScriptsRegistered`, `SubscribeLifecycle*`, `GetContentRegistry`, `CreateContentPack`, `GetDataStore`, `GetRunSavedDataStore`, `GetKeywordRegistry`, `GetCardTagRegistry`, `GetCardPileRegistry`, `GetTimelineRegistry`, `GetUnlockRegistry`, `GetSecondaryResourceRegistry`, `GetNodeAttachmentRegistry`, `GetModelCloneRegistry`, `GetModelCapabilities`, `RegisterFreePlayBinding`, `RegisterCardOnPlayHookListener`, `RegisterHealHookListener`, `GetMaxHandSize`, `RegisterSingleTargetType`/`RegisterMultiTargetType`, `RegisterDynamicEnumValue<TEnum>`, `GetModRunRng`/`GetModPlayerRng`, `RegisterRelicVisibilityRule`, `RegisterRightClick<TModel>`, `RegisterModSettings`, `CreateLocalization*`, `RegisterI18NLocTableBridge`, telemetry and update-check registration. Content registration also has a fluent builder (`ModContentPackBuilder.For(modId)` with ~140 chained methods) and a full attribute-driven route (`Interop.AutoRegistration.{RegisterCard, RegisterRelic, RegisterPotion, RegisterCharacter, RegisterAct, RegisterMonster, RegisterActEncounter, RegisterGlobalEncounter, RegisterActEvent, RegisterSharedEvent, RegisterPower, RegisterOrb, RegisterEnchantment, RegisterAffliction, RegisterEpoch, ...}Attribute`, ~55 attributes).

## 3. Capability areas with full signatures

### 3.1 Monster/encounter registration and the non-Spine animation surface

Registration (see also §1.1): `ModMonsterTemplate`, `ModContentRegistry.RegisterMonster<TMonster>()`, `ModEncounterTemplate`, `ModContentRegistry.RegisterActEncounter<TAct,TEncounter>()` / `RegisterGlobalEncounter<TEncounter>()`, `IModEncounterActValidity.IsValidForAct(ActModel)`, `ModMonsterMoveStateMachines` (`SingleMoveLoop`, `Cycle`, `HeadThenRepeatTail(MoveState head, MoveState tail)`, `RandomEntry(string, Action<RandomBranchState>, IReadOnlyList<MonsterState>)`, `ConditionalEntry(string, Action<ConditionalBranchState>, IReadOnlyList<MonsterState>)` — maps 1:1 onto StS1 move patterns).

Non-Spine animation surface — what RitsuLib genuinely adds over BaseLib (needs no Godot animation resource at all):

- `VisualCueSet { IReadOnlyDictionary<string,string> TexturePathByCue; IReadOnlyDictionary<string,VisualFrameSequence> FrameSequenceByCue; IReadOnlyDictionary<string,VisualNodeStyle> TextureStyleByCue }`, `struct VisualFrame(string TexturePath, float DurationSeconds)`, `VisualFrameSequence { Frames; Loop; DefaultStyle; FrameStyles }`, builders `ModVisualCues.CueSet()` / `.FrameSequence()`, `VisualCueSetBuilder.Single(cue, path[, style|fps])` / `.Sequence(cue, ...)`.
- `Backends.CueAnimationBackend(Godot.Node, Godot.Sprite2D, VisualCueSet)` drives a bare `Sprite2D` from those paths. XML: *"Animation ids map to cue keys in FrameSequenceByCue (preferred) or TexturePathByCue (fallback static texture)."*
- A real state machine rather than play-by-name: `ModAnimStateMachine(IAnimationBackend)` with `ModAnimState(string id, bool isLooping)`, `NextState`, `AddBranch(string, ModAnimState, Func<bool>)`, `AddAnyState(...)`, `SetTrigger(string)`, `TryGetCurrentAnimationDuration(out float)`, events `AnimationStarted/Completed/Interrupted`; builder `ModAnimStateMachineBuilder.Create()/.AddState/.AddBranch/.AddAnyState/.Build(IAnimationBackend)/.BuildSpine(MegaSprite)/.BuildForVisualsRoot(Node, CharacterModel, VisualCueSet)`.
- Seven backends: `AnimatedSprite2DBackend`, `AnimationTreeStateMachineBackend`, `GodotAnimationPlayerBackend`, `SpineAnimationBackend`, `CueAnimationBackend`, `CompositeAnimationBackend`, `FormSwitchingAnimationBackend` (+ `SwitchForm(string,bool)`).
- Trigger routing that does not require subclassing: `IModCreatureCombatAnimationStateMachineFactory.TryCreateCombatAnimationStateMachine(Godot.Node visualsRoot)` — XML: *"any model type implementing this interface is routed through ModCreatureCombatAnimationPlaybackPatch — template subclassing is not required"*, *"receives the same trigger names that vanilla would dispatch to a Spine animator (`Idle`, `Attack`, `Cast`, `Hit`, `Dead`, `Revive`, and others)"*, *"calls this method at most once during each combat visuals lifetime."*

### 3.2 Character asset replacement

- `void ModContentRegistry.RegisterCharacterAssetReplacement(string characterId, CharacterAssetProfile)` — XML: *"Registers asset replacements for a character ID. Non-null fields from later registrations take precedence."*
- `void ModContentRegistry.RegisterGlobalCharacterAssetReplacement(CharacterAssetProfile)` — XML: *"Registers this mod's asset replacements for all characters. Character-specific replacements take precedence."*
- `static string ModContentRegistry.NormalizeCharacterAssetEntryKey(string)` — trim + invariant uppercase; builder form `ModContentPackBuilder.CharacterAssetReplacement(string, CharacterAssetProfile)`; entry type `CharacterAssetReplacementRegistrationEntry`.
- `ModContentRegistry.VanillaCharacterIds.{Ironclad, Silent, Defect, Regent, Necrobinder}`.
- `CharacterAssetProfile` record (~30 path slots): `Scenes` = `CharacterSceneAssetSet { VisualsPath, EnergyCounterPath, MerchantAnimPath, RestSiteAnimPath }`; `Ui` = `CharacterUiAssetSet { IconTexturePath, IconOutlineTexturePath, IconPath, CharacterSelectBgPath, CharacterSelectIconPath, CharacterSelectLockedIconPath, CharacterSelectTransitionPath, MapMarkerPath }`; `Vfx` = `CharacterVfxAssetSet { TrailPath, CharacterTrailStyle TrailStyle }` (10 nullable trail knobs); `Spine` = `{ CombatSkeletonDataPath }`; `Audio` = `{ CharacterSelectSfx, CharacterTransitionSfx, AttackSfx, CastSfx, DeathSfx }`; `Multiplayer` = `{ ArmPointingTexturePath, ArmRockTexturePath, ArmPaperTexturePath, ArmScissorsTexturePath }`; plus `VisualCues`, `WorldProceduralVisuals`, `VanillaRelic/Potion/CardVisualOverrides[]`.
- Composition helpers: `CharacterAssetProfiles.FromCharacterId(string)`, `Resolve(profile, placeholderId)`, `Merge(a,b)`, `FillMissingFrom(profile, fallback)`, `WithPlaceholder(profile, characterId)`, canned `Ironclad()/Silent()/Defect()/Regent()/Necrobinder()`.
- Full alternative base: `abstract class ModCharacterTemplate<TCardPool,TRelicPool,TPotionPool> : CharacterModel, IModCharacterAssetOverrides, IModCreatureVisualsFactory, IModCharacterCreatureVisualsFactory, IModCreatureAnimatorFactory, IModCharacterCreatureAnimatorFactory, IModCreatureCombatAnimationStateMachineFactory, IModNonSpineAnimationStateMachineFactory, IModCharacterMerchantAnimationStateMachineFactory, IModCharacterRestSiteAnimationStateMachineFactory, ...` with `string PlaceholderCharacterId`, `family CharacterAssetProfile ResolvedAssetProfile`, 24 `Custom*Path` overrides, and `family virtual ModAnimStateMachine SetupCustomCombatAnimationStateMachine/SetupCustomNonSpineAnimationStateMachine/SetupCustomMerchantAnimationStateMachine/SetupCustomRestSiteAnimationStateMachine(Godot.Node, CharacterModel)`.
- Non-Spine world visuals: `ModAnimStateMachines.StandardCue / StandardMerchantCue / StandardRestSiteCue(Godot.Node, CharacterModel, string idle, string? dead, bool deadLoop, string? hit, bool hitLoop, string? attack, bool attackLoop, string? cast, bool castLoop, string? relaxed, bool relaxedLoop, VisualCueSet)`; `ModAnimStateMachines.Standard(MegaSprite, ...)`; `ModCreatureVisualPlayback.TryPlayCue/TryPlayOnVisualRoot/TryPlayFromCreatureAnimatorTrigger`; `ModWorldSceneVisualNodeFactory.TryInstantiateMerchantCharacter(CharacterModel)` / `TryCreateRestSiteCharacter(Player, int)`.
- Caveats: precedence over a BaseLib `CustomCharacterModel`'s own getters is `[INFERENCE]` (from 27 internal `[HarmonyAfter("BaseLib")]` patch classes, list in §1.2); no shipped consumer validates this surface (§4).

### 3.3 The free-play registry (`Cards.FreePlay`)

`static class STS2RitsuLib.Cards.FreePlay.FreePlayBindingRegistry` — XML: *"Provides an extensible registry for determining whether a card play is free."*

```
static void Register(string id, Func<CardPlay,bool> detector)
static bool IsFreeForPlay(CardPlay)
static FreePlayResolution Resolve(CardPlay)
static bool IsCardFreeForUpcomingPlay(CardModel)
static void MarkCardFreeNextPlay(CardModel)
static void MarkCardFreeThisTurn(CardModel)
static void MarkCardFreeThisCombat(CardModel)
static void MarkCurrentPlayFree(CardPlay)
static bool ClearCardFreeThisTurn(CardModel)
static bool ClearCardFreeAfterPlayed(CardModel)
record FreePlayResolution(bool IsAutoPlayNoSpend, bool IsCardBindingFree, bool IsRegisteredDetectorFree) { bool IsFree { get; } }
static void CardModelFreePlayExtensions.SetToFreeForRestOfTurn(CardModel)
```

It observes vanilla via internal `Cards.FreePlay.Patches.CardModelSetToFreeThisTurnBindingPatch` — XML: *"Records game-level `SetToFree` calls in FreePlayBindingRegistry."* Companion pre-play hook: `Cards.ICardOnPlayHookListener.BeforeCardOnPlay(BeforeCardOnPlayContext) -> Task<bool>` and `AfterCardOnPlay(AfterCardOnPlayContext) -> Task`, where `BeforeCardOnPlayContext = { ICombatState CombatState; PlayerChoiceContext ChoiceContext; CardPlay CardPlay }`; registered via `CardOnPlayHook.RegisterGlobalListener(...)` or `RitsuLibFramework.RegisterCardOnPlayHookListener(...)`; returning `false` from Before suppresses the original `OnPlay`. Caveat `[UNVERIFIED]`: ordering versus `GeneratePlayCount` was never traced.

### 3.4 Act-enter forcing and the run/lobby staging surface

Act slot replacement:
```
void ModContentRegistry.RegisterActEnterForce<TAct>(int slotIndex, int priority, Func<ActEnterResolveContext,bool> eligible)
struct ActEnterResolveContext { RunManager RunManager; RunState RunState; int EnteringActIndex; Rng Rng; UnlockState UnlockState; bool IsMultiplayer }
```
XML: *"Registers a rule that replaces slotIndex with TAct when eligible. Higher priority wins, with earlier registration breaking ties."* The predicate can gate on "is this an StS1 dungeon run". Pool variants: `RegisterActEnterUniformPool(int)` + `RegisterActEnterUniformPoolCandidate<TAct>(int, Func<...,bool>)`; `RegisterActEnterWeightedPool(int)` + `RegisterActEnterWeightedPoolCandidate<TAct>(int, Func<...,bool>, Func<...,double>)` + `RegisterActEnterWeightedPoolBaseline(int, Func<...,double>)`; `enum ActEnterPoolModeKind { Uniform, Weighted }`; `static bool ModContentRegistry.HasAnyActEnterRegistration`. All mirrored on `ModContentPackBuilder`.

Acts: `abstract class ModActTemplate : ActModel, IModActAssetOverrides, IModActRandomListPolicy`, `RegisterAct<TAct>()` — XML: *"This does not add it to the vanilla randomized act list; implement IModActRandomListPolicy to opt in"* (`bool AllowInRandomActList`); `record ActAssetProfile(BackgroundScenePath, RestSiteBackgroundPath, MapTopBgPath, MapMidBgPath, MapBotBgPath, ChestSpineResourcePath, BackgroundLayersDirectoryPath)`.

Run/lobby staging (dungeon-choice handoff incl. co-op):
```
class RunSavedDataLobbyScope<T> { T GetOrCreate(StartRunLobby); bool TryGet(StartRunLobby, out T); void Set(...); bool Remove(...); T Modify(StartRunLobby, Action<T>) }
class PlayerRunSavedDataLobbyScope<T> { ... (StartRunLobby, ulong netId | Player) overloads }
static class RunSavedDataLobby { void NotifyStagingChanged(StartRunLobby); bool TryPushContribution(StartRunLobby) }
class RunSavedDataLobbyStagingEvent { StartRunLobby Lobby; bool IsMultiplayer; bool IsHost; RunSavedDataLobbyStagingReason Reason }
```
XML: *"Notifies mods that start-run lobby staging data can be read or changed before it is committed to the run."* Plus `RunSavedDataStore`, `RunSavedData<T>`, `RunSavedDataOptions`, `RunSavedDataWritePolicy`, `RunSavedDataPreparingEvent`.

Lifecycle events (96 structs): `RitsuLibFramework.SubscribeLifecycle<TEvent>(Action<TEvent>, bool)` / `SubscribeLifecycleOnce<TEvent>(...)` / `SubscribeLifecycle(ILifecycleObserver, bool)`; relevant ones `ActEnteringEvent { RunManager, int TargetActIndex, bool DoTransition }`, `ActEnteredEvent { IRunState, int CurrentActIndex }`, `MapGeneratedEvent { IRunState, ActMap Map, int ActIndex }`, `RunStartedEvent`/`RunLoadedEvent { RunState, bool IsMultiplayer, bool IsDaily }`, `RunEndedEvent { SerializableRun Run, bool IsVictory, bool IsAbandoned }`, `MainMenuReadyEvent`, `GameReadyEvent { NGame Game }`, `ContentRegistrationClosedEvent { string Reason }`; `IReplayableFrameworkLifecycleEvent` events replay to late subscribers.

### 3.5 Multiplayer transport

- `RitsuLibManagedNetActions.Register<T>(RitsuLibManagedNetActionDescriptor<T>) -> ulong` / `Request<T>(RunManager, descriptor, T, ulong?)`, `const int MaxPayloadBytes`; descriptor = `{ ModuleId, ActionKey, Func<T,byte[]> Serialize, Func<ReadOnlySpan<byte>,T> Deserialize, Func<RitsuLibManagedNetActionContext<T>,Task> Execute, GameActionType ActionType }`.
- `RitsuNetMessageTailExtensions.RegisterBytes<TMessage>(string, int, Func<TMessage,byte[]>, Action<int,ReadOnlyMemory<byte>>)` + `Write<TMessage>(PacketWriter, TMessage)` / `Read<TMessage>(PacketReader)` — appends mod bytes to a vanilla net message tail.
- `Networking.Sidecar.*` (58 public types, index in §2.2): `RitsuLibSidecarBus.RegisterHandler(ulong, Action<RitsuLibSidecarDispatchContext>)`, `WaitForNextAsync(ulong, TimeSpan, Func<...,bool>, bool, CancellationToken)`, `RitsuLibSidecar.CreateEnvelope/CreateEnvelopeCompressed/CreateEnvelopeWithDelivery`, `RitsuLibSidecarConfigSyncService.RegisterTopic/PublishHostState/TopicChanged`, `RitsuLibSidecarSessionManager.HandshakeCompleted`, `IRitsuLibSidecarCapabilityValidationRoute`, chunked streaming.
- Per-run deterministic RNG: `RitsuLibFramework.GetModRunRng(RunState, string, string)` / `GetModRunRng(Player, ...)` / `GetModPlayerRng(Player, ...)`, backed by `RunRngs.ModRunRngRegistry`, persisted via `ModRunRngState`/`ModRunRngSnapshot`.

### 3.6 Relic icon override

`static class Scaffolding.Content.Patches.ExternalAssetOverrideRegistry`:
```
static void RegisterRelicIconPathProvider(string key, Func<RelicModel,string>)
static void RegisterRelicIconOutlinePathProvider(string key, Func<RelicModel,string>)
static void RegisterRelicIconTextureProvider(string key, Func<RelicModel,Godot.Texture2D>)
static void RegisterRelicIconOutlineTextureProvider(string key, Func<RelicModel,Godot.Texture2D>)
static void RegisterRelicBigIconTextureProvider(string key, Func<RelicModel,Godot.Texture2D>)
static bool Unregister(string key)
static void Clear()
```
Model-agnostic `Func<RelicModel,…>`, so it should cover BaseLib-declared relics and replace the `mod/Spire1Code/Extensions/StringExtensions.cs:49-65` fallback with real per-relic art. Live refresh: `RuntimeAssetRefreshCoordinator.RequestRelicsWhere(Predicate<RelicModel>)` / `Request(RuntimeAssetRefreshScope)`. Same registry has 40 provider methods covering powers, potions, orbs, acts, events, encounters, ancients, afflictions, enchantments and modifiers; sibling registries `ExternalCardMaterialOverrideRegistry` and `ExternalBadgeIconOverrideRegistry`.

### 3.7 Screens and node attachment

- `Scaffolding.Godot.NodeAttachments.ModNodeAttachmentRegistry.RegisterReadyChild<TParent,TNode>(string localId, Func<TParent,TNode> factory, Action<TParent,TNode> setup, NodeAttachmentOptions)` (plus `RegisterReadyChildFromScene`/`FromConvertedScene`); `NodeAttachmentOptions { Name, Order, UniqueNameInOwner, IncludeDerivedParentTypes, DuplicatePolicy, AddMode, AttachParentSelector, SetupTiming, ChildIndex, InsertBeforeName, InsertAfterName, QueueFreeReplacedNode }`; `enum NodeAttachmentAddMode`, `NodeAttachmentDuplicatePolicy`, `NodeAttachmentSetupTiming`.
- `Screens.ModScreenService.Open/Close/Toggle(ICapstoneScreen)`.
- `Ui.Windows.RitsuFloatingWindow`; `TopBar.ModTopBarButtonRegistry`.
- Character select: no purpose-built hook; only asset paths (`CharacterSelectBgPath`, `CharacterSelectIconPath`, `CharacterSelectLockedIconPath`, `CharacterSelectTransitionPath`, `CharacterAssetPathHelper.GetCharacterSelectBackgroundPath/GetCharacterSelectIconPath/GetCharacterSelectLockedIconPath`) plus `IModCharacterVanillaSelectionPolicy { HideFromVanillaCharacterSelect; AllowInVanillaRandomCharacterSelect; HideInCardLibraryCompendium }`.
- Proof the screen is patchable at all: RitsuLib itself ships `Scaffolding.Characters.Patches.NCharacterButtonStripScroller`, a scroller it grafts onto the character button strip.
- `Scaffolding.Godot.RitsuGodotTreeCompat.AddChildSafely/MoveChildSafely` (the two Godot tree-safety helpers the one real skin mod uses, see §4).

## 4. Per-consumer usage (ground truth)

Extracted from each consumer DLL's `TypeRef`/`MemberRef`/`InterfaceImpl` tables filtered to the `STS2-RitsuLib` assembly reference. Workshop IDs as in the batch context.

### 4.1 ShowPlayerHandCards 0.6.3 (`3747606660`)

Refs `STS2-RitsuLib 0.0.60.0`; manifest `min_version 0.2.27`, `affects_gameplay: false` — 31 types, 64 members. **Zero content, zero visuals.** Actually calls:
- `RitsuLibFramework.CreateLogger/CreatePatcher/ApplyRequiredPatcher/BeginModDataRegistration/RegisterModSettings`
- `Data.ModDataStore.For/Register/Get/Modify/Save`
- three `Utils.Persistence.Migration.IMigration` implementations + `ModDataMigrationConfig`
- ~20 members of `Settings.{ModSettingsPageBuilder, ModSettingsSectionBuilder, ModSettingsBindings, ModSettingsUiControlTheming, ModSettingsStructuredData, ModSettingsText}`
- `RuntimeInput.RuntimeHotkeyService.Register` + `RuntimeHotkeyOptions`
- `Utils.I18N`
- two `Patching.Models.IPatchMethod` implementations + `ModPatchTarget`

### 4.2 MultiPlayerPotionView 0.3.3 (`3747606792`)

Refs `STS2-RitsuLib 0.0.48.0`; `min_version 0.2.27` — 27 types, 51 members. A near-identical subset to 4.1, plus `Patching.Models.IModPatches`. Again zero content or visuals.

### 4.3 MultiplayerLimitBreak 0.2.7 (`3747606832`)

Refs `STS2-RitsuLib 0.5.4.0`; `min_version 0.5.4`; `affects_gameplay: true`; 16-player expansion — 30 types, 53 members, and **27 of its own types implement `Patching.Models.IPatchMethod`**. The most informative witness. It uses:
- `Networking.Sidecar.RitsuLibSidecarConfigSyncService.RegisterTopic/PublishHostState/TopicChanged`, `RitsuLibSidecarSessionManager.HandshakeCompleted`, `SidecarConfigTopicChangedEvent.Topic/StateJson`, `SidecarHandshakeCompletedEvent.PeerNetId`
- `RitsuNetMessageTailExtensions.RegisterBytes/Write/Read`
- `DynamicPatchBuilder.ctor/Add/FromMethod/Patches` with `ModPatcher.RegisterDynamicPatches/ApplyDynamicPatches`
- `Utils.HarmonyIl.HarmonyIlRewriter.From/RedirectCalls/ReplaceEach/InstructionsChecked` and `HarmonyIlRewriteReport.RequireExactly/RequireSucceeded`, `HarmonyIl.Call/IsCallTo`
- `Ui.Toast.RitsuToastService.ShowInfo/ShowWarning`
- `Data.ModDataStore`; `Settings.*`

Its patch classes are named after what they hook: `StartENetHostMaxClientsPatch`, `StartSteamHostMaxClientsPatch`, `ClientLobbyJoinRequestMaxPlayersPatch`, `ConnectedToClientAsHostMaxPlayersPatch`, `NetMessageBusDeserializePatch`, `StartRunLobbyConstructorPatch`, `LobbyBeginRunHandlerPatch`, `ScaleMonsterHpPatch`, `ModifyBlockScalingPatch`, `GameplayRelevantModNameListPatch`, `TreasureRoomRelicLayoutPatches.*`, `RestSiteRoomLayoutPatches.ReadyPatch`, `MerchantRoomLayoutPatches.AfterRoomLoadedPatch`. So a gameplay-affecting multiplayer mod treats RitsuLib as (a) a Harmony harness plus IL rewriter and (b) a side-channel network transport — **not** as a content library.

### 4.4 MesugakiRegentSkinFix 0.1.1 (`3783173082`)

Refs `STS2-RitsuLib 0.4.13.0`; deps RitsuLib `0.4.13` + Mesugaki `0.1.2` — the direct answer to the skin question. It uses **only 7 types and 15 members**:
- `RitsuLibFramework.CreateLogger/CreatePatcher/ApplyRequiredPatcher`
- `Patching.Builders.DynamicPatchBuilder.ctor/Add/FromMethod`
- `Patching.Core.ModPatcherExtensions.RegisterPatch/ApplyDynamic`
- `Patching.Models.IPatchMethod` + `ModPatchTarget`
- `Scaffolding.Godot.RitsuGodotTreeCompat.AddChildSafely/MoveChildSafely`

Its two patch classes are `RegentCombatAnimatorPatch` and `NFakeMerchantAfterRoomIsLoadedPatch`. Its manifest describes the work as *"character-select animation timing, Spine animation API compatibility, Regent combat animation fallbacks, and Regent-only Fake Merchant visual replacement"*.

**It touches none of `Scaffolding.Characters`, `CharacterAssetProfile`, `RegisterCharacterAssetReplacement`, `VisualCueSet`, or `ModAnimStateMachine`.** It uses RitsuLib purely as a Harmony wrapper plus two Godot tree-safety helpers and hand-writes the animation fixes itself. So the character-visual API is real, public and well-documented, but there is **no shipped consumer of it** — treat it as unproven in the field. Read plainly: RitsuLib is demonstrably the right tool for *patching* and for *networking*; it is not demonstrably the right tool for character skins.

## 5. Dependency and risk assessment

### 5.1 Distribution layout (`lib/<game-version>/`)

Workshop item `3747602295` contains a root `STS2-RitsuLib.dll` which is **not** the library — it is `STS2-RitsuLib.Loader 1.0.0.0`, 33 792 bytes, one public member `STS2RitsuLib.Loader.Bootstrap.Initialize()`, referencing `System.Runtime.Loader`. Alongside it: `lib/{0.107.1, 0.109.0, 0.110.0, 0.111.0}/` each with `STS2-RitsuLib.dll` (~8.4 MB), `.pdb`, `.xml` (~6 MB), `compat-target.txt`; plus `ritsulib-variants.manifest` and a `viewer/` directory (an offline API-doc web viewer, ~296 KB).

### 5.2 `compat-target.txt` and loader variant validation

`compat-target.txt` contains **exactly the folder's own version string and nothing else** — 8 bytes each: `0.107.1`, `0.109.0`, `0.110.0`, `0.111.0`. It is a marker, not a range. `ritsulib-variants.manifest` is `{"schema":1,"variants":[{compatTarget, directory, assembly, sha256} × 4]}`; the 0.111.0 sha256 is `3e42c7441748b397634c83b8009c9eda15f45c658375e52809f32b0d31cbcf0b`.

**Loader behaviour on a game update** — read from UTF-16 string constants in the loader DLL. It reads the manifest and rejects a variant for: missing `lib` directory, path outside `lib`, mismatched directory, missing or mismatched `compat-target.txt` marker, unexpected assembly name, missing `STS2-RitsuLib.dll`, or **mismatched hash**. It logs `Host version label=… ; picked variant …`. The two fallback paths that matter: `Host numeric version unknown; using newest bundled variant.` and `No bundled variant <= host …; using newest bundled variant as best-effort fallback.` Then it associates the variant with the mod through the host's `AssociateAssemblyWithMod`. **So on a game update past 0.111.0 it keeps running the 0.111.0 build rather than failing — graceful degradation, but zero compatibility guarantee until OLC ships a new variant.**

### 5.3 How a consumer references the package

Per the official docs at `sts2-ritsulib.ritsukage.com/guide/getting-started`: `<PackageReference Include="STS2.RitsuLib" />`, with `STS2.RitsuLib.Compat.<api-version>` only for older API branches. The package is live on nuget.org — 231 versions, current 0.5.13. Our `NuGet.config:5` already pins `globalPackagesFolder` to `G:\omp works\sts2-spire1\.nuget\packages`, so a restore stays off `C:`.

The nupkg ships **only** `lib/net9.0/STS2-RitsuLib.dll` + `.xml` + its own `mod_manifest.json` (`min_game_version: 0.111.0`) — no loader, no other variants. The nupkg DLL and the workshop `lib/0.111.0` DLL are **not** byte-identical (sha256 `5be797e3ee68bbd261228ad617cc67fd6eba5ed23e5e94674888938399465dcc` vs `3e42c744…`, both exactly 8 387 584 bytes) but expose an **identical public API** — a diff of the two full signature dumps is empty once unresolved TypeSpec row indexes are normalised, same assembly identity `STS2-RitsuLib 0.5.13.0`. The correct arrangement is: compile against the package, let the workshop loader supply the runtime copy. Note that `buildTransitive/STS2.RitsuLib.targets` defines `CopyRitsuLibPackageModFiles` (`AfterTargets="Build"`) which copies dll/pdb/xml/manifest/viewer into `$(RitsuLibDeployDir)`; `RitsuLibAutoCopy` defaults to `true` but the target no-ops unless `RitsuLibDeployDir` is set — worth knowing so it does not surprise the central build.

### 5.4 Declared-dependency form in mod manifests

All four dependents use `{"id": "STS2-RitsuLib", "min_version": "x.y.z"}`. RitsuLib's own manifest: `id STS2-RitsuLib`, `pck_name STS2-RitsuLib`, `author OLC`, `version 0.5.13`, `has_pck: false`, `has_dll: true`, `affects_gameplay: false`, `dependencies: []`, `min_game_version: 0.107.1`.

### 5.5 Licence, strong-naming, Harmony coexistence

**Licence — MIT, confirmed.** `STS2.RitsuLib.nuspec` carries `<license type="expression">MIT</license>`, `licenseUrl https://licenses.nuget.org/MIT`, `projectUrl` and `repository` `https://github.com/BAKAOLC/STS2-RitsuLib` (commit `6a1d7db2989ded7471071f62218d35a0f54bea3b`), author `OLC`. Caveat: there is **no licence file anywhere in the shipped workshop artifacts and the `LegalCopyright` field in the DLL's version resource is empty** — the MIT statement lives only in NuGet metadata. MIT is fine for using it as a library, and since we would call an API rather than copy content, it does not touch the vanilla-fidelity rule.

**Strong-naming:** not strong-named (CLI header flags `0x1`, no strong-name signature) → version-agnostic simple-name binding.

**Harmony coexistence with BaseLib.** RitsuLib clearly anticipates running alongside BaseLib: the 27 `[HarmonyAfter("BaseLib")]` patch classes (full list in §1.2), `Combat.HandSize.BaseLibMaxHandSizeBridge` (*"Bridges BaseLib's maximum-hand-size support by detecting its active patches, extending its calculator with RitsuLib modifiers, and using its result as the base value when available"*), and `Models.Capabilities.ICardTypeTextModifier` (*"The method signature and composition contract match BaseLib's `ICardTypeTextModifier`"*) — genuinely reassuring, but it is still two patch layers on one build.

### 5.6 Update-check and telemetry endpoints, consent model

- Update check: `https://sts2-ritsulib.ritsukage.com/ritsulib-update.json` (embedded in the DLL).
- Telemetry: `https://ritsulib-telemetry.ritsukage.com/v1/ingest` (PostHog-style).
- Telemetry is opt-in by contract: `enum TelemetryConsentState { Unknown, Denied, Granted }` with XML *"The user has not made a decision; telemetry is not sent"* for `Unknown`, and `ITelemetryClient` *"Capture calls do nothing when the matching request is not authorized"*, backed by an in-game consent prompt. Not runtime-verified that nothing is sent before consent — `[UNVERIFIED]`. Either way, adopting it means our users see a third party's consent prompt and update-check traffic.

### 5.7 Coupling risks, worst first

1. **It is a framework, not a utility.** 1 325 public types, and `RitsuLibFramework.Initialize()` *"registers and applies required patches"* at startup. Adopting it means two frameworks patching the same model accessors on one build (mitigations in §5.5).
2. **Network endpoints baked in** (§5.6).
3. **Pre-1.0 cadence.** 231 published versions, currently 0.5.x. A `min_version` bump in our manifest is a hard, user-visible requirement.
4. **Bootstrap intrusion.** The documented init requires `ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly)` (needed for CLR-attribute registration), and `RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, logger)` if we have C# scripts attached to `.tscn` scenes. If we want it optional we would guard with `Compat.RitsuModManager.IsModLoaded("STS2-RitsuLib")` / `WillModLoad` / `TryGetModInfo`.
5. Incidental: `mod/Spire1.json:11` currently declares `{"id": "BaseLib", "min_version": "3.4.5"}` while 3.3.5 is what is installed. Flagged by the scout; not its ticket.

## 6. Recommendation

With BaseLib already providing `CustomMonsterModel`, `CustomEncounterModel`, `CustomActModel`, `CustomCharacterModel`, `CustomOrbModel`, `CustomPetModel`, and `PlaceholderCharacterModel` reusing shipped `res://scenes/creature_visuals/*.tscn`:

- **RitsuLib is not needed for M2.** BaseLib is sufficient for the reuse-shipped-scenes plan. RitsuLib's monster value is confined to the case where we author our own StS1 PNG art and want frame animation without `.tscn`/`SpriteFrames`.
- **Three surgical, high-confidence wins justify adoption if we want them:** `Cards.FreePlay.FreePlayBindingRegistry` for `Necronomicon`; `ExternalAssetOverrideRegistry.RegisterRelicIcon*Provider` for relic art; `ModContentRegistry.RegisterActEnterForce<TAct>` for the M3 act sequence. The scout did not exhaustively audit BaseLib's `CustomActModel`, so "nothing in BaseLib does act-slot replacement with a run-state predicate" is `[UNVERIFIED]` — worth a five-minute check before deciding.
- **`N'loth's Gift`: flat NO.** No rarity or odds API exists anywhere in RitsuLib.
- **Character visuals: rich API, no proven consumer**, and the one RitsuLib-dependent skin mod ignores all of it. If we go there, budget a smoke test of `RegisterCharacterAssetReplacement` against a BaseLib-declared character before building on it.

Adopt/skip per use case:
| Use case | Decision |
|---|---|
| `Necronomicon` `freeToPlayOnce` | ADOPT — `Cards.FreePlay.FreePlayBindingRegistry` (cleanest win) |
| Per-relic icons | ADOPT — `ExternalAssetOverrideRegistry.RegisterRelicIcon*Provider` + `RuntimeAssetRefreshCoordinator.RequestRelicsWhere` |
| M3 act-slot replacement | ADOPT — `ModContentRegistry.RegisterActEnterForce<TAct>` |
| M2 monsters | SKIP — BaseLib sufficient; RitsuLib adds nothing for the shipped-scenes plan |
| Character skins | UNPROVEN — do not build on it without a smoke test |
| `MutagenicStrength` temp-Strength icon | SKIP — BaseLib `CustomTemporaryPowerModel` already covers it |
| Face relics / `Madness` | SKIP — RitsuLib irrelevant; blocker is jar data extraction |

## 7. Tooling inventory (`G:/omp works/sts2-spire1/.tmp/ritsu/`)

Existence confirmed on disk (sizes from `ls -la`, 29 files total in the directory); `sec-all.txt` = 16 339 lines exactly, as the scout reported. The named files, what each is for:

- **`mdparse.mjs`** (16.7K) — self-contained ECMA-335 CLI-metadata parser: PE headers → CLI header → metadata root → `#~`/`#Strings`/`#Blob` streams → `TypeDef`, `MethodDef`, `Field`, `Property`, `PropertyMap`, `Event`, `EventMap`, `MethodSemantics`, `NestedClass`, `GenericParam`, `InterfaceImpl`, `AssemblyRef`, `CustomAttribute`, with full blob signature decoding. Run: `node mdparse.mjs <dll> <out.json>`.
- **`api-0.111.0.json`** (15.1M) — extracted API model of `lib/0.111.0/STS2-RitsuLib.dll` (workshop copy). Pair with **`api-nupkg.json`** (15.1M) — same extraction of the NuGet package DLL, used for the byte-different/API-identical comparison.
- **`sec-all.txt`** (979.9K, 16 339 lines) — C#-like render of the entire public surface of the workshop build (via `dump.mjs`, 2.9K). **`sec-nupkg.txt`** (979.9K) is the same render for the NuGet build; `a.txt`/`b.txt`/`a2.txt`/`b2.txt` are the normalised diffs (empty result → identical public API).
- **`nsindex.md`** (52.6K, 182 lines) — namespace → type index (the exact dump reproduced in §2.2); **`ns.txt`** (9.1K) is a plainer namespace listing.
- **`refs.mjs`** (12.6K) — consumer extraction: `TypeRef`/`MemberRef`/`InterfaceImpl` tables filtered to the `STS2-RitsuLib` assembly reference (produced §4's per-mod call lists).
- **`hpatch.mjs`** (10.9K) — `CustomAttribute` blob decoding; **`allpatches.txt`** (5.0K, 117 lines) — the decoded `[HarmonyAfter("BaseLib")]` / `[HarmonyPriority(0)]` patch-class list (§1.2 evidence).
- Supporting files also on disk: `xdoc.mjs` (1.2K, XML-doc extractor), `events.txt` (18.3K, lifecycle events), `sec-content.txt` (91.3K) / `sec-content2.txt` (85.8K) / `sec-cards.txt` (28.2K) / `sec-patching.txt` (44.7K) / `sec-registry.txt` (17.5K) / `sec-rewards.txt` (8.2K) / `sec-visuals.txt` (57.4K) — per-area dumps from `dump.mjs`; `loader.json` (42.0K, loader-DLL strings), `sn.mjs` (885B, strong-naming probe), `pkg.zip` (3.7M, the downloaded NuGet package), `x_STS2-RitsuLib.dll` (8.0M) + `x_mod_manifest.json` (414B) (unpacked nupkg contents).

Nothing else in `.tmp/` was modified. This document is the complete transcription of the scout's findings; where the transcript was silent on a point, the section above states `not established by the transcript` — none of the seven required sections required that fallback.
