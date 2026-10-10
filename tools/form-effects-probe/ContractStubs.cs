// Narrow collaborators only. The six form implementations are Compile Link inputs.
// Engine provenance: G:\omp works\Sts\sts2-spire1\research\engine-dllsrc.
// No Godot, native history subscribers, full hook enumeration, network, save, HP or deck simulation.
global using MegaCrit.Sts2.Core.Models;
global using FormsNs = Forms.FormsCode;
global using FormEffectsProbe;
global using MegaCrit.Sts2.Core.Combat;
global using MegaCrit.Sts2.Core.Entities.Cards;
global using MegaCrit.Sts2.Core.Entities.Creatures;
global using MegaCrit.Sts2.Core.Entities.Players;
global using MegaCrit.Sts2.Core.Entities.Powers;
global using MegaCrit.Sts2.Core.GameActions.Multiplayer;
global using MegaCrit.Sts2.Core.Random;
global using MegaCrit.Sts2.Core.Runs;
global using MegaCrit.Sts2.Core.Rooms;
global using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Abstracts
{
    public abstract class CustomPowerModel : PowerModel
    {
        public virtual List<(string, string)>? Localization => null;
    }

    // Localization is a compile-only collaborator; these entries are not rendered or verified.
    public sealed class PowerLoc : List<(string, string)>
    {
        public PowerLoc(string title, string description, string smartDescription)
            : base(new[] { ("title", title), ("description", description), ("smartDescription", smartDescription) }) { }
    }
}

namespace BaseLib.Utils.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class CustomIDAttribute : System.Attribute
    {
        public CustomIDAttribute(string id) { Id = id; }
        public string Id { get; }
    }
}

namespace MegaCrit.Sts2.Core.Rooms
{
    // Compile-only collaborator for AbstractModel.AfterCombatEnd; no room lifecycle simulation.
    public sealed class CombatRoom { }
}

namespace MegaCrit.Sts2.Core.Entities.Powers
{
    public enum PowerType { Buff, Debuff }
    public enum PowerStackType { Single, Counter, Duration }
    public enum PowerInstanceType { None, Instanced, InstancedPerApplier }
}

namespace MegaCrit.Sts2.Core.Combat
{
    public enum CombatSide { None, Player, Enemy }

    public interface ICombatState
    {
        int RoundNumber { get; set; }
        IRunState RunState { get; }
        IReadOnlyList<Creature> HittableEnemies { get; }
        bool ContainsCreature(Creature creature);
    }

    public sealed class CombatManager
    {
        public static CombatManager Instance { get; } = new();
        public History.CombatHistory History { get; } = new();
        // Fixture-controlled lifecycle inputs, not an imitation of engine transition scheduling.
        public bool IsInProgress { get; set; } = true;
        public bool IsEnding { get; set; }
        public bool IsOverOrEnding => !IsInProgress || IsEnding;
    }
}

namespace MegaCrit.Sts2.Core.GameActions.Multiplayer
{
    public abstract class PlayerChoiceContext { }
    public sealed class ThrowingPlayerChoiceContext : PlayerChoiceContext { }
}

namespace MegaCrit.Sts2.Core.Entities.Players
{
    public sealed class Player
    {
        public string Name { get; }
        public Creature Creature { get; }

        // Probe construction only. Production Player construction is outside this executable.
        public Player(string name)
        {
            Name = name;
            Creature = new Creature(name, CombatSide.Player, this);
        }
    }
}

namespace MegaCrit.Sts2.Core.Entities.Creatures
{
    public sealed class Creature
    {
        private readonly List<PowerModel> _powers = new();
        public string Name { get; }
        public Player? Player { get; }
        public Player? PetOwner { get; }
        public CombatSide Side { get; }
        public ICombatState? CombatState { get; set; }
        public bool IsDead { get; set; }
        public bool IsAlive => !IsDead;
        // Represents the ShouldAllowHitting input; no implicit invented dead-target rule.
        public bool ProbeAllowHitting { get; set; } = true;
        public bool CanReceivePowers => CombatState != null && ProbeAllowHitting;
        public IReadOnlyList<PowerModel> Powers => _powers;
        public event Action<PowerModel, int, bool>? ProbeAmountChanged;

        public Creature(string name, CombatSide side, Player? player = null, Player? petOwner = null)
        {
            Name = name;
            Side = side;
            Player = player;
            PetOwner = petOwner;
        }

