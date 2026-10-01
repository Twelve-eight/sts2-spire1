using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.ValueProps;
using Spire1.Spire1Code.Extensions;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Forms;

/// <summary>
/// Optional reflection-only bridge for Watcher 0.9.28. All targets are checked before installation,
/// and only this Harmony owner's patches are removed if any target fails. No Watcher AssemblyRef.
/// </summary>
public static class FormStanceWatcherBridge
{
    public const string HarmonyId = "Spire1.FormStanceMode.Watcher";
    private const BindingFlags DeclaredMethods = BindingFlags.Public | BindingFlags.NonPublic
        | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private static Binding? _binding;
    private static bool _attempted;
    private static Func<Player, Type?, Type?, Task>? _nativeNotification;

    public static bool IsAvailable => _binding != null;
    public static string UnavailableReason { get; private set; } = "Watcher 兼容桥尚未绑定";
    public static IReadOnlyList<string> BoundTargets { get; private set; } = Array.Empty<string>();

    private sealed record PatchSpec(MethodInfo Target, string? Prefix = null, string? Postfix = null, string? Transpiler = null);

    private sealed class Binding
    {
        public required Type Calm { get; init; }
        public required Type Wrath { get; init; }
        public required Type Divinity { get; init; }
        public required Func<Player, CardModel?, Task> EnterCalm { get; init; }
        public required Func<Player, CardModel?, Task> EnterWrath { get; init; }
        public required Func<Player, CardModel?, Task> EnterDivinity { get; init; }
        public required Func<Player, Task> ExitStance { get; init; }
        public required Func<Creature, Type?> GetCurrentStance { get; init; }
        public required Func<Player, Type?, Type?, Task> OnStanceChanged { get; init; }
        public required Func<Player, Func<PlayerChoiceContext, Task>, Task> RunWithHookContext { get; init; }
    }

    /// <summary>Called synchronously after ModelDb.Init, not by an assembly-load event.</summary>
    public static bool TryBind()
    {
        if (_attempted)
            return IsAvailable;
        _attempted = true;
        var specs = new List<PatchSpec>();
        var harmony = new Harmony(HarmonyId);
        try
        {
            Binding candidate = ValidateBinding(specs);
            _nativeNotification = candidate.OnStanceChanged;
            foreach (PatchSpec spec in specs)
                harmony.Patch(spec.Target, ToPatch(spec.Prefix), ToPatch(spec.Postfix), ToPatch(spec.Transpiler));
            foreach (PatchSpec spec in specs)
                VerifyInstalled(spec);
            // Publish capability only after ALL patches, including the menu entry, have succeeded.
            _binding = candidate;
            BoundTargets = Array.AsReadOnly(specs.Select(spec => Describe(spec.Target)).ToArray());
            UnavailableReason = "";
            Log.Info("Spire1 Forms: Watcher bridge bound; custom-run modifier available. Targets: " + string.Join(", ", BoundTargets));
            return true;
        }
        catch (Exception error)
        {
            _binding = null;
            BoundTargets = Array.Empty<string>();
            var rollbackFailures = new List<string>();
            foreach (MethodInfo target in specs.Select(spec => spec.Target).Distinct().Reverse())
            {
                try
                {
                    harmony.Unpatch(target, HarmonyPatchType.All, HarmonyId);
                    if (Harmony.GetPatchInfo(target)?.Owners.Contains(HarmonyId) == true)
                        rollbackFailures.Add(Describe(target) + ": owner still present");
                }
                catch (Exception rollbackError)
                {
                    rollbackFailures.Add(Describe(target) + ": " + rollbackError.Message);
                }
            }
            UnavailableReason = error.Message;
            if (rollbackFailures.Count != 0)
                UnavailableReason += "; rollback failures: " + string.Join("; ", rollbackFailures);
            Log.Error("Spire1 Forms: bridge disabled; custom-run entry unavailable. " + UnavailableReason);
            return false;
        }
    }

