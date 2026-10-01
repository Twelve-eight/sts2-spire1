using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace FormEffectsProbe;

public sealed record PowerRequest(string Operation, PowerModel Power, Creature Target, decimal Amount, Creature? Applier, CardModel? CardSource, bool Silent);
public sealed record HistoryRequest(ICombatState CombatState, PowerModel Power, decimal Amount, Creature? Applier);
public sealed record DamageRequest(Creature Target, decimal Amount, ValueProp Props, Creature? Dealer, CardModel? CardSource, CardPlay? CardPlay);
public sealed record EnergyRequest(Player Player, decimal Amount);
public sealed record DrawRequest(Player Player, decimal Amount, bool FromHandDraw);

public static class Journal
{
    public static List<PowerRequest> PowerRequests { get; } = new();
    public static List<HistoryRequest> HistoryRequests { get; } = new();
    public static List<PowerModel> Removals { get; } = new();
    public static List<DamageRequest> DamageRequests { get; } = new();
    public static List<EnergyRequest> EnergyRequests { get; } = new();
    public static List<DrawRequest> DrawRequests { get; } = new();
    public static Func<DamageRequest, Task>? DamagePause { get; set; }
    public static Func<EnergyRequest, Task>? EnergyPause { get; set; }

    public static void Reset()
    {
        PowerRequests.Clear();
        HistoryRequests.Clear();
        Removals.Clear();
        DamageRequests.Clear();
        EnergyRequests.Clear();
        DrawRequests.Clear();
        DamagePause = null;
        EnergyPause = null;
        PowerHooks.Reset();
        CombatManager.Instance.IsEnding = false;
        CombatManager.Instance.IsInProgress = true;
    }

    public static string Snapshot()
        => $"calls: history={HistoryRequests.Count}, damage={DamageRequests.Count}, energy={EnergyRequests.Count}, draw={DrawRequests.Count}, powers=[" +
           string.Join(",", PowerRequests.Select(r => $"{r.Operation}:{r.Power.GetType().Name}:{r.Target.Name}:{r.Amount}")) + "]";
}

// Only the received-amount modifier stage needed by these scenarios is simulated.
// Given modifiers, multiplayer scaling, full listener ordering and history are not covered.
public abstract class ReceivedPowerRule
{
    public abstract bool TryModify(PowerRequest request, out decimal modifiedAmount);
    public virtual Task AfterModifying(PowerModel power) => Task.CompletedTask;
}

public sealed class FirstPositiveStrengthDoubleRule : ReceivedPowerRule
{
    private readonly Creature _owner;
    public bool Used { get; private set; }
    public int Modifications { get; private set; }
    public FirstPositiveStrengthDoubleRule(Creature owner) => _owner = owner;

    // RuinedHelmet.cs:32-59: consume in AfterModifying, not while computing the factor.
    public override bool TryModify(PowerRequest request, out decimal modifiedAmount)
    {
        modifiedAmount = request.Amount;
        if (request.Power is not StrengthPower || request.Target != _owner || request.Amount <= 0m || Used) return false;
        modifiedAmount *= 2m;
        Modifications++;
        return true;
    }
    public override Task AfterModifying(PowerModel power)
    {
        Used = true;
        return Task.CompletedTask;
    }
}

public sealed class RejectPositiveStrengthRule : ReceivedPowerRule
{
    private readonly Creature _owner;
    public bool Enabled { get; set; } = true;
    public int Rejections { get; private set; }
    public RejectPositiveStrengthRule(Creature owner) => _owner = owner;
    public override bool TryModify(PowerRequest request, out decimal modifiedAmount)
    {
        modifiedAmount = request.Amount;
        if (!Enabled || request.Power is not StrengthPower || request.Target != _owner || request.Amount <= 0m) return false;
        Rejections++;
        modifiedAmount = 0m;
        return true;
    }
}

public sealed class RejectNegativeStrengthRule : ReceivedPowerRule
{
    private readonly Creature _owner;
    public int Rejections { get; private set; }
    public RejectNegativeStrengthRule(Creature owner) => _owner = owner;
    public override bool TryModify(PowerRequest request, out decimal modifiedAmount)
    {
        modifiedAmount = request.Amount;
        if (request.Power is not StrengthPower || request.Target != _owner || request.Amount >= 0m) return false;
        Rejections++;
        modifiedAmount = 0m;
        return true;
    }
}

