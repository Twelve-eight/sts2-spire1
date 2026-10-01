using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Actions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Forms;

namespace Spire1.Spire1Code.Run;

internal static class FormNativeSmokeRunner
{
    private const string ReportEnvironmentVariable = "SPIRE1_FORM_SMOKE_REPORT";
    private const string FixedSeed = "FORMNATIVE20261001";
    private const int StartupTimeoutSeconds = 120;
    private const int RunTimeoutSeconds = 120;
    private const int CombatTimeoutSeconds = 120;
    private const int ActionTimeoutSeconds = 60;
    private const int CardInjectionTimeoutSeconds = 60;
    private const int DetachedDrainSeconds = 10;
    private const int MainThreadGateSeconds = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static int _started;

    public static void TryStart(NGame game)
    {
        SmokeRequest request = ParseRequest(System.Environment.GetCommandLineArgs());
        if (!request.Requested)
        {
            return;
        }
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        TaskHelper.RunSafely(RunAndQuitAsync(game, request));
    }

    private static SmokeRequest ParseRequest(IReadOnlyList<string> args)
    {
        HashSet<string> scenarios = new(StringComparer.OrdinalIgnoreCase);
        bool requested = false;
        bool all = false;

        foreach (string raw in args)
        {
            string arg = raw.Trim();
            if (arg.Equals("--form-native-smoke", StringComparison.OrdinalIgnoreCase))
            {
                requested = true;
                all = true;
                continue;
            }
            if (arg.Equals("--form-native-smoke-calm", StringComparison.OrdinalIgnoreCase))
            {
                requested = true;
                scenarios.Add("calm");
                continue;
            }
            if (arg.Equals("--form-native-smoke-wrath", StringComparison.OrdinalIgnoreCase))
            {
                requested = true;
                scenarios.Add("wrath");
                continue;
            }
            if (arg.Equals("--form-native-smoke-divinity", StringComparison.OrdinalIgnoreCase))
            {
                requested = true;
                scenarios.Add("divinity");
                continue;
            }
            if (arg.StartsWith("--form-native-smoke=", StringComparison.OrdinalIgnoreCase)
                || arg.StartsWith("--form-native-smoke:", StringComparison.OrdinalIgnoreCase))
            {
                requested = true;
                string value = arg[(arg.IndexOfAny(new[] { '=', ':' }) + 1)..].Trim();
                if (value.Equals("calm", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("wrath", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("divinity", StringComparison.OrdinalIgnoreCase))
                {
                    scenarios.Add(value.ToLowerInvariant());
                }
                else
                {
                    scenarios.Add("invalid:" + value);
                }
            }
        }

        if (all)
        {
            scenarios.Clear();
            scenarios.Add("calm");
            scenarios.Add("wrath");
            scenarios.Add("divinity");
        }

        return new SmokeRequest(requested, scenarios.ToArray());
    }

    private static async Task RunAndQuitAsync(NGame game, SmokeRequest request)
    {
        var unobservedFaults = new ConcurrentQueue<string>();
        void OnUnobservedFault(Exception exception) => unobservedFaults.Enqueue(exception.ToString());
        TaskHelper.UnobservedFault += OnUnobservedFault;

        int exitCode = 1;
        var quitOperations = new List<DetachedOperation>();
        try
        {
            exitCode = await RunAsync(game, request, unobservedFaults);
        }
        catch (Exception exception)
        {
            MainFile.Logger.Error("Form native smoke runner failed: " + exception);
            exitCode = 1;
        }
        finally
        {
            TaskHelper.UnobservedFault -= OnUnobservedFault;
            var finalPayload = new Dictionary<string, object?>
            {
                ["status"] = exitCode == 0 ? "pending" : "failed",
                ["exitCode"] = exitCode,
                ["quitStatus"] = "pending",
                ["quitDrainSettled"] = false,
                ["quitDrainOutcome"] = "not-observed",
                ["finalEvidencePhase"] = "pre-quit"
            };
            if (exitCode != 0)
            {
                finalPayload["failure"] = "Business smoke failed with exitCode=" + exitCode;
            }

            // Persist an honest pending final evidence before requesting SceneTree.Quit.
            JsonWriteResult preQuitWrite = WriteJsonIfConfigured("final", finalPayload);
            if (!preQuitWrite.Succeeded)
            {
                string failure = "Final JSON evidence write failed: " + preQuitWrite.Failure;
                finalPayload["status"] = "failed";
                finalPayload["evidenceWriteFailure"] = failure;
                finalPayload["failure"] = AppendFailure(finalPayload["failure"]?.ToString(), failure);
                MainFile.Logger.Error(failure);
                JsonWriteResult retry = WriteJsonIfConfigured("final", finalPayload);
                if (!retry.Succeeded)
                {
                    MainFile.Logger.Error("Final JSON evidence retry failed: " + retry.Failure);
                }
            }

            string quitStatus = await QuitOnMainThreadAsync(game, exitCode, quitOperations);
            finalPayload["quitStatus"] = quitStatus;

            bool quitDrainSettled = true;
            if (quitOperations.Count > 0)
            {
                try
                {
                    quitDrainSettled = await DrainDetachedOperationsAsync(quitOperations, finalPayload);
                    if (finalPayload.TryGetValue("detachedOperations", out object? detachedEvidence))
                    {
                        finalPayload["quitDetachedOperations"] = detachedEvidence;
                        finalPayload.Remove("detachedOperations");
                    }
                }
                catch (Exception drainException)
                {
                    quitDrainSettled = false;
                    finalPayload["quitDrainFailure"] = drainException.ToString();
                    MainFile.Logger.Error("Final quit detached drain failed: " + drainException);
                }
            }
            finalPayload["quitDrainSettled"] = quitDrainSettled;
            finalPayload["quitDrainOutcome"] = quitDrainSettled ? "settled" : "not-settled";
            finalPayload["finalEvidencePhase"] = "post-quit";
            if (!quitStatus.StartsWith("executed", StringComparison.Ordinal))
            {
                finalPayload["failure"] = AppendFailure(
                    finalPayload["failure"]?.ToString(),
                    "Final SceneTree.Quit failed: " + quitStatus);
            }
            if (!quitDrainSettled)
            {
                finalPayload["failure"] = AppendFailure(
                    finalPayload["failure"]?.ToString(),
                    "Final quit detached operation did not settle within bounded drain");
            }
            finalPayload["status"] = !finalPayload.ContainsKey("evidenceWriteFailure")
                && exitCode == 0
                && quitStatus.StartsWith("executed", StringComparison.Ordinal)
                && quitDrainSettled
                ? "completed"
                : "failed";

            JsonWriteResult finalWrite = WriteJsonIfConfigured("final", finalPayload);
            if (!finalWrite.Succeeded)
            {
                string failure = "Final JSON evidence write failed: " + finalWrite.Failure;
                finalPayload["status"] = "failed";
                finalPayload["evidenceWriteFailure"] = failure;
                finalPayload["failure"] = AppendFailure(finalPayload["failure"]?.ToString(), failure);
                MainFile.Logger.Error(failure);
                JsonWriteResult retry = WriteJsonIfConfigured("final", finalPayload);
                if (!retry.Succeeded)
                {
                    MainFile.Logger.Error("Final JSON evidence retry failed: " + retry.Failure);
                }
            }
        }
    }

    private static async Task<int> RunAsync(
        NGame game,
        SmokeRequest request,
        ConcurrentQueue<string> unobservedFaults)
    {
        if (request.Scenarios.Length == 0 || request.Scenarios.Any(s => s.StartsWith("invalid:", StringComparison.Ordinal)))
        {
            string reason = request.Scenarios.Length == 0
                ? "No supported scenario was supplied"
                : "Unsupported scenario: " + string.Join(",", request.Scenarios);
            MainFile.Logger.Error("Form native smoke request rejected: " + reason);
            var invalidPayload = new Dictionary<string, object?>
            {
                ["status"] = "failed",
                ["failure"] = reason,
                ["scenarios"] = request.Scenarios
            };
            JsonWriteResult invalidWrite = WriteJsonIfConfigured("invalid", invalidPayload);
            if (!invalidWrite.Succeeded)
            {
                MainFile.Logger.Error("Invalid request JSON evidence write failed: " + invalidWrite.Failure);
            }
            return 2;
        }

        var startupOperations = new List<DetachedOperation>();
        try
        {
            await WaitWithTimeoutAsync(
                () => game.GameStartupComplete,
                TimeSpan.FromSeconds(StartupTimeoutSeconds),
                "game startup completion",
                startupOperations);
        }
        catch (Exception exception)
        {
            var startupDrainPayload = new Dictionary<string, object?>();
            bool startupDrainSettled = true;
            if (startupOperations.Count > 0)
            {
                try
                {
                    startupDrainSettled = await DrainDetachedOperationsAsync(startupOperations, startupDrainPayload);
                }
                catch (Exception drainException)
                {
                    startupDrainSettled = false;
                    startupDrainPayload["detachedDrainFailure"] = drainException.ToString();
                    MainFile.Logger.Error("Startup detached drain failed: " + drainException);
                }
            }
            foreach (string scenario in request.Scenarios)
            {
                Dictionary<string, object?> result = FailedScenario(scenario, exception.ToString(), unobservedFaults);
                result["terminalFailure"] = true;
                result["startupDrainSettled"] = startupDrainSettled;
                if (startupDrainPayload.TryGetValue("detachedOperations", out object? detachedEvidence))
                {
                    result["detachedOperations"] = detachedEvidence;
                }
                if (!startupDrainSettled)
                {
                    result["terminalOperationOutcome"] = "isolated-unfinished-operation";
                    result["cleanup"] = "skipped";
                    result["cleanupSkipped"] = true;
                    result["cleanupSkipReason"] = "startup detached operation did not settle within bounded drain";
                }
                if (!TryWriteScenarioEvidence(scenario, result))
                {
                    break;
                }
            }
            return 1;
        }

        bool bridgeAvailable;
        try
        {
            bridgeAvailable = FormStanceWatcherBridge.TryBind() && FormStanceWatcherBridge.IsAvailable;
        }
        catch (Exception exception)
        {
            bridgeAvailable = false;
            MainFile.Logger.Error("Watcher form bridge bind failed: " + exception);
        }
        if (!bridgeAvailable)
        {
            string reason = "Watcher form bridge unavailable: " + FormStanceWatcherBridge.UnavailableReason;
            MainFile.Logger.Error(reason);
            foreach (string scenario in request.Scenarios)
            {
                Dictionary<string, object?> result = FailedScenario(scenario, reason, unobservedFaults);
                result["terminalFailure"] = true;
                if (!TryWriteScenarioEvidence(scenario, result))
                {
                    break;
                }
            }
            return 1;
        }

        Dictionary<string, object?>? sharedFailure = null;
        foreach (string scenario in request.Scenarios)
        {
            Dictionary<string, object?> result;
            var setupOperations = new List<DetachedOperation>();
            try
            {
                CharacterModel character = await InvokeOnMainThreadWithTimeoutAsync(
                    FindWatcherCharacter,
                    "find Watcher character",
                    setupOperations);
                IReadOnlyList<ActModel> acts = await InvokeOnMainThreadWithTimeoutAsync(
                    ActModel.GetDefaultList,
                    "load default acts",
                    setupOperations);
                EncounterModel encounter = await InvokeOnMainThreadWithTimeoutAsync(
                    () => FindEncounter(acts),
                    "find encounter",
                    setupOperations);
                result = await RunScenarioAsync(
                    game,
                    scenario,
                    character,
                    acts,
                    encounter,
                    unobservedFaults);
            }
            catch (Exception exception)
            {
                result = FailedScenario(scenario, exception.ToString(), unobservedFaults);
                result["terminalFailure"] = true;
                if (setupOperations.Count > 0)
                {
                    bool setupDrainSettled = true;
                    try
                    {
                        setupDrainSettled = await DrainDetachedOperationsAsync(setupOperations, result);
                    }
                    catch (Exception drainException)
                    {
                        setupDrainSettled = false;
                        result["detachedDrainFailure"] = drainException.ToString();
                        MainFile.Logger.Error("Scenario setup detached drain failed: " + drainException);
                    }
                    result["setupDrainSettled"] = setupDrainSettled;
                    if (!setupDrainSettled)
                    {
                        result["terminalOperationOutcome"] = "isolated-unfinished-operation";
                        result["cleanup"] = "skipped";
                        result["cleanupSkipped"] = true;
                        result["cleanupSkipReason"] = "scenario setup detached operation did not settle within bounded drain";
                    }
                }
                MainFile.Logger.Error("Form native smoke " + scenario + " setup failed: " + exception);
            }
            if (!TryWriteScenarioEvidence(scenario, result))
            {
                sharedFailure = result;
                break;
            }
            if (!string.Equals(result["status"]?.ToString(), "passed", StringComparison.Ordinal))
            {
                sharedFailure = result;
            }
            if (result.TryGetValue("terminalFailure", out object? terminalFailure)
                && terminalFailure is true)
            {
                break;
            }
        }

        return sharedFailure == null ? 0 : 1;
    }
    private static async Task<Dictionary<string, object?>> RunScenarioAsync(
        NGame game,
        string scenario,
        CharacterModel character,
        IReadOnlyList<ActModel> acts,
        EncounterModel encounter,
        ConcurrentQueue<string> unobservedFaults)
    {
        var result = new Dictionary<string, object?>
        {
            ["scenario"] = scenario,
            ["startedUtc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["status"] = "failed",
            ["failure"] = "unknown",
            ["formBridgeAvailable"] = FormStanceWatcherBridge.IsAvailable,
            ["encounter"] = encounter.Id.Entry,
            ["encounterRoomType"] = encounter.RoomType.ToString(),
            ["card"] = "unknown",
            ["cardPlay"] = "unknown",
            ["formGateFailureLatched"] = false,
            ["unobservedFaults"] = Array.Empty<string>()
        };

        RunManager? runManager = null;
        int faultsAtStart = unobservedFaults.Count;
        PlayCardAction? action = null;
        Dictionary<string, object?>? actionResult = null;
        bool actionFailureLatched = false;
        bool formFailureLatched = false;
        ScenarioFormExpectation formExpectation = GetScenarioFormExpectation(scenario);
        var terminalOperations = new List<DetachedOperation>();
        try
        {
            runManager = await InvokeOnMainThreadWithTimeoutAsync(
                () => RunManager.Instance,
                "get RunManager instance",
                terminalOperations);
            try
            {
                await InvokeOnMainThreadWithTimeoutAsync(
                    () =>
                    {
                        if (runManager.IsInProgress)
                        {
                            runManager.CleanUp(graceful: false);
                        }
                        return true;
                    },
                    "initial run cleanup",
                    terminalOperations);
            }
            catch
            {
                result["terminalFailure"] = true;
                throw;
            }

            CardModel canonicalCard = await InvokeOnMainThreadWithTimeoutAsync(
                () => FindScenarioCard(scenario),
                "find scenario card",
                terminalOperations);
            result["card"] = canonicalCard.Id.Entry;

            Task startRun = await InvokeOnMainThreadWithTimeoutAsync(
                () => NGame.Instance.StartNewSingleplayerRun(
                    character,
                    shouldSave: false,
                    acts,
                    new[] { ModelDb.Modifier<FormStanceModifier>().ToMutable() },
                    FixedSeed,
                    GameMode.Custom,
                    ascensionLevel: 0),
                "start singleplayer run",
                terminalOperations);
            await AwaitOperationWithTimeoutAsync(
                startRun,
                TimeSpan.FromSeconds(RunTimeoutSeconds),
                "singleplayer run creation for " + scenario,
                terminalOnFailure: true,
                detachedOperations: terminalOperations);

            await WaitForConditionWithTimeoutAsync(
                game,
                () => runManager.IsInProgress && runManager.DebugOnlyGetState()?.Players.Count > 0,
                TimeSpan.FromSeconds(RunTimeoutSeconds),
                "run setup for " + scenario,
                terminalOperations);

            RunState runState = await InvokeOnMainThreadWithTimeoutAsync(
                () => runManager.DebugOnlyGetState()
                    ?? throw new InvalidOperationException("RunState was not available after starting the run"),
                "read run state",
                terminalOperations);
            Player player = await InvokeOnMainThreadWithTimeoutAsync(
                () => runState.Players.FirstOrDefault()
                    ?? throw new InvalidOperationException("Singleplayer run did not create a local player"),
                "read local player",
                terminalOperations);

            Task enterRoom = await InvokeOnMainThreadWithTimeoutAsync(
                () => runManager.EnterRoomDebug(
                    encounter.RoomType,
                    MapPointType.Unassigned,
                    encounter.ToMutable(),
                    showTransition: false),
                "enter encounter room",
                terminalOperations);
            await AwaitOperationWithTimeoutAsync(
                enterRoom,
                TimeSpan.FromSeconds(CombatTimeoutSeconds),
                "encounter entry for " + scenario,
                terminalOnFailure: true,
                detachedOperations: terminalOperations);

            await WaitForConditionWithTimeoutAsync(
                game,
                () => CombatManager.Instance.IsInProgress,
                TimeSpan.FromSeconds(CombatTimeoutSeconds),
                "combat start for " + scenario,
                terminalOperations);
            await WaitForConditionWithTimeoutAsync(
                game,
                () => player.PlayerCombatState?.Phase == PlayerTurnPhase.Play,
                TimeSpan.FromSeconds(CombatTimeoutSeconds),
                "player play phase for " + scenario,
                terminalOperations);

            Creature? target = await InvokeOnMainThreadWithTimeoutAsync(
                () => canonicalCard.TargetType == TargetType.AnyEnemy
                    ? player.Creature.CombatState?.HittableEnemies.FirstOrDefault()
                    : null,
                "find combat target",
                terminalOperations);
            Dictionary<string, object?> beforeSnapshot = await InvokeOnMainThreadWithTimeoutAsync(
                () => Snapshot(player, target),
                "snapshot before card",
                terminalOperations);
            result["before"] = beforeSnapshot;
            result["formGateBefore"] = BuildFormGateEvidence(beforeSnapshot, formExpectation);

            CardModel card = await InvokeOnMainThreadWithTimeoutAsync(
                () => player.Creature.CombatState?.CreateCard(canonicalCard, player)
                    ?? throw new InvalidOperationException("CombatState was unavailable while creating the card"),
                "create combat card",
                terminalOperations);
            Task addCard = await InvokeOnMainThreadWithTimeoutAsync(
                () => CardPileCmd.Add(card, PileType.Hand, skipVisuals: true),
                "add card to hand",
                terminalOperations);
            await AwaitOperationWithTimeoutAsync(
                addCard,
                TimeSpan.FromSeconds(CardInjectionTimeoutSeconds),
                "card injection for " + scenario,
                terminalOnFailure: true,
                detachedOperations: terminalOperations);
            bool cardInHand = await InvokeOnMainThreadWithTimeoutAsync(
                () => card.Pile?.Type == PileType.Hand,
                "verify card hand pile",
                terminalOperations);
            if (!cardInHand)
            {
                throw new InvalidOperationException("The canonical card was not added to the hand");
            }

            PlayCardAction currentAction = await InvokeOnMainThreadWithTimeoutAsync(
                () => new PlayCardAction(card, target),
                "create PlayCardAction",
                terminalOperations);
            action = currentAction;
            Dictionary<string, object?> currentActionResult = await InvokeOnMainThreadWithTimeoutAsync(
                () => CreateActionResult(currentAction),
                "record initial action evidence",
                terminalOperations);
            actionResult = currentActionResult;
            await InvokeOnMainThreadWithTimeoutAsync(
                () =>
                {
                    runManager.ActionQueueSynchronizer.RequestEnqueue(currentAction);
                    return true;
                },
                "enqueue PlayCardAction",
                terminalOperations);
            try
            {
                await AwaitOperationWithTimeoutAsync(
                    currentAction.CompletionTask,
                    TimeSpan.FromSeconds(ActionTimeoutSeconds),
                    "card action completion for " + scenario,
                    terminalOnFailure: true,
                    detachedOperations: terminalOperations);
            }
            catch (TerminalOperationException exception)
            {
                actionFailureLatched = true;
                result["terminalFailure"] = true;
                result["terminalOperation"] = exception.Description;
                result["terminalOperationOutcome"] = exception.TimedOut
                    ? "timed-out-no-cancellation"
                    : "failed";
                await TryCancelActionAsync(
                    currentAction,
                    currentActionResult,
                    exception.TimedOut
                        ? "Game action completion timed out"
                        : "Game action completion failed",
                    exception,
                    terminalOperations);
            }
            catch (OperationCanceledException exception)
            {
                actionFailureLatched = true;
                result["terminalFailure"] = true;
                await TryCancelActionAsync(
                    currentAction,
                    currentActionResult,
                    "Game action completion was canceled",
                    exception,
                    terminalOperations);
            }
            catch (TimeoutException exception)
            {
                actionFailureLatched = true;
                result["terminalFailure"] = true;
                await TryCancelActionAsync(
                    currentAction,
                    currentActionResult,
                    "Game action completion timed out",
                    exception,
                    terminalOperations);
            }
            catch (Exception exception)
            {
                actionFailureLatched = true;
                result["terminalFailure"] = true;
                await RecordActionEvidenceAsync(
                    currentAction,
                    currentActionResult,
                    exception.ToString(),
                    terminalOperations);
            }

            await RecordActionEvidenceAsync(
                currentAction,
                currentActionResult,
                currentActionResult["failure"]?.ToString(),
                terminalOperations);
            result["cardPlay"] = currentActionResult;
            bool hasNewUnobservedFault = unobservedFaults.Count > faultsAtStart;
            bool actionPassed;
            try
            {
                actionPassed = await InvokeOnMainThreadWithTimeoutAsync(
                    () => !actionFailureLatched
                        && currentActionResult["failure"] is null
                        && currentAction.State == GameActionState.Finished
                        && currentAction.Exception == null
                        && currentAction.CompletionTask.Status == TaskStatus.RanToCompletion
                        && !hasNewUnobservedFault,
                    "evaluate action result",
                    terminalOperations);
            }
            catch (Exception exception)
            {
                actionPassed = false;
                actionFailureLatched = true;
                result["terminalFailure"] = true;
                currentActionResult["evidenceFailure"] = exception.ToString();
                currentActionResult["failure"] = exception.ToString();
                result["failure"] = exception.ToString();
            }
            if (actionPassed)
            {
                Dictionary<string, object?> afterSnapshot = await InvokeOnMainThreadWithTimeoutAsync(
                    () => Snapshot(player, target),
                    "snapshot after card",
                    terminalOperations);
                result["after"] = afterSnapshot;
                Dictionary<string, object?> formGate = BuildFormGateEvidence(afterSnapshot, formExpectation);
                result["formGateAfter"] = formGate;
                if (formGate["passed"] is true && !formFailureLatched)
                {
                    result["status"] = "passed";
                    result["failure"] = null;
                }
                else
                {
                    formFailureLatched = true;
                    result["formGateFailureLatched"] = true;
                    result["status"] = "failed";
                    result["failure"] = formGate["failure"]?.ToString()
                        ?? "Form state gate failed after successful card action";
                }
            }
            else
            {
                actionFailureLatched = true;
                result["terminalFailure"] = true;
                string failure = currentActionResult["failure"]?.ToString()
                    ?? BuildActionFailure(
                        currentActionResult,
                        hasNewUnobservedFault);
                currentActionResult["failure"] = failure;
                await RecordActionEvidenceAsync(
                    currentAction,
                    currentActionResult,
                    failure,
                    terminalOperations);
                result["cardPlay"] = currentActionResult;
                result["status"] = "failed";
                result["failure"] = failure;
            }
        }
        catch (TerminalOperationException exception)
        {
            result["status"] = "failed";
            result["terminalFailure"] = true;
            result["terminalOperation"] = exception.Description;
            result["terminalOperationOutcome"] = exception.TimedOut
                ? "timed-out-no-cancellation"
                : "failed";
            result["failure"] = exception.ToString();
            MainFile.Logger.Error("Form native smoke " + scenario + " terminal operation failed: " + exception);
        }
        catch (Exception exception)
        {
            result["status"] = "failed";
            if (exception is MainThreadGateException || exception is TimeoutException)
            {
                result["terminalFailure"] = true;
            }
            result["failure"] = actionResult?["failure"]?.ToString() ?? exception.ToString();
            if (action != null && actionResult != null)
            {
                actionFailureLatched = true;
                await RecordActionEvidenceAsync(
                    action,
                    actionResult,
                    result["failure"]?.ToString(),
                    terminalOperations);
                result["cardPlay"] = actionResult;
            }
            MainFile.Logger.Error("Form native smoke " + scenario + " failed: " + exception);
        }
        finally
        {
            // Final action evidence is itself a Godot read; register its deferred gate before draining.
            if (action != null && actionResult != null)
            {
                try
                {
                    await RecordActionEvidenceAsync(
                        action,
                        actionResult,
                        actionResult["failure"]?.ToString(),
                        terminalOperations);
                }
                catch (Exception evidenceException)
                {
                    actionFailureLatched = true;
                    actionResult["failureLatched"] = true;
                    actionResult["status"] = "failed";
                    actionResult["evidenceFailure"] = evidenceException.ToString();
                    actionResult["failure"] = AppendFailure(
                        actionResult["failure"]?.ToString(),
                        evidenceException.ToString());
                    result["status"] = "failed";
                    result["terminalFailure"] = true;
                    result["failure"] = AppendFailure(
                        result["failure"]?.ToString(),
                        evidenceException.ToString());
                }
                actionResult["failureLatched"] = actionFailureLatched;
                result["cardPlay"] = actionResult;
            }

            bool cleanupAllowed = true;
            if (terminalOperations.Count > 0)
            {
                try
                {
                    cleanupAllowed = await DrainDetachedOperationsAsync(terminalOperations, result);
                    if (!cleanupAllowed)
                    {
                        result["terminalFailure"] = true;
                        result["terminalOperationOutcome"] = "isolated-unfinished-operation";
                        result["cleanup"] = "skipped";
                        result["cleanupSkipped"] = true;
                        result["cleanupSkipReason"] = "detached operation did not settle within bounded drain";
                        result["status"] = "failed";
                        result["failure"] = result["failure"]?.ToString()
                            ?? "Detached operation was not settled; cleanup was skipped";
                    }
                }
                catch (Exception drainException)
                {
                    cleanupAllowed = false;
                    result["terminalFailure"] = true;
                    result["terminalOperationOutcome"] = "drain-failed";
                    result["detachedDrainFailure"] = drainException.ToString();
                    result["cleanup"] = "skipped";
                    result["cleanupSkipped"] = true;
                    result["cleanupSkipReason"] = "detached operation drain failed";
                    result["status"] = "failed";
                    result["failure"] = drainException.ToString();
                }
            }
            result["formGateFailureLatched"] = formFailureLatched;
            result["unobservedFaults"] = unobservedFaults.ToArray();
            result["completedUtc"] = DateTimeOffset.UtcNow.ToString("O");
            if (cleanupAllowed && runManager != null)
            {
                int detachedCountBeforeCleanup = terminalOperations.Count;
                try
                {
                    await InvokeOnMainThreadWithTimeoutAsync(
                        () =>
                        {
                            if (runManager.IsInProgress)
                            {
                                runManager.CleanUp(graceful: false);
                            }
                            return true;
                        },
                        "final run cleanup",
                        terminalOperations);
                    result["cleanup"] = "completed";
                }
                catch (Exception cleanupException)
                {
                    result["cleanup"] = "failed";
                    result["cleanupFailure"] = cleanupException.ToString();
                    result["terminalFailure"] = true;
                    result["status"] = "failed";
                    result["failure"] = cleanupException.ToString();

                    IReadOnlyList<DetachedOperation> cleanupDetached = terminalOperations
                        .Skip(detachedCountBeforeCleanup)
                        .ToArray();
                    if (cleanupDetached.Count > 0)
                    {
                        try
                        {
                            bool cleanupGateSettled = await DrainDetachedOperationsAsync(cleanupDetached, result);
                            if (!cleanupGateSettled)
                            {
                                result["cleanup"] = "skipped";
                                result["cleanupSkipped"] = true;
                                result["cleanupSkipReason"] = "final cleanup deferred gate did not settle within bounded drain";
                                result["terminalOperationOutcome"] = "isolated-unfinished-operation";
                            }
                        }
                        catch (Exception cleanupDrainException)
                        {
                            result["cleanup"] = "skipped";
                            result["cleanupSkipped"] = true;
                            result["cleanupSkipReason"] = "final cleanup deferred gate drain failed";
                            result["terminalOperationOutcome"] = "drain-failed";
                            result["detachedDrainFailure"] = cleanupDrainException.ToString();
                            result["failure"] = AppendFailure(
                                result["failure"]?.ToString(),
                                cleanupDrainException.ToString());
                        }
                    }
                }
            }
            else
            {
                result["cleanup"] = "skipped";
                result["cleanupSkipped"] = true;
                if (runManager == null)
                {
                    result["cleanupSkipReason"] = "RunManager was unavailable after a terminal main-thread gate failure";
                }
            }
        }

        return result;
    }

    private static Dictionary<string, object?> CreateActionResult(PlayCardAction action)
    {
        return new Dictionary<string, object?>
        {
            ["startedUtc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["status"] = "pending",
            ["state"] = action.State.ToString(),
            ["completionTaskOutcome"] = DescribeCompletionTask(action.CompletionTask),
            ["completedUtc"] = "unknown",
            ["cancelled"] = action.State == GameActionState.Canceled || action.CompletionTask.IsCanceled,
            ["exception"] = action.Exception?.ToString(),
            ["failure"] = null,
            ["failureLatched"] = false
        };
    }

    private static async Task TryCancelActionAsync(
        PlayCardAction action,
        Dictionary<string, object?> actionResult,
        string failure,
        Exception exception,
        List<DetachedOperation> detachedOperations)
    {
        actionResult["failure"] = failure + ": " + exception;
        try
        {
            await InvokeOnMainThreadWithTimeoutAsync(
                () =>
                {
                    if (action.State != GameActionState.Finished && action.State != GameActionState.Canceled)
                    {
                        action.Cancel();
                        actionResult["cancelRequest"] = "requested";
                    }
                    else
                    {
                        actionResult["cancelRequest"] = "not-needed";
                    }
                    RecordActionEvidence(actionResult, action, actionResult["failure"]?.ToString());
                    return true;
                },
                "cancel PlayCardAction",
                detachedOperations);
        }
        catch (Exception cancelException)
        {
            actionResult["cancelRequest"] = "failed: " + cancelException;
            actionResult["failure"] = failure + ": " + exception + "; cancel failed: " + cancelException;
            actionResult["evidenceFailure"] = cancelException.ToString();
        }
    }

    private static async Task RecordActionEvidenceAsync(
        PlayCardAction action,
        Dictionary<string, object?> actionResult,
        string? failure,
        List<DetachedOperation> detachedOperations)
    {
        await InvokeOnMainThreadWithTimeoutAsync(
            () =>
            {
                RecordActionEvidence(actionResult, action, failure);
                return true;
            },
            "record PlayCardAction evidence",
            detachedOperations);
    }

    private static void RecordActionEvidence(
        Dictionary<string, object?> actionResult,
        PlayCardAction action,
        string? failure)
    {
        actionResult["state"] = action.State.ToString();
        actionResult["completionTaskOutcome"] = DescribeCompletionTask(action.CompletionTask);
        bool cancelled = action.State == GameActionState.Canceled || action.CompletionTask.IsCanceled;
        actionResult["cancelled"] = cancelled;
        string? exception = action.Exception?.ToString();
        actionResult["exception"] = exception;
        actionResult["completedUtc"] = action.CompletionTask.IsCompleted
            ? DateTimeOffset.UtcNow.ToString("O")
            : "unknown";
        if (failure != null)
        {
            actionResult["failure"] = failure;
        }

        actionResult["status"] = DeriveActionStatus(
            action,
            cancelled,
            exception,
            actionResult["failure"]?.ToString());
    }

    private static string BuildActionFailure(
        Dictionary<string, object?> actionResult,
        bool hasNewUnobservedFault)
    {
        if (hasNewUnobservedFault)
        {
            return "TaskHelper.UnobservedFault was raised while evaluating the action";
        }

        string state = actionResult["state"]?.ToString() ?? "unknown";
        string completion = actionResult["completionTaskOutcome"]?.ToString() ?? "unknown";
        bool cancelled = actionResult["cancelled"] is true;
        string? exception = actionResult["exception"]?.ToString();
        return "Game action did not finish successfully: state=" + state
            + ", completionTaskOutcome=" + completion
            + ", cancelled=" + cancelled
            + (exception == null ? string.Empty : ", exception=" + exception);
    }

    private static string DeriveActionStatus(
        PlayCardAction action,
        bool cancelled,
        string? exception,
        string? failure)
    {
        if (ContainsIgnoreCase(failure, "timed out") || ContainsIgnoreCase(failure, "timeout"))
        {
            return "timeout";
        }
        if (cancelled
            || ContainsIgnoreCase(failure, "canceled")
            || ContainsIgnoreCase(failure, "cancelled")
            || ContainsIgnoreCase(failure, "cancel"))
        {
            return "cancelled";
        }
        if (exception != null
            || action.CompletionTask.IsFaulted
            || ContainsIgnoreCase(failure, "UnobservedFault"))
        {
            return "faulted";
        }
        if (!action.CompletionTask.IsCompleted)
        {
            return "pending";
        }
        if (action.State == GameActionState.Finished
            && action.CompletionTask.Status == TaskStatus.RanToCompletion
            && failure == null)
        {
            return "completed";
        }
        return "failed";
    }

    private static bool ContainsIgnoreCase(string? value, string fragment)
        => value?.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;

    private static string DescribeCompletionTask(Task task)
    {
        if (task.IsCanceled)
        {
            return "canceled";
        }
        if (task.IsFaulted)
        {
            return "faulted: " + task.Exception;
        }
        if (task.IsCompleted)
        {
            return "completed";
        }
        return "pending";
    }

    private static Dictionary<string, object?> FailedScenario(
        string scenario,
        string reason,
        ConcurrentQueue<string> unobservedFaults)
    {
        return new Dictionary<string, object?>
        {
            ["scenario"] = scenario,
            ["status"] = "failed",
            ["failure"] = reason,
            ["formBridgeAvailable"] = FormStanceWatcherBridge.IsAvailable,
            ["unobservedFaults"] = unobservedFaults.ToArray(),
            ["completedUtc"] = DateTimeOffset.UtcNow.ToString("O")
        };
    }

    private static CharacterModel FindWatcherCharacter()
    {
        CharacterModel? character = ModelDb.AllCharacters.FirstOrDefault(model =>
            string.Equals(model.GetType().FullName, "WatcherMod.Watcher", StringComparison.Ordinal));
        return character ?? throw new InvalidOperationException("Real Watcher character was not found in ModelDb.AllCharacters");
    }

    private static EncounterModel FindEncounter(IReadOnlyList<ActModel> acts)
    {
        ActModel act = acts.FirstOrDefault()
            ?? throw new InvalidOperationException("ActModel.GetDefaultList returned no acts");
        EncounterModel? encounter = act.AllRegularEncounters.FirstOrDefault()
            ?? act.AllEliteEncounters.FirstOrDefault();
        return encounter ?? throw new InvalidOperationException("Default act has no regular or elite encounter");
    }

    private static CardModel FindScenarioCard(string scenario)
    {
        string entry = scenario switch
        {
            "calm" => "WATCHER_VIGILANCE",
            "wrath" => "WATCHER_ERUPTION_P",
            "divinity" => "WATCHER_BLASPHEMY",
            _ => throw new InvalidOperationException("Unsupported scenario: " + scenario)
        };
        CardModel? card = ModelDb.AllCards.FirstOrDefault(model =>
            string.Equals(model.Id.Entry, entry, StringComparison.Ordinal));
        return card ?? throw new InvalidOperationException("Real Watcher card was not found in ModelDb.AllCards: " + entry);
    }

    private sealed record ScenarioFormExpectation(
        FormStanceKind NativeKind,
        Type CarrierType,
        Type FirstEffectType,
        Type SecondEffectType);

    private static ScenarioFormExpectation GetScenarioFormExpectation(string scenario)
    {
        return scenario switch
        {
            "calm" => new(
                FormStanceKind.Calm,
                typeof(VoidSerpentStancePower),
                typeof(VoidFormEffectPower),
                typeof(SerpentFormPower)),
            "wrath" => new(
                FormStanceKind.Wrath,
                typeof(DemonReaperStancePower),
                typeof(DemonFormPower),
                typeof(ReaperFormEffectPower)),
            "divinity" => new(
                FormStanceKind.Divinity,
                typeof(EchoCelestialStancePower),
                typeof(EchoFormEffectPower),
                typeof(CelestialFormPower)),
            _ => throw new InvalidOperationException("Unsupported scenario: " + scenario)
        };
    }

    private static Dictionary<string, object?> BuildFormGateEvidence(
        Dictionary<string, object?> snapshot,
        ScenarioFormExpectation expectation)
    {
        string actualNativeStance = snapshot.TryGetValue("nativeWatcherStance", out object? nativeStance)
            ? nativeStance?.ToString() ?? "unknown"
            : "missing";
        bool formModeSelected = snapshot.TryGetValue("formModeSelected", out object? selected)
            && selected is true;
        string[] carrierTypes = snapshot.TryGetValue("formCarrierTypes", out object? carriers)
            && carriers is string[] carrierArray
            ? carrierArray
            : Array.Empty<string>();
        string[] effectTypes = snapshot.TryGetValue("formEffectTypes", out object? effects)
            && effects is string[] effectArray
            ? effectArray
            : Array.Empty<string>();
        string expectedCarrierType = TypeName(expectation.CarrierType);
        string expectedFirstEffectType = TypeName(expectation.FirstEffectType);
        string expectedSecondEffectType = TypeName(expectation.SecondEffectType);
        int carrierCount = carrierTypes.Count(type => string.Equals(type, expectedCarrierType, StringComparison.Ordinal));
        int firstEffectCount = effectTypes.Count(type => string.Equals(type, expectedFirstEffectType, StringComparison.Ordinal));
        int secondEffectCount = effectTypes.Count(type => string.Equals(type, expectedSecondEffectType, StringComparison.Ordinal));
        bool nativeStanceMatches = string.Equals(
            actualNativeStance,
            expectation.NativeKind.ToString(),
            StringComparison.Ordinal);
        bool carrierPresent = carrierCount > 0;
        bool firstEffectPresent = firstEffectCount > 0;
        bool secondEffectPresent = secondEffectCount > 0;
        bool passed = formModeSelected
            && nativeStanceMatches
            && carrierPresent
            && firstEffectPresent
            && secondEffectPresent;
        string? failure = passed
            ? null
            : "Form state gate failed: selected=" + formModeSelected
                + ", nativeStance=" + actualNativeStance
                + ", expectedNativeStance=" + expectation.NativeKind
                + ", carrier=" + carrierPresent
                + " (" + expectedCarrierType + ", count=" + carrierCount + ")"
                + ", firstEffect=" + firstEffectPresent
                + " (" + expectedFirstEffectType + ", count=" + firstEffectCount + ")"
                + ", secondEffect=" + secondEffectPresent
                + " (" + expectedSecondEffectType + ", count=" + secondEffectCount + ")";
        return new Dictionary<string, object?>
        {
            ["passed"] = passed,
            ["formModeSelected"] = formModeSelected,
            ["nativeStance"] = actualNativeStance,
            ["expectedNativeStance"] = expectation.NativeKind.ToString(),
            ["nativeStanceMatches"] = nativeStanceMatches,
            ["carrierTypes"] = carrierTypes,
            ["expectedCarrierType"] = expectedCarrierType,
            ["carrierCount"] = carrierCount,
            ["carrierPresent"] = carrierPresent,
            ["effectTypes"] = effectTypes,
            ["expectedEffectTypes"] = new[] { expectedFirstEffectType, expectedSecondEffectType },
            ["effectCounts"] = new Dictionary<string, int>
            {
                [expectedFirstEffectType] = firstEffectCount,
                [expectedSecondEffectType] = secondEffectCount
            },
            ["firstEffectPresent"] = firstEffectPresent,
            ["secondEffectPresent"] = secondEffectPresent,
            ["failure"] = failure
        };
    }

    private static string TypeName(Type type) => type.FullName ?? type.Name;
    private static Dictionary<string, object?> Snapshot(Player player, Creature? target)
    {
        string nativeStance = "unknown";
        try
        {
            nativeStance = FormStanceWatcherBridge.CurrentKind(player).ToString();
        }
        catch
        {
        }

        string[] formCarrierTypes = player.Creature.Powers
            .OfType<WatcherFormStancePower>()
            .Select(power => power.GetType().FullName ?? power.GetType().Name)
            .ToArray();
        string[] formEffectTypes = player.Creature.Powers
            .Where(power => !(power is WatcherFormStancePower))
            .Select(power => power.GetType().FullName ?? power.GetType().Name)
            .Where(name => name.IndexOf("Form", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        return new Dictionary<string, object?>
        {
            ["nativeWatcherStance"] = nativeStance,
            ["formCarrierTypes"] = formCarrierTypes,
            ["formEffectTypes"] = formEffectTypes,
            ["formCarrierOrEffectTypes"] = formCarrierTypes.Concat(formEffectTypes).ToArray(),
            ["formModeSelected"] = FormStanceMode.IsSelected(player),
            ["formBridgeAvailable"] = FormStanceWatcherBridge.IsAvailable,
            ["energy"] = player.PlayerCombatState is { } combatState ? combatState.Energy : "unknown",
            ["handCount"] = PileType.Hand.GetPile(player).Cards.Count,
            ["turnNumber"] = player.PlayerCombatState is { } combatStateForTurn ? combatStateForTurn.TurnNumber : "unknown",
            ["combatRound"] = player.Creature.CombatState is { } combatStateForRound ? combatStateForRound.RoundNumber : "unknown",
            ["target"] = target == null
                ? "unknown"
                : new Dictionary<string, object?>
                {
                    ["type"] = target.GetType().FullName ?? target.GetType().Name,
                    ["logName"] = target.LogName,
                    ["currentHp"] = target.CurrentHp,
                    ["maxHp"] = target.MaxHp,
                    ["isDead"] = target.IsDead,
                    ["powerTypes"] = target.Powers
                        .Select(power => power.GetType().FullName ?? power.GetType().Name)
                        .ToArray()
                }
        };
    }

    private static async Task WaitWithTimeoutAsync(
        Func<Task> operationFactory,
        TimeSpan timeout,
        string description,
        List<DetachedOperation> detachedOperations)
    {
        Task operation = await InvokeOnMainThreadWithTimeoutAsync(
            operationFactory,
            description + " main thread submission",
            detachedOperations);
        await AwaitOperationWithTimeoutAsync(
            operation,
            timeout,
            description,
            terminalOnFailure: true,
            detachedOperations: detachedOperations);
    }

    private static async Task WaitForConditionWithTimeoutAsync(
        NGame game,
        Func<bool> condition,
        TimeSpan timeout,
        string description,
        List<DetachedOperation> detachedOperations)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;
        while (true)
        {
            TimeSpan remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                throw new TerminalOperationException(
                    description,
                    timedOut: true,
                    detachedOperation: null,
                    innerException: null);
            }

            bool conditionMet = await InvokeOnMainThreadWithTimeoutAsync(
                condition,
                description + " condition",
                detachedOperations,
                BoundedMainThreadGateTimeout(remaining));
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TerminalOperationException(
                    description,
                    timedOut: true,
                    detachedOperation: null,
                    innerException: null);
            }
            if (conditionMet)
            {
                return;
            }

            remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                throw new TerminalOperationException(
                    description,
                    timedOut: true,
                    detachedOperation: null,
                    innerException: null);
            }

            using CancellationTokenSource frameTimeout = new();
            Task frameTask = await InvokeOnMainThreadWithTimeoutAsync(
                () => game.AwaitProcessFrame(),
                description + " process frame submission",
                detachedOperations,
                BoundedMainThreadGateTimeout(remaining));
            remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                if (!frameTask.IsCompleted)
                {
                    var detachedOperation = new DetachedOperation(frameTask, description + " process frame");
                    detachedOperations.Add(detachedOperation);
                    ObserveDetachedTask(detachedOperation);
                    throw new TerminalOperationException(
                        description,
                        timedOut: true,
                        detachedOperation: detachedOperation,
                        innerException: null);
                }

                try
                {
                    await frameTask;
                }
                catch (Exception frameException)
                {
                    throw new TerminalOperationException(
                        description + " process frame",
                        timedOut: false,
                        detachedOperation: null,
                        innerException: frameException);
                }
                throw new TerminalOperationException(
                    description,
                    timedOut: true,
                    detachedOperation: null,
                    innerException: null);
            }

            Task timeoutTask = Task.Delay(remaining, frameTimeout.Token);
            Task completed = await Task.WhenAny(frameTask, timeoutTask);
            frameTimeout.Cancel();
            if (completed != frameTask)
            {
                var detachedOperation = new DetachedOperation(frameTask, description + " process frame");
                detachedOperations.Add(detachedOperation);
                ObserveDetachedTask(detachedOperation);
                throw new TerminalOperationException(
                    description,
                    timedOut: true,
                    detachedOperation: detachedOperation,
                    innerException: null);
            }

            try
            {
                await frameTask;
            }
            catch (Exception frameException)
            {
                throw new TerminalOperationException(
                    description + " process frame",
                    timedOut: false,
                    detachedOperation: null,
                    innerException: frameException);
            }
            if (DateTimeOffset.UtcNow >= deadline)
            {
                throw new TerminalOperationException(
                    description,
                    timedOut: true,
                    detachedOperation: null,
                    innerException: null);
            }
        }
    }
    private static Task<T> InvokeOnMainThreadAsync<T>(Func<T> operation, string description)
    {
        if (NGame.IsMainThread())
        {
            try
            {
                return Task.FromResult(operation());
            }
            catch (Exception exception)
            {
                return Task.FromException<T>(new MainThreadGateException(description, "direct", exception));
            }
        }

        var completion = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            Callable.From(() =>
            {
                try
                {
                    completion.TrySetResult(operation());
                }
                catch (Exception exception)
                {
                    completion.TrySetException(new MainThreadGateException(description, "deferred", exception));
                }
            }).CallDeferred();
        }
        catch (Exception exception)
        {
            completion.TrySetException(new MainThreadGateException(description, "submit", exception));
        }
        return completion.Task;
    }