    private static Binding ValidateBinding(List<PatchSpec> specs)
    {
        Assembly[] matches = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name == "Watcher").ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("Expected one loaded Watcher assembly, found " + matches.Length);
        Assembly assembly = matches[0];
        Type calm = RequireMarker(assembly, "WatcherMod.Calm");
        Type wrath = RequireMarker(assembly, "WatcherMod.Wrath");
        Type divinity = RequireMarker(assembly, "WatcherMod.Divinity");
        Type helper = assembly.GetType("WatcherMod.WatcherCombatHelper", throwOnError: true)!;
        if (!helper.IsAbstract || !helper.IsSealed)
            throw new InvalidOperationException("WatcherCombatHelper is no longer a static class");

        MethodInfo enterCalm = RequireMethod(helper, "EnterCalm", true, typeof(Task), typeof(Player), typeof(CardModel));
        MethodInfo enterWrath = RequireMethod(helper, "EnterWrath", true, typeof(Task), typeof(Player), typeof(CardModel));
        MethodInfo enterDivinity = RequireMethod(helper, "EnterDivinity", true, typeof(Task), typeof(Player), typeof(CardModel));
        MethodInfo exit = RequireMethod(helper, "ExitStance", true, typeof(Task), typeof(Player));
        MethodInfo current = RequireMethod(helper, "GetCurrentStance", true, typeof(Type), typeof(Creature));
        MethodInfo changed = RequireMethod(helper, "OnStanceChanged", true, typeof(Task), typeof(Player), typeof(Type), typeof(Type));
        MethodInfo context = RequireMethod(helper, "RunWithHookContext", true, typeof(Task), typeof(Player), typeof(Func<PlayerChoiceContext, Task>));
        if (!enterCalm.IsPublic || !enterWrath.IsPublic || !enterDivinity.IsPublic || !exit.IsPublic
            || !current.IsPrivate || !changed.IsPrivate)
            throw new InvalidOperationException("Watcher stance method visibility changed");
        MethodInfo[] changes = helper.GetMethods(DeclaredMethods).Where(method => method.Name == "ChangeStance").ToArray();
        if (changes.Length != 1 || !changes[0].IsPrivate || !changes[0].IsStatic
            || !changes[0].IsGenericMethodDefinition || changes[0].GetGenericArguments().Length != 1
            || changes[0].ReturnType != typeof(Task)
            || !changes[0].GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(Player), typeof(CardModel) })
            || !changes[0].GetGenericArguments()[0].GetGenericParameterConstraints().Contains(typeof(PowerModel)))
            throw new InvalidOperationException("Watcher ChangeStance<T> contract changed");

        foreach (Type marker in new[] { calm, wrath, divinity })
        {
            specs.Add(new PatchSpec(RequireMethod(marker, "AfterApplied", false, typeof(Task), typeof(Creature), typeof(CardModel)),
                Postfix: nameof(MarkerAppliedPostfix)));
            specs.Add(new PatchSpec(RequireMethod(marker, "AfterRemoved", false, typeof(Task), typeof(Creature)),
                Postfix: nameof(MarkerRemovedPostfix)));
        }
        Type[] damageArgs = { typeof(Creature), typeof(decimal), typeof(ValueProp), typeof(Creature), typeof(CardModel), typeof(CardPlay) };
        foreach (Type marker in new[] { wrath, divinity })
            specs.Add(new PatchSpec(RequireMethod(marker, "ModifyDamageMultiplicative", false, typeof(decimal), damageArgs),
                Prefix: nameof(NeutralizeDamagePrefix)));
        specs.Add(new PatchSpec(RequireMethod(divinity, "AfterSideTurnEnd", false, typeof(Task),
            typeof(PlayerChoiceContext), typeof(CombatSide), typeof(IEnumerable<Creature>)), Prefix: nameof(KeepDivinityUntilNextTurnPrefix)));
        specs.Add(new PatchSpec(changed, Prefix: nameof(StanceChangedPrefix), Postfix: nameof(StanceChangedPostfix)));

        MethodInfo divinityApplied = RequireMethod(divinity, "AfterApplied", false, typeof(Task), typeof(Creature), typeof(CardModel));
        MethodInfo divinityMoveNext = RequireMoveNext(divinityApplied);
        ValidateDivinityEnergySite(PatchProcessor.GetOriginalInstructions(divinityMoveNext));
        specs.Add(new PatchSpec(divinityMoveNext, Transpiler: nameof(DivinityEnergyTranspiler)));

        // EndTurnSafely has a second, multiplayer/non-play-phase removal path outside AfterSideTurnEnd.
        MethodInfo endTurn = RequireMethod(helper, "EndTurnSafely", true, typeof(Task), typeof(Player));
        MethodInfo endTurnMoveNext = RequireMoveNext(endTurn);
        ValidateEndTurnSites(PatchProcessor.GetOriginalInstructions(endTurnMoveNext));
        specs.Add(new PatchSpec(endTurnMoveNext, Transpiler: nameof(EndTurnTranspiler)));

        MethodInfo menu = RequireMethod(typeof(NCustomRunModifiersList), "GetAllModifiers", false, typeof(IEnumerable<ModifierModel>));
        if (!menu.IsPrivate)
            throw new InvalidOperationException("Custom-run modifier list contract changed");
        specs.Add(new PatchSpec(menu, Postfix: nameof(CustomModifiersPostfix)));
        foreach (Type model in new[]
        {
            typeof(FormStanceModifier), typeof(VoidSerpentStancePower), typeof(DemonReaperStancePower),
            typeof(EchoCelestialStancePower), typeof(VoidFormEffectPower), typeof(SerpentFormPower),
            typeof(DemonFormPower), typeof(ReaperFormEffectPower), typeof(EchoFormEffectPower), typeof(CelestialFormPower)
        })
        {
            if (!ModelDb.Contains(model))
                throw new InvalidOperationException("Required form model missing after ModelDb.Init: " + model.FullName);
        }
        return new Binding
        {
            Calm = calm,
            Wrath = wrath,
            Divinity = divinity,
            EnterCalm = enterCalm.CreateDelegate<Func<Player, CardModel?, Task>>(),
            EnterWrath = enterWrath.CreateDelegate<Func<Player, CardModel?, Task>>(),
            EnterDivinity = enterDivinity.CreateDelegate<Func<Player, CardModel?, Task>>(),
            ExitStance = exit.CreateDelegate<Func<Player, Task>>(),
            GetCurrentStance = current.CreateDelegate<Func<Creature, Type?>>(),
            OnStanceChanged = changed.CreateDelegate<Func<Player, Type?, Type?, Task>>(),
            RunWithHookContext = context.CreateDelegate<Func<Player, Func<PlayerChoiceContext, Task>, Task>>()
        };
    }

    private static Type RequireMarker(Assembly assembly, string name)
    {
        Type type = assembly.GetType(name, throwOnError: true)!;
        if (!type.IsSealed || type.IsAbstract || !typeof(PowerModel).IsAssignableFrom(type))
            throw new InvalidOperationException(name + " is no longer a sealed PowerModel");
        return type;
    }

    private static MethodInfo RequireMethod(Type type, string name, bool isStatic, Type result, params Type[] parameters)
    {
        MethodInfo[] methods = type.GetMethods(DeclaredMethods).Where(method => method.Name == name).ToArray();
        if (methods.Length != 1 || methods[0].IsStatic != isStatic || methods[0].IsGenericMethod
            || methods[0].ReturnType != result
            || !methods[0].GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameters))
            throw new InvalidOperationException(type.FullName + "." + name + " signature changed");
        return methods[0];
    }

    private static MethodInfo RequireMoveNext(MethodInfo asyncMethod)
    {
        Type? stateMachine = asyncMethod.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType;
        if (stateMachine == null || stateMachine.DeclaringType != asyncMethod.DeclaringType
            || !typeof(IAsyncStateMachine).IsAssignableFrom(stateMachine))
            throw new InvalidOperationException(Describe(asyncMethod) + " async state-machine contract changed");
        return RequireMethod(stateMachine, "MoveNext", false, typeof(void));
    }

    private static string Describe(MethodInfo method) => method.DeclaringType?.FullName + "." + method.Name;
    private static MethodInfo Hook(string name) => typeof(FormStanceWatcherBridge).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(typeof(FormStanceWatcherBridge).FullName, name);
    private static HarmonyMethod? ToPatch(string? name) => name == null ? null : new HarmonyMethod(Hook(name));

    private static void VerifyInstalled(PatchSpec spec)
    {
        HarmonyLib.Patches? info = Harmony.GetPatchInfo(spec.Target);
        if (info == null
            || (spec.Prefix != null && !info.Prefixes.Any(patch => patch.owner == HarmonyId && patch.PatchMethod == Hook(spec.Prefix)))
            || (spec.Postfix != null && !info.Postfixes.Any(patch => patch.owner == HarmonyId && patch.PatchMethod == Hook(spec.Postfix)))
            || (spec.Transpiler != null && !info.Transpilers.Any(patch => patch.owner == HarmonyId && patch.PatchMethod == Hook(spec.Transpiler))))
            throw new InvalidOperationException("Incomplete form bridge patch: " + Describe(spec.Target));
    }

    public static FormStanceKind KindOfMarker(Type? markerType)
    {
        Binding? binding = _binding;
        if (binding == null || markerType == null)
            return FormStanceKind.None;
        if (markerType == binding.Calm) return FormStanceKind.Calm;
        if (markerType == binding.Wrath) return FormStanceKind.Wrath;
        if (markerType == binding.Divinity) return FormStanceKind.Divinity;
        return FormStanceKind.None;
    }

    public static FormStanceKind CurrentKind(Player player)
    {
        FormStanceMode.RequireAvailable();
        return KindOfMarker(_binding!.GetCurrentStance(player.Creature));
    }

    public static async Task Enter(Player player, FormStanceKind kind, CardModel? source)
    {
        if (!FormStanceMode.IsEnabled(player))
            throw new InvalidOperationException("Watcher form entry requires the custom-run modifier");
        Binding binding = _binding!;
        // Every supported internal caller now comes here, rather than applying a second stance system.
        await RemoveLegacyInternalStances(player);
        switch (kind)
        {
            case FormStanceKind.Calm: await binding.EnterCalm(player, source); break;
            case FormStanceKind.Wrath: await binding.EnterWrath(player, source); break;
            case FormStanceKind.Divinity: await binding.EnterDivinity(player, source); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public static async Task Exit(Player player)
    {
        if (!FormStanceMode.IsEnabled(player))
            throw new InvalidOperationException("Watcher form exit requires the custom-run modifier");
        await _binding!.ExitStance(player);
    }

    private static async Task RemoveLegacyInternalStances(Player player)
    {
        // Protect an old/direct internal caller without ever removing the new form carriers here.
        foreach (StancePower legacy in player.Creature.Powers.OfType<StancePower>()
            .Where(power => power is CalmPower or WrathPower or DivinityPower).ToArray())
            await PowerCmd.Remove(legacy);
    }

    private static void MarkerAppliedPostfix(PowerModel __instance, CardModel? __1, ref Task __result)
    {
        if (FormStanceMode.IsEnabled(__instance.Owner.Player))
            __result = AfterMarkerApplied(__result, __instance, __1);
    }

    private static async Task AfterMarkerApplied(Task original, PowerModel marker, CardModel? source)
    {
        await original;
        Creature owner = marker.Owner;
        if (owner.Player == null || owner.CombatState == null || CombatManager.Instance.IsEnding || !owner.Powers.Contains(marker))
            return;
        await RemoveLegacyInternalStances(owner.Player);
        if (!owner.Powers.Contains(marker))
            return;
        WatcherFormStancePower[] carriers = owner.Powers.OfType<WatcherFormStancePower>().ToArray();
        if (carriers.Any(carrier => ReferenceEquals(carrier.NativeMarker, marker)))
            return;
        foreach (WatcherFormStancePower old in carriers)
            await PowerCmd.Remove(old);
        if (!owner.Powers.Contains(marker) || CombatManager.Instance.IsEnding)
            return;
        WatcherFormStancePower tracking = FormStanceMode.CreateTracking(KindOfMarker(marker.GetType()), marker);
        await PowerCmd.Apply(new ThrowingPlayerChoiceContext(), tracking, owner, 1m, owner, source);
        if (!CombatManager.Instance.IsEnding && owner.Powers.Contains(marker) && !owner.Powers.Contains(tracking))
            throw new InvalidOperationException("A power hook rejected the required Watcher form carrier");
    }

    private static void MarkerRemovedPostfix(PowerModel __instance, Creature __0, ref Task __result)
    {
        if (FormStanceMode.IsEnabled(__0.Player))
            __result = AfterMarkerRemoved(__result, __instance, __0);
    }

    private static async Task AfterMarkerRemoved(Task original, PowerModel marker, Creature owner)
    {
        try
        {
            await original; // Preserve native VFX removal and native Calm's sole +2 energy.
        }
        finally
        {
            foreach (WatcherFormStancePower carrier in owner.Powers.OfType<WatcherFormStancePower>()
                .Where(carrier => ReferenceEquals(carrier.NativeMarker, marker)).ToArray())
                await PowerCmd.Remove(carrier);
        }
    }

    private static bool NeutralizeDamagePrefix(PowerModel __instance, ref decimal __result)
    {
        if (!FormStanceMode.IsEnabled(__instance.Owner.Player))
            return true;
        __result = 1m;
        return false;
    }

    private static bool KeepDivinityUntilNextTurnPrefix(PowerModel __instance, ref Task __result)
    {
        if (!FormStanceMode.IsEnabled(__instance.Owner.Player))
            return true;
        __result = Task.CompletedTask;
        return false;
    }

    private static bool StanceChangedPrefix(Player __0, ref Task __result)
    {
        if (!FormStanceMode.IsEnabled(__0) || !CombatManager.Instance.IsOverOrEnding)
            return true;
        // Combat-end cleanup must not pay Violet Lotus or draw cards through this notification.
        __result = Task.CompletedTask;
        return false;
    }

    private static void StanceChangedPostfix(Player __0, Type? __1, Type? __2, ref Task __result)
    {
        if (FormStanceMode.IsEnabled(__0) && __1 != __2 && !CombatManager.Instance.IsOverOrEnding)
            __result = AfterStanceChanged(__result, __0, __1, __2);
    }

    private static async Task AfterStanceChanged(Task original, Player player, Type? previous, Type? next)
    {
        await original; // Original Type comparisons dispatch Watcher's Rushdown, Fortress, Flurry and Lotus.
        if (CombatManager.Instance.IsOverOrEnding)
            return;
        StancePower? from = FormStanceMode.LogicalStance(KindOfMarker(previous));
        StancePower? to = FormStanceMode.LogicalStance(KindOfMarker(next));
        // Foreseen is not one of the replaced stances. Do not invent a local None -> None change.
        if (from == null && to == null)
            return;
        await _binding!.RunWithHookContext(player, ctx => StanceCmd.Dispatch(player, ctx, from, to));
    }

    private static IEnumerable<ModifierModel> CustomModifiersPostfix(IEnumerable<ModifierModel> __result)
    {
        bool found = false;
        foreach (ModifierModel modifier in __result)
        {
            if (modifier is FormStanceModifier)
            {
                if (!IsAvailable || found)
                    continue;
                found = true;
            }
            yield return modifier;
        }
        if (IsAvailable && !found)
            yield return ModelDb.Modifier<FormStanceModifier>().ToMutable();
    }

    private static bool Calls(CodeInstruction instruction, MethodInfo method)
        => (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt)
            && instruction.operand is MethodInfo operand && operand == method;

    private static MethodInfo GainEnergyMethod => RequireMethod(typeof(PlayerCmd), nameof(PlayerCmd.GainEnergy), true,
        typeof(Task), typeof(decimal), typeof(Player));
    private static MethodInfo RemovePowerMethod => typeof(PowerCmd).GetMethod(nameof(PowerCmd.Remove), new[] { typeof(PowerModel) })
        ?? throw new MissingMethodException("PowerCmd.Remove(PowerModel)");

    private static int ValidateDivinityEnergySite(IReadOnlyList<CodeInstruction> instructions)
    {
        // Validate the actual transpiler input too, not just the unpatched body during preflight.
        MethodInfo gain = GainEnergyMethod;
        List<CodeInstruction> body = instructions.Where(instruction => instruction.opcode != OpCodes.Nop).ToList();
        int[] gainSites = body.Select((instruction, index) => (instruction, index))
            .Where(item => item.instruction.operand is MethodInfo method && method.DeclaringType == typeof(PlayerCmd)
                && method.Name == nameof(PlayerCmd.GainEnergy)).Select(item => item.index).ToArray();
        if (gainSites.Length != 1 || !Calls(body[gainSites[0]], gain))
            throw new InvalidOperationException("Divinity.AfterApplied must contain exactly one PlayerCmd.GainEnergy(decimal, Player) call");
        int site = gainSites[0];
        MethodInfo ownerGetter = typeof(PowerModel).GetProperty(nameof(PowerModel.Owner))!.GetMethod!;
        MethodInfo playerGetter = typeof(Creature).GetProperty(nameof(Creature.Player))!.GetMethod!;
        if (site < 5 || !Calls(body[site - 1], playerGetter) || !Calls(body[site - 2], ownerGetter))
            throw new InvalidOperationException("Divinity entry energy recipient is no longer this.Owner.Player");
        int load = site - 3;
        int constructor;
        if (IsLocalLoad(body[load]))
            constructor = load - 1;
        else if (body[load].opcode == OpCodes.Ldfld && body[load].operand is FieldInfo field
            && field.FieldType.FullName == "WatcherMod.Divinity" && load > 0 && body[load - 1].opcode == OpCodes.Ldarg_0)
            constructor = load - 2;
        else
            throw new InvalidOperationException("Divinity entry energy receiver load changed");
        ConstructorInfo decimalInt = typeof(decimal).GetConstructor(new[] { typeof(int) })!;
        if (constructor < 1 || body[constructor].opcode != OpCodes.Newobj
            || !Equals(body[constructor].operand, decimalInt) || !LoadsThree(body[constructor - 1]))
            throw new InvalidOperationException("Divinity entry energy must be the literal decimal 3");
        return instructions.IndexOfReference(body[site]);
    }

    private static bool IsLocalLoad(CodeInstruction instruction)
        => instruction.opcode == OpCodes.Ldloc || instruction.opcode == OpCodes.Ldloc_S
            || instruction.opcode == OpCodes.Ldloc_0 || instruction.opcode == OpCodes.Ldloc_1
            || instruction.opcode == OpCodes.Ldloc_2 || instruction.opcode == OpCodes.Ldloc_3;

    private static bool LoadsThree(CodeInstruction instruction)
        => instruction.opcode == OpCodes.Ldc_I4_3
            || ((instruction.opcode == OpCodes.Ldc_I4 || instruction.opcode == OpCodes.Ldc_I4_S)
                && Convert.ToInt32(instruction.operand) == 3);

    private static IEnumerable<CodeInstruction> DivinityEnergyTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> body = instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
        int site = ValidateDivinityEnergySite(body);
        body[site].opcode = OpCodes.Call;
        body[site].operand = Hook(nameof(GainDivinityEntryEnergy));
        return body;
    }

    private static async Task GainDivinityEntryEnergy(decimal amount, Player player)
    {
        if (!FormStanceMode.IsEnabled(player))
            await PlayerCmd.GainEnergy(amount, player);
        // In form mode CelestialFormPower alone grants max(round, 3). No grant-then-subtract.
    }

    private static (int Remove, int Notify) ValidateEndTurnSites(IReadOnlyList<CodeInstruction> body)
    {
        int[] removes = body.Select((instruction, index) => (instruction, index))
            .Where(item => Calls(item.instruction, RemovePowerMethod)).Select(item => item.index).ToArray();
        int[] notifications = body.Select((instruction, index) => (instruction, index))
            .Where(item => item.instruction.operand is MethodInfo method
                && method.DeclaringType?.FullName == "WatcherMod.WatcherCombatHelper" && method.Name == "OnStanceChanged")
            .Select(item => item.index).ToArray();
        int currentCalls = body.Count(instruction => instruction.operand is MethodInfo method
            && method.DeclaringType?.FullName == "WatcherMod.WatcherCombatHelper" && method.Name == "GetCurrentStance");
        int endCalls = body.Count(instruction => instruction.operand is MethodInfo method
            && method.DeclaringType == typeof(PlayerCmd) && method.Name == nameof(PlayerCmd.EndTurn));
        int divinityTokens = body.Count(instruction => instruction.opcode == OpCodes.Ldtoken
            && instruction.operand is Type type && type.FullName == "WatcherMod.Divinity");
        if (removes.Length != 1 || notifications.Length != 1 || removes[0] >= notifications[0]
            || currentCalls != 1 || endCalls != 2 || divinityTokens != 1)
            throw new InvalidOperationException("Watcher EndTurnSafely fallback removal/notification contract changed");
        MethodInfo notify = (MethodInfo)body[notifications[0]].operand;
        if (!notify.IsStatic || notify.ReturnType != typeof(Task)
            || !notify.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(Player), typeof(Type), typeof(Type) }))
            throw new InvalidOperationException("EndTurnSafely notification signature changed");
        return (removes[0], notifications[0]);
    }

    private static IEnumerable<CodeInstruction> EndTurnTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        List<CodeInstruction> body = instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
        (int remove, int notify) = ValidateEndTurnSites(body);
        body[remove].opcode = OpCodes.Call;
        body[remove].operand = Hook(nameof(RemoveEndTurnDivinity));
        body[notify].opcode = OpCodes.Call;
        body[notify].operand = Hook(nameof(NotifyEndTurnDivinity));
        return body;
    }

    private static async Task RemoveEndTurnDivinity(PowerModel? marker)
    {
        if (marker != null && FormStanceMode.IsEnabled(marker.Owner.Player)
            && KindOfMarker(marker.GetType()) == FormStanceKind.Divinity)
            return;
        await PowerCmd.Remove(marker);
    }

    private static async Task NotifyEndTurnDivinity(Player player, Type? previous, Type? next)
    {
        if (FormStanceMode.IsEnabled(player) && KindOfMarker(previous) == FormStanceKind.Divinity && previous == next)
            return;
        // The bound delegate calls the unchanged Watcher notification (and our awaited postfix).
        await (_nativeNotification ?? throw new InvalidOperationException("Native Watcher notification delegate missing"))(player, previous, next);
    }
}

internal static class FormStanceInstructionLookup
{
    internal static int IndexOfReference(this IReadOnlyList<CodeInstruction> instructions, CodeInstruction target)
    {
        for (int i = 0; i < instructions.Count; i++)
            if (ReferenceEquals(instructions[i], target))
                return i;
        throw new InvalidOperationException("Validated IL instruction disappeared");
    }
}