public static class PowerHooks
{
    public static List<ReceivedPowerRule> Received { get; } = new();
    public static Func<PowerRequest, Task>? BeforeAmountChanged { get; set; }
    public static Func<PowerRequest, Task>? AfterAmountChanged { get; set; }
    public static Task BeforeChange(PowerRequest request) => BeforeAmountChanged?.Invoke(request) ?? Task.CompletedTask;
    public static Task AfterChange(PowerRequest request) => AfterAmountChanged?.Invoke(request) ?? Task.CompletedTask;

    public static (decimal, List<ReceivedPowerRule>) ModifyReceived(PowerRequest request)
    {
        decimal value = request.Amount;
        var modifiers = new List<ReceivedPowerRule>();
        foreach (ReceivedPowerRule rule in Received.ToArray())
        {
            if (!rule.TryModify(request with { Amount = value }, out decimal next)) continue;
            value = next;
            modifiers.Add(rule);
        }
        return (value, modifiers);
    }

    public static void Reset()
    {
        Received.Clear();
        BeforeAmountChanged = null;
        AfterAmountChanged = null;
    }
}

public sealed class ProbeCombatState : ICombatState
{
    public int RoundNumber { get; set; }
    public IRunState RunState { get; }
    public List<Creature> Creatures { get; } = new();
    public List<Creature> Enemies { get; } = new();
    public IReadOnlyList<Creature> HittableEnemies => Enemies;
    public ProbeCombatState(int round, Rng rng) { RoundNumber = round; RunState = new RunState(rng); }
    public bool ContainsCreature(Creature creature) => Creatures.Contains(creature);
}

public sealed class Fixture
{
    public Player OwnerPlayer { get; } = new("Owner");
    public Player OtherPlayer { get; } = new("Other");
    public Creature Owner => OwnerPlayer.Creature;
    public Creature Other => OtherPlayer.Creature;
    public Creature OwnerPet { get; }
    public Creature OtherPet { get; }
    public ProbeCombatState Combat { get; }
    public Rng Targets => Combat.RunState.Rng.CombatTargets;
    public PlayerChoiceContext Context { get; } = new ThrowingPlayerChoiceContext();
    public int Strength => Owner.GetPowerAmount<StrengthPower>();

    public Fixture(int round = 1, int[]? rng = null, int enemyCount = 3)
    {
        Combat = new ProbeCombatState(round, new Rng(rng ?? Enumerable.Repeat(0, 32).ToArray()));
        OwnerPet = new Creature("OwnerPet", CombatSide.Player, petOwner: OwnerPlayer);
        OtherPet = new Creature("OtherPet", CombatSide.Player, petOwner: OtherPlayer);
        foreach (Creature creature in new[] { Owner, Other, OwnerPet, OtherPet }) Add(creature);
        for (int i = 0; i < enemyCount; i++)
        {
            var enemy = new Creature($"Enemy{i}", CombatSide.Enemy);
            Add(enemy);
            Combat.Enemies.Add(enemy);
        }
    }

    public void Add(Creature creature)
    {
        creature.CombatState = Combat;
        Combat.Creatures.Add(creature);
    }

    public CardModel Card(Player? player = null, PileType? pile = PileType.Hand, CardType type = CardType.Skill)
        => new() { Owner = player ?? OwnerPlayer, Pile = pile.HasValue ? new CardPile(pile.Value) : null, Type = type };

    public static CardPlay Play(CardModel card, bool auto = false, int index = 0, int count = 1, Player? player = null)
        => new()
        {
            Card = card, Player = player ?? card.Owner, Target = null, ResultPile = PileType.Discard,
            Resources = new ResourceInfo { EnergySpent = 0, EnergyValue = 0, StarsSpent = 0, StarValue = 0 },
            IsAutoPlay = auto, PlayIndex = index, PlayCount = count
        };

    // Install only. Each scenario explicitly decides whether/when to call AfterApplied.
    public T Attach<T>(Creature? owner = null) where T : PowerModel
    {
        var power = (T)ModelDb.Power<T>().ToMutable();
        power.ApplyInternal(owner ?? Owner, 1m, silent: true);
        return power;
    }

    public T CloneAndAttach<T>(T source, Creature? owner = null) where T : PowerModel
    {
        var clone = (T)source.MutableClone();
        Check.False(clone.ProbeHasOwner, "clone clears owner before binding");
        clone.ApplyInternal(owner ?? Owner, 1m, silent: true);
        return clone;
    }

