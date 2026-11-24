using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// Single canonical continuation-IL matcher, emitter and observer for the four admitted
/// owners. Host bodies are pinned by exact signature, state machine type, MoveNext token
/// and raw original IL length/SHA before any edit is allowed. Owner guard methods are
/// resolved by exact full name from this module only when that owner is requested,
/// already present in the admitted stream, or expected by Observe; a narrow build that
/// contains one owner helper never needs the others. The type owns IL structure only;
/// business predicates and leases stay in the owner helpers.
/// </summary>
internal static class Spire1ContinuationIl
{
    internal enum Owner
    {
        Reboot,
        Ftl,
        Exhaust,
        Echo
    }

    internal enum BlockKind
    {
        RebootInitial,
        RebootPost,
        RebootDrawPost,
        RebootCommit,
        FtlDrawPost,
        FtlDrawStop,
        FtlAttackStop,
        FtlAttackIdentity,
        FtlLoopOr,
        ExhaustGuardAttack,
        ExhaustGuardDamage,
        ExhaustGuardReward,
        ExhaustGuardExhaust,
        ExhaustGuardDispatch,
        ExhaustGuardNative,
        Bridge,
        DrawAddComposed,
        EchoBridge
    }

    internal enum BridgeForm
    {
        Direct,
        Loader,
        Loader2,
        ComposedDrawAdd
    }

    internal enum BridgeMethodId
    {
        ExhaustAdd,
        ExhaustDrawShuffle,
        ExhaustAfterCardDrawn,
        ExhaustShuffleAdd,
        ExhaustBeforeDamageReceived,
        ExhaustAfterDamageGiven,
        ExhaustAfterDamageReceived,
        ExhaustDamageKill,
        ExhaustKillOne,
        ExhaustKillCreature,
        ExhaustShuffleNow,
        ExhaustDrawAdd,
        ExhaustDispatch,
        ExhaustInvokeListener,
        ExhaustDamageFromAttack,
        FtlAddDrawn,
        FtlInvokeDrawn,
        FtlAfterCardPlayed,
        FtlShouldDraw,
        FtlDrawFromUse,
        EchoModifyOne
    }

    private enum HostBridgeId
    {
        CardPileAdd5,
        CardPileAddPileType5,
        CardPileDraw4,
        HookShouldDraw,
        ShuffleIfNecessary,
        HookAfterCardDrawn,
        CardModelInvokeDrawn,
        CardPileAdd6,
        HookBeforeDamageReceived,
        HookAfterDamageGiven,
        HookAfterDamageReceived,
        CreatureKill3,
        KillWithout4,
        CreatureKill2,
        CardPileShuffle2,
        HookAfterCardPlayed,
        HookAfterCardExhausted4,
        AbstractModelListener3,
        CreatureDamage8,
        AbstractModelCount3
    }

    internal sealed class Body
    {
        internal int Id { get; }
        internal MethodInfo Entry { get; }
        internal MethodInfo Move { get; }
        internal FieldInfo? Selector { get; }

        internal readonly Spec Spec;
        internal readonly List<CodeInstruction> Original;
        internal FieldInfo? StateField;
        internal FieldInfo? ContextField;
        internal FieldInfo? ModelField;
        internal FieldInfo? ThisField;
        internal FieldInfo? GuardField;
        internal int InitialAnchor;
        internal readonly List<int> Awaits;
        internal readonly List<bool> AwaitPlain;
        internal readonly List<int> PlainAwaits;
        internal readonly Dictionary<int, PostSlot> PostSlots;
        internal readonly Dictionary<int, CommitSite> Commits;
        internal readonly List<BridgeSite> BridgeSites;
        internal readonly Dictionary<int, BridgeSite> BridgeByIndex;
        internal int AttackDeadSite;
        internal int AttackCleanup;
        internal int DrawStopAnchor;
        internal int DrawResultAnchor;

        internal Body(Spec spec, MethodInfo entry, MethodInfo move, FieldInfo? selector, List<CodeInstruction> original)
        {
            Id = spec.Id;
            Entry = entry;
            Move = move;
            Selector = selector;
            Spec = spec;
            Original = original;
            InitialAnchor = -1;
            Awaits = new List<int>();
            AwaitPlain = new List<bool>();
            PlainAwaits = new List<int>();
            PostSlots = new Dictionary<int, PostSlot>();
            Commits = new Dictionary<int, CommitSite>();
            BridgeSites = new List<BridgeSite>();
            BridgeByIndex = new Dictionary<int, BridgeSite>();
            AttackDeadSite = -1;
            AttackCleanup = -1;
            DrawStopAnchor = -1;
            DrawResultAnchor = -1;
        }

        internal bool Has(Owner owner) => Spec.Has(owner);
    }

    internal sealed class Observation
    {
        internal int BodyId { get; }
        internal MethodInfo Target { get; }
        internal IReadOnlyList<CodeInstruction> Effective { get; }
        internal IReadOnlyList<AwaitObservation> Awaits { get; }
        internal IReadOnlyList<EditObservation> Edits { get; }
        internal IReadOnlyList<int> OriginalToEffective { get; }

        internal Observation(int bodyId, MethodInfo target, IReadOnlyList<CodeInstruction> effective,
            IReadOnlyList<AwaitObservation> awaits, IReadOnlyList<EditObservation> edits,
            IReadOnlyList<int> originalToEffective)
        {
            BodyId = bodyId;
            Target = target;
            Effective = effective;
            Awaits = awaits;
            Edits = edits;
            OriginalToEffective = originalToEffective;
        }
    }

    internal sealed class AwaitObservation
    {
        internal int Ordinal { get; }
        internal int OriginalIndex { get; }
        internal int EffectiveIndex { get; }
        internal MethodInfo GetResult { get; }
        internal IReadOnlyList<Owner> GuardOwners { get; }

        internal AwaitObservation(int ordinal, int originalIndex, int effectiveIndex, MethodInfo getResult,
            IReadOnlyList<Owner> guardOwners)
        {
            Ordinal = ordinal;
            OriginalIndex = originalIndex;
            EffectiveIndex = effectiveIndex;
            GetResult = getResult;
            GuardOwners = guardOwners;
        }
    }

    internal sealed class EditObservation
    {
        internal Owner Owner { get; }
        internal string Kind { get; }
        internal int OriginalIndex { get; }
        internal int EffectiveStart { get; }
        internal int InstructionCount { get; }

        internal EditObservation(Owner owner, string kind, int originalIndex, int effectiveStart, int instructionCount)
        {
            Owner = owner;
            Kind = kind;
            OriginalIndex = originalIndex;
            EffectiveStart = effectiveStart;
            InstructionCount = instructionCount;
        }
    }

    internal sealed class Spec
    {
        internal int Id;
        internal Type Type = null!;
        internal string Name = "";
        internal Type[] Parameters = Array.Empty<Type>();
        internal Type Return = typeof(void);
        internal bool Static = true;
        internal string? State;
        internal int MoveToken;
        internal int IlBytes;
        internal string Sha = "";
        internal int EntryToken;
        internal int EntryIlBytes;
        internal string? EntrySha;
        internal string? SelectorName;
        internal Type? SelectorType;
        internal string? ContextName;
        internal Type? ContextType;
        internal string? ModelName;
        internal Type? ModelType;
        internal string? ThisName;
        internal Type? ThisType;
        internal string? GuardFieldName;
        internal Type? GuardFieldType;
        internal Owner[] Emitters = Array.Empty<Owner>();
        internal AwaitPin? AwaitExpectation;
        internal CommitPin[] CommitPins = Array.Empty<CommitPin>();

        internal bool Has(Owner owner) => Array.IndexOf(Emitters, owner) >= 0;
    }

    internal sealed class AwaitPin
    {
        internal readonly int Total;
        internal readonly int Plain;
        internal readonly int Generic;
        internal readonly Type? GenericType;

        internal AwaitPin(int total, int plain, int generic, Type? genericType)
        {
            Total = total;
            Plain = plain;
            Generic = generic;
            GenericType = genericType;
        }
    }

    internal sealed class CommitPin
    {
        internal readonly Type DeclaringType;
        internal readonly string Name;
        internal readonly Type[] Parameters;
        internal readonly bool StartsCardMove;

        internal CommitPin(Type declaringType, string name, Type[] parameters, bool startsCardMove)
        {
            DeclaringType = declaringType;
            Name = name;
            Parameters = parameters;
            StartsCardMove = startsCardMove;
        }
    }

    internal sealed class PostSlot
    {
        internal int OriginalIndex;
        internal bool IsAwait;
        internal BlockKind[] Allowed = Array.Empty<BlockKind>();
        internal BlockKind? RequiredFtl;
    }

    internal sealed class CommitSite
    {
        internal int OriginalIndex;
        internal bool StartsCardMove;
    }

    internal sealed class BridgeSite
    {
        internal int OriginalIndex;
        internal int RawOffset;
        internal Owner Owner;
        internal BridgeForm Form;
        internal BridgeMethodId Method;
        internal MethodInfo Original = null!;
        internal OpCode ReplacementOpcode;
        internal FieldInfo? LoaderField;
        internal FieldInfo? LoaderField2;
        internal string Kind = "";
    }

    internal sealed class BlockFact
    {
        internal Owner Owner;
        internal BlockKind Kind;
        internal int OriginalIndex;
        internal int Start;
        internal int Length;
        internal int Matched;
        internal int BranchIndex = -1;
    }

    internal sealed class SlotFact
    {
        internal PostSlot Slot = null!;
        internal int Start;
        internal int End;
        internal readonly List<BlockFact> Blocks = new();
    }

    internal sealed class BridgeFact
    {
        internal BridgeSite Site = null!;
        internal BridgeForm Form;
        internal int Start;
        internal int Length;
        internal int Matched;
        internal bool ComposedIsFtl;
    }

    internal sealed class ParseOutcome
    {
        internal readonly int[] OriginalToEffective;
        internal readonly List<BlockFact> Blocks = new();
        internal readonly List<SlotFact> Slots = new();
        internal readonly List<BridgeFact> Bridges = new();
        internal bool FtlPresent;
        internal bool ExhaustPresent;
        internal bool ComposedIsFtl;

        internal ParseOutcome(int originalCount)
        {
            OriginalToEffective = new int[originalCount];
            Array.Fill(OriginalToEffective, -1);
        }
    }

    private sealed class Pinned
    {
        internal readonly Spec Spec;
        internal MethodInfo? Entry;
        internal MethodInfo? Move;
        internal Body? Body;
        internal string? Error;

        internal Pinned(Spec spec)
        {
            Spec = spec;
        }
    }

    private sealed class RebootBinding
    {
        internal readonly MethodInfo CheckInitial;
        internal readonly MethodInfo CheckContinuation;
        internal readonly MethodInfo CheckDrawContinuation;
        internal readonly MethodInfo BeforeCommit;

        internal RebootBinding(MethodInfo checkInitial, MethodInfo checkContinuation, MethodInfo checkDraw,
            MethodInfo beforeCommit)
        {
            CheckInitial = checkInitial;
            CheckContinuation = checkContinuation;
            CheckDrawContinuation = checkDraw;
            BeforeCommit = beforeCommit;
        }
    }

    private sealed class FtlBinding
    {
        internal readonly MethodInfo CheckDrawContinuation;
        internal readonly MethodInfo ShouldStopAttackLoop;
        internal readonly MethodInfo CheckAttackIdentity;
        internal readonly MethodInfo AddDrawn;
        internal readonly MethodInfo InvokeDrawn;
        internal readonly MethodInfo AfterCardPlayed;
        internal readonly MethodInfo ShouldDraw;
        internal readonly MethodInfo ShouldStopDrawLoop;
        internal readonly MethodInfo DrawFromUse;

        internal FtlBinding(MethodInfo checkDraw, MethodInfo shouldStop, MethodInfo identity, MethodInfo addDrawn,
            MethodInfo invokeDrawn, MethodInfo afterCardPlayed, MethodInfo shouldDraw, MethodInfo shouldStopDraw,
            MethodInfo drawFromUse)
        {
            CheckDrawContinuation = checkDraw;
            ShouldStopAttackLoop = shouldStop;
            CheckAttackIdentity = identity;
            AddDrawn = addDrawn;
            InvokeDrawn = invokeDrawn;
            AfterCardPlayed = afterCardPlayed;
            ShouldDraw = shouldDraw;
            ShouldStopDrawLoop = shouldStopDraw;
            DrawFromUse = drawFromUse;
        }
    }

    private sealed class ExhaustBinding
    {
        internal readonly MethodInfo GuardAttack;
        internal readonly MethodInfo GuardDamage;
        internal readonly MethodInfo GuardReward;
        internal readonly MethodInfo GuardExhaust;
        internal readonly MethodInfo GuardDispatch;
        internal readonly MethodInfo GuardNative;
        internal readonly MethodInfo DamageFromAttack;
        internal readonly MethodInfo DispatchExhaust;
        internal readonly MethodInfo InvokeExhaustListener;
        internal readonly MethodInfo ExhaustAdd;
        internal readonly MethodInfo DrawShuffle;
        internal readonly MethodInfo AfterCardDrawn;
        internal readonly MethodInfo ShuffleAdd;
        internal readonly MethodInfo BeforeDamageReceived;
        internal readonly MethodInfo AfterDamageGiven;
        internal readonly MethodInfo AfterDamageReceived;
        internal readonly MethodInfo DamageKill;
        internal readonly MethodInfo KillOne;
        internal readonly MethodInfo KillCreature;
        internal readonly MethodInfo ShuffleNow;
        internal readonly MethodInfo DrawAdd;

        internal ExhaustBinding(MethodInfo guardAttack, MethodInfo guardDamage, MethodInfo guardReward,
            MethodInfo guardExhaust, MethodInfo guardDispatch, MethodInfo guardNative, MethodInfo damageFromAttack,
            MethodInfo dispatchExhaust, MethodInfo invokeExhaustListener, MethodInfo exhaustAdd, MethodInfo drawShuffle,
            MethodInfo afterCardDrawn, MethodInfo shuffleAdd, MethodInfo beforeDamageReceived, MethodInfo afterDamageGiven,
            MethodInfo afterDamageReceived, MethodInfo damageKill, MethodInfo killOne, MethodInfo killCreature,
            MethodInfo shuffleNow, MethodInfo drawAdd)
        {
            GuardAttack = guardAttack;
            GuardDamage = guardDamage;
            GuardReward = guardReward;
            GuardExhaust = guardExhaust;
            GuardDispatch = guardDispatch;
            GuardNative = guardNative;
            DamageFromAttack = damageFromAttack;
            DispatchExhaust = dispatchExhaust;
            InvokeExhaustListener = invokeExhaustListener;
            ExhaustAdd = exhaustAdd;
            DrawShuffle = drawShuffle;
            AfterCardDrawn = afterCardDrawn;
            ShuffleAdd = shuffleAdd;
            BeforeDamageReceived = beforeDamageReceived;
            AfterDamageGiven = afterDamageGiven;
            AfterDamageReceived = afterDamageReceived;
            DamageKill = damageKill;
            KillOne = killOne;
            KillCreature = killCreature;
            ShuffleNow = shuffleNow;
            DrawAdd = drawAdd;
        }

        internal MethodInfo Resolve(BridgeMethodId id) => id switch
        {
            BridgeMethodId.ExhaustAdd => ExhaustAdd,
            BridgeMethodId.ExhaustDrawShuffle => DrawShuffle,
            BridgeMethodId.ExhaustAfterCardDrawn => AfterCardDrawn,
            BridgeMethodId.ExhaustShuffleAdd => ShuffleAdd,
            BridgeMethodId.ExhaustBeforeDamageReceived => BeforeDamageReceived,
            BridgeMethodId.ExhaustAfterDamageGiven => AfterDamageGiven,
            BridgeMethodId.ExhaustAfterDamageReceived => AfterDamageReceived,
            BridgeMethodId.ExhaustDamageKill => DamageKill,
            BridgeMethodId.ExhaustKillOne => KillOne,
            BridgeMethodId.ExhaustKillCreature => KillCreature,
            BridgeMethodId.ExhaustShuffleNow => ShuffleNow,
            BridgeMethodId.ExhaustDrawAdd => DrawAdd,
            BridgeMethodId.ExhaustDispatch => DispatchExhaust,
            BridgeMethodId.ExhaustInvokeListener => InvokeExhaustListener,
            BridgeMethodId.ExhaustDamageFromAttack => DamageFromAttack,
            _ => throw new InvalidOperationException("ContinuationIL: method is not an Exhaust bridge")
        };
    }

    private sealed class EchoBinding
    {
        internal readonly MethodInfo ModifyOne;

        internal EchoBinding(MethodInfo modifyOne)
        {
            ModifyOne = modifyOne;
        }
    }

    private sealed class Bindings
    {
        private readonly Dictionary<Owner, object> _bindings = new();

        internal void Set(Owner owner, object binding) => _bindings[owner] = binding;
        internal bool Has(Owner owner) => _bindings.ContainsKey(owner);

        internal RebootBinding? Reboot => _bindings.TryGetValue(Owner.Reboot, out object? binding)
            ? (RebootBinding)binding
            : null;

        internal FtlBinding? Ftl => _bindings.TryGetValue(Owner.Ftl, out object? binding)
            ? (FtlBinding)binding
            : null;

        internal ExhaustBinding? Exhaust => _bindings.TryGetValue(Owner.Exhaust, out object? binding)
            ? (ExhaustBinding)binding
            : null;

        internal EchoBinding? Echo => _bindings.TryGetValue(Owner.Echo, out object? binding)
            ? (EchoBinding)binding
            : null;
    }

    private const string RebootTypeName = "Spire1.Spire1Code.Patches.Spire1RebootTransaction";
    private const string FtlTypeName = "Spire1.Spire1Code.Patches.Spire1FtlPlayScope";
    private const string ExhaustTypeName = "Spire1.Spire1Code.Patches.Spire1ExhaustTransaction";
    private const string ExhaustDispatchTypeName = "Spire1.Spire1Code.Patches.Spire1ExhaustTransaction+ExhaustDispatch";
    private const string EchoTypeName = "Spire1.Spire1Code.Patches.Spire1EchoReplay";

    private static readonly Guid HostMvid = Guid.Parse("9d417b39-1d40-4fc7-b993-28acdc29e151");
    private static readonly Guid BaseLibMvid = Guid.Parse("a92e73f7-2563-4953-a000-d3cae93f53c3");
    private static readonly BindingFlags DeclaredAll = BindingFlags.DeclaredOnly | BindingFlags.Instance |
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly object Gate = new();

    private static Pinned[]? _pinned;