    private static TimeSpan BoundedMainThreadGateTimeout(TimeSpan remaining)
    {
        TimeSpan gateLimit = TimeSpan.FromSeconds(MainThreadGateSeconds);
        return remaining < gateLimit ? remaining : gateLimit;
    }

    private static Task CreateDetachedTaskDrain<T>(Task<T> invocation)
    {
        if (!typeof(Task).IsAssignableFrom(typeof(T)))
        {
            return invocation;
        }
        return AwaitNestedTaskAsync(invocation);
    }

    private static async Task AwaitNestedTaskAsync<T>(Task<T> invocation)
    {
        T nested = await invocation;
        if (nested is Task nestedTask)
        {
            await nestedTask;
        }
    }
    private static async Task<T> InvokeOnMainThreadWithTimeoutAsync<T>(
        Func<T> operation,
        string description,
        List<DetachedOperation> detachedOperations,
        TimeSpan? timeout = null)
    {
        Task<T> invocation = InvokeOnMainThreadAsync(operation, description);
        TimeSpan gateTimeout = timeout ?? TimeSpan.FromSeconds(MainThreadGateSeconds);
        Task completed = await Task.WhenAny(
            invocation,
            Task.Delay(gateTimeout));
        if (completed != invocation)
        {
            var detachedOperation = new DetachedOperation(
                CreateDetachedTaskDrain(invocation),
                description + " main-thread gate");
            detachedOperations.Add(detachedOperation);
            ObserveDetachedTask(detachedOperation);
            throw new MainThreadGateException(description, "timeout", null, detachedOperation);
        }
        return await invocation;
    }
    private static async Task<string> QuitOnMainThreadAsync(
        NGame game,
        int exitCode,
        List<DetachedOperation> detachedOperations)
    {
        try
        {
            await InvokeOnMainThreadWithTimeoutAsync(
                () =>
                {
                    game.GetTree().Quit(exitCode);
                    return true;
                },
                "final SceneTree.Quit",
                detachedOperations);
            return NGame.IsMainThread()
                ? "executed-main-thread"
                : "executed-deferred-main-thread";
        }
        catch (Exception exception)
        {
            return "failed: " + exception;
        }
    }

