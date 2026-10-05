using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BaseLib.Utils.Attributes;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Interop;

/// <summary>
/// Optional, reflection-only bridge to the standalone Forms mod. Spire1 never takes a compile-time
/// or metadata reference to Forms: the assembly is found by simple name, the entry point type and
/// every method signature are validated explicitly, and only then are delegates published.
///
/// Fail-closed contract:
///   * Forms absent and this run did not select the form modifier -> IsSelected returns false and the
///     ordinary Spire1 stance path continues untouched.
///   * This run selected the form modifier, but the entry point cannot be bound, the Forms runtime
///     reports IsAvailable == false (Retryable/Terminal/ShuttingDown), or a forwarded call fails ->
///     IsSelected/Enter/Exit/KindOf/CurrentKind throw explicitly; the run never silently falls back
///     to ordinary stance rules.
///   * Signature validation success and runtime availability are separate facts. A valid signature
///     is cached so it is not re-reflected on every call, but IsAvailable is read from the Forms
///     entry point before every use; a runtime Retryable/Terminal/ShuttingDown state is never hidden
///     by the signature cache.
///   * Assembly identity is re-checked before reusing a cached binding. A same-simple-name assembly
///     conflict, a collectible assembly that is no longer loaded, or any other identity change
///     fails closed. This bridge does not claim true in-process hot replacement; restart the game
///     when Forms was replaced or unloaded.
///
/// Return values of KindOf/CurrentKind follow the stable protocol: 0 None, 1 Calm, 2 Wrath, 3 Divinity.
/// </summary>
internal static class FormsCompatibilityBridge
{
    private const string FormsAssemblySimpleName = "Forms";
    private const string EntryPointTypeFullName = "Forms.FormsCode.Interop.FormsRuntimeEntryPoint";
    private const string FormStanceModifierTypeFullName = "Forms.FormsCode.FormStanceModifier";
    private const string LegacyFormStanceModifierCustomId = "SPIRE1-FORM_STANCE_MODIFIER";

    internal const int KindNone = 0;
    internal const int KindCalm = 1;
    internal const int KindWrath = 2;
    internal const int KindDivinity = 3;

    private static readonly object ProbeLock = new();
    private static int _assemblyLoadRevision;
    private static int _probedRevision = -1;
    private static bool _signatureValid;
    private static string _disabledReason = "Forms entry point has not been probed yet.";
    private static string _lastLoggedDisabledReason = "";
    private static Bridge? _bridge;

