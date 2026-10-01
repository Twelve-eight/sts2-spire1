using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

// Metadata-only collaborators let the genuine production patch files compile without Harmony.
// This file does not implement Harmony discovery, registration, IL rewriting or runtime patching.
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HarmonyPatch : Attribute
    {
        public Type DeclaringType { get; }
        public string MethodName { get; }
        public HarmonyPatch(Type declaringType, string methodName)
        {
            DeclaringType = declaringType;
            MethodName = methodName;
        }
    }

    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyPrefix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyPostfix : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyFinalizer : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public sealed class HarmonyTranspiler : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class HarmonyPriority : Attribute
    {
        public int Value { get; }
        public HarmonyPriority(int value) => Value = value;
    }

    public static class Priority { public const int Last = 0; }

    public sealed class CodeInstruction
    {
        public OpCode opcode;
        public object? operand;

        public CodeInstruction(OpCode opcode, object? operand = null)
        {
            this.opcode = opcode;
            this.operand = operand;
        }
    }

    public static class AccessTools
    {
        private const BindingFlags AnyMember =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        public static FieldInfo? Field(Type type, string name)
            => type.GetField(name, AnyMember);

        public static MethodInfo? Method(Type type, string name)
            => type.GetMethod(name, AnyMember);
    }
}

namespace MegaCrit.Sts2.Core.Combat.History
{
    public sealed class CombatHistory
    {
        // CombatHistory.cs:108-110. Record the call, then execute the linked production postfix.
        // Native history subscribers and persistence are intentionally not reproduced.
        public void PowerReceived(ICombatState combatState, PowerModel power, decimal amount, Creature? applier)
        {
            FormEffectsProbe.Journal.HistoryRequests.Add(
                new FormEffectsProbe.HistoryRequest(combatState, power, amount, applier));
            FormEffectsProbe.ProductionPatchCalls.AfterHistory(power);
        }
    }
}

namespace FormEffectsProbe
{
    // ONLY invokes real private production patch methods. No copied ledger, reservation or
    // AsyncLocal algorithm lives in this adapter.
    internal static class ProductionPatchCalls
    {
        private static readonly MethodInfo? VoidPrefix = Find("VoidFormReserveBeforeSpendPatch", "Prefix");
        private static readonly MethodInfo? VoidPostfix = Find("VoidFormReserveBeforeSpendPatch", "Postfix");
        private static readonly MethodInfo? VoidFinalizer = Find("VoidFormReserveBeforeSpendPatch", "Finalizer");
        private static readonly MethodInfo? OnPlayPrefix = Find("VoidFormOnPlayWrapperTransactionPatch", "Prefix");
        private static readonly MethodInfo? OnPlayPostfix = Find("VoidFormOnPlayWrapperTransactionPatch", "Postfix");
        private static readonly MethodInfo? OnPlayFinalizer = Find("VoidFormOnPlayWrapperTransactionPatch", "Finalizer");
        private static readonly MethodInfo? DepthPrefix = Find("DemonFormModifyAmountDepthPatch", "Prefix");
        private static readonly MethodInfo? DepthPostfix = Find("DemonFormModifyAmountDepthPatch", "Postfix");
        private static readonly MethodInfo? ArmPostfix = Find("DemonFormArmTransactionPatch", "Postfix");
        private static readonly MethodInfo? SetPrefix = Find("DemonFormSetAmountCapturePatch", "Prefix");
        private static readonly MethodInfo? SetFinalizer = Find("DemonFormSetAmountCapturePatch", "Finalizer");

        public static bool HasVoidPatch => VoidPrefix != null && VoidPostfix != null && VoidFinalizer != null;
        public static bool HasOnPlayPatch => OnPlayPrefix != null && OnPlayPostfix != null && OnPlayFinalizer != null;
        public static bool HasDemonPatch => DepthPrefix != null && DepthPostfix != null && SetPrefix != null && SetFinalizer != null;

        private static MethodInfo? Find(string typeName, string methodName)
        {
            Type? type = typeof(ProductionPatchCalls).Assembly.GetType("Spire1.Spire1Code.Forms." + typeName);
            if (type == null) return null;
            return type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Linked production patch is missing {typeName}.{methodName}");
        }

        private static object? Invoke(MethodInfo? method, object?[] args)
        {
            if (method == null) return null;
            try
            {
                return method.Invoke(null, args);
            }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(error.InnerException).Throw();
                throw;
            }
        }

        public static Task<int> ModifyAmount(PowerModel power, Func<Task<int>> body)
        {
            object?[] prefixArgs = { power, false };
            Invoke(DepthPrefix, prefixArgs);
            Task<int> result = body();
            object?[] postfixArgs = { result, prefixArgs[1] };
            Invoke(DepthPostfix, postfixArgs);
            return (Task<int>)postfixArgs[0]!;
        }

        public static void AfterHistory(PowerModel power)
            => Invoke(ArmPostfix, new object?[] { power });

        public static void SetAmount(PowerModel power, Action write)
        {
            object?[] prefixArgs = { power, null };
            Invoke(SetPrefix, prefixArgs);
            Exception? exception = null;
            try
            {
                write();
            }
            catch (Exception error)
            {
                exception = error;
            }

            if (SetFinalizer != null)
            {
                exception = (Exception?)Invoke(
                    SetFinalizer,
                    new object?[] { power, prefixArgs[1], exception });
            }

            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }

        public static Task<(int, int)> SpendResources(
            CardModel card,
            Func<Task<(int, int)>> body)
        {
            object?[] prefixArgs = { card, null };
            Task<(int, int)>? result = null;
            Exception? exception = null;
            try
            {
                Invoke(VoidPrefix, prefixArgs);
                result = body();
                object?[] postfixArgs = { result, prefixArgs[1] };
                Invoke(VoidPostfix, postfixArgs);
                result = (Task<(int, int)>)postfixArgs[0]!;
            }
            catch (Exception error)
            {
                exception = error;
            }

            if (VoidFinalizer != null)
            {
                exception = (Exception?)Invoke(
                    VoidFinalizer,
                    new object?[] { prefixArgs[1], exception });
            }

            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return result ?? throw new InvalidOperationException("Spend body did not return a Task.");
        }

        public static Task OnPlayWrapper(
            CardModel card,
            PlayerChoiceContext choiceContext,
            Creature? target,
            bool isAutoPlay,
            ResourceInfo resources,
            bool skipCardPileVisuals,
            Func<Task> body)
        {
            object?[] prefixArgs = { card, isAutoPlay, null };
            Task? result = null;
            Exception? exception = null;
            try
            {
                Invoke(OnPlayPrefix, prefixArgs);
                result = body();
                object?[] postfixArgs = { result, prefixArgs[2] };
                Invoke(OnPlayPostfix, postfixArgs);
                result = (Task)postfixArgs[0]!;
            }
            catch (Exception error)
            {
                exception = error;
            }

            if (OnPlayFinalizer != null)
            {
                exception = (Exception?)Invoke(
                    OnPlayFinalizer,
                    new object?[] { prefixArgs[2], exception });
            }

            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }

            return result ?? throw new InvalidOperationException("OnPlay body did not return a Task.");
        }
    }
}