    public void SeedStrength(int amount, Creature? owner = null)
    {
        if (amount == 0) return;
        // Fixture input: existing permanent Strength, before installing grant modifiers.
        var strength = (StrengthPower)ModelDb.Power<StrengthPower>().ToMutable();
        strength.ApplyInternal(owner ?? Owner, amount, silent: true);
    }

    public async Task Complete(PowerModel power, CardPlay play)
    {
        await power.BeforeCardPlayed(play);
        await power.AfterCardPlayed(Context, play);
    }

    public Task StartOwnTurn(PowerModel power, int round)
    {
        Combat.RoundNumber = round;
        return power.AfterPlayerTurnStart(Context, OwnerPlayer);
    }

    public Task StartSide(PowerModel power, params Creature[] participants)
        => power.BeforeSideTurnStart(Context, CombatSide.Player, participants, Combat);
}

public sealed class ProbeFailure : Exception
{
    public ProbeFailure(string message) : base(message) { }
}

public static class Check
{
    public static void Equal<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new ProbeFailure($"{label}: expected={expected}, actual={actual}");
    }
    public static void True(bool condition, string label) { if (!condition) throw new ProbeFailure(label); }
    public static void False(bool condition, string label) => True(!condition, label);
    public static void Same(object? expected, object? actual, string label) => True(ReferenceEquals(expected, actual), label);
    public static void NotSame(object? left, object? right, string label) => False(ReferenceEquals(left, right), label);
    public static void Sequence<T>(IEnumerable<T> expected, IEnumerable<T> actual, string label)
    {
        T[] first = expected.ToArray();
        T[] second = actual.ToArray();
        if (!first.SequenceEqual(second)) throw new ProbeFailure($"{label}: expected=[{string.Join(",", first)}], actual=[{string.Join(",", second)}]");
    }
    public static void Cost(PowerModel power, CardModel card, bool free, string label)
    {
        bool energy = power.TryModifyEnergyCostInCombatLate(card, 3m, out decimal energyCost);
        bool stars = power.TryModifyStarCost(card, 4m, out decimal starCost);
        Equal(free, energy, label + " energy modified");
        Equal(free, stars, label + " star modified");
        Equal(free ? 0m : 3m, energyCost, label + " energy value");
        Equal(free ? 0m : 4m, starCost, label + " star value");
    }
    public static void NoResources(string label)
    {
        Equal(0, Journal.EnergyRequests.Count, label + " energy calls");
        Equal(0, Journal.DrawRequests.Count, label + " draw calls");
    }
    public static void FreshInternalData(PowerModel original, PowerModel clone)
    {
        NotSame(original, clone, "clone must be a different PowerModel");
        if (original.ProbeInternalDataIdentity != null)
            NotSame(original.ProbeInternalDataIdentity, clone.ProbeInternalDataIdentity, "InitInternalData must reset opaque instance state");
        else
            Same(null, clone.ProbeInternalDataIdentity, "stateless power remains stateless");
    }
}

public sealed class ProbeSuite
{
    private readonly List<(string Name, Func<Task> Run)> _cases = new();
    public void Add(string name, Func<Task> run)
    {
        if (_cases.Any(c => c.Name == name)) throw new InvalidOperationException("Duplicate scenario name: " + name);
        _cases.Add((name, run));
    }

    public async Task<int> Run(string? filter, bool listOnly)
    {
        var selected = _cases.Where(c => filter == null || c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (selected.Length == 0)
        {
            Console.WriteLine("ERROR no scenarios selected");
            return 2;
        }
        if (listOnly)
        {
            foreach (var item in selected) Console.WriteLine(item.Name);
            Console.WriteLine($"SELECTED {selected.Length}");
            return 0;
        }
        int passed = 0, failed = 0;
        foreach (var item in selected)
        {
            Journal.Reset();
            try
            {
                await item.Run().WaitAsync(TimeSpan.FromSeconds(5));
                passed++;
                Console.WriteLine("PASS " + item.Name);
            }
            catch (Exception error)
            {
                failed++;
                Console.WriteLine($"FAIL {item.Name}: {error.GetType().Name}: {error.Message}");
                Console.WriteLine("  " + Journal.Snapshot());
                if (error is TimeoutException)
                {
                    Console.WriteLine("ABORT timeout: remaining scenarios were not executed");
                    break;
                }
            }
        }
        Console.WriteLine($"TOTAL {passed + failed} PASS {passed} FAIL {failed} SELECTED {selected.Length}");
        return failed == 0 ? 0 : 1;
    }
}