    static FormsCompatibilityBridge()
    {
        try
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }
        catch (Exception)
        {
            // AssemblyLoad subscription is only an optimisation: a failed subscription leaves the
            // bridge probing on demand, which is still correct.
        }
    }

    private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
    {
        try
        {
            if (string.Equals(args.LoadedAssembly.GetName().Name, FormsAssemblySimpleName, StringComparison.Ordinal))
            {
                // Notification only: never touch Godot or Harmony from this arbitrary load thread.
                Interlocked.Increment(ref _assemblyLoadRevision);
            }
        }
        catch (Exception)
        {
            // A malformed/reflection-only assembly must not break the load notification thread.
        }
    }

    /// <summary>
    /// True only while a signature-valid bridge is bound to the currently loaded Forms assembly and
    /// the Forms runtime itself reports IsAvailable. The runtime getter is read on every call, so a
    /// cached signature never masks a Retryable/Terminal/ShuttingDown transition.
    /// </summary>
    internal static bool IsAvailable
    {
        get
        {
            Bridge? bridge = GetUsableBridge();
            if (bridge is null)
            {
                return false;
            }

            try
            {
                return bridge.IsAvailable();
            }
            catch (Exception e)
            {
                MarkCallFailure("IsAvailable", e);
                return false;
            }
        }
    }

    internal static string DisabledReason
    {
        get
        {
            GetUsableBridge();
            return DisabledReasonSnapshot();
        }
    }

    /// <summary>
    /// True only when the current run carries the exact standalone Forms form-stance modifier
    /// (type full name or the preserved old CustomID) and the bridge is bound and runtime-available.
    /// The run modifier identity is checked first, without requiring the Forms assembly to be
    /// loaded, so a selected save cannot silently degrade to ordinary rules just because the
    /// assembly is absent or assembly enumeration fails. A selected run with an unusable bridge
    /// throws. If the bridge is usable but Forms reports IsSelected=false for an identity-confirmed
    /// run, that selection disagreement also throws (fail closed); IsSelected never returns false
    /// for a run that carries the exact form-stance identity.
    /// </summary>
    internal static bool IsSelected(Player player)
    {
        if (player is null)
        {
            return false;
        }

        // Ordinary path: no exact Forms form-stance modifier in this run. This check is independent
        // of whether the Forms assembly is currently loaded.
        if (!HasFormsDeclaredModifier(player))
        {
            return false;
        }

        Bridge? bridge = GetUsableBridge();
        if (bridge is null)
        {
            throw new InvalidOperationException(
                "Spire1 Forms compatibility bridge unavailable for a selected form run: "
                + DisabledReasonSnapshot());
        }

        bool selected;
        try
        {
            selected = bridge.IsSelected(player);
        }
        catch (Exception e)
        {
            MarkCallFailure("IsSelected", e);
            throw new InvalidOperationException(
                "Spire1 Forms compatibility bridge failed during IsSelected: "
                + e.GetType().Name + ": " + e.Message, e);
        }

        // Forms 侧 FormStanceMode.IsSelected 用的是 `modifier is FormStanceModifier` 类型谓词,
        // 与本桥的 "全名或旧 CustomID" 谓词不完全等价. 本局既然已经命中精确身份
        // (HasFormsDeclaredModifier == true), Forms 却报告未选中, 就是身份判定分歧; 此时绝不
        // 静默回落到普通姿态规则, 必须显式失败 (fail closed).
        if (!selected)
        {
            throw new InvalidOperationException(
                "Forms runtime reported IsSelected=false for a run whose modifier carries the exact "
                + "standalone Forms form-stance identity (" + LegacyFormStanceModifierCustomId
                + "); selection disagreement is fail-closed, not an ordinary-rules fallback.");
        }

        return true;
    }

    /// <summary>
    /// True only for a modifier that is exactly the standalone Forms form-stance modifier: either
    /// the canonical type full name or the preserved CustomID used by old saves. A future/other
    /// modifier declared by the Forms assembly is not treated as a form-run selection.
    /// </summary>
    private static bool HasFormsDeclaredModifier(Player? player)
    {
        if (player is null)
        {
            return false;
        }

        var runState = player.RunState;
        if (runState is null)
        {
            return false;
        }

        foreach (ModifierModel modifier in runState.Modifiers)
        {
            if (IsFormStanceModifier(modifier))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFormStanceModifier(ModifierModel modifier)
    {
        Type type = modifier.GetType();
        if (string.Equals(type.FullName, FormStanceModifierTypeFullName, StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            CustomIDAttribute? attribute = type.GetCustomAttribute<CustomIDAttribute>(inherit: false);
            return attribute is not null
                && string.Equals(attribute.ID, LegacyFormStanceModifierCustomId, StringComparison.Ordinal);
        }
        catch (Exception e)
        {
            // 属性反射失败时无法排除该 modifier 携带旧 CustomID 的可能, 不能以 false 把它当成普通局
            // (那会把一个可能已选的形态局静默降级). 这里显式失败; 全名精确命中的路径已在上面无反射地
            // 返回 true, 不受影响. 正常返回 null 属性的 vanilla/其它 mod modifier 不进入本 catch,
            // 身份解析不变.
            throw new InvalidOperationException(
                "failed to read CustomIDAttribute from modifier type "
                + (type.FullName ?? type.Name)
                + "; cannot rule out the legacy form-stance identity "
                + LegacyFormStanceModifierCustomId + " (fail closed, no ordinary-rules fallback).", e);
        }
    }

    /// <summary>
    /// True only when <paramref name="type"/> is declared by the standalone Forms assembly. This
    /// check is deliberately independent of entry-point signature validation and of the Forms
    /// runtime state: the Spire1 powers gate must keep independent Forms powers out of its scope
    /// even while Forms is Retryable/Terminal/ShuttingDown.
    /// </summary>
    internal static bool IsFormsType(Type? type)
    {
        if (type is null)
        {
            return false;
        }

        Assembly assembly = type.Assembly;
        try
        {
            if (!string.Equals(assembly.GetName().Name, FormsAssemblySimpleName, StringComparison.Ordinal))
            {
                return false;
            }

            if (assembly.IsCollectible && !IsAssemblyStillLoaded(assembly))
            {
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Maps a native stance type to the stable protocol kind.</summary>
    internal static int KindOf(Type stanceType)
    {
        if (stanceType is null)
        {
            return KindNone;
        }

        Bridge? bridge = GetUsableBridge();
        if (bridge is null)
        {
            throw new InvalidOperationException(
                "Spire1 Forms compatibility bridge unavailable during KindOf: "
                + DisabledReasonSnapshot());
        }

        try
        {
            return bridge.KindOf(stanceType);
        }
        catch (Exception e)
        {
            MarkCallFailure("KindOf", e);
            throw new InvalidOperationException(
                "Spire1 Forms compatibility bridge failed during KindOf: "
                + e.GetType().Name + ": " + e.Message, e);
        }
    }

    /// <summary>Current form kind for the player.</summary>
    internal static int CurrentKind(Player player)
    {
        Bridge? bridge = GetUsableBridge();
        if (bridge is null)
        {
            throw new InvalidOperationException(
                "Spire1 Forms compatibility bridge unavailable during CurrentKind: "
                + DisabledReasonSnapshot());
        }

        try
        {
            return bridge.CurrentKind(player);
        }
        catch (Exception e)
        {
            MarkCallFailure("CurrentKind", e);
            throw new InvalidOperationException(
                "Spire1 Forms compatibility bridge failed during CurrentKind: "
                + e.GetType().Name + ": " + e.Message, e);
        }
    }

    /// <summary>
    /// Forwards form entry. Only reached after IsSelected proved a selected form run; an unavailable
    /// bridge or a forwarded failure is surfaced, never silently swallowed.
    /// </summary>
    internal static async Task Enter(PlayerChoiceContext ctx, Player player, Type nativeStanceType, CardModel? source)
    {
        Bridge? bridge = GetUsableBridge();
        if (bridge is null)
        {
            throw new InvalidOperationException(
                "Spire1 Forms compatibility bridge unavailable during Enter: "
                + DisabledReasonSnapshot());
        }

        try
        {
            await bridge.Enter(ctx, player, nativeStanceType, source);
        }
        catch (Exception e)
        {
            MarkCallFailure("Enter", e);
            throw new InvalidOperationException(
                "Spire1 Forms compatibility bridge failed during Enter: "
                + e.GetType().Name + ": " + e.Message, e);
        }
    }

    /// <summary>
    /// Forwards form exit. Only reached after IsSelected proved a selected form run; an unavailable
    /// bridge or a forwarded failure is surfaced, never silently swallowed.
    /// </summary>
    internal static async Task Exit(PlayerChoiceContext ctx, Player player, CardModel? source)
    {
        Bridge? bridge = GetUsableBridge();
        if (bridge is null)
        {
            throw new InvalidOperationException(
                "Spire1 Forms compatibility bridge unavailable during Exit: "
                + DisabledReasonSnapshot());
        }

        try
        {
            await bridge.Exit(ctx, player, source);
        }
        catch (Exception e)
        {
            MarkCallFailure("Exit", e);
            throw new InvalidOperationException(
                "Spire1 Forms compatibility bridge failed during Exit: "
                + e.GetType().Name + ": " + e.Message, e);
        }
    }

    private static Bridge? GetUsableBridge()
    {
        int revision = Volatile.Read(ref _assemblyLoadRevision);
        if (Volatile.Read(ref _signatureValid) && revision == Volatile.Read(ref _probedRevision))
        {
            Bridge? cached = Volatile.Read(ref _bridge);
            if (cached is not null && IsBridgeUsable(cached))
            {
                return cached;
            }
        }

        lock (ProbeLock)
        {
            revision = Volatile.Read(ref _assemblyLoadRevision);

            // If the Forms assembly itself did not change, the cached delegates still belong to the
            // correct identity. Only re-read runtime availability here; do not re-reflect on a
            // Retryable/Terminal/ShuttingDown transition.
            if (_signatureValid && revision == _probedRevision && _bridge is not null)
            {
                return IsBridgeUsable(_bridge) ? _bridge : null;
            }

            // A new Forms assembly was observed (or this is the first probe): re-validate so a
            // reload/replace never keeps a stale delegate. A missing Forms simply re-disables.
            _probedRevision = revision;
            ProbeLocked();

            if (!_signatureValid || _bridge is null)
            {
                return null;
            }

            return IsBridgeUsable(_bridge) ? _bridge : null;
        }
    }


    private static void ProbeLocked()
    {
        _bridge = null;
        Volatile.Write(ref _signatureValid, false);

        try
        {
            Assembly? forms = FindFormsAssembly();
            if (forms is null)
            {
                _disabledReason = "Forms assembly '" + FormsAssemblySimpleName + "' is not loaded.";
                return;
            }

            Type? entryPoint = forms.GetType(EntryPointTypeFullName, throwOnError: false);
            if (entryPoint is null)
            {
                _disabledReason = "Forms entry point type not found: " + EntryPointTypeFullName + ".";
                return;
            }

            Bridge candidate = Bridge.Validate(entryPoint);
            _bridge = candidate;
            Volatile.Write(ref _signatureValid, true);
            _disabledReason = "";
            _lastLoggedDisabledReason = "";
        }
        catch (Exception e)
        {
            _bridge = null;
            Volatile.Write(ref _signatureValid, false);
            _disabledReason = "Forms entry point signature validation failed: "
                + e.GetType().Name + ": " + e.Message;
        }
    }

    /// <summary>
    /// Re-checks the cached binding before every use: the assembly identity must still be the one
    /// currently loaded, a collectible assembly must still be present, and the Forms runtime must
    /// report IsAvailable. Any failure is a fail-closed disable, not a stale delegate reuse.
    /// </summary>
    private static bool IsBridgeUsable(Bridge bridge)
    {
        try
        {
            Assembly? current = FindFormsAssembly();
            if (current is null)
            {
                SetDisabled("Forms assembly is no longer loaded; restart is required.");
                return false;
            }

            if (!ReferenceEquals(current, bridge.DeclaringAssembly))
            {
                SetDisabled("Forms assembly identity changed (same simple name, different assembly); restart is required.");
                return false;
            }

            if (bridge.DeclaringAssembly.IsCollectible && !IsAssemblyStillLoaded(bridge.DeclaringAssembly))
            {
                SetDisabled("Forms collectible assembly is no longer loaded; restart is required.");
                return false;
            }

            if (!bridge.IsAvailable())
            {
                SetDisabled("Forms runtime reports IsAvailable=false (Retryable/Terminal/ShuttingDown).");
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            SetDisabled("Forms runtime availability check failed: " + e.GetType().Name + ": " + e.Message);
            return false;
        }
    }

    private static bool IsAssemblyStillLoaded(Assembly assembly)
    {
        try
        {
            foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (ReferenceEquals(loaded, assembly))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static Assembly? FindFormsAssembly()
    {
        Assembly[] matches;
        try
        {
            matches = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => string.Equals(
                    assembly.GetName().Name, FormsAssemblySimpleName, StringComparison.Ordinal))
                .ToArray();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                "enumerating loaded assemblies failed: " + e.GetType().Name + ": " + e.Message, e);
        }

        if (matches.Length == 0)
        {
            return null;
        }

        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                "expected exactly one loaded Forms assembly, found " + matches.Length
                + "; same-simple-name replacement is not supported in-process, restart is required");
        }

        return matches[0];
    }

    private static void SetDisabled(string reason)
    {
        bool shouldLog;
        lock (ProbeLock)
        {
            _disabledReason = reason;
            shouldLog = !string.Equals(_lastLoggedDisabledReason, reason, StringComparison.Ordinal);
            if (shouldLog)
            {
                _lastLoggedDisabledReason = reason;
            }
        }

        if (shouldLog)
        {
            LogBridgeError(reason);
        }
    }

    private static void MarkCallFailure(string method, Exception e)
    {
        string reason = method + " failed: " + e.GetType().Name + ": " + e.Message
            + "; bridge marked unavailable; a later call will re-probe. If Forms was replaced or "
            + "unloaded in-process, restart is required.";

        lock (ProbeLock)
        {
            _bridge = null;
            Volatile.Write(ref _signatureValid, false);
            _disabledReason = reason;
            _lastLoggedDisabledReason = reason;
        }

        LogBridgeError(reason);
    }

    private static string DisabledReasonSnapshot()
    {
        lock (ProbeLock)
        {
            return _disabledReason;
        }
    }

    private static void LogBridgeError(string reason)
    {
        try
        {
            MainFile.Logger.Error("[Spire1] Forms bridge: " + reason);
        }
        catch (Exception)
        {
            // Logging must never become the failure path.
        }
    }

    /// <summary>
    /// Immutable set of validated delegates. Construction throws on the first contract mismatch, and
    /// the caller turns that into a cached disabled state. Runtime availability is read through its
    /// own delegate; it is not folded into signature-validation success.
    /// </summary>
    private sealed class Bridge
    {
        private readonly Func<bool> _isAvailable;
        private readonly Func<Player, bool> _isSelected;
        private readonly Func<Type, int> _kindOf;
        private readonly Func<Player, int> _currentKind;
        private readonly Func<PlayerChoiceContext, Player, Type, CardModel?, Task> _enter;
        private readonly Func<PlayerChoiceContext, Player, CardModel?, Task> _exit;

        internal Assembly DeclaringAssembly { get; }

        private Bridge(
            Assembly declaringAssembly,
            Func<bool> isAvailable,
            Func<Player, bool> isSelected,
            Func<Type, int> kindOf,
            Func<Player, int> currentKind,
            Func<PlayerChoiceContext, Player, Type, CardModel?, Task> enter,
            Func<PlayerChoiceContext, Player, CardModel?, Task> exit)
        {
            DeclaringAssembly = declaringAssembly;
            _isAvailable = isAvailable;
            _isSelected = isSelected;
            _kindOf = kindOf;
            _currentKind = currentKind;
            _enter = enter;
            _exit = exit;
        }

        internal static Bridge Validate(Type entryPoint)
        {
            if (!entryPoint.IsAbstract || !entryPoint.IsSealed)
            {
                throw new InvalidOperationException(
                    entryPoint.FullName + " is no longer a static class.");
            }

            MethodInfo isAvailableGetter = RequireStaticPropertyGetter(entryPoint, "IsAvailable", typeof(bool));

            MethodInfo isSelected = RequireMethod(entryPoint, "IsSelected", typeof(bool), typeof(Player));
            MethodInfo kindOf = RequireMethod(entryPoint, "KindOf", typeof(int), typeof(Type));
            MethodInfo currentKind = RequireMethod(entryPoint, "CurrentKind", typeof(int), typeof(Player));
            MethodInfo enter = RequireMethod(
                entryPoint, "Enter", typeof(Task), typeof(PlayerChoiceContext), typeof(Player), typeof(Type), typeof(CardModel));
            MethodInfo exit = RequireMethod(
                entryPoint, "Exit", typeof(Task), typeof(PlayerChoiceContext), typeof(Player), typeof(CardModel));

            return new Bridge(
                entryPoint.Assembly,
                isAvailableGetter.CreateDelegate<Func<bool>>(),
                isSelected.CreateDelegate<Func<Player, bool>>(),
                kindOf.CreateDelegate<Func<Type, int>>(),
                currentKind.CreateDelegate<Func<Player, int>>(),
                enter.CreateDelegate<Func<PlayerChoiceContext, Player, Type, CardModel?, Task>>(),
                exit.CreateDelegate<Func<PlayerChoiceContext, Player, CardModel?, Task>>());
        }

        internal bool IsAvailable() => _isAvailable();

        internal bool IsSelected(Player player) => _isSelected(player);

        internal int KindOf(Type stanceType) => _kindOf(stanceType);

        internal int CurrentKind(Player player) => _currentKind(player);

        internal Task Enter(PlayerChoiceContext ctx, Player player, Type nativeStanceType, CardModel? source)
            => _enter(ctx, player, nativeStanceType, source);

        internal Task Exit(PlayerChoiceContext ctx, Player player, CardModel? source)
            => _exit(ctx, player, source);

        private static MethodInfo RequireMethod(
            Type type, string name, Type returnType, params Type[] parameterTypes)
        {
            MethodInfo[] candidates = type
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => string.Equals(method.Name, name, StringComparison.Ordinal))
                .ToArray();

            if (candidates.Length != 1)
            {
                throw new InvalidOperationException(
                    type.FullName + "." + name + ": expected exactly one declared static method, found "
                    + candidates.Length + ".");
            }

            MethodInfo method = candidates[0];
            if (method.IsGenericMethodDefinition || method.ReturnType != returnType)
            {
                throw new InvalidOperationException(
                    type.FullName + "." + name + " has an unexpected return type or is generic.");
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != parameterTypes.Length)
            {
                throw new InvalidOperationException(
                    type.FullName + "." + name + ": expected " + parameterTypes.Length
                    + " parameter(s), found " + parameters.Length + ".");
            }

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (parameters[i].ParameterType != parameterTypes[i])
                {
                    throw new InvalidOperationException(
                        type.FullName + "." + name + ": parameter " + i + " expected "
                        + parameterTypes[i].FullName + ", found " + parameters[i].ParameterType.FullName + ".");
                }
            }

            return method;
        }

        private static MethodInfo RequireStaticPropertyGetter(Type type, string name, Type propertyType)
        {
            PropertyInfo? property = type.GetProperty(
                name, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            if (property is null || property.PropertyType != propertyType)
            {
                throw new InvalidOperationException(
                    type.FullName + "." + name + " is not a public static " + propertyType.Name + " property.");
            }

            MethodInfo? getter = property.GetGetMethod(nonPublic: false);
            if (getter is null || !getter.IsStatic)
            {
                throw new InvalidOperationException(
                    type.FullName + "." + name + " has no public static getter.");
            }

            if (getter.GetParameters().Length != 0 || getter.IsGenericMethodDefinition)
            {
                throw new InvalidOperationException(
                    type.FullName + "." + name + " getter has an unexpected signature.");
            }

            return getter;
        }
    }
}

/// <summary>
/// Optional notification entry point that the standalone Forms mod may call by reflection after it
/// changes its own stance. It is public so reflection-based discovery works across assemblies, and
/// it takes only BCL/sts2 types (no Forms type appears in its signature).
///
/// Kind protocol: 0 None, 1 Calm, 2 Wrath, 3 Divinity. The mapping resolves Spire1's local canonical
/// stance models purely as read-only notification arguments; it never applies, mounts or removes a
/// power, so no second stance system, energy or resource is triggered. The actual fan-out is the
/// existing StanceCmd.Dispatch, which already honours the powers content group gate.
/// </summary>
public static class FormsCompatibilityEntryPoint
{
    /// <summary>
    /// Forward a Forms stance transition to Spire1's own IOnStanceChanged consumers.
    /// </summary>
    public static async Task Dispatch(
        Player player,
        PlayerChoiceContext ctx,
        int previousKind,
        int nextKind)
    {
        if (player is null)
        {
            throw new ArgumentNullException(nameof(player));
        }

        StancePower? from = LogicalStance(previousKind);
        StancePower? to = LogicalStance(nextKind);
        await Spire1.Spire1Code.Extensions.StanceCmd.Dispatch(player, ctx, from, to);
    }

    private static StancePower? LogicalStance(int kind) => kind switch
    {
        0 => null,
        1 => ModelDb.Power<CalmPower>(),
        2 => ModelDb.Power<WrathPower>(),
        3 => ModelDb.Power<DivinityPower>(),
        _ => throw new ArgumentOutOfRangeException(
            nameof(kind), kind, "Stance kind must be 0 (None), 1 (Calm), 2 (Wrath) or 3 (Divinity).")
    };
}