        public T? GetPower<T>() where T : PowerModel => _powers.OfType<T>().FirstOrDefault();
        public int GetPowerAmount<T>() where T : PowerModel => GetPower<T>()?.Amount ?? 0;

        public void ApplyPowerInternal(PowerModel power)
        {
            if (!ReferenceEquals(power.Owner, this))
                throw new InvalidOperationException("Power owner must be assigned before attachment.");
            if (power.InstanceType == PowerInstanceType.None && _powers.Any(p => p.GetType() == power.GetType()))
                throw new InvalidOperationException("Duplicate non-instanced power.");
            _powers.Add(power);
        }

        public void RemovePowerInternal(PowerModel power)
        {
            if (!ReferenceEquals(power.Owner, this))
                throw new InvalidOperationException("Power owner mismatch on removal.");
            _powers.Remove(power);
        }

        public void InvokePowerModified(PowerModel power, int change, bool silent)
            => ProbeAmountChanged?.Invoke(power, change, silent);
    }

    // DamageResult.cs:30,41,63,101-105. In particular TotalDamage includes block.
    public sealed class DamageResult
    {
        public Creature Receiver { get; }
        public ValueProp Props { get; }
        public int BlockedDamage { get; set; }
        public int UnblockedDamage { get; init; }
        public int TotalDamage => BlockedDamage + UnblockedDamage;
        public DamageResult(Creature receiver, ValueProp props) { Receiver = receiver; Props = props; }
    }
}

namespace MegaCrit.Sts2.Core.Entities.Cards
{
    public enum PileType { None, Draw, Hand, Discard, Exhaust, Play, Deck }
    public enum CardType { None, Attack, Skill, Power, Status, Curse, Quest }
    public sealed class CardPile
    {
        public PileType Type { get; }
        public CardPile(PileType type) => Type = type;
    }

    public struct ResourceInfo
    {
        public required int EnergySpent { get; init; }
        public required int EnergyValue { get; init; }
        public required int StarsSpent { get; init; }
        public required int StarValue { get; init; }
    }

    // CardPlay.cs:16-73. Intentionally a reference-identity class, not a value record.
    public sealed class CardPlay
    {
        public required CardModel Card { get; init; }
        public required Player Player { get; init; }
        public required Creature? Target { get; init; }
        public required PileType ResultPile { get; init; }
        public required ResourceInfo Resources { get; init; }
        public required bool IsAutoPlay { get; init; }
        public required int PlayIndex { get; init; }
        public required int PlayCount { get; init; }
        public bool IsFirstInSeries => PlayIndex == 0;
        public bool IsLastInSeries => PlayIndex == PlayCount - 1;
    }
}

namespace MegaCrit.Sts2.Core.ValueProps
{
    [Flags]
    public enum ValueProp { Unblockable = 2, Unpowered = 4, Move = 8, SkipHurtAnim = 16 }
    public static class ValuePropExtensions
    {
        public static bool IsPoweredAttack(this ValueProp props)
            => props.HasFlag(ValueProp.Move) && !props.HasFlag(ValueProp.Unpowered);
    }
}

namespace MegaCrit.Sts2.Core.Models
{
    public sealed class CardModel
    {
        public required Player Owner { get; set; }
        public CardPile? Pile { get; set; }
        public CardType Type { get; init; } = CardType.Skill;

        // Probe-only body hook. The transaction adapter invokes the real production patch methods
        // around this body; this stub does not copy the production card-play ledger.
        public Func<Task>? ProbeOnPlayBody { get; set; }

        // CardModel.cs:1803-1820. Metadata-only probe stub; resource state is not simulated here.
        public Task<(int, int)> SpendResources()
            => Task.FromException<(int, int)>(new NotSupportedException("SpendResources is not simulated by the form-effects probe."));

        // CardModel.cs:1858-1887. The body is supplied by the scenario and is wrapped by the
        // production VoidFormOnPlayWrapperTransactionPatch through ProductionPatchCalls.
        public Task OnPlayWrapper(
            PlayerChoiceContext choiceContext,
            Creature? target,
            bool isAutoPlay,
            ResourceInfo resources,
            bool skipCardPileVisuals = false)
            => ProductionPatchCalls.OnPlayWrapper(
                this, choiceContext, target, isAutoPlay, resources, skipCardPileVisuals,
                () => ProbeOnPlayBody?.Invoke() ?? Task.CompletedTask);
    }