    private sealed class MainThreadGateException : Exception
    {
        public MainThreadGateException(
            string description,
            string phase,
            Exception? innerException,
            DetachedOperation? detachedOperation = null)
            : base("Main thread gate failed for " + description + " at " + phase, innerException)
        {
            Description = description;
            Phase = phase;
            DetachedOperation = detachedOperation;
        }

        public string Description { get; }
        public string Phase { get; }
        public DetachedOperation? DetachedOperation { get; }
    }

    private sealed class DetachedOperation
    {
        public DetachedOperation(Task task, string description)
        {
            Task = task is Task<Task> nestedTask ? nestedTask.Unwrap() : task;
            Description = description;
        }

        public Task Task { get; }
        public string Description { get; }
        public string Outcome { get; set; } = "pending";
        public string? Failure { get; set; }
    }

    private sealed class TerminalOperationException : Exception
    {
        public TerminalOperationException(
            string description,
            bool timedOut,
            DetachedOperation? detachedOperation,
            Exception? innerException)
            : base(
                (timedOut ? "Timed out waiting for " : "Operation failed for ") + description,
                innerException)
        {
            Description = description;
            TimedOut = timedOut;
            DetachedOperation = detachedOperation;
        }

        public string Description { get; }
        public bool TimedOut { get; }
        public DetachedOperation? DetachedOperation { get; }
    }