    internal static Body ForKickoff(MethodInfo entry)
    {
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));
        Pinned[] pinned = EnsurePinned();
        foreach (Pinned candidate in pinned)
        {
            Spec spec = candidate.Spec;
            if (entry.DeclaringType != spec.Type || entry.Name != spec.Name ||
                !ParameterTypesEqual(entry, spec.Parameters))
                continue;
            if (candidate.Entry == null)
                throw SpecError(spec, "host identity pin failed: " + candidate.Error);
            if (!candidate.Entry.Equals(entry))
                throw SpecError(spec, "kickoff identity changed for the pinned signature");
            return GetBody(candidate);
        }
        throw new InvalidOperationException(
            "ContinuationIL: unknown kickoff " + entry.DeclaringType?.FullName + "." + entry.Name +
            " on module " + entry.Module.ModuleVersionId + "; no exact pinned body");
    }

    internal static Body ForMove(MethodBase move)
    {
        if (move == null)
            throw new ArgumentNullException(nameof(move));
        Pinned[] pinned = EnsurePinned();
        foreach (Pinned candidate in pinned)
            if (candidate.Move != null && candidate.Move.Equals(move))
                return GetBody(candidate);
        foreach (Pinned candidate in pinned)
            if (candidate.Entry != null && candidate.Entry.Equals(move))
                throw new InvalidOperationException(
                    "ContinuationIL body " + candidate.Spec.Id +
                    ": observe/rewrite the generated MoveNext target, not the kickoff " +
                    move.DeclaringType?.FullName + "." + move.Name);
        foreach (Pinned candidate in pinned)
        {
            Spec spec = candidate.Spec;
            bool structural = spec.State == null
                ? move.DeclaringType == spec.Type && move.Name == spec.Name
                : move.DeclaringType?.DeclaringType == spec.Type && move.DeclaringType.FullName == spec.State;
            if (!structural)
                continue;
            if (candidate.Move == null)
                throw SpecError(spec, "host identity pin failed: " + candidate.Error);
            if (!candidate.Move.Equals(move))
                throw SpecError(spec, "continuation target identity changed");
            return GetBody(candidate);
        }
        throw new InvalidOperationException(
            "ContinuationIL: unknown continuation target " + move.DeclaringType?.FullName + "." + move.Name +
            " on module " + move.Module.ModuleVersionId + "; no exact pinned body");
    }

    internal static void ValidateIncoming(Owner requestingOwner, MethodBase target,
        IReadOnlyList<CodeInstruction> incoming)
    {
        if (incoming == null)
            throw new ArgumentNullException(nameof(incoming));
        Body body = ForMove(target);
        if (!body.Has(requestingOwner))
            throw Error(body, -1, requestingOwner, "owner/body combination is not approved");
        Bindings bindings = ForParse(body, incoming, requestingOwner, null);
        Parse(body, bindings, incoming, requestingOwner, null);
    }

    internal static IReadOnlyList<CodeInstruction> Rewrite(Owner owner, MethodBase target,
        IReadOnlyList<CodeInstruction> incoming)
    {
        if (incoming == null)
            throw new ArgumentNullException(nameof(incoming));
        Body body = ForMove(target);
        if (!body.Has(owner))
            throw Error(body, -1, owner, "owner/body combination is not approved");
        Bindings bindings = ForParse(body, incoming, owner, null);
        ParseOutcome outcome = Parse(body, bindings, incoming, owner, null);

        var before = new Dictionary<int, List<CodeInstruction>>();
        var after = new Dictionary<int, List<CodeInstruction>>();
        var replace = new Dictionary<int, List<CodeInstruction>>();
        var moveOpener = new HashSet<int>();
        var moveLabels = new HashSet<int>();

        foreach ((BlockKind kind, int anchor) in Required(body, owner))
        {
            switch (kind)
            {
                case BlockKind.RebootInitial:
                {
                    int at = outcome.OriginalToEffective[anchor];
                    AddAction(before, at, EmitInitial(body, bindings), body, owner);
                    moveOpener.Add(at);
                    break;
                }
                case BlockKind.RebootCommit:
                {
                    int at = outcome.OriginalToEffective[anchor];
                    AddAction(before, at, EmitCommit(body, bindings, body.Commits[anchor].StartsCardMove), body, owner);
                    moveLabels.Add(at);
                    break;
                }
                case BlockKind.FtlLoopOr:
                {
                    SlotFact slot = FindSlot(outcome, anchor);
                    AddAction(after, slot.End - 1, EmitLoopOr(body, bindings), body, owner);
                    break;
                }
                case BlockKind.DrawAddComposed:
                {
                    if (owner == Owner.Exhaust && outcome.FtlPresent)
                        break;
                    BridgeSite site = body.BridgeByIndex[anchor];
                    int at = outcome.OriginalToEffective[anchor];
                    AddAction(replace, at, EmitComposedBridge(body, bindings, site, owner), body, owner);
                    break;
                }
                case BlockKind.Bridge:
                {
                    BridgeSite site = body.BridgeByIndex[anchor];
                    int at = outcome.OriginalToEffective[anchor];
                    AddAction(replace, at, EmitBridgeSite(body, bindings, site), body, owner);
                    break;
                }
                default:
                {
                    SlotFact slot = FindSlot(outcome, anchor);
                    int at = slot.End - 1;
                    if (owner != Owner.Ftl)
                    {
                        foreach (BlockFact existing in slot.Blocks)
                        {
                            if (existing.Kind == BlockKind.FtlDrawStop ||
                                existing.Kind == BlockKind.FtlAttackStop)
                            {
                                at = existing.Start - 1;
                                break;
                            }
                        }
                    }
                    Label? cleanup = kind == BlockKind.FtlAttackStop
                        ? CleanupLabel(body, incoming, outcome)
                        : kind == BlockKind.FtlDrawStop
                            ? DrawCleanupLabel(body, incoming, outcome)
                            : null;
                    AddAction(after, at, EmitBlock(body, bindings, kind, cleanup), body, owner);
                    break;
                }
            }
        }

        var result = new List<CodeInstruction>(incoming.Count + 16);
        for (int i = 0; i < incoming.Count; i++)
        {
            var copy = new CodeInstruction(incoming[i]);
            if (before.TryGetValue(i, out List<CodeInstruction>? pre))
            {
                if (moveOpener.Contains(i))
                {
                    ExceptionBlock? opener = null;
                    for (int k = 0; k < copy.blocks.Count; k++)
                    {
                        ExceptionBlock candidate = copy.blocks[k];
                        if (candidate.blockType == ExceptionBlockType.BeginExceptionBlock && candidate.catchType == typeof(object))
                        {
                            opener = candidate;
                            copy.blocks.RemoveAt(k);
                            break;
                        }
                    }
                    if (opener == null)
                        throw Error(body, i, owner, "builder try opener is missing from the incoming anchor");
                    pre[0].blocks.Add(opener);
                }
                if (moveLabels.Contains(i))
                {
                    pre[0].labels.AddRange(copy.labels);
                    copy.labels.Clear();
                }
                result.AddRange(pre);
            }
            if (replace.TryGetValue(i, out List<CodeInstruction>? replacement))
            {
                if (replacement.Count == 1)
                {
                    replacement[0].labels.AddRange(copy.labels);
                    replacement[0].blocks.AddRange(copy.blocks);
                }
                else if (copy.labels.Count != 0 || copy.blocks.Count != 0)
                {
                    throw Error(body, i, owner,
                        "named multi-instruction bridge target unexpectedly carries labels/exception blocks");
                }
                result.AddRange(replacement);
            }
            else
            {
                result.Add(copy);
            }
            if (after.TryGetValue(i, out List<CodeInstruction>? post))
                result.AddRange(post);
        }
        return result;
    }

    internal static Observation Observe(MethodBase target, IReadOnlyCollection<Owner> expectedOwners)
    {
        if (expectedOwners == null)
            throw new ArgumentNullException(nameof(expectedOwners));
        Body body = ForMove(target);
        List<CodeInstruction> effective;
        try
        {
            effective = PatchProcessor.GetCurrentInstructions(target, int.MaxValue, null!);
        }
        catch (Exception error)
        {
            throw Error(body, -1, null, "effective instruction reader failed: " + error.Message);
        }
        if (effective == null || effective.Count == 0)
            throw Error(body, -1, null, "effective instruction reader returned an empty stream");

        Bindings bindings = ForParse(body, effective, null, expectedOwners);
        ParseOutcome outcome = Parse(body, bindings, effective, null, expectedOwners);

        var awaits = new List<AwaitObservation>(body.Awaits.Count);
        for (int ordinal = 0; ordinal < body.Awaits.Count; ordinal++)
        {
            int originalIndex = body.Awaits[ordinal];
            SlotFact slot = FindSlot(outcome, originalIndex);
            var owners = new List<Owner>();
            foreach (BlockFact block in slot.Blocks)
                if (!owners.Contains(block.Owner))
                    owners.Add(block.Owner);
            awaits.Add(new AwaitObservation(ordinal, originalIndex, outcome.OriginalToEffective[originalIndex],
                (MethodInfo)body.Original[originalIndex].operand!, owners));
        }

        var edits = new List<EditObservation>(outcome.Blocks.Count + outcome.Bridges.Count + 1);
        foreach (BlockFact block in outcome.Blocks)
            edits.Add(new EditObservation(block.Owner, KindName(block.Kind), block.OriginalIndex, block.Start, block.Length));
        foreach (BridgeFact bridge in outcome.Bridges)
        {
            if (bridge.Site.Form == BridgeForm.ComposedDrawAdd)
            {
                if (bridge.ComposedIsFtl)
                {
                    edits.Add(new EditObservation(Owner.Ftl, "draw-add-composed", bridge.Site.OriginalIndex,
                        bridge.Start, bridge.Length));
                    if (outcome.ExhaustPresent)
                        edits.Add(new EditObservation(Owner.Exhaust, "draw-add-composed", bridge.Site.OriginalIndex,
                            bridge.Start, bridge.Length));
                }
                else
                {
                    edits.Add(new EditObservation(Owner.Exhaust, "draw-add-composed", bridge.Site.OriginalIndex,
                        bridge.Start, bridge.Length));
                }
            }
            else
            {
                edits.Add(new EditObservation(bridge.Site.Owner, bridge.Site.Kind, bridge.Site.OriginalIndex,
                    bridge.Start, bridge.Length));
            }
        }
        edits.Sort((left, right) => left.EffectiveStart.CompareTo(right.EffectiveStart));

        return new Observation(body.Id, body.Move, effective, awaits, edits,
            Array.AsReadOnly(outcome.OriginalToEffective));
    }

    private static Pinned[] EnsurePinned()
    {
        Pinned[]? built = Volatile.Read(ref _pinned);
        if (built != null)
            return built;
        lock (Gate)
        {
            if (_pinned != null)
                return _pinned;
            Spec[] specs = BuildSpecs();
            var pinned = new Pinned[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                if (specs[i].Id != i)
                    throw new InvalidOperationException("ContinuationIL spec table is out of order at index " + i);
                pinned[i] = new Pinned(specs[i]);
                TryPinIdentity(pinned[i]);
            }
            Volatile.Write(ref _pinned, pinned);
            return pinned;
        }
    }

    private static void TryPinIdentity(Pinned pinned)
    {
        Spec spec = pinned.Spec;
        try
        {
            MethodInfo entry = ResolveEntry(spec);
            if (entry.IsStatic != spec.Static)
                throw new InvalidOperationException("pinned kickoff static/instance shape changed");
            if (entry.Module.ModuleVersionId != HostMvid)
                throw new InvalidOperationException("host module MVID " + entry.Module.ModuleVersionId +
                    " is not current1131 " + HostMvid);
            if (entry.ReturnType != spec.Return)
                throw new InvalidOperationException("return type is " + entry.ReturnType + ", pinned " + spec.Return);
            ParameterInfo[] parameters = entry.GetParameters();
            if (parameters.Length != spec.Parameters.Length)
                throw new InvalidOperationException("parameter count changed");
            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i].ParameterType != spec.Parameters[i])
                    throw new InvalidOperationException("parameter " + i + " type is " +
                        parameters[i].ParameterType + ", pinned " + spec.Parameters[i]);
            if (spec.EntryIlBytes > 0)
            {
                if (entry.MetadataToken != spec.EntryToken)
                    throw new InvalidOperationException("entry token " + entry.MetadataToken +
                        " is not pinned " + spec.EntryToken);
                byte[] entryIl = entry.GetMethodBody()?.GetILAsByteArray()
                    ?? throw new InvalidOperationException("pinned kickoff has no IL body");
                if (entryIl.Length != spec.EntryIlBytes)
                    throw new InvalidOperationException("entry raw IL length is " + entryIl.Length +
                        ", pinned " + spec.EntryIlBytes);
                string entrySha = Convert.ToHexString(SHA256.HashData(entryIl));
                if (!entrySha.Equals(spec.EntrySha, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("entry raw IL SHA-256 is " + entrySha +
                        ", pinned " + spec.EntrySha);
            }
            pinned.Entry = entry;

            MethodInfo move;
            if (spec.State == null)
            {
                if (entry.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
                    throw new InvalidOperationException("synchronous body unexpectedly carries AsyncStateMachineAttribute");
                move = entry;
                if (move.MetadataToken != spec.MoveToken)
                    throw new InvalidOperationException("method token " + move.MetadataToken +
                        " is not pinned " + spec.MoveToken);
            }
            else
            {
                AsyncStateMachineAttribute? attribute = entry.GetCustomAttribute<AsyncStateMachineAttribute>();
                if (attribute == null)
                    throw new InvalidOperationException("kickoff no longer carries AsyncStateMachineAttribute");
                Type state = attribute.StateMachineType;
                if (state.FullName != spec.State)
                    throw new InvalidOperationException("state machine type is " + state.FullName +
                        ", pinned " + spec.State);
                if (state.DeclaringType != entry.DeclaringType)
                    throw new InvalidOperationException("state machine is not nested in the pinned kickoff type");
                move = state.GetMethod("MoveNext", DeclaredAll)
                    ?? throw new InvalidOperationException("generated MoveNext is missing");
                if (move.IsStatic || move.ReturnType != typeof(void) || move.GetParameters().Length != 0)
                    throw new InvalidOperationException("generated MoveNext signature changed");
                if (move.MetadataToken != spec.MoveToken)
                    throw new InvalidOperationException("MoveNext token " + move.MetadataToken +
                        " is not pinned " + spec.MoveToken);
            }
            if (move.Module.ModuleVersionId != HostMvid)
                throw new InvalidOperationException("continuation target module MVID is not current1131");
            pinned.Move = move;
        }
        catch (Exception error)
        {
            pinned.Error = error.Message;
        }
    }

    private static Body GetBody(Pinned pinned)
    {
        if (pinned.Body != null)
            return pinned.Body;
        lock (Gate)
        {
            if (pinned.Body != null)
                return pinned.Body;
            if (pinned.Entry == null || pinned.Move == null)
                throw SpecError(pinned.Spec, "host identity pin failed: " + pinned.Error);
            pinned.Body = BuildBody(pinned.Spec, pinned.Entry, pinned.Move);
            return pinned.Body;
        }
    }

    private static Body BuildBody(Spec spec, MethodInfo entry, MethodInfo move)
    {
        FieldInfo? stateField = null;
        FieldInfo? selector = null;
        FieldInfo? context = null;
        FieldInfo? model = null;
        FieldInfo? thisField = null;
        FieldInfo? guardField = null;

        if (spec.State != null)
        {
            Type state = move.DeclaringType!;
            stateField = RequireField(state, "<>1__state", typeof(int), spec);
            if (spec.SelectorName != null)
                selector = RequireField(state, spec.SelectorName, spec.SelectorType!, spec);
            if (spec.ContextName != null)
                context = RequireField(state, spec.ContextName, spec.ContextType!, spec);
            if (spec.ModelName != null)
                model = RequireField(state, spec.ModelName, spec.ModelType!, spec);
            if (spec.ThisName != null)
                thisField = RequireField(state, spec.ThisName, spec.ThisType!, spec);
            if (spec.GuardFieldName != null)
                guardField = RequireField(state, spec.GuardFieldName, spec.GuardFieldType!, spec);
        }

        byte[] il;
        try
        {
            il = move.GetMethodBody()?.GetILAsByteArray()
                ?? throw new InvalidOperationException("continuation target has no IL body");
        }
        catch (Exception error)
        {
            throw SpecError(spec, "continuation target body is not readable: " + error.Message);
        }
        if (il.Length != spec.IlBytes)
            throw SpecError(spec, "raw IL length is " + il.Length + ", pinned " + spec.IlBytes);
        string sha = Convert.ToHexString(SHA256.HashData(il));
        if (!sha.Equals(spec.Sha, StringComparison.OrdinalIgnoreCase))
            throw SpecError(spec, "raw IL SHA-256 is " + sha + ", pinned " + spec.Sha);

        List<CodeInstruction> original = ReadOriginal(move, spec);
        if (spec.Has(Owner.Reboot) && selector == null)
            throw SpecError(spec, "approved Reboot body has no pinned selector field");
        if (spec.State != null && stateField == null)
            throw SpecError(spec, "approved async body has no pinned machine-state field");
        if ((spec.Id == 23) && thisField == null)
            throw SpecError(spec, "attack body has no pinned captured command field");
        if (spec.Id == 4 && guardField == null)
            throw SpecError(spec, "damage body has no pinned target-data guard field");

        var body = new Body(spec, entry, move, selector, original)
        {
            StateField = stateField,
            ContextField = context,
            ModelField = model,
            ThisField = thisField,
            GuardField = guardField
        };
        ParseOriginal(body);
        return body;
    }

    private static List<CodeInstruction> ReadOriginal(MethodInfo move, Spec spec)
    {
        List<CodeInstruction>? original;
        try
        {
            original = PatchProcessor.GetOriginalInstructions(move);
        }
        catch (Exception error)
        {
            throw SpecError(spec, "original instruction reader failed: " + error.Message);
        }
        if (original == null || original.Count == 0)
            throw SpecError(spec, "original instruction reader returned an empty stream");
        return original;
    }

    private static MethodInfo ResolveEntry(Spec spec)
    {
        MethodInfo? entry = spec.Type.GetMethod(spec.Name, DeclaredAll, null, spec.Parameters, null);
        if (entry == null)
            throw new InvalidOperationException("pinned kickoff method is missing");
        if (entry.DeclaringType != spec.Type)
            throw new InvalidOperationException("pinned kickoff resolved on another declaring type");
        return entry;
    }

    private static FieldInfo RequireField(Type state, string name, Type type, Spec spec)
    {
        FieldInfo? field = state.GetField(name, DeclaredAll);
        if (field == null)
            throw SpecError(spec, "generated field " + name + " is missing");
        if (field.IsStatic || field.DeclaringType != state || field.FieldType != type)
            throw SpecError(spec, "generated field " + name + " changed type/owner");
        return field;
    }

    private static bool ParameterTypesEqual(MethodBase method, Type[] expected)
    {
        ParameterInfo[] actual = method.GetParameters();
        if (actual.Length != expected.Length)
            return false;
        for (int i = 0; i < actual.Length; i++)
            if (actual[i].ParameterType != expected[i])
                return false;
        return true;
    }

    private static void ParseOriginal(Body body)
    {
        List<CodeInstruction> original = body.Original;

        if (body.Spec.State != null)
        {
            int anchor = -1;
            for (int i = 0; i < original.Count; i++)
            {
                if (original[i].blocks.Count == 0)
                    continue;
                anchor = i;
                break;
            }
            if (anchor < 0)
                throw OriginalError(body, -1, "pinned original has no exception-region opener");
            if (!IsExactlyOpener(original[anchor].blocks))
                throw OriginalError(body, anchor, "first exception-region boundary is not the builder try opener");
            body.InitialAnchor = anchor;
        }
        else
        {
            body.InitialAnchor = -1;
        }

        for (int i = 0; i < original.Count; i++)
        {
            CodeInstruction instruction = original[i];
            if (!TryGetAwaitResult(instruction, out MethodInfo result))
                continue;
            if (instruction.labels.Count != 0)
                throw OriginalError(body, i, "GetResult carries labels");
            if (instruction.blocks.Count != 0)
                throw OriginalError(body, i, "GetResult carries exception blocks");
            if (i == 0)
                throw OriginalError(body, i, "GetResult has no local receiver");
            CodeInstruction receiver = original[i - 1];
            if (receiver.opcode != OpCodes.Ldloca && receiver.opcode != OpCodes.Ldloca_S)
                throw OriginalError(body, i, "GetResult receiver is not a local address");
            if (receiver.operand is not LocalVariableInfo local || local.LocalType != result.DeclaringType)
                throw OriginalError(body, i, "GetResult receiver local type changed");
            if (EhDepth(original, i) <= 0)
                throw OriginalError(body, i, "GetResult is outside the original exception region");
            body.Awaits.Add(i);
            bool plain = result.DeclaringType == typeof(TaskAwaiter);
            body.AwaitPlain.Add(plain);
            if (plain)
                body.PlainAwaits.Add(i);
        }
        if (body.Spec.State != null && body.Awaits.Count == 0)
            throw OriginalError(body, -1, "pinned original has no awaited completion");

        AwaitPin? pin = body.Spec.AwaitExpectation;
        if (pin != null)
        {
            int plain = 0;
            foreach (bool value in body.AwaitPlain)
                if (value)
                    plain++;
            int generic = body.Awaits.Count - plain;
            if (pin.Total >= 0 && body.Awaits.Count != pin.Total)
                throw OriginalError(body, -1, "await count is " + body.Awaits.Count + ", pinned " + pin.Total);
            if (pin.Plain >= 0 && plain != pin.Plain)
                throw OriginalError(body, -1, "plain await count is " + plain + ", pinned " + pin.Plain);
            if (pin.Generic >= 0 && generic != pin.Generic)
                throw OriginalError(body, -1, "generic await count is " + generic + ", pinned " + pin.Generic);
            if (pin.GenericType != null)
            {
                int matched = 0;
                foreach (int index in body.Awaits)
                {
                    var method = (MethodInfo)original[index].operand!;
                    if (method.DeclaringType != typeof(TaskAwaiter) &&
                        method.DeclaringType!.GetGenericArguments()[0] == pin.GenericType)
                        matched++;
                }
                if (matched != pin.Generic)
                    throw OriginalError(body, -1, "generic await result type/count changed");
            }
        }

        foreach (CommitPin commit in body.Spec.CommitPins)
        {
            MethodInfo target = ExactMember(commit.DeclaringType, commit.Name, commit.Parameters)
                ?? throw OriginalError(body, -1, "pinned commit member " + commit.DeclaringType.FullName +
                    "." + commit.Name + " is missing");
            int found = 0;
            for (int i = 0; i < original.Count; i++)
            {
                if (!original[i].Calls(target))
                    continue;
                if (original[i].blocks.Count != 0)
                    throw OriginalError(body, i, "commit call carries an exception block");
                if (body.Commits.ContainsKey(i))
                    throw OriginalError(body, i, "two pinned commit members share one call site");
                body.Commits.Add(i, new CommitSite { OriginalIndex = i, StartsCardMove = commit.StartsCardMove });
                found++;
            }
            if (found == 0)
                throw OriginalError(body, -1, "pinned commit member " + commit.DeclaringType.FullName +
                    "." + commit.Name + " has no call site");
        }

        int[] rawOffsets = RawOffsets(body);
        if (body.Id == 0)
        {
            body.DrawStopAnchor = Array.IndexOf(rawOffsets, 541);
            body.DrawResultAnchor = Array.IndexOf(rawOffsets, 1011);
            if (body.Has(Owner.Ftl) && (body.DrawStopAnchor < 0 || body.DrawResultAnchor < 0))
                throw OriginalError(body, -1, "draw stop/result anchors are not pinned");
        }
        BuildBridgeSites(body, rawOffsets);

        if (body.Spec.Id == 23)
        {
            MethodInfo isDead = ExactPropertyGetter(typeof(Creature), "IsDead", typeof(bool))
                ?? throw OriginalError(body, -1, "pinned Creature.IsDead getter is missing");
            var deadSites = new List<int>();
            for (int i = 0; i < original.Count; i++)
                if (original[i].Calls(isDead))
                    deadSites.Add(i);
            if (deadSites.Count != 2)
                throw OriginalError(body, -1, "attack loop dead-check count is " + deadSites.Count + ", pinned 2");
            int dead = deadSites[1];
            body.AttackDeadSite = dead;
            if (dead + 1 >= original.Count)
                throw OriginalError(body, dead, "second dead check has no native condition branch");
            CodeInstruction condition = original[dead + 1];
            if (condition.opcode != OpCodes.Brtrue && condition.opcode != OpCodes.Brtrue_S)
                throw OriginalError(body, dead + 1, "second dead check is not followed by brtrue");
            if (condition.operand is not Label cleanupLabel)
                throw OriginalError(body, dead + 1, "native cleanup branch has no label operand");
            var placement = LabelPlacement(original);
            if (!placement.TryGetValue(cleanupLabel, out int cleanup))
                throw OriginalError(body, dead + 1, "native cleanup label is not placed in the body");
            body.AttackCleanup = cleanup;
            int history = -1;
            for (int i = cleanup; i < original.Count; i++)
            {
                if (original[i].operand is MethodInfo method &&
                    method.DeclaringType == typeof(MegaCrit.Sts2.Core.Combat.History.CombatHistory) &&
                    method.Name == "CreatureAttacked")
                {
                    history = i;
                    break;
                }
            }
            if (history < 0)
                throw OriginalError(body, cleanup, "attack cleanup no longer reaches CombatHistory.CreatureAttacked");
            MethodInfo damage = ExactStatic(typeof(CreatureCmd), "Damage", typeof(Task<IEnumerable<DamageResult>>),
                typeof(PlayerChoiceContext), typeof(IEnumerable<DamageTargetData>), typeof(decimal), typeof(ValueProp),
                typeof(Creature), typeof(CardModel), typeof(CardPlay), typeof(ModifyDamageHookType));
            int cacheBranch = Array.IndexOf(rawOffsets, 2099);
            if (!IsAttackHistoryDelegateCacheBranch(body, rawOffsets, placement, cleanup, history, cacheBranch))
                throw OriginalError(body, cacheBranch, "attack history delegate-cache diamond changed");
            for (int i = cleanup; i < history; i++)
            {
                if (original[i].opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch && i != cacheBranch)
                    throw OriginalError(body, i, "attack cleanup path introduces a branch before history");
                if (original[i].Calls(damage))
                    throw OriginalError(body, i, "attack cleanup path calls CreatureCmd.Damage before history");
            }
        }

        LabelPlacement(original);
        BuildPostSlots(body);
    }

    private static bool IsAttackHistoryDelegateCacheBranch(Body body, int[] offsets,
        Dictionary<Label, int> placement, int cleanup, int history, int branch)
    {
        List<CodeInstruction> original = body.Original;
        if (branch != cleanup + 10 || history != branch + 9 || branch < 2 || history >= original.Count ||
            offsets[cleanup] != 2065 || offsets[branch] != 2099 || offsets[branch + 7] != 2124 || offsets[history] != 2134 ||
            original[branch].opcode != OpCodes.Brtrue_S || original[branch].operand is not Label merge ||
            !placement.TryGetValue(merge, out int target) || target != branch + 7 ||
            original[target].labels.Count != 1 || !original[target].labels.Contains(merge) ||
            original[branch].labels.Count != 0 || EhDepth(original, cleanup) != 1)
            return false;
        for (int i = cleanup; i <= history; i++)
            if (original[i].blocks.Count != 0)
                return false;

        Type? closure = typeof(AttackCommand).GetNestedType("<>c", BindingFlags.Public | BindingFlags.NonPublic);
        Type selectorType = typeof(Func<List<DamageResult>, IEnumerable<DamageResult>>);
        FieldInfo? cache = closure?.GetField("<>9__91_0", DeclaredAll);
        FieldInfo? singleton = closure?.GetField("<>9", DeclaredAll);
        MethodInfo? selector = closure == null ? null : ExactMethod(closure, "<Execute>b__91_0",
            typeof(IEnumerable<DamageResult>), false, new[] { typeof(List<DamageResult>) });
        ConstructorInfo? constructor = selectorType.GetConstructor(new[] { typeof(object), typeof(IntPtr) });
        FieldInfo? combat = body.Move.DeclaringType?.GetField("<combatState>5__2", DeclaredAll);
        FieldInfo? results = typeof(AttackCommand).GetField("_results", DeclaredAll);
        if (cache == null || !cache.IsStatic || cache.DeclaringType != closure || cache.FieldType != selectorType ||
            singleton == null || !singleton.IsStatic || singleton.DeclaringType != closure || singleton.FieldType != closure ||
            selector == null || constructor == null || combat == null || combat.IsStatic ||
            combat.DeclaringType != body.Move.DeclaringType || combat.FieldType != typeof(ICombatState) ||
            results == null || results.IsStatic || results.DeclaringType != typeof(AttackCommand) ||
            results.FieldType != typeof(List<List<DamageResult>>))
            return false;
        byte[]? selectorIl = selector.GetMethodBody()?.GetILAsByteArray();
        if (selectorIl == null || selectorIl.Length != 2 || selectorIl[0] != 0x03 || selectorIl[1] != 0x2a)
            return false;

        MethodInfo? manager = ExactPropertyGetter(typeof(CombatManager), "Instance", typeof(CombatManager));
        MethodInfo? getHistory = ExactPropertyGetter(typeof(CombatManager), "History",
            typeof(MegaCrit.Sts2.Core.Combat.History.CombatHistory));
        MethodInfo? attacker = ExactPropertyGetter(typeof(AttackCommand), "Attacker", typeof(Creature));
        MethodInfo? commit = ExactMethod(typeof(MegaCrit.Sts2.Core.Combat.History.CombatHistory), "CreatureAttacked",
            typeof(void), false, new[] { typeof(ICombatState), typeof(Creature), typeof(IReadOnlyList<DamageResult>) });
        if (manager == null || getHistory == null || attacker == null || commit == null ||
            original[target].opcode != OpCodes.Call || original[target].operand is not MethodInfo selectMany ||
            selectMany.DeclaringType != typeof(Enumerable) || selectMany.Name != nameof(Enumerable.SelectMany) ||
            !selectMany.IsStatic || !selectMany.IsGenericMethod || selectMany.ContainsGenericParameters ||
            selectMany.ReturnType != typeof(IEnumerable<DamageResult>) ||
            !ParameterTypesEqual(selectMany, new[] { typeof(IEnumerable<List<DamageResult>>), selectorType }) ||
            !selectMany.GetGenericArguments().SequenceEqual(new[] { typeof(List<DamageResult>), typeof(DamageResult) }) ||
            original[target + 1].operand is not MethodInfo toList || toList.DeclaringType != typeof(Enumerable) ||
            toList.Name != nameof(Enumerable.ToList) || !toList.IsStatic || !toList.IsGenericMethod ||
            toList.ContainsGenericParameters || toList.ReturnType != typeof(List<DamageResult>) ||
            !ParameterTypesEqual(toList, new[] { typeof(IEnumerable<DamageResult>) }) ||
            !toList.GetGenericArguments().SequenceEqual(new[] { typeof(DamageResult) }))
            return false;

        return original[cleanup].opcode == OpCodes.Call && Equals(original[cleanup].operand, manager) &&
            Instr(original[cleanup + 1], OpCodes.Callvirt, getHistory) &&
            Instr(original[cleanup + 2], OpCodes.Ldarg_0, null) &&
            Instr(original[cleanup + 3], OpCodes.Ldfld, combat) &&
            Instr(original[cleanup + 4], OpCodes.Ldloc_1, null) &&
            Instr(original[cleanup + 5], OpCodes.Call, attacker) &&
            Instr(original[cleanup + 6], OpCodes.Ldloc_1, null) &&
            Instr(original[cleanup + 7], OpCodes.Ldfld, results) &&
            Instr(original[branch - 2], OpCodes.Ldsfld, cache) &&
            Instr(original[branch - 1], OpCodes.Dup, null) &&
            Instr(original[branch + 1], OpCodes.Pop, null) &&
            Instr(original[branch + 2], OpCodes.Ldsfld, singleton) &&
            Instr(original[branch + 3], OpCodes.Ldftn, selector) &&
            Instr(original[branch + 4], OpCodes.Newobj, constructor) &&
            Instr(original[branch + 5], OpCodes.Dup, null) &&
            Instr(original[branch + 6], OpCodes.Stsfld, cache) &&
            Instr(original[target + 1], OpCodes.Call, toList) &&
            Instr(original[history], OpCodes.Callvirt, commit);
    }

    private static void BuildBridgeSites(Body body, int[] offsets)
    {
        switch (body.Id)
        {
            case 0:
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustDrawShuffle,
                    HostBridgeId.ShuffleIfNecessary, 453, OpCodes.Call, null, "exhaust.bridge.draw-shuffle");
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.ComposedDrawAdd, BridgeMethodId.ExhaustDrawAdd,
                    HostBridgeId.CardPileAdd5, 653, OpCodes.Call, body.ContextField, "draw-add-composed");
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustAfterCardDrawn,
                    HostBridgeId.HookAfterCardDrawn, 804, OpCodes.Call, null, "exhaust.bridge.after-card-drawn");
                AddBridge(body, offsets, Owner.Ftl, BridgeForm.Loader, BridgeMethodId.FtlInvokeDrawn,
                    HostBridgeId.CardModelInvokeDrawn, 903, OpCodes.Call, body.ContextField,
                    "ftl.bridge.invoke-drawn");
                AddBridge(body, offsets, Owner.Ftl, BridgeForm.Direct, BridgeMethodId.FtlShouldDraw,
                    HostBridgeId.HookShouldDraw, 82, OpCodes.Call, null, "ftl.bridge.should-draw");
                break;
            case 2:
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustShuffleAdd,
                    HostBridgeId.CardPileAdd6, 334, OpCodes.Call, null, "exhaust.bridge.shuffle-add");
                break;
            case 4:
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustBeforeDamageReceived,
                    HostBridgeId.HookBeforeDamageReceived, 637, OpCodes.Call, null,
                    "exhaust.bridge.before-damage-received");
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustAfterDamageGiven,
                    HostBridgeId.HookAfterDamageGiven, 3875, OpCodes.Call, null,
                    "exhaust.bridge.after-damage-given");
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustAfterDamageReceived,
                    HostBridgeId.HookAfterDamageReceived, 4069, OpCodes.Call, null,
                    "exhaust.bridge.after-damage-received");
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustDamageKill,
                    HostBridgeId.CreatureKill3, 4377, OpCodes.Call, null, "exhaust.bridge.damage-kill");
                break;
            case 5:
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustKillOne,
                    HostBridgeId.KillWithout4, 154, OpCodes.Call, null, "exhaust.bridge.kill-one");
                break;
            case 6:
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustDamageKill,
                    HostBridgeId.CreatureKill3, 1550, OpCodes.Call, null, "exhaust.bridge.damage-kill");
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustKillCreature,
                    HostBridgeId.CreatureKill2, 1738, OpCodes.Call, null, "exhaust.bridge.kill-creature");
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustKillOne,
                    HostBridgeId.KillWithout4, 2280, OpCodes.Call, null, "exhaust.bridge.kill-one");
                break;
            case 23:
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Loader, BridgeMethodId.ExhaustDamageFromAttack,
                    HostBridgeId.CreatureDamage8, 1896, OpCodes.Call, body.ThisField,
                    "exhaust.bridge.damage-from-attack");
                break;
            case 24:
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustAdd,
                    HostBridgeId.CardPileAddPileType5, 138, OpCodes.Call, null, "exhaust.bridge.exhaust-add");
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustDispatch,
                    HostBridgeId.HookAfterCardExhausted4, 287, OpCodes.Call, null, "exhaust.bridge.dispatch");
                break;
            case 25:
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustInvokeListener,
                    HostBridgeId.AbstractModelListener3, 99, OpCodes.Call, null, "exhaust.bridge.listener");
                break;
            case 26:
                AddBridge(body, offsets, Owner.Echo, BridgeForm.Direct, BridgeMethodId.EchoModifyOne,
                    HostBridgeId.AbstractModelCount3, -1, OpCodes.Call, null, "echo.bridge.count");
                break;
            case 27:
                AddBridge(body, offsets, Owner.Ftl, BridgeForm.Direct, BridgeMethodId.FtlAfterCardPlayed,
                    HostBridgeId.HookAfterCardPlayed, 2211, OpCodes.Call, null, "ftl.bridge.after-card-played");
                break;
            case 28:
                AddBridge(body, offsets, Owner.Exhaust, BridgeForm.Direct, BridgeMethodId.ExhaustShuffleNow,
                    HostBridgeId.CardPileShuffle2, 177, OpCodes.Call, null, "exhaust.bridge.shuffle-now");
                break;
            case 29:
                AddBridge(body, offsets, Owner.Ftl, BridgeForm.Loader2, BridgeMethodId.FtlDrawFromUse,
                    HostBridgeId.CardPileDraw4, 137, OpCodes.Call, body.ModelField, body.ThisField,
                    "ftl.bridge.draw-from-use");
                break;
        }
    }

    private static void AddBridge(Body body, int[] offsets, Owner owner, BridgeForm form, BridgeMethodId method,
        HostBridgeId originalId, int rawOffset, OpCode replacementOpcode, FieldInfo? loaderField, string kind)
        => AddBridge(body, offsets, owner, form, method, originalId, rawOffset, replacementOpcode,
            loaderField, null, kind);

    private static void AddBridge(Body body, int[] offsets, Owner owner, BridgeForm form, BridgeMethodId method,
        HostBridgeId originalId, int rawOffset, OpCode replacementOpcode, FieldInfo? loaderField,
        FieldInfo? loaderField2, string kind)
    {
        MethodInfo original = HostBridgeOriginal(originalId);
        OpCode originalOpcode = HostBridgeOpcode(originalId);
        int found = -1;
        int count = 0;
        for (int i = 0; i < body.Original.Count; i++)
        {
            if (body.Original[i].opcode != originalOpcode || !body.Original[i].Calls(original))
                continue;
            found = i;
            count++;
        }
        if (count != 1 || found < 0)
            throw OriginalError(body, -1, "bridge source " + original.DeclaringType?.FullName + "." +
                original.Name + " count is " + count + ", expected 1");
        if (rawOffset >= 0 && offsets[found] != rawOffset)
            throw OriginalError(body, found, "bridge source raw offset is " + offsets[found] +
                ", pinned " + rawOffset);
        if (form != BridgeForm.Direct &&
            (body.Original[found].labels.Count != 0 || body.Original[found].blocks.Count != 0))
            throw OriginalError(body, found, "bridge source carries labels/exception blocks");
        if (form == BridgeForm.Loader2 && (loaderField == null || loaderField2 == null))
            throw OriginalError(body, found, "two-field loader is missing an exact state field");
        bool explicitCallvirtToCall = method is BridgeMethodId.ExhaustInvokeListener or BridgeMethodId.EchoModifyOne;
        if (form == BridgeForm.Direct && !explicitCallvirtToCall && replacementOpcode != originalOpcode)
            throw OriginalError(body, found, "same-signature bridge changes the call opcode");
        if (body.BridgeByIndex.ContainsKey(found))
            throw OriginalError(body, found, "two bridge sites share one original call");
        var site = new BridgeSite
        {
            OriginalIndex = found,
            RawOffset = rawOffset,
            Owner = owner,
            Form = form,
            Method = method,
            Original = original,
            ReplacementOpcode = replacementOpcode,
            LoaderField = loaderField,
            LoaderField2 = loaderField2,
            Kind = kind
        };
        body.BridgeSites.Add(site);
        body.BridgeByIndex.Add(found, site);
    }

    private static int[] RawOffsets(Body body)
    {
        var offsets = new int[body.Original.Count];
        int offset = 0;
        for (int i = 0; i < body.Original.Count; i++)
        {
            offsets[i] = offset;
            CodeInstruction instruction = body.Original[i];
            offset += instruction.opcode.Size + OperandWidth(body, i, instruction);
            if (offset < offsets[i])
                throw OriginalError(body, i, "raw offset accumulated past the integer range");
        }
        if (offset != body.Spec.IlBytes)
            throw OriginalError(body, -1, "raw offset geometry totals " + offset + ", pinned " + body.Spec.IlBytes);
        return offsets;
    }

    private static int OperandWidth(Body body, int index, CodeInstruction instruction)
    {
        switch (instruction.opcode.OperandType)
        {
            case OperandType.InlineNone:
                return 0;
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
            case OperandType.ShortInlineBrTarget:
                return 1;
            case OperandType.InlineVar:
                return 2;
            case OperandType.InlineI:
            case OperandType.InlineBrTarget:
            case OperandType.InlineField:
            case OperandType.InlineMethod:
            case OperandType.InlineSig:
            case OperandType.InlineString:
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.ShortInlineR:
                return 4;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                return 8;
            case OperandType.InlineSwitch:
                if (instruction.operand is Label[] targets)
                    return 4 + 4 * targets.Length;
                throw OriginalError(body, index, "switch operand is not a label array");
            default:
                throw OriginalError(body, index, "unsupported operand type " + instruction.opcode.OperandType);
        }
    }

    private static void BuildPostSlots(Body body)
    {
        foreach (int index in body.Awaits)
        {
            var allowed = new List<BlockKind>();
            if (body.Has(Owner.Reboot))
                allowed.Add(body.Id == 0 ? BlockKind.RebootDrawPost : BlockKind.RebootPost);
            BlockKind? requiredFtl = null;
            if (body.Has(Owner.Ftl))
            {
                if (body.Id == 0)
                {
                    BlockKind form = index == body.DrawStopAnchor
                        ? BlockKind.FtlDrawStop
                        : BlockKind.FtlDrawPost;
                    allowed.Add(form);
                    requiredFtl = form;
                }
                else if (body.Id == 23)
                {
                    allowed.Add(BlockKind.FtlAttackStop);
                    allowed.Add(BlockKind.FtlAttackIdentity);
                    int plainOrdinal = body.PlainAwaits.IndexOf(index);
                    requiredFtl = plainOrdinal >= 0 && plainOrdinal < 5
                        ? BlockKind.FtlAttackStop
                        : BlockKind.FtlAttackIdentity;
                }
                else if (body.Id != 27 && body.Id != 29)
                {
                    throw Error(body, -1, Owner.Ftl, "body has no approved FTL guard form");
                }
            }
            if (body.Has(Owner.Exhaust))
                allowed.Add(ExhaustGuardKind(body));
            body.PostSlots.Add(index, new PostSlot
            {
                OriginalIndex = index,
                IsAwait = true,
                Allowed = allowed.ToArray(),
                RequiredFtl = requiredFtl
            });
        }
        if (body.Id == 23 && body.Has(Owner.Ftl))
        {
            if (body.AttackDeadSite < 0)
                throw OriginalError(body, -1, "attack loop dead site was not pinned");
            body.PostSlots.Add(body.AttackDeadSite, new PostSlot
            {
                OriginalIndex = body.AttackDeadSite,
                IsAwait = false,
                Allowed = new[] { BlockKind.FtlLoopOr }
            });
        }
    }

    private static BlockKind ExhaustGuardKind(Body body) => body.Id switch
    {
        0 or 3 => BlockKind.ExhaustGuardReward,
        4 => BlockKind.ExhaustGuardDamage,
        23 => BlockKind.ExhaustGuardAttack,
        24 => BlockKind.ExhaustGuardExhaust,
        25 => BlockKind.ExhaustGuardDispatch,
        1 or 2 or 5 or 6 or 8 or 14 or 19 or 20 or 28 => BlockKind.ExhaustGuardNative,
        _ => throw Error(body, -1, Owner.Exhaust, "body has no approved Exhaust guard form")
    };

    private static List<(BlockKind Kind, int Anchor)> Required(Body body, Owner owner)
    {
        var required = new List<(BlockKind Kind, int Anchor)>();
        switch (owner)
        {
            case Owner.Reboot:
                required.Add((BlockKind.RebootInitial, body.InitialAnchor));
                foreach (int index in body.Awaits)
                    required.Add((body.Id == 0 ? BlockKind.RebootDrawPost : BlockKind.RebootPost, index));
                foreach (CommitSite commit in body.Commits.Values)
                    required.Add((BlockKind.RebootCommit, commit.OriginalIndex));
                break;
            case Owner.Ftl:
                if (body.Id == 0)
                {
                    foreach (int index in body.Awaits)
                        required.Add((index == body.DrawStopAnchor
                            ? BlockKind.FtlDrawStop
                            : BlockKind.FtlDrawPost, index));
                }
                else if (body.Id == 23)
                {
                    foreach (int index in body.Awaits)
                    {
                        int plainOrdinal = body.PlainAwaits.IndexOf(index);
                        required.Add((plainOrdinal >= 0 && plainOrdinal < 5
                            ? BlockKind.FtlAttackStop
                            : BlockKind.FtlAttackIdentity, index));
                    }
                    required.Add((BlockKind.FtlLoopOr, body.AttackDeadSite));
                }
                else if (body.Id != 27 && body.Id != 29)
                {
                    throw Error(body, -1, owner, "body has no approved FTL edit form");
                }
                foreach (BridgeSite site in body.BridgeSites)
                {
                    if (site.Form == BridgeForm.ComposedDrawAdd)
                        required.Add((BlockKind.DrawAddComposed, site.OriginalIndex));
                    else if (site.Owner == owner)
                        required.Add((BlockKind.Bridge, site.OriginalIndex));
                }
                break;
            case Owner.Exhaust:
                foreach (int index in body.Awaits)
                    required.Add((ExhaustGuardKind(body), index));
                foreach (BridgeSite site in body.BridgeSites)
                {
                    if (site.Form == BridgeForm.ComposedDrawAdd)
                        required.Add((BlockKind.DrawAddComposed, site.OriginalIndex));
                    else if (site.Owner == owner)
                        required.Add((BlockKind.Bridge, site.OriginalIndex));
                }
                break;
            case Owner.Echo:
                foreach (BridgeSite site in body.BridgeSites)
                    required.Add((BlockKind.Bridge, site.OriginalIndex));
                break;
            default:
                throw Error(body, -1, owner, "unapproved owner");
        }
        return required;
    }

    private static Bindings ForParse(Body body, IReadOnlyList<CodeInstruction> incoming, Owner? requesting,
        IReadOnlyCollection<Owner>? expected)
    {
        var bindings = new Bindings();
        HashSet<Owner> present = DetectOwnerCalls(incoming);
        if (requesting is Owner requested)
            bindings.Set(requested, RequireBinding(requested));
        if (expected != null)
        {
            foreach (Owner owner in expected)
            {
                if (!body.Has(owner))
                    throw ParseError(body, -1, owner, "expected owner is not admitted for this body");
                if (!bindings.Has(owner))
                    bindings.Set(owner, RequireBinding(owner));
            }
        }
        foreach (Owner owner in body.Spec.Emitters)
        {
            if (bindings.Has(owner))
                continue;
            if (!present.Contains(owner))
                continue;
            bindings.Set(owner, RequireBinding(owner));
        }
        foreach (Owner owner in present)
        {
            if (!body.Has(owner))
                throw ParseError(body, -1, owner, "owner/body combination is not approved");
            if (expected != null && !Contains(expected, owner))
                throw ParseError(body, -1, owner, "owner is present but not listed in expectedOwners");
        }
        ValidateBoundBridges(body, bindings);
        return bindings;
    }

    private static bool Contains(IReadOnlyCollection<Owner> owners, Owner owner)
    {
        foreach (Owner candidate in owners)
            if (candidate == owner)
                return true;
        return false;
    }

    private static HashSet<Owner> DetectOwnerCalls(IReadOnlyList<CodeInstruction> incoming)
    {
        var present = new HashSet<Owner>();
        foreach (CodeInstruction instruction in incoming)
        {
            Type? declaring = (instruction.operand as MethodBase)?.DeclaringType;
            string? name = declaring?.FullName;
            if (name == null)
                continue;
            switch (name)
            {
                case RebootTypeName:
                    present.Add(Owner.Reboot);
                    break;
                case FtlTypeName:
                    present.Add(Owner.Ftl);
                    break;
                case ExhaustTypeName:
                case ExhaustDispatchTypeName:
                    present.Add(Owner.Exhaust);
                    break;
                case EchoTypeName:
                    present.Add(Owner.Echo);
                    break;
            }
        }
        return present;
    }

    private static object RequireBinding(Owner owner) => owner switch
    {
        Owner.Reboot => BindReboot(),
        Owner.Ftl => BindFtl(),
        Owner.Exhaust => BindExhaust(),
        Owner.Echo => BindEcho(),
        _ => throw new InvalidOperationException("ContinuationIL: unapproved owner " + owner)
    };

    private static Type HelperType(string fullName)
    {
        Type? type = typeof(Spire1ContinuationIl).Assembly.GetType(fullName, throwOnError: false);
        if (type == null)
            throw new InvalidOperationException("ContinuationIL: owner helper type " + fullName +
                " is not present in the loaded module");
        return type;
    }

    private static RebootBinding BindReboot()
    {
        Type type = HelperType(RebootTypeName);
        return new RebootBinding(
            ExactStatic(type, "CheckInitial", typeof(void), typeof(object), typeof(int), typeof(int)),
            ExactStatic(type, "CheckContinuation", typeof(void), typeof(object), typeof(int),
                typeof(PlayerChoiceContext), typeof(AbstractModel)),
            ExactStatic(type, "CheckDrawContinuation", typeof(void), typeof(Player)),
            ExactStatic(type, "BeforeCommit", typeof(void), typeof(int), typeof(bool)));
    }

    private static FtlBinding BindFtl()
    {
        Type type = HelperType(FtlTypeName);
        return new FtlBinding(
            ExactStatic(type, "CheckDrawContinuation", typeof(void), typeof(Player)),
            ExactStatic(type, "ShouldStopAttackLoop", typeof(bool), typeof(AttackCommand)),
            ExactStatic(type, "CheckAttackIdentity", typeof(void), typeof(AttackCommand)),
            ExactStatic(type, "AddDrawn", typeof(Task<CardPileAddResult>), typeof(CardModel), typeof(CardPile),
                typeof(CardPilePosition), typeof(AbstractModel), typeof(bool), typeof(PlayerChoiceContext)),
            ExactStatic(type, "InvokeDrawn", typeof(void), typeof(CardModel), typeof(PlayerChoiceContext)),
            ExactStatic(type, "AfterCardPlayed", typeof(Task), typeof(ICombatState),
                typeof(PlayerChoiceContext), typeof(CardPlay)),
            ExactStatic(type, "ShouldDraw", typeof(bool), typeof(ICombatState), typeof(Player), typeof(bool),
                typeof(AbstractModel).MakeByRefType()),
            ExactStatic(type, "ShouldStopDrawLoop", typeof(bool), typeof(Player)),
            ExactStatic(type, "DrawFromUse", typeof(Task<IEnumerable<CardModel>>),
                typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool), typeof(CardPlay),
                typeof(MegaCrit.Sts2.Core.Models.Relics.IronClub)));
    }

    private static ExhaustBinding BindExhaust()
    {
        Type type = HelperType(ExhaustTypeName);
        Type dispatch = HelperType(ExhaustDispatchTypeName);
        return new ExhaustBinding(
            ExactStatic(type, "GuardAttack", typeof(void), typeof(AttackCommand)),
            ExactStatic(type, "GuardDamage", typeof(void), typeof(IEnumerable<DamageTargetData>)),
            ExactStatic(type, "GuardReward", typeof(void)),
            ExactStatic(type, "GuardExhaust", typeof(void)),
            ExactStatic(dispatch, "Guard", typeof(void)),
            ExactStatic(type, "GuardNative", typeof(void)),
            ExactStatic(type, "DamageFromAttack", typeof(Task<IEnumerable<DamageResult>>),
                typeof(PlayerChoiceContext), typeof(IEnumerable<DamageTargetData>), typeof(decimal), typeof(ValueProp),
                typeof(Creature), typeof(CardModel), typeof(CardPlay), typeof(ModifyDamageHookType),
                typeof(AttackCommand)),
            ExactStatic(type, "DispatchExhaust", typeof(Task), typeof(ICombatState), typeof(PlayerChoiceContext),
                typeof(CardModel), typeof(bool)),
            ExactStatic(type, "InvokeExhaustListener", typeof(Task), typeof(AbstractModel),
                typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)),
            ExactStatic(type, "ExhaustAdd", typeof(Task<CardPileAddResult>), typeof(CardModel), typeof(PileType),
                typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)),
            ExactStatic(type, "DrawShuffle", typeof(Task), typeof(PlayerChoiceContext), typeof(Player)),
            ExactStatic(type, "AfterCardDrawn", typeof(Task), typeof(ICombatState), typeof(PlayerChoiceContext),
                typeof(CardModel), typeof(bool)),
            ExactStatic(type, "ShuffleAdd", typeof(Task<IReadOnlyList<CardPileAddResult>>),
                typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel),
                typeof(bool), typeof(bool)),
            ExactStatic(type, "BeforeDamageReceived", typeof(Task), typeof(PlayerChoiceContext), typeof(IRunState),
                typeof(ICombatState), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature),
                typeof(CardModel)),
            ExactStatic(type, "AfterDamageGiven", typeof(Task), typeof(PlayerChoiceContext), typeof(ICombatState),
                typeof(Creature), typeof(DamageResult), typeof(ValueProp), typeof(Creature), typeof(CardModel)),
            ExactStatic(type, "AfterDamageReceived", typeof(Task), typeof(PlayerChoiceContext), typeof(IRunState),
                typeof(ICombatState), typeof(Creature), typeof(DamageResult), typeof(ValueProp), typeof(Creature),
                typeof(CardModel)),
            ExactStatic(type, "DamageKill", typeof(Task), typeof(IReadOnlyCollection<Creature>), typeof(CardModel),
                typeof(bool)),
            ExactStatic(type, "KillOne", typeof(Task), typeof(Creature), typeof(CardModel), typeof(bool), typeof(int)),
            ExactStatic(type, "KillCreature", typeof(Task), typeof(Creature), typeof(bool)),
            ExactStatic(type, "ShuffleNow", typeof(Task), typeof(PlayerChoiceContext), typeof(Player)),
            ExactStatic(type, "DrawAdd", typeof(Task<CardPileAddResult>), typeof(CardModel), typeof(CardPile),
                typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)));
    }

    private static EchoBinding BindEcho()
    {
        Type type = HelperType(EchoTypeName);
        return new EchoBinding(ExactStatic(type, "ModifyOne", typeof(int), typeof(AbstractModel),
            typeof(CardModel), typeof(Creature), typeof(int)));
    }

    private static void ValidateBoundBridges(Body body, Bindings bindings)
    {
        foreach (BridgeSite site in body.BridgeSites)
        {
            if (site.Form == BridgeForm.ComposedDrawAdd)
            {
                if (bindings.Exhaust != null)
                    ValidateSiteAbi(body, site, bindings);
                if (bindings.Ftl != null)
                    ValidateComposedAbi(body, site, bindings);
                continue;
            }
            if (site.Owner == Owner.Exhaust && bindings.Exhaust == null)
                continue;
            if (site.Owner == Owner.Ftl && bindings.Ftl == null)
                continue;
            if (site.Owner == Owner.Echo && bindings.Echo == null)
                continue;
            ValidateSiteAbi(body, site, bindings);
        }
    }

    private static void ValidateSiteAbi(Body body, BridgeSite site, Bindings bindings)
    {
        MethodInfo replacement = SiteMethod(bindings, site);
        Type[] originalParameters = Array.ConvertAll(site.Original.GetParameters(), parameter => parameter.ParameterType);
        ParameterInfo[] replacementParameters = replacement.GetParameters();
        bool shape;
        switch (site.Method)
        {
            case BridgeMethodId.ExhaustInvokeListener:
            case BridgeMethodId.EchoModifyOne:
                shape = replacementParameters.Length == originalParameters.Length + 1 &&
                    replacementParameters[0].ParameterType == typeof(AbstractModel) &&
                    replacement.ReturnType == site.Original.ReturnType;
                for (int i = 0; shape && i < originalParameters.Length; i++)
                    shape = replacementParameters[i + 1].ParameterType == originalParameters[i];
                break;
            case BridgeMethodId.ExhaustDamageFromAttack:
                shape = replacementParameters.Length == originalParameters.Length + 1 &&
                    replacementParameters[^1].ParameterType == typeof(AttackCommand) &&
                    replacement.ReturnType == site.Original.ReturnType;
                for (int i = 0; shape && i < originalParameters.Length; i++)
                    shape = replacementParameters[i].ParameterType == originalParameters[i];
                break;
            case BridgeMethodId.FtlAddDrawn:
                shape = replacementParameters.Length == originalParameters.Length + 1 &&
                    replacementParameters[^1].ParameterType == typeof(PlayerChoiceContext) &&
                    replacement.ReturnType == site.Original.ReturnType;
                for (int i = 0; shape && i < originalParameters.Length; i++)
                    shape = replacementParameters[i].ParameterType == originalParameters[i];
                break;
            case BridgeMethodId.FtlInvokeDrawn:
                shape = replacementParameters.Length == 2 &&
                    replacementParameters[0].ParameterType == typeof(CardModel) &&
                    replacementParameters[1].ParameterType == typeof(PlayerChoiceContext) &&
                    replacement.ReturnType == site.Original.ReturnType;
                break;
            case BridgeMethodId.FtlDrawFromUse:
                shape = replacementParameters.Length == originalParameters.Length + 2 &&
                    replacementParameters[^2].ParameterType == typeof(CardPlay) &&
                    replacementParameters[^1].ParameterType ==
                        typeof(MegaCrit.Sts2.Core.Models.Relics.IronClub) &&
                    replacement.ReturnType == site.Original.ReturnType;
                for (int i = 0; shape && i < originalParameters.Length; i++)
                    shape = replacementParameters[i].ParameterType == originalParameters[i];
                break;
            default:
                shape = replacementParameters.Length == originalParameters.Length &&
                    replacement.ReturnType == site.Original.ReturnType;
                for (int i = 0; shape && i < originalParameters.Length; i++)
                    shape = replacementParameters[i].ParameterType == originalParameters[i];
                break;
        }
        if (!shape)
            throw Error(body, site.OriginalIndex, site.Owner,
                "bridge replacement ABI does not consume the original arguments");
    }

    private static void ValidateComposedAbi(Body body, BridgeSite site, Bindings bindings)
    {
        MethodInfo ftl = bindings.Ftl!.AddDrawn;
        Type[] originalParameters = Array.ConvertAll(site.Original.GetParameters(), parameter => parameter.ParameterType);
        ParameterInfo[] replacement = ftl.GetParameters();
        bool shape = replacement.Length == originalParameters.Length + 1 &&
            replacement[^1].ParameterType == typeof(PlayerChoiceContext) &&
            ftl.ReturnType == site.Original.ReturnType;
        for (int i = 0; shape && i < originalParameters.Length; i++)
            shape = replacement[i].ParameterType == originalParameters[i];
        if (!shape)
            throw Error(body, site.OriginalIndex, Owner.Ftl, "composed AddDrawn ABI changed");
    }

    private static ParseOutcome Parse(Body body, Bindings bindings, IReadOnlyList<CodeInstruction> incoming,
        Owner? requesting, IReadOnlyCollection<Owner>? expected)
    {
        if (incoming.Count == 0)
            throw ParseError(body, -1, null, "effective stream is empty");
        var outcome = new ParseOutcome(body.Original.Count);
        int oi = 0;
        int ii = 0;
        while (oi < body.Original.Count)
        {
            bool initialInserted = false;
            if (body.Has(Owner.Reboot) && body.InitialAnchor == oi)
            {
                if (TryMatchBlock(body, bindings, BlockKind.RebootInitial, incoming, ii, 0, oi,
                        out BlockFact initial, out string initialReason))
                {
                    AddBlock(body, outcome, initial);
                    ii += initial.Length;
                    initialInserted = true;
                }
                else if (initial.Matched > 0)
                {
                    throw ParseError(body, oi, Owner.Reboot,
                        "initial guard block is incomplete or malformed: " + initialReason);
                }
            }

            bool commitInserted = false;
            if (body.Commits.TryGetValue(oi, out CommitSite? commit))
            {
                int flag = commit.StartsCardMove ? 1 : 0;
                if (TryMatchBlock(body, bindings, BlockKind.RebootCommit, incoming, ii, flag, oi,
                        out BlockFact commitFact, out string commitReason))
                {
                    AddBlock(body, outcome, commitFact);
                    ii += commitFact.Length;
                    commitInserted = true;
                }
                else if (commitFact.Matched > 0)
                {
                    throw ParseError(body, oi, Owner.Reboot, "pre-commit guard is incomplete: " + commitReason);
                }
            }

            if (TryMatchExternalMaxHand(body, oi, incoming, ii, out int maxCallOffset,
                    out int maxLength, out int maxMatched, out string maxReason))
            {
                outcome.OriginalToEffective[oi] = ii + maxCallOffset;
                ii += maxLength;
                oi++;
                continue;
            }
            if (maxMatched > 0)
                throw ParseError(body, oi, null,
                    "maxhand external edit is incomplete or changed: " + maxReason);

            if (body.BridgeByIndex.TryGetValue(oi, out BridgeSite? bridgeSite))
            {
                if (TryMatchBridge(body, bindings, bridgeSite, incoming, ii, out BridgeFact bridge,
                        out string bridgeReason))
                {
                    outcome.Bridges.Add(bridge);
                    outcome.OriginalToEffective[oi] = bridge.Start;
                    ii += bridge.Length;
                }
                else if (TryMatchOriginal(body, oi, incoming, ii, initialInserted, commitInserted,
                             out string originalReason))
                {
                    outcome.OriginalToEffective[oi] = ii;
                    ii++;
                }
                else
                {
                    throw ParseError(body, oi, bridgeSite.Owner,
                        "expected the original call or its exact named bridge; bridge: " + bridgeReason +
                        "; original: " + originalReason);
                }
            }
            else
            {
                if (!TryMatchOriginal(body, oi, incoming, ii, initialInserted, commitInserted, out string reason))
                    throw ParseError(body, oi, null, reason);
                outcome.OriginalToEffective[oi] = ii;
                ii++;
            }

            if (body.PostSlots.TryGetValue(oi, out PostSlot? slot))
            {
                var slotFact = new SlotFact { Slot = slot, Start = ii };
                while (ii < incoming.Count)
                {
                    BlockFact? parsed = null;
                    bool prefix = false;
                    foreach (BlockKind kind in slot.Allowed)
                    {
                        if (TryMatchBlock(body, bindings, kind, incoming, ii, 0, oi, out BlockFact candidate, out _))
                        {
                            parsed = candidate;
                            break;
                        }
                        if (candidate.Matched > 0)
                            prefix = true;
                    }
                    if (parsed == null)
                    {
                        if (StartsExpectedNext(body, bindings, oi, incoming, ii))
                            break;
                        if (prefix)
                            throw ParseError(body, oi, null, "incomplete guard block at post slot");
                        break;
                    }
                    foreach (BlockFact existing in slotFact.Blocks)
                        if (existing.Owner == parsed.Owner)
                            throw ParseError(body, oi, parsed.Owner, "duplicate guard at one post slot");
                    slotFact.Blocks.Add(parsed);
                    AddBlock(body, outcome, parsed);
                    ii += parsed.Length;
                }
                slotFact.End = ii;
                ValidateSlotOrder(body, slotFact);
                outcome.Slots.Add(slotFact);
                if (ii < incoming.Count && !StartsExpectedNext(body, bindings, oi, incoming, ii))
                    throw ParseError(body, oi, null, "unexpected instruction immediately after post slot");
            }
            oi++;
        }
        if (ii != incoming.Count)
            throw ParseError(body, body.Original.Count, null,
                "trailing instructions after the pinned original: " + (incoming.Count - ii));

        VerifyBranches(body, incoming, outcome);
        CheckOwners(body, outcome, requesting, expected);
        return outcome;
    }

    private static void AddBlock(Body body, ParseOutcome outcome, BlockFact block)
    {
        if (!body.Has(block.Owner))
            throw ParseError(body, block.OriginalIndex, block.Owner, "owner/body combination is not approved");
        outcome.Blocks.Add(block);
    }

    private static void ValidateSlotOrder(Body body, SlotFact slot)
    {
        BlockFact? ftl = null;
        foreach (BlockFact block in slot.Blocks)
            if (block.Owner == Owner.Ftl)
                ftl = block;
        if (ftl == null)
            return;
        if (slot.Slot.RequiredFtl is BlockKind required && ftl.Kind != required)
            throw ParseError(body, slot.Slot.OriginalIndex, Owner.Ftl,
                "FTL used " + KindName(ftl.Kind) + " at a slot pinned for " + KindName(required));
        if ((ftl.Kind == BlockKind.FtlAttackStop || ftl.Kind == BlockKind.FtlDrawStop) &&
            slot.Blocks[^1] != ftl)
            throw ParseError(body, slot.Slot.OriginalIndex, Owner.Ftl,
                "FTL conditional branch is not the last sibling guard in its slot");
    }

    private static bool StartsExpectedNext(Body body, Bindings bindings, int oi,
        IReadOnlyList<CodeInstruction> list, int index)
    {
        int next = oi + 1;
        if (next >= body.Original.Count)
            return false;
        if (TryMatchExternalMaxHand(body, next, list, index, out _, out _, out _, out _))
            return true;
        if (body.Has(Owner.Reboot) && body.InitialAnchor == next)
        {
            if (TryMatchBlock(body, bindings, BlockKind.RebootInitial, list, index, 0, next, out _, out _))
                return true;
            return TryMatchOriginal(body, next, list, index, false, false, out _);
        }
        if (body.Commits.TryGetValue(next, out CommitSite? commit))
        {
            if (TryMatchBlock(body, bindings, BlockKind.RebootCommit, list, index, commit.StartsCardMove ? 1 : 0,
                    next, out _, out _))
                return true;
            return TryMatchOriginal(body, next, list, index, false, false, out _);
        }
        if (body.BridgeByIndex.TryGetValue(next, out BridgeSite? bridge))
        {
            if (TryMatchBridge(body, bindings, bridge, list, index, out _, out _))
                return true;
            return TryMatchOriginal(body, next, list, index, false, false, out _);
        }
        return TryMatchOriginal(body, next, list, index, false, false, out _);
    }

    private static bool TryMatchExternalMaxHand(Body body, int oi, IReadOnlyList<CodeInstruction> list,
        int index, out int callOffset, out int length, out int matched, out string reason)
    {
        callOffset = 0;
        length = 0;
        matched = 0;
        reason = "";
        CodeInstruction original = body.Original[oi];
        if (!IsMaxHandGetter(original))
            return false;
        if (original.labels.Count != 0 || original.blocks.Count != 0)
            return false;

        int prefix = 0;
        if (body.Selector != null && body.Selector.FieldType == typeof(Player) &&
            index < list.Count && list[index].opcode == OpCodes.Ldarg_0)
        {
            matched = 1;
            if (list[index].labels.Count != 0 || list[index].blocks.Count != 0)
            {
                reason = "maxhand player-field prefix carries labels/EH";
                return false;
            }
            if (index + 1 >= list.Count || !Instr(list[index + 1], OpCodes.Ldfld, body.Selector))
            {
                reason = "maxhand player-field prefix is not the exact current player field";
                return false;
            }
            prefix = 2;
            matched = 2;
        }
        if (prefix == 0 && index < list.Count && list[index].opcode == OpCodes.Ldloc_2 &&
            list[index].labels.Count == 0 && list[index].blocks.Count == 0)
        {
            matched = 1;
            MethodBody? locals = body.Move.GetMethodBody();
            if (locals == null || locals.LocalVariables.Count <= 2 ||
                locals.LocalVariables[2].LocalType != typeof(Player))
            {
                reason = "maxhand player-local slot2 is not Player";
                return false;
            }
            prefix = 1;
            matched = 1;
        }
        if (prefix == 0)
        {
            matched = 0;
            return false;
        }

        callOffset = prefix;
        int call = index + prefix;
        if (call >= list.Count || !Instr(list[call], original.opcode, original.operand))
        {
            reason = "maxhand shifted original getter call is missing or changed";
            return false;
        }
        matched = prefix + 1;

        int suffix = call + 1;
        if (suffix >= list.Count || !IsMaxHandSuffix(list[suffix]))
        {
            reason = "maxhand BaseLib GetMaxHandSize suffix is missing or changed";
            return false;
        }
        matched = prefix + 2;
        length = prefix + 2;
        return true;
    }

    private static bool IsMaxHandGetter(CodeInstruction instruction)
    {
        if (instruction.opcode != OpCodes.Call || instruction.operand is not MethodInfo method)
            return false;
        return method.IsStatic && method.DeclaringType == typeof(CardPile) &&
            method.Name == "get_MaxCardsInHand" && method.ReturnType == typeof(int) &&
            method.GetParameters().Length == 0;
    }

    private static bool IsMaxHandSuffix(CodeInstruction instruction)
    {
        if (instruction.opcode != OpCodes.Call || instruction.labels.Count != 0 ||
            instruction.blocks.Count != 0 || instruction.operand is not MethodInfo method)
            return false;
        if (!method.IsStatic || method.Name != "GetMaxHandSize" ||
            method.ReturnType != typeof(int) ||
            method.DeclaringType?.FullName != "BaseLib.Patches.Hooks.MaxHandSizePatch" ||
            method.Module.Assembly.GetName().Name != "BaseLib" ||
            method.Module.ModuleVersionId != BaseLibMvid)
            return false;
        return ParameterTypesEqual(method, new[] { typeof(Player), typeof(int) });
    }

    private static bool TryMatchOriginal(Body body, int oi, IReadOnlyList<CodeInstruction> list, int index,
        bool initialInserted, bool commitInserted, out string reason)
    {
        reason = "";
        CodeInstruction original = body.Original[oi];
        if (index < 0 || index >= list.Count)
        {
            reason = "missing incoming instruction for original[" + oi + "]";
            return false;
        }
        CodeInstruction candidate = list[index];
        if (candidate.opcode != original.opcode)
        {
            reason = "opcode at original[" + oi + "] is " + candidate.opcode + ", pinned " + original.opcode;
            return false;
        }
        if (!OperandShapeEquals(original.operand, candidate.operand))
        {
            reason = "operand at original[" + oi + "] is " + Describe(candidate.operand) +
                ", pinned " + Describe(original.operand);
            return false;
        }
        int expectedLabels = commitInserted && body.Commits.ContainsKey(oi) ? 0 : original.labels.Count;
        if (candidate.labels.Count != expectedLabels)
        {
            reason = "label count at original[" + oi + "] is " + candidate.labels.Count + ", pinned " + expectedLabels;
            return false;
        }
        if (body.Has(Owner.Reboot) && body.InitialAnchor == oi && initialInserted)
        {
            if (candidate.blocks.Count != 0)
            {
                reason = "builder try opener is still attached to original[" + oi + "]";
                return false;
            }
        }
        else if (!BlocksEqual(original.blocks, candidate.blocks))
        {
            reason = "exception-block boundary at original[" + oi + "] changed";
            return false;
        }
        return true;
    }

    private static bool TryMatchBridge(Body body, Bindings bindings, BridgeSite site,
        IReadOnlyList<CodeInstruction> list, int index, out BridgeFact fact, out string reason)
    {
        fact = new BridgeFact { Site = site, Start = index };
        reason = "";
        if (site.Form == BridgeForm.ComposedDrawAdd)
        {
            if (bindings.Ftl != null && TryMatchLoader(body, bindings.Ftl.AddDrawn, site.LoaderField, list, index,
                    out int ftlLength))
            {
                fact.Form = BridgeForm.Loader;
                fact.Length = ftlLength;
                fact.Matched = ftlLength;
                fact.ComposedIsFtl = true;
                return true;
            }
            if (bindings.Exhaust != null && TryMatchDirect(body, site, bindings.Exhaust.DrawAdd, list, index))
            {
                fact.Form = BridgeForm.Direct;
                fact.Length = 1;
                fact.Matched = 1;
                fact.ComposedIsFtl = false;
                return true;
            }
            return false;
        }
        switch (site.Form)
        {
            case BridgeForm.Direct:
                if (bindings.Has(site.Owner) &&
                    TryMatchDirect(body, site, SiteMethod(bindings, site), list, index))
                {
                    fact.Form = BridgeForm.Direct;
                    fact.Length = 1;
                    fact.Matched = 1;
                    return true;
                }
                return false;
            case BridgeForm.Loader:
                if (bindings.Has(site.Owner) &&
                    TryMatchLoader(body, SiteMethod(bindings, site), site.LoaderField, list, index,
                        out int length))
                {
                    fact.Form = BridgeForm.Loader;
                    fact.Length = length;
                    fact.Matched = length;
                    return true;
                }
                return false;
            case BridgeForm.Loader2:
                if (bindings.Has(site.Owner) &&
                    TryMatchLoader2(body, site, SiteMethod(bindings, site), list, index, out int length2))
                {
                    fact.Form = BridgeForm.Loader2;
                    fact.Length = length2;
                    fact.Matched = length2;
                    return true;
                }
                return false;
            default:
                reason = "bridge form is not approved";
                return false;
        }
    }

    private static bool TryMatchDirect(Body body, BridgeSite site, MethodInfo replacement,
        IReadOnlyList<CodeInstruction> list, int index)
    {
        if (index >= list.Count)
            return false;
        CodeInstruction call = list[index];
        if (call.opcode != site.ReplacementOpcode || !Equals(call.operand, replacement))
            return false;
        CodeInstruction source = body.Original[site.OriginalIndex];
        if (call.labels.Count != source.labels.Count || !BlocksEqual(source.blocks, call.blocks))
            return false;
        return true;
    }

    private static bool TryMatchLoader(Body body, MethodInfo replacement, FieldInfo? field,
        IReadOnlyList<CodeInstruction> list, int index, out int length)
    {
        length = 3;
        if (index + 2 >= list.Count)
            return false;
        if (!Instr(list[index], OpCodes.Ldarg_0, null) ||
            !Instr(list[index + 1], OpCodes.Ldfld, field) ||
            !Instr(list[index + 2], OpCodes.Call, replacement))
            return false;
        return true;
    }

    private static bool TryMatchLoader2(Body body, BridgeSite site, MethodInfo replacement,
        IReadOnlyList<CodeInstruction> list, int index, out int length)
    {
        length = 5;
        if (site.LoaderField == null || site.LoaderField2 == null || index + 4 >= list.Count)
            return false;
        if (!Instr(list[index], OpCodes.Ldarg_0, null) ||
            !Instr(list[index + 1], OpCodes.Ldfld, site.LoaderField) ||
            !Instr(list[index + 2], OpCodes.Ldarg_0, null) ||
            !Instr(list[index + 3], OpCodes.Ldfld, site.LoaderField2) ||
            !Instr(list[index + 4], OpCodes.Call, replacement))
            return false;
        return true;
    }

    private static bool TryMatchBlock(Body body, Bindings bindings, BlockKind kind,
        IReadOnlyList<CodeInstruction> list, int index, int commitFlag, int anchor, out BlockFact fact,
        out string reason)
    {
        fact = new BlockFact
        {
            Owner = OwnerOfBlock(kind),
            Kind = kind,
            OriginalIndex = anchor,
            Start = index,
            Length = BlockLength(body, kind)
        };
        reason = "";
        fact.Matched = LoosePrefix(body, bindings, kind, list, index, commitFlag);
        if (fact.Matched < fact.Length)
        {
            reason = "matched " + fact.Matched + " of " + fact.Length + " instructions";
            return false;
        }
        if (index + fact.Length > list.Count)
        {
            reason = "truncated block";
            return false;
        }
        switch (kind)
        {
            case BlockKind.RebootInitial:
                if (list[index].opcode != OpCodes.Ldarg_0 ||
                    !OperandShapeEquals(list[index].operand, null) ||
                    list[index].labels.Count != 0 ||
                    !IsExactlyOpener(list[index].blocks) ||
                    !Instr(list[index + 1], OpCodes.Ldfld, body.Selector) ||
                    !Instr(list[index + 2], OpCodes.Ldc_I4, body.Id) ||
                    !Instr(list[index + 3], OpCodes.Ldarg_0, null) ||
                    !Instr(list[index + 4], OpCodes.Ldfld, body.StateField) ||
                    !Instr(list[index + 5], OpCodes.Call, bindings.Reboot!.CheckInitial))
                {
                    reason = "initial guard shape differs";
                    return false;
                }
                return true;
            case BlockKind.RebootPost:
            {
                int at = index;
                if (!Instr(list[at++], OpCodes.Ldarg_0, null) ||
                    !Instr(list[at++], OpCodes.Ldfld, body.Selector) ||
                    !Instr(list[at++], OpCodes.Ldc_I4, body.Id))
                {
                    reason = "post-continuation guard prefix differs";
                    return false;
                }
                if (body.ContextField != null)
                {
                    if (!Instr(list[at++], OpCodes.Ldarg_0, null) ||
                        !Instr(list[at++], OpCodes.Ldfld, body.ContextField))
                    {
                        reason = "post-continuation context field differs";
                        return false;
                    }
                }
                else if (!Instr(list[at++], OpCodes.Ldnull, null))
                {
                    reason = "post-continuation context null differs";
                    return false;
                }
                if (body.ModelField != null)
                {
                    if (!Instr(list[at++], OpCodes.Ldarg_0, null) ||
                        !Instr(list[at++], OpCodes.Ldfld, body.ModelField))
                    {
                        reason = "post-continuation model field differs";
                        return false;
                    }
                }
                else if (!Instr(list[at++], OpCodes.Ldnull, null))
                {
                    reason = "post-continuation model null differs";
                    return false;
                }
                if (!Instr(list[at], OpCodes.Call, bindings.Reboot!.CheckContinuation))
                {
                    reason = "post-continuation call differs";
                    return false;
                }
                return true;
            }
            case BlockKind.RebootDrawPost:
                if (!Instr(list[index], OpCodes.Ldarg_0, null) ||
                    !Instr(list[index + 1], OpCodes.Ldfld, body.Selector) ||
                    !Instr(list[index + 2], OpCodes.Call, bindings.Reboot!.CheckDrawContinuation))
                {
                    reason = "draw-continuation guard shape differs";
                    return false;
                }
                return true;
            case BlockKind.RebootCommit:
            {
                CodeInstruction original = body.Original[anchor];
                if (list[index].opcode != OpCodes.Ldc_I4 || list[index].operand is not int id || id != body.Id ||
                    list[index].blocks.Count != 0 || list[index].labels.Count != original.labels.Count ||
                    !Instr(list[index + 1], commitFlag == 1 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0, null) ||
                    !Instr(list[index + 2], OpCodes.Call, bindings.Reboot!.BeforeCommit))
                {
                    reason = "pre-commit guard shape differs";
                    return false;
                }
                return true;
            }
            case BlockKind.FtlDrawPost:
                if (!Instr(list[index], OpCodes.Ldarg_0, null) ||
                    !Instr(list[index + 1], OpCodes.Ldfld, body.Selector) ||
                    !Instr(list[index + 2], OpCodes.Call, bindings.Ftl!.CheckDrawContinuation))
                {
                    reason = "FTL draw guard shape differs";
                    return false;
                }
                return true;
            case BlockKind.FtlDrawStop:
                if (!Instr(list[index], OpCodes.Ldarg_0, null) ||
                    !Instr(list[index + 1], OpCodes.Ldfld, body.Selector) ||
                    !Instr(list[index + 2], OpCodes.Call, bindings.Ftl!.ShouldStopDrawLoop))
                {
                    reason = "FTL draw stop guard shape differs";
                    return false;
                }
                if (list[index + 3].opcode != OpCodes.Brtrue || list[index + 3].operand is not Label ||
                    list[index + 3].labels.Count != 0 || list[index + 3].blocks.Count != 0)
                {
                    reason = "FTL draw stop branch shape differs";
                    return false;
                }
                fact.BranchIndex = index + 3;
                return true;
            case BlockKind.FtlAttackStop:
                if (!Instr(list[index], OpCodes.Ldarg_0, null) ||
                    !Instr(list[index + 1], OpCodes.Ldfld, body.ThisField) ||
                    !Instr(list[index + 2], OpCodes.Call, bindings.Ftl!.ShouldStopAttackLoop))
                {
                    reason = "FTL attack stop guard shape differs";
                    return false;
                }
                if (list[index + 3].opcode != OpCodes.Brtrue || list[index + 3].operand is not Label ||
                    list[index + 3].labels.Count != 0 || list[index + 3].blocks.Count != 0)
                {
                    reason = "FTL attack stop branch shape differs";
                    return false;
                }
                fact.BranchIndex = index + 3;
                return true;
            case BlockKind.FtlAttackIdentity:
                if (!Instr(list[index], OpCodes.Ldarg_0, null) ||
                    !Instr(list[index + 1], OpCodes.Ldfld, body.ThisField) ||
                    !Instr(list[index + 2], OpCodes.Call, bindings.Ftl!.CheckAttackIdentity))
                {
                    reason = "FTL attack identity guard shape differs";
                    return false;
                }
                return true;
            case BlockKind.FtlLoopOr:
                if (!Instr(list[index], OpCodes.Ldarg_0, null) ||
                    !Instr(list[index + 1], OpCodes.Ldfld, body.ThisField) ||
                    !Instr(list[index + 2], OpCodes.Call, bindings.Ftl!.ShouldStopAttackLoop) ||
                    !Instr(list[index + 3], OpCodes.Or, null))
                {
                    reason = "FTL loop OR rewrite shape differs";
                    return false;
                }
                return true;
            case BlockKind.ExhaustGuardAttack:
                if (!Instr(list[index], OpCodes.Ldarg_0, null) ||
                    !Instr(list[index + 1], OpCodes.Ldfld, body.ThisField) ||
                    !Instr(list[index + 2], OpCodes.Call, bindings.Exhaust!.GuardAttack))
                {
                    reason = "Exhaust attack guard shape differs";
                    return false;
                }
                return true;
            case BlockKind.ExhaustGuardDamage:
                if (!Instr(list[index], OpCodes.Ldarg_0, null) ||
                    !Instr(list[index + 1], OpCodes.Ldfld, body.GuardField) ||
                    !Instr(list[index + 2], OpCodes.Call, bindings.Exhaust!.GuardDamage))
                {
                    reason = "Exhaust damage guard shape differs";
                    return false;
                }
                return true;
            case BlockKind.ExhaustGuardReward:
                if (!Instr(list[index], OpCodes.Call, bindings.Exhaust!.GuardReward))
                {
                    reason = "Exhaust reward guard shape differs";
                    return false;
                }
                return true;
            case BlockKind.ExhaustGuardExhaust:
                if (!Instr(list[index], OpCodes.Call, bindings.Exhaust!.GuardExhaust))
                {
                    reason = "Exhaust command guard shape differs";
                    return false;
                }
                return true;
            case BlockKind.ExhaustGuardDispatch:
                if (!Instr(list[index], OpCodes.Call, bindings.Exhaust!.GuardDispatch))
                {
                    reason = "Exhaust dispatch guard shape differs";
                    return false;
                }
                return true;
            case BlockKind.ExhaustGuardNative:
                if (!Instr(list[index], OpCodes.Call, bindings.Exhaust!.GuardNative))
                {
                    reason = "Exhaust native guard shape differs";
                    return false;
                }
                return true;
            default:
                reason = "block kind is not admitted here";
                return false;
        }
    }

    private static int LoosePrefix(Body body, Bindings bindings, BlockKind kind,
        IReadOnlyList<CodeInstruction> list, int index, int commitFlag)
    {
        int p = index;
        bool At(OpCode opcode, object? operand)
        {
            if (p >= list.Count)
                return false;
            CodeInstruction candidate = list[p];
            if (candidate.opcode != opcode || !OperandShapeEquals(candidate.operand, operand))
                return false;
            p++;
            return true;
        }
        bool Branch()
        {
            if (p >= list.Count)
                return false;
            CodeInstruction candidate = list[p];
            if (candidate.opcode != OpCodes.Brtrue || candidate.operand is not Label)
                return false;
            p++;
            return true;
        }
        switch (kind)
        {
            case BlockKind.RebootInitial:
            {
                RebootBinding? rebind = bindings.Reboot;
                if (rebind == null)
                    return 0;
                At(OpCodes.Ldarg_0, null);
                At(OpCodes.Ldfld, body.Selector);
                At(OpCodes.Ldc_I4, body.Id);
                At(OpCodes.Ldarg_0, null);
                At(OpCodes.Ldfld, body.StateField);
                At(OpCodes.Call, rebind.CheckInitial);
                break;
            }
            case BlockKind.RebootPost:
            {
                RebootBinding? rebind = bindings.Reboot;
                if (rebind == null)
                    return 0;
                At(OpCodes.Ldarg_0, null);
                At(OpCodes.Ldfld, body.Selector);
                At(OpCodes.Ldc_I4, body.Id);
                if (body.ContextField != null)
                {
                    At(OpCodes.Ldarg_0, null);
                    At(OpCodes.Ldfld, body.ContextField);
                }
                else
                {
                    At(OpCodes.Ldnull, null);
                }
                if (body.ModelField != null)
                {
                    At(OpCodes.Ldarg_0, null);
                    At(OpCodes.Ldfld, body.ModelField);
                }
                else
                {
                    At(OpCodes.Ldnull, null);
                }
                At(OpCodes.Call, rebind.CheckContinuation);
                break;
            }
            case BlockKind.RebootDrawPost:
            {
                RebootBinding? rebind = bindings.Reboot;
                if (rebind == null)
                    return 0;
                At(OpCodes.Ldarg_0, null);
                At(OpCodes.Ldfld, body.Selector);
                At(OpCodes.Call, rebind.CheckDrawContinuation);
                break;
            }
            case BlockKind.RebootCommit:
            {
                RebootBinding? rebind = bindings.Reboot;
                if (rebind == null)
                    return 0;
                At(OpCodes.Ldc_I4, body.Id);
                At(commitFlag == 1 ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0, null);
                At(OpCodes.Call, rebind.BeforeCommit);
                break;
            }
            case BlockKind.FtlDrawPost:
            {
                FtlBinding? ftl = bindings.Ftl;
                if (ftl == null)
                    return 0;
                At(OpCodes.Ldarg_0, null);
                At(OpCodes.Ldfld, body.Selector);
                At(OpCodes.Call, ftl.CheckDrawContinuation);
                break;
            }
            case BlockKind.FtlDrawStop:
            {
                FtlBinding? ftl = bindings.Ftl;
                if (ftl == null)
                    return 0;
                At(OpCodes.Ldarg_0, null);
                At(OpCodes.Ldfld, body.Selector);
                At(OpCodes.Call, ftl.ShouldStopDrawLoop);
                Branch();
                break;
            }
            case BlockKind.FtlAttackStop:
            {
                FtlBinding? ftl = bindings.Ftl;
                if (ftl == null)
                    return 0;
                At(OpCodes.Ldarg_0, null);
                At(OpCodes.Ldfld, body.ThisField);
                At(OpCodes.Call, ftl.ShouldStopAttackLoop);
                Branch();
                break;
            }
            case BlockKind.FtlAttackIdentity:
            {
                FtlBinding? ftl = bindings.Ftl;
                if (ftl == null)
                    return 0;
                At(OpCodes.Ldarg_0, null);
                At(OpCodes.Ldfld, body.ThisField);
                At(OpCodes.Call, ftl.CheckAttackIdentity);
                break;
            }
            case BlockKind.FtlLoopOr:
            {
                FtlBinding? ftl = bindings.Ftl;
                if (ftl == null)
                    return 0;
                At(OpCodes.Ldarg_0, null);
                At(OpCodes.Ldfld, body.ThisField);
                At(OpCodes.Call, ftl.ShouldStopAttackLoop);
                At(OpCodes.Or, null);
                break;
            }
            case BlockKind.ExhaustGuardAttack:
            {
                ExhaustBinding? exhaust = bindings.Exhaust;
                if (exhaust == null)
                    return 0;
                At(OpCodes.Ldarg_0, null);
                At(OpCodes.Ldfld, body.ThisField);
                At(OpCodes.Call, exhaust.GuardAttack);
                break;
            }
            case BlockKind.ExhaustGuardDamage:
            {
                ExhaustBinding? exhaust = bindings.Exhaust;
                if (exhaust == null)
                    return 0;
                At(OpCodes.Ldarg_0, null);
                At(OpCodes.Ldfld, body.GuardField);
                At(OpCodes.Call, exhaust.GuardDamage);
                break;
            }
            case BlockKind.ExhaustGuardReward:
            {
                ExhaustBinding? exhaust = bindings.Exhaust;
                if (exhaust == null)
                    return 0;
                At(OpCodes.Call, exhaust.GuardReward);
                break;
            }
            case BlockKind.ExhaustGuardExhaust:
            {
                ExhaustBinding? exhaust = bindings.Exhaust;
                if (exhaust == null)
                    return 0;
                At(OpCodes.Call, exhaust.GuardExhaust);
                break;
            }
            case BlockKind.ExhaustGuardDispatch:
            {
                ExhaustBinding? exhaust = bindings.Exhaust;
                if (exhaust == null)
                    return 0;
                At(OpCodes.Call, exhaust.GuardDispatch);
                break;
            }
            case BlockKind.ExhaustGuardNative:
            {
                ExhaustBinding? exhaust = bindings.Exhaust;
                if (exhaust == null)
                    return 0;
                At(OpCodes.Call, exhaust.GuardNative);
                break;
            }
        }
        return p - index;
    }

    private static void VerifyBranches(Body body, IReadOnlyList<CodeInstruction> incoming, ParseOutcome outcome)
    {
        Dictionary<Label, int> originalPlace = LabelPlacement(body.Original);
        Dictionary<Label, int> effectivePlace = LabelPlacement(incoming);

        var effectiveToOriginal = new int[incoming.Count];
        Array.Fill(effectiveToOriginal, -1);
        for (int oi = 0; oi < outcome.OriginalToEffective.Length; oi++)
        {
            int ei = outcome.OriginalToEffective[oi];
            if (ei < 0)
                throw ParseError(body, oi, null, "original instruction has no effective counterpart");
            effectiveToOriginal[ei] = oi;
        }
        foreach (BlockFact block in outcome.Blocks)
            if (block.Kind == BlockKind.RebootCommit)
                effectiveToOriginal[block.Start] = block.OriginalIndex;
        foreach (BridgeFact bridge in outcome.Bridges)
            for (int k = 0; k < bridge.Length; k++)
                effectiveToOriginal[bridge.Start + k] = bridge.Site.OriginalIndex;

        var initialStarts = new HashSet<int>();
        var commitStarts = new HashSet<int>();
        var bridgeStarts = new Dictionary<int, BridgeFact>();
        foreach (BlockFact block in outcome.Blocks)
        {
            if (block.Kind == BlockKind.RebootInitial)
                initialStarts.Add(block.Start);
            else if (block.Kind == BlockKind.RebootCommit)
                commitStarts.Add(block.Start);
        }
        foreach (BridgeFact bridge in outcome.Bridges)
            bridgeStarts.Add(bridge.Start, bridge);

        for (int i = 0; i < incoming.Count; i++)
        {
            CodeInstruction candidate = incoming[i];
            if (initialStarts.Contains(i))
            {
                if (candidate.labels.Count != 0)
                    throw ParseError(body, -1, Owner.Reboot, "initial guard carries labels");
                if (!IsExactlyOpener(candidate.blocks))
                    throw ParseError(body, -1, Owner.Reboot, "initial guard does not carry the transferred opener");
                continue;
            }
            if (commitStarts.Contains(i))
            {
                if (candidate.blocks.Count != 0)
                    throw ParseError(body, -1, Owner.Reboot, "pre-commit guard carries exception blocks");
                continue;
            }
            if (bridgeStarts.TryGetValue(i, out BridgeFact? bridge))
            {
                CodeInstruction source = body.Original[bridge.Site.OriginalIndex];
                if (bridge.Site.Form == BridgeForm.Direct)
                {
                    if (candidate.labels.Count != source.labels.Count ||
                        !BlocksEqual(source.blocks, candidate.blocks))
                        throw ParseError(body, bridge.Site.OriginalIndex, bridge.Site.Owner,
                            "named bridge did not preserve labels/exception blocks");
                }
                else if (candidate.labels.Count != 0 || candidate.blocks.Count != 0)
                {
                    throw ParseError(body, bridge.Site.OriginalIndex, bridge.Site.Owner,
                        "multi-instruction bridge loader carries labels/exception blocks");
                }
                continue;
            }
            if (effectiveToOriginal[i] >= 0)
                continue;
            if (candidate.labels.Count != 0)
                throw ParseError(body, -1, null, "label attached to inserted guard at effective index " + i);
            if (candidate.blocks.Count != 0)
                throw ParseError(body, -1, null, "exception block attached to inserted guard at effective index " + i);
        }

        for (int oi = 0; oi < body.Original.Count; oi++)
        {
            object? operand = body.Original[oi].operand;
            int ei = outcome.OriginalToEffective[oi];
            if (operand is Label)
            {
                int expected = ResolveTarget(body, oi, body.Original, originalPlace, oi);
                int actual = ResolveTarget(body, oi, incoming, effectivePlace, ei);
                int projected = ProjectTarget(body, actual, incoming.Count, effectiveToOriginal);
                if (projected != expected)
                    throw ParseError(body, oi, null,
                        "branch target moved: pinned original[" + expected + "], effective original[" +
                        projected + "]");
            }
            else if (operand is Label[] originalSwitch)
            {
                if (incoming[ei].operand is not Label[] effectiveSwitch ||
                    effectiveSwitch.Length != originalSwitch.Length)
                    throw ParseError(body, oi, null, "switch target count changed");
                for (int k = 0; k < originalSwitch.Length; k++)
                {
                    int expected = ResolveSingleTarget(body, oi, originalPlace, originalSwitch[k]);
                    int actual = ResolveSingleTarget(body, oi, effectivePlace, effectiveSwitch[k]);
                    int projected = ProjectTarget(body, actual, incoming.Count, effectiveToOriginal);
                    if (projected != expected)
                        throw ParseError(body, oi, null,
                            "switch target " + k + " moved: pinned original[" + expected +
                            "], effective original[" + projected + "]");
                }
            }
        }

        foreach (BlockFact block in outcome.Blocks)
        {
            int expectedTarget;
            string targetName;
            if (block.Kind == BlockKind.FtlAttackStop)
            {
                expectedTarget = body.AttackCleanup;
                targetName = "attack cleanup";
            }
            else if (block.Kind == BlockKind.FtlDrawStop)
            {
                expectedTarget = body.DrawResultAnchor;
                targetName = "draw result";
            }
            else
            {
                continue;
            }
            if (incoming[block.BranchIndex].operand is not Label label)
                throw ParseError(body, block.OriginalIndex, Owner.Ftl, "conditional guard branch has no label");
            if (!effectivePlace.TryGetValue(label, out int target))
                throw ParseError(body, block.OriginalIndex, Owner.Ftl,
                    "conditional guard branch target is not a placed cleanup label");
            int projected = ProjectTarget(body, target, incoming.Count, effectiveToOriginal);
            if (projected != expectedTarget)
                throw ParseError(body, block.OriginalIndex, Owner.Ftl,
                    "conditional guard does not branch to the pinned original " + targetName);
        }
    }

    private static int ResolveTarget(Body body, int originalIndex, IReadOnlyList<CodeInstruction> list,
        Dictionary<Label, int> placement, int instructionIndex)
    {
        if (list[instructionIndex].operand is not Label label)
            throw new InvalidOperationException("ContinuationIL internal: branch instruction has no label operand");
        if (placement.TryGetValue(label, out int target))
            return target;
        throw ParseError(body, originalIndex, null,
            "branch instruction " + instructionIndex + " targets an unplaced label");
    }

    private static int ResolveSingleTarget(Body body, int originalIndex, Dictionary<Label, int> placement,
        Label label)
    {
        if (placement.TryGetValue(label, out int target))
            return target;
        throw ParseError(body, originalIndex, null, "switch target label is unplaced");
    }

    private static int ProjectTarget(Body body, int effectiveTarget, int effectiveCount, int[] effectiveToOriginal)
    {
        if (effectiveTarget < 0 || effectiveTarget >= effectiveCount)
            throw new InvalidOperationException("ContinuationIL internal: branch target out of range");
        int projected = effectiveToOriginal[effectiveTarget];
        if (projected < 0)
            throw ParseError(body, -1, null, "branch target is an inserted guard, not an original instruction");
        return projected;
    }

    private static Dictionary<Label, int> LabelPlacement(IReadOnlyList<CodeInstruction> list)
    {
        var placement = new Dictionary<Label, int>();
        for (int i = 0; i < list.Count; i++)
        {
            foreach (Label label in list[i].labels)
            {
                if (!placement.TryAdd(label, i))
                    throw new InvalidOperationException(
                        "ContinuationIL: duplicate label placement at instruction " + i);
            }
        }
        return placement;
    }

    private static void CheckOwners(Body body, ParseOutcome outcome, Owner? requesting,
        IReadOnlyCollection<Owner>? expected)
    {
        HashSet<Owner>? expectedSet = null;
        if (expected != null)
        {
            expectedSet = new HashSet<Owner>(expected);
            if (expectedSet.Count != expected.Count)
                throw ParseError(body, -1, null, "expected-owner list contains duplicates");
        }

        var found = new Dictionary<Owner, List<(BlockKind Kind, int Anchor)>>();
        foreach (Owner owner in Enum.GetValues<Owner>())
            found[owner] = new List<(BlockKind Kind, int Anchor)>();
        foreach (BlockFact block in outcome.Blocks)
            found[block.Owner].Add((block.Kind, block.OriginalIndex));

        foreach (BridgeFact bridge in outcome.Bridges)
        {
            if (bridge.Site.Form == BridgeForm.ComposedDrawAdd)
                continue;
            found[bridge.Site.Owner].Add((BlockKind.Bridge, bridge.Site.OriginalIndex));
        }

        BridgeFact? composed = null;
        foreach (BridgeFact bridge in outcome.Bridges)
            if (bridge.Site.Form == BridgeForm.ComposedDrawAdd)
                composed = bridge;
        bool ftlOther = found[Owner.Ftl].Count != 0;
        bool exhaustOther = found[Owner.Exhaust].Count != 0;
        bool composedFtl = composed != null && composed.ComposedIsFtl;
        bool composedExhaust = composed != null && !composed.ComposedIsFtl;
        if (ftlOther && composed != null && !composedFtl)
            throw ParseError(body, composed.Site.OriginalIndex, Owner.Ftl,
                "FTL is present but the final Add site is not the exact FTL AddDrawn span");
        outcome.FtlPresent = ftlOther || composedFtl;
        outcome.ExhaustPresent = exhaustOther || composedExhaust;
        outcome.ComposedIsFtl = composedFtl;
        if (composedFtl)
        {
            found[Owner.Ftl].Add((BlockKind.DrawAddComposed, composed!.Site.OriginalIndex));
            if (outcome.ExhaustPresent)
                found[Owner.Exhaust].Add((BlockKind.DrawAddComposed, composed.Site.OriginalIndex));
        }
        else if (composedExhaust)
        {
            found[Owner.Exhaust].Add((BlockKind.DrawAddComposed, composed!.Site.OriginalIndex));
        }

        foreach (Owner owner in Enum.GetValues<Owner>())
        {
            List<(BlockKind Kind, int Anchor)> owned = found[owner];
            if (!body.Has(owner))
            {
                if (owned.Count != 0)
                    throw ParseError(body, owned[0].Anchor, owner, "owner/body combination is not approved");
                continue;
            }
            List<(BlockKind Kind, int Anchor)> required = Required(body, owner);
            if (requesting == owner)
            {
                if (owned.Count != 0)
                    throw ParseError(body, owned[0].Anchor, owner,
                        "requesting owner's edits are already present (duplicate)");
                continue;
            }
            if (expectedSet != null)
            {
                if (expectedSet.Contains(owner))
                {
                    if (!MatchesRequired(required, owned))
                        throw ParseError(body, -1, owner,
                            "expected owner has " + owned.Count + " of " + required.Count + " complete edits");
                }
                else if (owned.Count != 0)
                {
                    throw ParseError(body, owned[0].Anchor, owner,
                        "owner is present but not listed in expectedOwners");
                }
            }
            else if (owned.Count != 0 && !MatchesRequired(required, owned))
            {
                throw ParseError(body, owned[0].Anchor, owner,
                    "sibling owner has " + owned.Count + " of " + required.Count + " complete edits");
            }
        }
    }

    private static bool MatchesRequired(List<(BlockKind Kind, int Anchor)> required,
        List<(BlockKind Kind, int Anchor)> found)
    {
        if (required.Count != found.Count)
            return false;
        var used = new bool[required.Count];
        foreach ((BlockKind kind, int anchor) in found)
        {
            bool matched = false;
            for (int i = 0; i < required.Count; i++)
            {
                if (used[i] || required[i].Kind != kind || required[i].Anchor != anchor)
                    continue;
                used[i] = true;
                matched = true;
                break;
            }
            if (!matched)
                return false;
        }
        return true;
    }

    private static SlotFact FindSlot(ParseOutcome outcome, int originalIndex)
    {
        foreach (SlotFact slot in outcome.Slots)
            if (slot.Slot.OriginalIndex == originalIndex)
                return slot;
        throw new InvalidOperationException(
            "ContinuationIL internal: no parsed slot for original index " + originalIndex);
    }

    private static void AddAction(Dictionary<int, List<CodeInstruction>> actions, int index,
        List<CodeInstruction> instructions, Body body, Owner owner)
    {
        if (!actions.TryAdd(index, instructions))
            throw Error(body, index, owner, "internal rewrite insertion point collision");
    }

    private static List<CodeInstruction> EmitInitial(Body body, Bindings bindings)
    {
        if (body.Selector == null || body.StateField == null)
            throw Error(body, body.InitialAnchor, Owner.Reboot, "initial guard field identity is missing");
        RebootBinding reboot = bindings.Reboot
            ?? throw Error(body, body.InitialAnchor, Owner.Reboot, "Reboot binding is missing");
        return new List<CodeInstruction>
        {
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, body.Selector),
            new(OpCodes.Ldc_I4, body.Id),
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, body.StateField),
            new(OpCodes.Call, reboot.CheckInitial)
        };
    }

    private static List<CodeInstruction> EmitCommit(Body body, Bindings bindings, bool startsCardMove)
    {
        RebootBinding reboot = bindings.Reboot
            ?? throw Error(body, -1, Owner.Reboot, "Reboot binding is missing");
        return new List<CodeInstruction>
        {
            new(OpCodes.Ldc_I4, body.Id),
            new(startsCardMove ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0),
            new(OpCodes.Call, reboot.BeforeCommit)
        };
    }

    private static List<CodeInstruction> EmitBlock(Body body, Bindings bindings, BlockKind kind, Label? cleanup)
    {
        switch (kind)
        {
            case BlockKind.RebootInitial:
                return EmitInitial(body, bindings);
            case BlockKind.RebootCommit:
                throw Error(body, -1, Owner.Reboot, "commit block requires its startsCardMove flag");
            case BlockKind.RebootPost:
            {
                RebootBinding reboot = bindings.Reboot
                    ?? throw Error(body, -1, Owner.Reboot, "Reboot binding is missing");
                var result = new List<CodeInstruction>
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, body.Selector),
                    new(OpCodes.Ldc_I4, body.Id)
                };
                if (body.ContextField != null)
                {
                    result.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    result.Add(new CodeInstruction(OpCodes.Ldfld, body.ContextField));
                }
                else
                {
                    result.Add(new CodeInstruction(OpCodes.Ldnull));
                }
                if (body.ModelField != null)
                {
                    result.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    result.Add(new CodeInstruction(OpCodes.Ldfld, body.ModelField));
                }
                else
                {
                    result.Add(new CodeInstruction(OpCodes.Ldnull));
                }
                result.Add(new CodeInstruction(OpCodes.Call, reboot.CheckContinuation));
                return result;
            }
            case BlockKind.RebootDrawPost:
            {
                RebootBinding reboot = bindings.Reboot
                    ?? throw Error(body, -1, Owner.Reboot, "Reboot binding is missing");
                return new List<CodeInstruction>
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, body.Selector),
                    new(OpCodes.Call, reboot.CheckDrawContinuation)
                };
            }
            case BlockKind.FtlDrawPost:
            {
                FtlBinding ftl = bindings.Ftl ?? throw Error(body, -1, Owner.Ftl, "FTL binding is missing");
                return new List<CodeInstruction>
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, body.Selector),
                    new(OpCodes.Call, ftl.CheckDrawContinuation)
                };
            }
            case BlockKind.FtlDrawStop:
            {
                FtlBinding ftl = bindings.Ftl ?? throw Error(body, -1, Owner.Ftl, "FTL binding is missing");
                if (cleanup == null)
                    throw Error(body, body.DrawStopAnchor, Owner.Ftl,
                        "draw cleanup label is missing for the conditional control edit");
                return new List<CodeInstruction>
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, body.Selector),
                    new(OpCodes.Call, ftl.ShouldStopDrawLoop),
                    new(OpCodes.Brtrue, cleanup.Value)
                };
            }
            case BlockKind.FtlAttackStop:
            {
                FtlBinding ftl = bindings.Ftl ?? throw Error(body, -1, Owner.Ftl, "FTL binding is missing");
                if (cleanup == null)
                    throw Error(body, body.AttackDeadSite, Owner.Ftl,
                        "cleanup label is missing for the conditional guard");
                return new List<CodeInstruction>
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, body.ThisField),
                    new(OpCodes.Call, ftl.ShouldStopAttackLoop),
                    new(OpCodes.Brtrue, cleanup.Value)
                };
            }
            case BlockKind.FtlAttackIdentity:
            {
                FtlBinding ftl = bindings.Ftl ?? throw Error(body, -1, Owner.Ftl, "FTL binding is missing");
                return new List<CodeInstruction>
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, body.ThisField),
                    new(OpCodes.Call, ftl.CheckAttackIdentity)
                };
            }
            case BlockKind.FtlLoopOr:
                return EmitLoopOr(body, bindings);
            case BlockKind.ExhaustGuardAttack:
            {
                ExhaustBinding exhaust = bindings.Exhaust
                    ?? throw Error(body, -1, Owner.Exhaust, "Exhaust binding is missing");
                return new List<CodeInstruction>
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, body.ThisField),
                    new(OpCodes.Call, exhaust.GuardAttack)
                };
            }
            case BlockKind.ExhaustGuardDamage:
            {
                ExhaustBinding exhaust = bindings.Exhaust
                    ?? throw Error(body, -1, Owner.Exhaust, "Exhaust binding is missing");
                return new List<CodeInstruction>
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, body.GuardField),
                    new(OpCodes.Call, exhaust.GuardDamage)
                };
            }
            case BlockKind.ExhaustGuardReward:
            {
                ExhaustBinding exhaust = bindings.Exhaust
                    ?? throw Error(body, -1, Owner.Exhaust, "Exhaust binding is missing");
                return new List<CodeInstruction> { new(OpCodes.Call, exhaust.GuardReward) };
            }
            case BlockKind.ExhaustGuardExhaust:
            {
                ExhaustBinding exhaust = bindings.Exhaust
                    ?? throw Error(body, -1, Owner.Exhaust, "Exhaust binding is missing");
                return new List<CodeInstruction> { new(OpCodes.Call, exhaust.GuardExhaust) };
            }
            case BlockKind.ExhaustGuardDispatch:
            {
                ExhaustBinding exhaust = bindings.Exhaust
                    ?? throw Error(body, -1, Owner.Exhaust, "Exhaust binding is missing");
                return new List<CodeInstruction> { new(OpCodes.Call, exhaust.GuardDispatch) };
            }
            case BlockKind.ExhaustGuardNative:
            {
                ExhaustBinding exhaust = bindings.Exhaust
                    ?? throw Error(body, -1, Owner.Exhaust, "Exhaust binding is missing");
                return new List<CodeInstruction> { new(OpCodes.Call, exhaust.GuardNative) };
            }
            default:
                throw Error(body, -1, OwnerOfBlock(kind), "block kind is not a guard form");
        }
    }

    private static List<CodeInstruction> EmitLoopOr(Body body, Bindings bindings)
    {
        FtlBinding ftl = bindings.Ftl ?? throw Error(body, body.AttackDeadSite, Owner.Ftl, "FTL binding is missing");
        return new List<CodeInstruction>
        {
            new(OpCodes.Ldarg_0),
            new(OpCodes.Ldfld, body.ThisField),
            new(OpCodes.Call, ftl.ShouldStopAttackLoop),
            new(OpCodes.Or)
        };
    }

    private static List<CodeInstruction> EmitBridgeSite(Body body, Bindings bindings, BridgeSite site)
    {
        MethodInfo replacement = SiteMethod(bindings, site);
        switch (site.Form)
        {
            case BridgeForm.Direct:
                return new List<CodeInstruction> { new(site.ReplacementOpcode, replacement) };
            case BridgeForm.Loader:
                return new List<CodeInstruction>
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, site.LoaderField),
                    new(OpCodes.Call, replacement)
                };
            case BridgeForm.Loader2:
                return new List<CodeInstruction>
                {
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, site.LoaderField),
                    new(OpCodes.Ldarg_0),
                    new(OpCodes.Ldfld, site.LoaderField2),
                    new(OpCodes.Call, replacement)
                };
            default:
                throw Error(body, site.OriginalIndex, site.Owner, "bridge form is not emitted directly");
        }
    }

    private static List<CodeInstruction> EmitComposedBridge(Body body, Bindings bindings, BridgeSite site,
        Owner owner)
    {
        if (owner == Owner.Ftl)
        {
            FtlBinding ftl = bindings.Ftl
                ?? throw Error(body, site.OriginalIndex, Owner.Ftl, "FTL binding is missing");
            return new List<CodeInstruction>
            {
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, site.LoaderField),
                new(OpCodes.Call, ftl.AddDrawn)
            };
        }
        ExhaustBinding exhaust = bindings.Exhaust
            ?? throw Error(body, site.OriginalIndex, Owner.Exhaust, "Exhaust binding is missing");
        return new List<CodeInstruction> { new(OpCodes.Call, exhaust.DrawAdd) };
    }

    private static Label CleanupLabel(Body body, IReadOnlyList<CodeInstruction> incoming, ParseOutcome outcome)
    {
        int at = outcome.OriginalToEffective[body.AttackCleanup];
        CodeInstruction cleanup = incoming[at];
        if (cleanup.labels.Count == 0)
            throw Error(body, body.AttackCleanup, Owner.Ftl,
                "original cleanup label is missing from the incoming stream");
        return cleanup.labels[0];
    }

    private static Label DrawCleanupLabel(Body body, IReadOnlyList<CodeInstruction> incoming, ParseOutcome outcome)
    {
        int at = outcome.OriginalToEffective[body.DrawResultAnchor];
        CodeInstruction cleanup = incoming[at];
        if (cleanup.labels.Count == 0)
            throw Error(body, body.DrawResultAnchor, Owner.Ftl,
                "original draw result label is missing from the incoming stream");
        return cleanup.labels[0];
    }

    private static Owner OwnerOfBlock(BlockKind kind) => kind switch
    {
        BlockKind.RebootInitial or BlockKind.RebootPost or BlockKind.RebootDrawPost or BlockKind.RebootCommit =>
            Owner.Reboot,
        BlockKind.FtlDrawPost or BlockKind.FtlDrawStop or BlockKind.FtlAttackStop or
            BlockKind.FtlAttackIdentity or BlockKind.FtlLoopOr =>
            Owner.Ftl,
        BlockKind.ExhaustGuardAttack or BlockKind.ExhaustGuardDamage or BlockKind.ExhaustGuardReward or
            BlockKind.ExhaustGuardExhaust or BlockKind.ExhaustGuardDispatch or BlockKind.ExhaustGuardNative or
            BlockKind.Bridge or BlockKind.DrawAddComposed => Owner.Exhaust,
        BlockKind.EchoBridge => Owner.Echo,
        _ => throw new InvalidOperationException("ContinuationIL: unknown block kind " + kind)
    };

    private static string KindName(BlockKind kind) => kind switch
    {
        BlockKind.RebootInitial => "reboot.initial",
        BlockKind.RebootPost => "reboot.post",
        BlockKind.RebootDrawPost => "reboot.draw",
        BlockKind.RebootCommit => "reboot.commit",
        BlockKind.FtlDrawPost => "ftl.draw",
        BlockKind.FtlDrawStop => "ftl.draw.stop.control",
        BlockKind.FtlAttackStop => "ftl.attack.stop",
        BlockKind.FtlAttackIdentity => "ftl.attack.identity",
        BlockKind.FtlLoopOr => "ftl.attack.loop",
        BlockKind.ExhaustGuardAttack => "exhaust.guard.attack",
        BlockKind.ExhaustGuardDamage => "exhaust.guard.damage",
        BlockKind.ExhaustGuardReward => "exhaust.guard.reward",
        BlockKind.ExhaustGuardExhaust => "exhaust.guard.exhaust",
        BlockKind.ExhaustGuardDispatch => "exhaust.guard.dispatch",
        BlockKind.ExhaustGuardNative => "exhaust.guard.native",
        BlockKind.Bridge => "bridge",
        BlockKind.DrawAddComposed => "draw-add-composed",
        BlockKind.EchoBridge => "echo.bridge.count",
        _ => throw new InvalidOperationException("ContinuationIL: unknown block kind " + kind)
    };

    private static int BlockLength(Body body, BlockKind kind) => kind switch
    {
        BlockKind.RebootInitial => 6,
        BlockKind.RebootPost => 3 + (body.ContextField != null ? 2 : 1) +
            (body.ModelField != null ? 2 : 1) + 1,
        BlockKind.RebootDrawPost => 3,
        BlockKind.RebootCommit => 3,
        BlockKind.FtlDrawPost => 3,
        BlockKind.FtlDrawStop => 4,
        BlockKind.FtlAttackStop => 4,
        BlockKind.FtlAttackIdentity => 3,
        BlockKind.FtlLoopOr => 4,
        BlockKind.ExhaustGuardAttack => 3,
        BlockKind.ExhaustGuardDamage => 3,
        BlockKind.ExhaustGuardReward => 1,
        BlockKind.ExhaustGuardExhaust => 1,
        BlockKind.ExhaustGuardDispatch => 1,
        BlockKind.ExhaustGuardNative => 1,
        BlockKind.Bridge => 1,
        BlockKind.DrawAddComposed => 1,
        BlockKind.EchoBridge => 1,
        _ => throw new InvalidOperationException("ContinuationIL: unknown block kind " + kind)
    };

    private static bool TryGetAwaitResult(CodeInstruction instruction, out MethodInfo result)
    {
        result = null!;
        if (instruction.opcode != OpCodes.Call || instruction.operand is not MethodInfo method)
            return false;
        Type? declaring = method.DeclaringType;
        if (declaring == null)
            return false;
        bool awaiter = declaring == typeof(TaskAwaiter) ||
            (declaring.IsGenericType && declaring.GetGenericTypeDefinition() == typeof(TaskAwaiter<>));
        if (!awaiter || method.Name != "GetResult" || method.GetParameters().Length != 0)
            return false;
        result = method;
        return true;
    }

    private static int EhDepth(IReadOnlyList<CodeInstruction> list, int index)
    {
        int depth = 0;
        for (int i = 0; i <= index; i++)
        {
            foreach (ExceptionBlock block in list[i].blocks)
            {
                if (block.blockType == ExceptionBlockType.BeginExceptionBlock)
                    depth++;
                else if (block.blockType == ExceptionBlockType.EndExceptionBlock)
                    depth--;
            }
        }
        return depth;
    }

    private static bool IsExactlyOpener(List<ExceptionBlock> blocks)
        => blocks.Count == 1 && blocks[0].blockType == ExceptionBlockType.BeginExceptionBlock &&
           blocks[0].catchType == typeof(object);

    private static bool BlocksEqual(List<ExceptionBlock> left, List<ExceptionBlock> right)
    {
        if (left.Count != right.Count)
            return false;
        for (int i = 0; i < left.Count; i++)
        {
            if (left[i].blockType != right[i].blockType)
                return false;
            if (!Equals(left[i].catchType, right[i].catchType))
                return false;
        }
        return true;
    }

    private static bool Instr(CodeInstruction instruction, OpCode opcode, object? operand)
        => instruction.opcode == opcode && instruction.labels.Count == 0 && instruction.blocks.Count == 0 &&
           OperandShapeEquals(instruction.operand, operand);

    private static bool OperandShapeEquals(object? left, object? right)
    {
        if (left == null || right == null)
            return left == null && right == null;
        if (left is Label || right is Label)
            return left is Label && right is Label;
        if (left is Label[] || right is Label[])
            return left is Label[] && right is Label[];
        if (left is LocalVariableInfo leftLocal && right is LocalVariableInfo rightLocal)
            return leftLocal.LocalIndex == rightLocal.LocalIndex && leftLocal.LocalType == rightLocal.LocalType;
        if (left is MemberInfo leftMember && right is MemberInfo rightMember)
            return leftMember.MemberType == rightMember.MemberType && leftMember.Equals(rightMember);
        if (left is string leftText && right is string rightText)
            return string.Equals(leftText, rightText, StringComparison.Ordinal);
        if (left.GetType() != right.GetType())
            return false;
        return left.Equals(right);
    }

    private static string Describe(object? operand) => operand switch
    {
        null => "null",
        MethodInfo method => "method " + method.DeclaringType?.FullName + "." + method.Name,
        FieldInfo field => "field " + field.DeclaringType?.FullName + "." + field.Name,
        LocalVariableInfo local => "local[" + local.LocalIndex + "]:" + local.LocalType,
        Type type => "type " + type,
        string text => "\"" + text + "\"",
        _ => operand.GetType().Name + ":" + operand
    };

    private static MethodInfo? ExactMethod(Type owner, string name, Type? result, bool? requireStatic,
        Type[] parameters)
    {
        MethodInfo? found = null;
        foreach (MethodInfo method in owner.GetMethods(DeclaredAll))
        {
            if (method.Name != name || method.DeclaringType != owner)
                continue;
            if (method.IsGenericMethod || method.IsGenericMethodDefinition)
                continue;
            if ((method.CallingConvention & CallingConventions.VarArgs) != 0)
                continue;
            if (result != null && method.ReturnType != result)
                continue;
            if (requireStatic.HasValue && method.IsStatic != requireStatic.Value)
                continue;
            if (!ParameterTypesEqual(method, parameters))
                continue;
            if (found != null)
                throw new InvalidOperationException("ContinuationIL ambiguous exact member " +
                    owner.FullName + "." + name);
            found = method;
        }
        return found;
    }

    private static MethodInfo? ExactMember(Type type, string name, Type[] parameters)
        => ExactMethod(type, name, null, null, parameters);

    private static MethodInfo? ExactPropertyGetter(Type type, string property, Type result)
    {
        PropertyInfo? info = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic);
        if (info == null || info.PropertyType != result)
            return null;
        return info.GetMethod;
    }

    private static MethodInfo ExactStatic(Type owner, string name, Type result, params Type[] parameters)
    {
        MethodInfo? method = ExactMethod(owner, name, result, true, parameters);
        if (method == null)
            throw new InvalidOperationException("ContinuationIL is missing exact helper " + owner.FullName + "." + name);
        return method;
    }

    private static MethodInfo ExactInstance(Type owner, string name, Type result, params Type[] parameters)
    {
        MethodInfo? method = ExactMethod(owner, name, result, false, parameters);
        if (method == null)
            throw new InvalidOperationException("ContinuationIL is missing exact host member " + owner.FullName + "." + name);
        return method;
    }

    private static MethodInfo HostBridgeOriginal(HostBridgeId id) => id switch
    {
        HostBridgeId.CardPileAdd5 => ExactStatic(typeof(CardPileCmd), "Add", typeof(Task<CardPileAddResult>),
            typeof(CardModel), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool)),
        HostBridgeId.CardPileAddPileType5 => ExactStatic(typeof(CardPileCmd), "Add",
            typeof(Task<CardPileAddResult>), typeof(CardModel), typeof(PileType), typeof(CardPilePosition),
            typeof(AbstractModel), typeof(bool)),
        HostBridgeId.CardPileDraw4 => ExactStatic(typeof(CardPileCmd), "Draw",
            typeof(Task<IEnumerable<CardModel>>), typeof(PlayerChoiceContext), typeof(decimal), typeof(Player),
            typeof(bool)),
        HostBridgeId.HookShouldDraw => ExactStatic(typeof(Hook), "ShouldDraw", typeof(bool),
            typeof(ICombatState), typeof(Player), typeof(bool), typeof(AbstractModel).MakeByRefType()),
        HostBridgeId.ShuffleIfNecessary => ExactStatic(typeof(CardPileCmd), "ShuffleIfNecessary", typeof(Task),
            typeof(PlayerChoiceContext), typeof(Player)),
        HostBridgeId.HookAfterCardDrawn => ExactStatic(typeof(Hook), "AfterCardDrawn", typeof(Task),
            typeof(ICombatState), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)),
        HostBridgeId.CardModelInvokeDrawn => ExactInstance(typeof(CardModel), "InvokeDrawn", typeof(void)),
        HostBridgeId.CardPileAdd6 => ExactStatic(typeof(CardPileCmd), "Add",
            typeof(Task<IReadOnlyList<CardPileAddResult>>), typeof(IEnumerable<CardModel>), typeof(CardPile),
            typeof(CardPilePosition), typeof(AbstractModel), typeof(bool), typeof(bool)),
        HostBridgeId.HookBeforeDamageReceived => ExactStatic(typeof(Hook), "BeforeDamageReceived", typeof(Task),
            typeof(PlayerChoiceContext), typeof(IRunState), typeof(ICombatState), typeof(Creature), typeof(decimal),
            typeof(ValueProp), typeof(Creature), typeof(CardModel)),
        HostBridgeId.HookAfterDamageGiven => ExactStatic(typeof(Hook), "AfterDamageGiven", typeof(Task),
            typeof(PlayerChoiceContext), typeof(ICombatState), typeof(Creature), typeof(DamageResult),
            typeof(ValueProp), typeof(Creature), typeof(CardModel)),
        HostBridgeId.HookAfterDamageReceived => ExactStatic(typeof(Hook), "AfterDamageReceived", typeof(Task),
            typeof(PlayerChoiceContext), typeof(IRunState), typeof(ICombatState), typeof(Creature),
            typeof(DamageResult), typeof(ValueProp), typeof(Creature), typeof(CardModel)),
        HostBridgeId.CreatureKill3 => ExactStatic(typeof(CreatureCmd), "Kill", typeof(Task),
            typeof(IReadOnlyCollection<Creature>), typeof(CardModel), typeof(bool)),
        HostBridgeId.KillWithout4 => ExactStatic(typeof(CreatureCmd), "KillWithoutCheckingWinCondition",
            typeof(Task), typeof(Creature), typeof(CardModel), typeof(bool), typeof(int)),
        HostBridgeId.CreatureKill2 => ExactStatic(typeof(CreatureCmd), "Kill", typeof(Task), typeof(Creature),
            typeof(bool)),
        HostBridgeId.CardPileShuffle2 => ExactStatic(typeof(CardPileCmd), "Shuffle", typeof(Task),
            typeof(PlayerChoiceContext), typeof(Player)),
        HostBridgeId.HookAfterCardPlayed => ExactStatic(typeof(Hook), "AfterCardPlayed", typeof(Task),
            typeof(ICombatState), typeof(PlayerChoiceContext), typeof(CardPlay)),
        HostBridgeId.HookAfterCardExhausted4 => ExactStatic(typeof(Hook), "AfterCardExhausted", typeof(Task),
            typeof(ICombatState), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)),
        HostBridgeId.AbstractModelListener3 => ExactInstance(typeof(AbstractModel), "AfterCardExhausted",
            typeof(Task), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool)),
        HostBridgeId.CreatureDamage8 => ExactStatic(typeof(CreatureCmd), "Damage",
            typeof(Task<IEnumerable<DamageResult>>), typeof(PlayerChoiceContext),
            typeof(IEnumerable<DamageTargetData>), typeof(decimal), typeof(ValueProp), typeof(Creature),
            typeof(CardModel), typeof(CardPlay), typeof(ModifyDamageHookType)),
        HostBridgeId.AbstractModelCount3 => ExactInstance(typeof(AbstractModel), "ModifyCardPlayCount", typeof(int),
            typeof(CardModel), typeof(Creature), typeof(int)),
        _ => throw new InvalidOperationException("ContinuationIL: unknown host bridge")
    };

    private static OpCode HostBridgeOpcode(HostBridgeId id) => id switch
    {
        HostBridgeId.CardModelInvokeDrawn or HostBridgeId.AbstractModelListener3 or
            HostBridgeId.AbstractModelCount3 => OpCodes.Callvirt,
        _ => OpCodes.Call
    };

    private static MethodInfo SiteMethod(Bindings bindings, BridgeSite site) => site.Method switch
    {
        BridgeMethodId.ExhaustAdd or BridgeMethodId.ExhaustDrawShuffle or
        BridgeMethodId.ExhaustAfterCardDrawn or BridgeMethodId.ExhaustShuffleAdd or
        BridgeMethodId.ExhaustBeforeDamageReceived or BridgeMethodId.ExhaustAfterDamageGiven or
        BridgeMethodId.ExhaustAfterDamageReceived or BridgeMethodId.ExhaustDamageKill or
        BridgeMethodId.ExhaustKillOne or BridgeMethodId.ExhaustKillCreature or
        BridgeMethodId.ExhaustShuffleNow or BridgeMethodId.ExhaustDrawAdd or
        BridgeMethodId.ExhaustDispatch or BridgeMethodId.ExhaustInvokeListener or
        BridgeMethodId.ExhaustDamageFromAttack => bindings.Exhaust?.Resolve(site.Method)
            ?? throw new InvalidOperationException("ContinuationIL: Exhaust binding is missing"),
        BridgeMethodId.FtlAddDrawn or BridgeMethodId.FtlInvokeDrawn or BridgeMethodId.FtlAfterCardPlayed or
        BridgeMethodId.FtlShouldDraw or BridgeMethodId.FtlDrawFromUse =>
            site.Method switch
            {
                BridgeMethodId.FtlAddDrawn => bindings.Ftl?.AddDrawn
                    ?? throw new InvalidOperationException("ContinuationIL: FTL binding is missing"),
                BridgeMethodId.FtlInvokeDrawn => bindings.Ftl?.InvokeDrawn
                    ?? throw new InvalidOperationException("ContinuationIL: FTL binding is missing"),
                BridgeMethodId.FtlShouldDraw => bindings.Ftl?.ShouldDraw
                    ?? throw new InvalidOperationException("ContinuationIL: FTL binding is missing"),
                BridgeMethodId.FtlDrawFromUse => bindings.Ftl?.DrawFromUse
                    ?? throw new InvalidOperationException("ContinuationIL: FTL binding is missing"),
                _ => bindings.Ftl?.AfterCardPlayed
                    ?? throw new InvalidOperationException("ContinuationIL: FTL binding is missing")
            },
        BridgeMethodId.EchoModifyOne => bindings.Echo?.ModifyOne
            ?? throw new InvalidOperationException("ContinuationIL: Echo binding is missing"),
        _ => throw new InvalidOperationException("ContinuationIL: unknown bridge method")
    };

    private static InvalidOperationException Error(Body? body, int originalIndex, Owner? owner, string message)
    {
        string prefix = body == null
            ? "ContinuationIL"
            : "ContinuationIL body " + body.Id + " (" + (body.Move.DeclaringType?.FullName ?? body.Move.Name) + ")";
        string where = originalIndex >= 0 ? " original[" + originalIndex + "]" : "";
        string who = owner.HasValue ? " owner=" + owner.Value : "";
        return new InvalidOperationException(prefix + where + who + ": " + message);
    }

    private static InvalidOperationException ParseError(Body body, int originalIndex, Owner? owner, string message)
        => Error(body, originalIndex, owner, message);

    private static InvalidOperationException OriginalError(Body body, int originalIndex, string message)
        => Error(body, originalIndex, null, "pinned original: " + message);

    private static InvalidOperationException SpecError(Spec spec, string message)
        => new("ContinuationIL body " + spec.Id + " (" + spec.Type.FullName + "." + spec.Name + "): " + message);

    private static Spec[] BuildSpecs()
    {
        return new[]
        {
            new Spec
            {
                Id = 0, Type = typeof(CardPileCmd), Name = "DrawInternal",
                Parameters = new Type[] { typeof(PlayerChoiceContext), typeof(decimal), typeof(Player), typeof(bool) },
                Return = typeof(Task<IEnumerable<CardModel>>),
                State = "MegaCrit.Sts2.Core.Commands.CardPileCmd+<DrawInternal>d__21",
                MoveToken = 0x0600c285, IlBytes = 1122,
                Sha = "f4ebd8d5848885739cea3532b201fd7f412eb6c660ab168e0eabb43560f855f1",
                SelectorName = "player", SelectorType = typeof(Player),
                ContextName = "choiceContext", ContextType = typeof(PlayerChoiceContext),
                Emitters = new Owner[] { Owner.Reboot, Owner.Ftl, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(4, 3, 1, typeof(CardPileAddResult))
            },
            new Spec
            {
                Id = 1, Type = typeof(CardPileCmd), Name = "Add",
                Parameters = new Type[] { typeof(IEnumerable<CardModel>), typeof(CardPile), typeof(CardPilePosition), typeof(AbstractModel), typeof(bool), typeof(bool) },
                Return = typeof(Task<IReadOnlyList<CardPileAddResult>>),
                State = "MegaCrit.Sts2.Core.Commands.CardPileCmd+<Add>d__10",
                MoveToken = 0x0600c269, IlBytes = 2398,
                Sha = "55d31ae34a2040cf83e542cb99169ded865e867d546582645d38c6b81cd0389a",
                SelectorName = "newPile", SelectorType = typeof(CardPile),
                Emitters = new Owner[] { Owner.Reboot, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(4, -1, -1, null),
                CommitPins = new[]
                {
                    new CommitPin(typeof(CardModel), "RemoveFromCurrentPile", new[] { typeof(bool) }, true),
                    new CommitPin(typeof(CardPile), "AddInternal", new[] { typeof(CardModel), typeof(int), typeof(bool) }, false)
                }
            },
            new Spec
            {
                Id = 2, Type = typeof(CardPileCmd), Name = "Shuffle",
                Parameters = new Type[] { typeof(PlayerChoiceContext), typeof(Player) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Commands.CardPileCmd+<Shuffle>d__22",
                MoveToken = 0x0600c291, IlBytes = 1341,
                Sha = "652e92488b7d0cf9fdf3b17da904b6ba6132a8bf06206060e6e98bf530823681",
                SelectorName = "player", SelectorType = typeof(Player),
                ContextName = "choiceContext", ContextType = typeof(PlayerChoiceContext),
                Emitters = new Owner[] { Owner.Reboot, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(5, -1, -1, null)
            },
            new Spec
            {
                Id = 3, Type = typeof(CreatureCmd), Name = "GainBlock",
                Parameters = new Type[] { typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardPlay), typeof(bool) },
                Return = typeof(Task<decimal>),
                State = "MegaCrit.Sts2.Core.Commands.CreatureCmd+<GainBlock>d__21",
                MoveToken = 0x0600c316, IlBytes = 1017,
                Sha = "47cc524563cba0476c1fcafb5fb0d050a7219b974b5ef366d3777edff3f48da4",
                SelectorName = "creature", SelectorType = typeof(Creature),
                Emitters = new Owner[] { Owner.Reboot, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(5, -1, -1, null),
                CommitPins = new[]
                {
                    new CommitPin(typeof(Creature), "GainBlockInternal", new[] { typeof(decimal) }, false)
                }
            },
            new Spec
            {
                Id = 4, Type = typeof(CreatureCmd), Name = "Damage",
                Parameters = new Type[] { typeof(PlayerChoiceContext), typeof(IEnumerable<DamageTargetData>), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay), typeof(ModifyDamageHookType) },
                Return = typeof(Task<IEnumerable<DamageResult>>),
                State = "MegaCrit.Sts2.Core.Commands.CreatureCmd+<Damage>d__13",
                MoveToken = 0x0600c302, IlBytes = 4725,
                Sha = "b2b078615ad7a5592c4664fde1b0333acedaee6f544bc88c215b98c01aeb29cd",
                SelectorName = "dealer", SelectorType = typeof(Creature),
                ContextName = "choiceContext", ContextType = typeof(PlayerChoiceContext),
                Emitters = new Owner[] { Owner.Reboot, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(12, -1, -1, null),
                GuardFieldName = "targetData", GuardFieldType = typeof(IEnumerable<DamageTargetData>),
                CommitPins = new[]
                {
                    new CommitPin(typeof(Creature), "DamageBlockInternal", new[] { typeof(decimal), typeof(ValueProp) }, false),
                    new CommitPin(typeof(Creature), "LoseHpInternal", new[] { typeof(decimal), typeof(ValueProp) }, false)
                }
            },
            new Spec
            {
                Id = 5, Type = typeof(CreatureCmd), Name = "Kill",
                Parameters = new Type[] { typeof(IReadOnlyCollection<Creature>), typeof(CardModel), typeof(bool) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Commands.CreatureCmd+<Kill>d__17",
                MoveToken = 0x0600c322, IlBytes = 696,
                Sha = "dffa70afc916e45eb9af51e0e7d4fdb2c9c908add8e9a4f134c75f58dded646e",
                SelectorName = "creatures", SelectorType = typeof(IReadOnlyCollection<Creature>),
                Emitters = new Owner[] { Owner.Reboot, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(1, -1, -1, null)
            },
            new Spec
            {
                Id = 6, Type = typeof(CreatureCmd), Name = "KillWithoutCheckingWinCondition",
                Parameters = new Type[] { typeof(Creature), typeof(CardModel), typeof(bool), typeof(int) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Commands.CreatureCmd+<KillWithoutCheckingWinCondition>d__18",
                MoveToken = 0x0600c324, IlBytes = 2460,
                Sha = "97cf8e1c08c172a6e755e3eb55367fc56272e744abd1f35db85dc9a0ddd7dabf",
                SelectorName = "creature", SelectorType = typeof(Creature),
                Emitters = new Owner[] { Owner.Reboot, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(11, -1, -1, null)
            },
            new Spec
            {
                Id = 7, Type = typeof(Hook), Name = "AfterShuffle",
                Parameters = new Type[] { typeof(ICombatState), typeof(PlayerChoiceContext), typeof(Player) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterShuffle>d__76",
                MoveToken = 0x0600bebf, IlBytes = 312,
                Sha = "54b24c6c6b53ad71b869af1ec44c9e74df340067292006372af2f934e6419196",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ContextName = "choiceContext", ContextType = typeof(PlayerChoiceContext),
                ModelName = "<model>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 8, Type = typeof(Hook), Name = "AfterCardDrawn",
                Parameters = new Type[] { typeof(ICombatState), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterCardDrawn>d__14",
                MoveToken = 0x0600be5d, IlBytes = 589,
                Sha = "53412e2f446c82e6aa6f27517a7cacff90ff94c3a58032e2e95e7fe980cd068a",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ContextName = "choiceContext", ContextType = typeof(PlayerChoiceContext),
                ModelName = "<model>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(2, -1, -1, null)
            },
            new Spec
            {
                Id = 9, Type = typeof(Hook), Name = "AfterCardChangedPiles",
                Parameters = new Type[] { typeof(IRunState), typeof(ICombatState), typeof(CardModel), typeof(PileType), typeof(AbstractModel) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterCardChangedPiles>d__12",
                MoveToken = 0x0600be59, IlBytes = 533,
                Sha = "64a7340d864ed7f2554d0ab14c256c2c446aeaf2d091c0e57baa5b40fcbb7534",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ModelName = "<model>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 10, Type = typeof(Hook), Name = "BeforeBlockGained",
                Parameters = new Type[] { typeof(ICombatState), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<BeforeBlockGained>d__9",
                MoveToken = 0x0600becf, IlBytes = 290,
                Sha = "af324b6e0abec2dbc6ed0f86f3880e498dc86e258c6249f5cb7915f966d6c70b",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ModelName = "<model>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 11, Type = typeof(Hook), Name = "AfterModifyingBlockAmount",
                Parameters = new Type[] { typeof(ICombatState), typeof(decimal), typeof(CardModel), typeof(CardPlay), typeof(IEnumerable<AbstractModel>) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterModifyingBlockAmount>d__43",
                MoveToken = 0x0600be87, IlBytes = 306,
                Sha = "7e86b10e388a85164e1f99e019a66a3d16d29d365707272a76c763341e0ad770",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ModelName = "<modifier>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 12, Type = typeof(Hook), Name = "AfterBlockGained",
                Parameters = new Type[] { typeof(ICombatState), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(CardModel) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterBlockGained>d__10",
                MoveToken = 0x0600be57, IlBytes = 290,
                Sha = "c6e1a14270526b7f19e666323e150cde3edfb5e4187340b04b693d8ea747756e",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ModelName = "<model>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 13, Type = typeof(Hook), Name = "AfterModifyingDamageAmount",
                Parameters = new Type[] { typeof(IRunState), typeof(ICombatState), typeof(CardModel), typeof(IEnumerable<AbstractModel>) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterModifyingDamageAmount>d__46",
                MoveToken = 0x0600be8d, IlBytes = 297,
                Sha = "ddb5e6addeacf20d347bf24f872445523f0d5e1d63451de8cbcf918b3f492f00",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ModelName = "<modifier>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 14, Type = typeof(Hook), Name = "BeforeDamageReceived",
                Parameters = new Type[] { typeof(PlayerChoiceContext), typeof(IRunState), typeof(ICombatState), typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<BeforeDamageReceived>d__28",
                MoveToken = 0x0600bedb, IlBytes = 345,
                Sha = "fe248e515cde05369b0f75ce7dd21396e2f0e7f18e6f427f00cdd5038b4dad5a",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ContextName = "choiceContext", ContextType = typeof(PlayerChoiceContext),
                ModelName = "<model>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(1, -1, -1, null)
            },
            new Spec
            {
                Id = 15, Type = typeof(Hook), Name = "AfterModifyingHpLostBeforeOsty",
                Parameters = new Type[] { typeof(IRunState), typeof(ICombatState), typeof(IEnumerable<AbstractModel>) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterModifyingHpLostBeforeOsty>d__50",
                MoveToken = 0x0600be97, IlBytes = 291,
                Sha = "b1c093a2b435896fcbf6b928e22b3fd06f5077311e55744955c1984b3f46c792",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ModelName = "<modifier>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 16, Type = typeof(Hook), Name = "AfterModifyingHpLostAfterOsty",
                Parameters = new Type[] { typeof(IRunState), typeof(ICombatState), typeof(IEnumerable<AbstractModel>) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterModifyingHpLostAfterOsty>d__51",
                MoveToken = 0x0600be95, IlBytes = 291,
                Sha = "420a8591ff35a00e36bc19cd38dec2ca6a587f376abccf9d38740e49062db67c",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ModelName = "<modifier>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 17, Type = typeof(Hook), Name = "AfterBlockBroken",
                Parameters = new Type[] { typeof(ICombatState), typeof(PlayerChoiceContext), typeof(Creature), typeof(Creature) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterBlockBroken>d__7",
                MoveToken = 0x0600be53, IlBytes = 284,
                Sha = "700c7ca4eeddd62cec25d96b1ffcad6d3073f1765a6ccd8fb62af491848c957b",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ContextName = "choiceContext", ContextType = typeof(PlayerChoiceContext),
                ModelName = "<model>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 18, Type = typeof(Hook), Name = "AfterCurrentHpChanged",
                Parameters = new Type[] { typeof(IRunState), typeof(ICombatState), typeof(Creature), typeof(decimal) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterCurrentHpChanged>d__26",
                MoveToken = 0x0600be6d, IlBytes = 284,
                Sha = "07ea731c72190225ad403631027eea28d1d0498f45a373a0224b53c8235f182f",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ModelName = "<model>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 19, Type = typeof(Hook), Name = "AfterDamageGiven",
                Parameters = new Type[] { typeof(PlayerChoiceContext), typeof(ICombatState), typeof(Creature), typeof(DamageResult), typeof(ValueProp), typeof(Creature), typeof(CardModel) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterDamageGiven>d__27",
                MoveToken = 0x0600be6f, IlBytes = 339,
                Sha = "fb110c811ce9e616755d1abc96b861355285e71f777506e1237a1b1273fadcd1",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ContextName = "choiceContext", ContextType = typeof(PlayerChoiceContext),
                ModelName = "<model>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(1, -1, -1, null)
            },
            new Spec
            {
                Id = 20, Type = typeof(Hook), Name = "AfterDamageReceived",
                Parameters = new Type[] { typeof(PlayerChoiceContext), typeof(IRunState), typeof(ICombatState), typeof(Creature), typeof(DamageResult), typeof(ValueProp), typeof(Creature), typeof(CardModel) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterDamageReceived>d__29",
                MoveToken = 0x0600be71, IlBytes = 643,
                Sha = "3e4d0073a66ff509d85e5604613e5ad9f75f1a3f20baea85b58fbf2cd97ec93c",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ContextName = "choiceContext", ContextType = typeof(PlayerChoiceContext),
                ModelName = "<model>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(2, -1, -1, null)
            },
            new Spec
            {
                Id = 21, Type = typeof(Hook), Name = "BeforeDeath",
                Parameters = new Type[] { typeof(IRunState), typeof(ICombatState), typeof(Creature) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<BeforeDeath>d__30",
                MoveToken = 0x0600bedd, IlBytes = 278,
                Sha = "ca7f194c0bccdfeac911b5fb6c32e99a8df54c55f65fcb130139efc9216ed15e",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ModelName = "<model>5__3", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 22, Type = typeof(Hook), Name = "AfterDeath",
                Parameters = new Type[] { typeof(IRunState), typeof(ICombatState), typeof(Creature), typeof(CardModel), typeof(bool), typeof(float) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterDeath>d__31",
                MoveToken = 0x0600be73, IlBytes = 381,
                Sha = "dec1a425dd4436a639df54f03cf1b481a14226ed9a229aee92441501c3660c8c",
                SelectorName = "combatState", SelectorType = typeof(ICombatState),
                ModelName = "<model>5__4", ModelType = typeof(AbstractModel),
                Emitters = new Owner[] { Owner.Reboot }
            },
            new Spec
            {
                Id = 23, Type = typeof(AttackCommand), Name = "Execute",
                Parameters = new Type[] { typeof(PlayerChoiceContext) },
                Return = typeof(Task<AttackCommand>),
                Static = false,
                State = "MegaCrit.Sts2.Core.Commands.Builders.AttackCommand+<Execute>d__91",
                MoveToken = 100713377, IlBytes = 2315,
                Sha = "bfaf245d804e26f689a9930172d9bbb3c3d6ce406ab3879ee213b4c11c10b1a5",
                Emitters = new Owner[] { Owner.Ftl, Owner.Exhaust },
                AwaitExpectation = new AwaitPin(7, 6, 1, typeof(IEnumerable<DamageResult>)),
                ThisName = "<>4__this", ThisType = typeof(AttackCommand)
            },
            new Spec
            {
                Id = 24, Type = typeof(CardCmd), Name = "Exhaust",
                Parameters = new Type[] { typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool), typeof(bool) },
                Return = typeof(Task<CardPileAddResult?>),
                State = "MegaCrit.Sts2.Core.Commands.CardCmd+<Exhaust>d__6",
                MoveToken = 100713024, IlBytes = 478,
                Sha = "f4ac6a976e612134bcc1b0d584e55005f1b4de23a80d8e34f5f06cb2be965fe9",
                Emitters = new Owner[] { Owner.Exhaust },
                AwaitExpectation = new AwaitPin(2, 1, 1, typeof(CardPileAddResult))
            },
            new Spec
            {
                Id = 25, Type = typeof(Hook), Name = "AfterCardExhausted",
                Parameters = new Type[] { typeof(ICombatState), typeof(PlayerChoiceContext), typeof(CardModel), typeof(bool) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Hooks.Hook+<AfterCardExhausted>d__16",
                MoveToken = 100712033, IlBytes = 318,
                Sha = "1ea8baf0fcc6ee325e2fc27c3f9a170cb06974ddd2bec0f967c80a4de35870be",
                Emitters = new Owner[] { Owner.Exhaust },
                AwaitExpectation = new AwaitPin(1, 1, 0, null)
            },
            new Spec
            {
                Id = 26, Type = typeof(Hook), Name = "ModifyCardPlayCount",
                Parameters = new Type[] { typeof(ICombatState), typeof(CardModel), typeof(int), typeof(Creature), typeof(List<AbstractModel>).MakeByRefType() },
                Return = typeof(int),
                MoveToken = 0x060053a5, IlBytes = 78, Sha = "b1fc035277777414760022b0b7940966ef407498b750b7fc7ff7f0552ce41e53",
                Emitters = new Owner[] { Owner.Echo }
            },
            new Spec
            {
                Id = 27, Type = typeof(CardModel), Name = "OnPlayWrapper",
                Parameters = new Type[] { typeof(PlayerChoiceContext), typeof(Creature), typeof(bool), typeof(ResourceInfo), typeof(bool) },
                Return = typeof(Task),
                Static = false,
                EntryToken = 0x06001a21, EntryIlBytes = 97,
                EntrySha = "3b20da91f5e60d98d3d6dbada36cd5266edc937767e8683634c1a88bbf6ba5a4",
                State = "MegaCrit.Sts2.Core.Models.CardModel+<OnPlayWrapper>d__359",
                MoveToken = 0x0600a7b9, IlBytes = 3918,
                Sha = "591eef3300ad7e9edcf755aa59f45611e7e19b90d55d8a26512d423053c712aa",
                Emitters = new Owner[] { Owner.Ftl }
            },
            new Spec
            {
                Id = 28, Type = typeof(CardPileCmd), Name = "ShuffleIfNecessary",
                Parameters = new Type[] { typeof(PlayerChoiceContext), typeof(Player) },
                Return = typeof(Task),
                State = "MegaCrit.Sts2.Core.Commands.CardPileCmd+<ShuffleIfNecessary>d__25",
                MoveToken = 100713109, IlBytes = 314,
                Sha = "52b400574a4d3e5200268abd8c8988e2020a9c8860347542c0c7aa1493f8f4e2",
                Emitters = new Owner[] { Owner.Exhaust },
                AwaitExpectation = new AwaitPin(2, -1, -1, null)
            },
            new Spec
            {
                Id = 29, Type = typeof(MegaCrit.Sts2.Core.Models.Relics.IronClub), Name = "AfterCardPlayed",
                Parameters = new Type[] { typeof(PlayerChoiceContext), typeof(CardPlay) },
                Return = typeof(Task),
                Static = false,
                EntryToken = 0x06002057, EntryIlBytes = 71,
                EntrySha = "de65e6419359ec401ef75cf4254c7725b3ce8abb540e6996e26403acdb6c027b",
                State = "MegaCrit.Sts2.Core.Models.Relics.IronClub+<AfterCardPlayed>d__19",
                MoveToken = 0x0600a937, IlBytes = 275,
                Sha = "0d336bff912d59c2d24d631ff86d2e833888fb44568bc422fb8a14e2d9d7806f",
                Emitters = new Owner[] { Owner.Ftl },
                ModelName = "cardPlay", ModelType = typeof(CardPlay),
                ThisName = "<>4__this", ThisType = typeof(MegaCrit.Sts2.Core.Models.Relics.IronClub)
            }
        };
    }
}