    public abstract class PowerModel
    {
        private object? _internalData;
        private Creature? _owner;
        private int _amount;
        public bool IsMutable { get; private set; }
        public Creature Owner => _owner ?? throw new InvalidOperationException("Power has not been attached.");
        public Creature? Applier { get; set; }
        public ICombatState CombatState => Owner.CombatState ?? throw new InvalidOperationException("No combat state.");
        public int Amount => _amount;
        public abstract PowerType Type { get; }
        public abstract PowerStackType StackType { get; }
        public virtual PowerInstanceType InstanceType => PowerInstanceType.None;
        public virtual bool AllowNegative => false;
        protected virtual bool IsVisibleInternal => true;
        public event Action? DisplayAmountChanged;
        public event Action? Removed;
        public int ProbeFlashCount { get; private set; }
        public object? ProbeInternalDataIdentity => _internalData;
        public bool ProbeHasOwner => _owner != null;

        protected virtual object? InitInternalData() => null;
        protected T GetInternalData<T>() => (T)(_internalData ?? throw new InvalidOperationException("Internal data not initialized by clone."));
        protected void Flash() => ProbeFlashCount++;
        public void AssertMutable()
        {
            if (!IsMutable) throw new InvalidOperationException("Canonical power mutation.");
        }

        // AbstractModel.cs:171-177 + PowerModel.cs:582-597. No new T() clone shortcut.
        public PowerModel MutableClone()
        {
            PowerModel clone = (PowerModel)MemberwiseClone();
            clone.IsMutable = true;
            clone.DeepCloneFields();
            clone.AfterCloned();
            return clone;
        }

        protected virtual void DeepCloneFields() => _internalData = InitInternalData();
        protected virtual void AfterCloned()
        {
            DisplayAmountChanged = null;
            Removed = null;
            _owner = null;
            ProbeFlashCount = 0;
        }

        public PowerModel ToMutable(int initialAmount = 0)
        {
            if (IsMutable) throw new InvalidOperationException("ToMutable requires a canonical instance.");
            PowerModel clone = MutableClone();
            clone.SetAmount(initialAmount);
            return clone;
        }

        // Exact narrow semantics of PowerModel.cs:542-580, including event/attachment order.
        public void SetAmount(int amount, bool silent = false)
            => ProductionPatchCalls.SetAmount(this, () => SetAmountCore(amount, silent));

        private void SetAmountCore(int amount, bool silent)
        {
            AssertMutable();
            amount = Math.Clamp(amount, -999999999, 999999999);
            int change = amount - _amount;
            if (change == 0) return;
            _amount = amount;
            // The real SetAmount transpiler callback is linked at the exact storage boundary.
            Forms.FormsCode.DemonFormStrengthTransaction.CaptureStoredAmount(this);
            DisplayAmountChanged?.Invoke();
            Owner.InvokePowerModified(this, change, silent);
        }

        public void ApplyInternal(Creature owner, decimal amount, bool silent = false)
        {
            if (amount == 0m) return;
            AssertMutable();
            _owner = owner;
            SetAmount((int)amount, silent);
            owner.ApplyPowerInternal(this);
        }

        public void RemoveInternal()
        {
            AssertMutable();
            Removed?.Invoke();
            Owner.RemovePowerInternal(this);
        }

        public bool ShouldRemoveDueToAmount() => AllowNegative ? Amount == 0 : Amount <= 0;
        public virtual Task BeforeApplied(Creature target, decimal amount, Creature? applier, CardModel? cardSource) => Task.CompletedTask;
        public virtual Task AfterApplied(Creature? applier, CardModel? cardSource) => Task.CompletedTask;
        public virtual Task AfterRemoved(Creature oldOwner) => Task.CompletedTask;
        public virtual Task AfterCombatEnd(CombatRoom room) => Task.CompletedTask;
        public virtual Task BeforeCardPlayed(CardPlay cardPlay) => Task.CompletedTask;
        public virtual Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay) => Task.CompletedTask;
        public virtual Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState) => Task.CompletedTask;
        public virtual Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player) => Task.CompletedTask;
        public virtual bool TryModifyEnergyCostInCombatLate(CardModel card, decimal originalCost, out decimal modifiedCost)
        { modifiedCost = originalCost; return false; }
        public virtual bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
        { modifiedCost = originalCost; return false; }
        public virtual decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay) => 0m;
        public virtual int ModifyCardPlayCount(CardModel card, Creature? target, int playCount) => playCount;
        public virtual Task AfterModifyingCardPlayCount(CardModel card) => Task.CompletedTask;
        public virtual Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource) => Task.CompletedTask;
    }

    public static class ModelDb
    {
        private static readonly Dictionary<Type, PowerModel> Canonicals = new();
        public static T Power<T>() where T : PowerModel
        {
            if (!Canonicals.TryGetValue(typeof(T), out PowerModel? power))
            {
                power = (PowerModel)(Activator.CreateInstance(typeof(T)) ?? throw new InvalidOperationException("Power constructor missing."));
                Canonicals.Add(typeof(T), power);
            }
            return (T)power;
        }
    }
}