    private static async Task AwaitOperationWithTimeoutAsync(
        Task operation,
        TimeSpan timeout,
        string description,
        bool terminalOnFailure = false,
        List<DetachedOperation>? detachedOperations = null)
    {
        if (operation.IsCompleted)
        {
            try
            {
                await operation;
            }
            catch (Exception exception) when (terminalOnFailure)
            {
                throw new TerminalOperationException(description, timedOut: false, detachedOperation: null, innerException: exception);
            }
            return;
        }

        using CancellationTokenSource timeoutSource = new();
        Task timeoutTask = Task.Delay(timeout, timeoutSource.Token);
        Task completed = await Task.WhenAny(operation, timeoutTask);
        timeoutSource.Cancel();
        if (completed != operation)
        {
            var detachedOperation = new DetachedOperation(operation, description);
            detachedOperations?.Add(detachedOperation);
            ObserveDetachedTask(detachedOperation);
            if (terminalOnFailure)
            {
                throw new TerminalOperationException(
                    description,
                    timedOut: true,
                    detachedOperation: detachedOperation,
                    innerException: null);
            }
            throw new TimeoutException("Timed out waiting for " + description);
        }

        try
        {
            await operation;
        }
        catch (Exception exception) when (terminalOnFailure)
        {
            throw new TerminalOperationException(description, timedOut: false, detachedOperation: null, innerException: exception);
        }
    }

    private static void ObserveDetachedTask(DetachedOperation detachedOperation)
    {
        _ = detachedOperation.Task.ContinueWith(
            completed =>
            {
                if (completed.IsCanceled)
                {
                    detachedOperation.Outcome = "task-reported-canceled";
                    return;
                }
                if (completed.IsFaulted && completed.Exception != null)
                {
                    detachedOperation.Outcome = "faulted";
                    detachedOperation.Failure = completed.Exception.ToString();
                    MainFile.Logger.Error(
                        "Detached form smoke task failed after "
                        + detachedOperation.Description
                        + ": "
                        + completed.Exception);
                    return;
                }
                detachedOperation.Outcome = "completed";
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static async Task<bool> DrainDetachedOperationsAsync(
        IReadOnlyList<DetachedOperation> detachedOperations,
        Dictionary<string, object?> result)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(DetachedDrainSeconds);
        bool allSettled = true;
        var evidence = new List<Dictionary<string, object?>>();
        foreach (DetachedOperation detachedOperation in detachedOperations)
        {
            TimeSpan remaining = deadline - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                detachedOperation.Outcome = "isolated-unfinished-operation";
                allSettled = false;
            }
            else
            {
                Task completed = await Task.WhenAny(detachedOperation.Task, Task.Delay(remaining));
                if (completed == detachedOperation.Task)
                {
                    try
                    {
                        await detachedOperation.Task;
                        detachedOperation.Outcome = "completed";
                    }
                    catch (OperationCanceledException exception)
                    {
                        detachedOperation.Outcome = "task-reported-canceled";
                        detachedOperation.Failure = exception.ToString();
                    }
                    catch (Exception exception)
                    {
                        detachedOperation.Outcome = "faulted";
                        detachedOperation.Failure = exception.ToString();
                    }
                }
                else
                {
                    detachedOperation.Outcome = "isolated-unfinished-operation";
                    allSettled = false;
                }
            }

            evidence.Add(new Dictionary<string, object?>
            {
                ["description"] = detachedOperation.Description,
                ["outcome"] = detachedOperation.Outcome,
                ["failure"] = detachedOperation.Failure,
                ["underlyingCancellationRequested"] = false,
                ["isolationBoundary"] = detachedOperation.Outcome == "isolated-unfinished-operation"
            });
        }

        result["detachedOperations"] = evidence;
        return allSettled;
    }

    private static bool TryWriteScenarioEvidence(
        string scenario,
        Dictionary<string, object?> payload)
    {
        JsonWriteResult write = WriteJsonIfConfigured(scenario, payload);
        if (write.Succeeded)
        {
            return true;
        }

        string failure = "Scenario JSON evidence write failed: " + write.Failure;
        payload["status"] = "failed";
        payload["terminalFailure"] = true;
        payload["evidenceWriteFailure"] = failure;
        payload["failure"] = AppendFailure(payload["failure"]?.ToString(), failure);
        MainFile.Logger.Error(failure);
        JsonWriteResult retry = WriteJsonIfConfigured(scenario, payload);
        if (!retry.Succeeded)
        {
            MainFile.Logger.Error("Scenario JSON evidence retry failed: " + retry.Failure);
        }
        return false;
    }

    private static string AppendFailure(string? existing, string addition)
        => string.IsNullOrWhiteSpace(existing) ? addition : existing + "; " + addition;

    private sealed record JsonWriteResult(bool Succeeded, string? FilePath, string? Failure);

    private static JsonWriteResult WriteJsonIfConfigured(
        string scenario,
        Dictionary<string, object?> payload)
    {
        string? configured = System.Environment.GetEnvironmentVariable(ReportEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            const string failure = "SPIRE1_FORM_SMOKE_REPORT is not configured";
            MainFile.Logger.Error(failure);
            return new JsonWriteResult(false, null, failure);
        }

        try
        {
            string directory = Path.GetFullPath(configured);
            if (!directory.StartsWith("G:\\", StringComparison.OrdinalIgnoreCase)
                && !directory.StartsWith("G:/", StringComparison.OrdinalIgnoreCase))
            {
                string failure = "Refusing form smoke JSON outside G: temporary storage: " + directory;
                MainFile.Logger.Error(failure);
                return new JsonWriteResult(false, null, failure);
            }

            Directory.CreateDirectory(directory);
            string fileName = "form-native-smoke-" + scenario + ".json";
            string path = Path.Combine(directory, fileName);
            string json = JsonSerializer.Serialize(payload, JsonOptions);
            File.WriteAllText(path, json);
            return new JsonWriteResult(true, path, null);
        }
        catch (Exception exception)
        {
            string failure = "Failed to write form smoke JSON: " + exception;
            MainFile.Logger.Error(failure);
            return new JsonWriteResult(false, null, failure);
        }
    }
    private sealed record SmokeRequest(bool Requested, string[] Scenarios);
}