namespace MegaCrit.Sts2.Core.Models.Powers
{
    public sealed class StrengthPower : PowerModel
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override bool AllowNegative => true;
    }
    public sealed class DoomPower : PowerModel
    {
        public override PowerType Type => PowerType.Debuff;
        public override PowerStackType StackType => PowerStackType.Counter;
    }
}

namespace MegaCrit.Sts2.Core.Random
{
    public sealed class Rng
    {
        private readonly int[] _script;
        private int _cursor;
        public int Counter => _cursor;
        public int NextItemCalls { get; private set; }
        public List<string> Trace { get; } = new();

        // Scripted collaborator, deliberately not a substitute implementation of MegaRandom.
        // Exact index inputs permit assertions about calls and ordering, not seeded game parity.
        public Rng(params int[] indices) => _script = (int[])indices.Clone();
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive) throw new ArgumentOutOfRangeException(nameof(minInclusive));
            if (_cursor >= _script.Length) throw new InvalidOperationException("Unexpected RNG consumption: script exhausted.");
            int value = _script[_cursor++];
            if (value < minInclusive || value >= maxExclusive) throw new InvalidOperationException("Scripted RNG index outside requested bounds.");
            Trace.Add($"NextInt({minInclusive},{maxExclusive})={value}");
            return value;
        }
        public T? NextItem<T>(IEnumerable<T> items)
        {
            NextItemCalls++;
            T[] candidates = items.ToArray();
            Trace.Add("NextItem:[" + string.Join(",", candidates.Select(x => x is Creature c ? c.Name : typeof(T).Name)) + "]");
            if (candidates.Length == 0) return default;
            return candidates[NextInt(0, candidates.Length)];
        }
    }
    public sealed class RunRngSet
    {
        public Rng CombatTargets { get; }
        public RunRngSet(Rng combatTargets) => CombatTargets = combatTargets;
    }
}

namespace MegaCrit.Sts2.Core.Runs
{
    public interface IRunState { RunRngSet Rng { get; } }
    public sealed class RunState : IRunState
    {
        public RunRngSet Rng { get; }
        public RunState(Rng combatTargets) => Rng = new RunRngSet(combatTargets);
    }
}

namespace MegaCrit.Sts2.Core.Commands
{
    public static class PowerCmd
    {
        private static PowerModel? FindExisting(PowerModel power, Creature target, Creature? applier)
            => power.InstanceType switch
            {
                PowerInstanceType.Instanced => null,
                PowerInstanceType.None => target.Powers.FirstOrDefault(p => p.GetType() == power.GetType()),
                PowerInstanceType.InstancedPerApplier => target.Powers.FirstOrDefault(p => p.GetType() == power.GetType() && p.Applier == applier),
                _ => throw new ArgumentOutOfRangeException(nameof(power))
            };

        // PowerCmd.cs:71-92. Do not replace the real dangling-instance behavior with assumed null.
        public static async Task<T?> Apply<T>(PlayerChoiceContext choiceContext, Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false) where T : PowerModel
        {
            if (CombatManager.Instance.IsEnding || !target.CanReceivePowers) return null;
            PowerModel canonical = ModelDb.Power<T>();
            PowerModel? power = FindExisting(canonical, target, applier);
            if (power == null)
            {
                power = canonical.ToMutable();
                await Apply(choiceContext, power, target, amount, applier, cardSource, silent);
            }
            else if (await ModifyAmount(choiceContext, power, amount, applier, cardSource, silent) == 0)
            {
                power = null;
            }
            return power as T;
        }

        public static async Task Apply(PlayerChoiceContext choiceContext, PowerModel power, Creature target, decimal amount, Creature? applier, CardModel? cardSource, bool silent = false)
        {
            if (CombatManager.Instance.IsEnding || amount == 0m || !target.CanReceivePowers) return;
            PowerModel? existing = FindExisting(power, target, applier);
            if (existing != null)
            {
                await ModifyAmount(choiceContext, existing, amount, applier, cardSource);
                return;
            }
            power.AssertMutable();
            power.Applier = applier;
            var request = new PowerRequest("Apply", power, target, amount, applier, cardSource, silent);
            Journal.PowerRequests.Add(request);
            await PowerHooks.BeforeChange(request);
            (decimal modified, List<ReceivedPowerRule> modifiers) = PowerHooks.ModifyReceived(request);
            await power.BeforeApplied(target, modified, applier, cardSource);
            if (!target.CanReceivePowers) return;
            power.ApplyInternal(target, modified, silent);
            foreach (ReceivedPowerRule modifier in modifiers) await modifier.AfterModifying(power);
            if (modified != 0m)
            {
                await power.AfterApplied(applier, cardSource);
                await PowerHooks.AfterChange(request with { Amount = modified });
            }
        }

        // Invoke the linked production depth patch at kickoff and unwind its Task on completion.
        // This reproduces only the researched command boundary, not Harmony injection or scheduling.
        public static Task<int> ModifyAmount(PlayerChoiceContext choiceContext, PowerModel power, decimal offset, Creature? applier, CardModel? cardSource, bool silent = false)
            => ProductionPatchCalls.ModifyAmount(power, () => ModifyAmountCore(choiceContext, power, offset, applier, cardSource, silent));

        private static async Task<int> ModifyAmountCore(PlayerChoiceContext choiceContext, PowerModel power, decimal offset, Creature? applier, CardModel? cardSource, bool silent)
        {
            if (CombatManager.Instance.IsEnding) return 0;
            Creature owner = power.Owner;
            if (owner.CombatState == null) return 0;
            var request = new PowerRequest("ModifyAmount", power, owner, offset, applier, cardSource, silent);
            Journal.PowerRequests.Add(request);
            await PowerHooks.BeforeChange(request);
            (decimal modified, List<ReceivedPowerRule> modifiers) = PowerHooks.ModifyReceived(request);
            // PowerCmd.cs:239-241: no await between history, computing the amount and SetAmount.
            CombatManager.Instance.History.PowerReceived(owner.CombatState, power, modified, applier);
            int newAmount = power.Amount + (int)modified;
            power.SetAmount(newAmount, silent);
            foreach (ReceivedPowerRule modifier in modifiers) await modifier.AfterModifying(power);
            if ((int)modified != 0) await PowerHooks.AfterChange(request with { Amount = modified });
            if (power.ShouldRemoveDueToAmount()) await Remove(power);
            return newAmount; // Intentionally not power.Amount after clamping or reactive hooks.
        }

        public static async Task Remove(PowerModel? power)
        {
            if (power == null) return;
            Journal.Removals.Add(power);
            power.RemoveInternal();
            await power.AfterRemoved(power.Owner); // RemoveInternal does not clear Owner.
        }
    }

    public static class CreatureCmd
    {
        public static async Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
        {
            var request = new DamageRequest(target, amount, props, dealer, cardSource, cardPlay);
            Journal.DamageRequests.Add(request);
            if (Journal.DamagePause != null) await Journal.DamagePause(request);
            // Invocation spy only: no fabricated HP, block or damage result.
            return Array.Empty<DamageResult>();
        }
    }

    public static class PlayerCmd
    {
        public static async Task GainEnergy(decimal amount, Player player)
        {
            var request = new EnergyRequest(player, amount);
            Journal.EnergyRequests.Add(request);
            if (Journal.EnergyPause != null) await Journal.EnergyPause(request);
        }
    }

    public static class CardPileCmd
    {
        public static Task<IEnumerable<CardModel>> Draw(PlayerChoiceContext choiceContext, decimal count, Player player, bool fromHandDraw = false)
        {
            Journal.DrawRequests.Add(new DrawRequest(player, count, fromHandDraw));
            // No deck simulation: only production command arguments are observable here.
            return Task.FromResult<IEnumerable<CardModel>>(Array.Empty<CardModel>());
        }
    }
}
