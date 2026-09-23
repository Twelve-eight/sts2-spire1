using EventRemovalProbe;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

// Derived from the coordinator's source-linked reproduction fixture.
// Only the used contract surface is modeled; this is not the game or BaseLib runtime.
namespace EventRemovalProbe
{
    internal static class ProbeState
    {
        public static readonly List<string> Trace = new();
        public static readonly List<CardModel> Added = new();
        // All cards submitted to the remove command, deliberately without filtering here.
        public static readonly List<CardModel> Removed = new();
        public static string? FinishedPage;
        public static Action<CardModel>? AfterSelection;

        public static void Reset()
        {
            Trace.Clear();
            Added.Clear();
            Removed.Clear();
            FinishedPage = null;
            AfterSelection = null;
            CardSelectCmd.Reset();
        }
    }
}

namespace BaseLib.Config
{
    public abstract class SimpleModConfig { }
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ConfigHoverTipsByDefaultAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class ConfigIgnoreAttribute : Attribute { }
}

namespace MegaCrit.Sts2.Core.Entities.Cards
{
    public enum PileType { Deck }
    public enum CardKeyword { Eternal }
}

namespace MegaCrit.Sts2.Core.Models
{
    public class CardModel
    {
        public string Name = "card";
        public string Title => Name;
        public Player Owner = null!;
        public HashSet<CardKeyword> Keywords = new();
        // The Eternal exclusion is the authoritative boundary covered by this fixture.
        public bool IsRemovable => !Keywords.Contains(CardKeyword.Eternal);
    }

    public class Player
    {
        public List<CardModel> Deck = new();
        public RunState RunState = new();
    }

    public class RunState
    {
        public T CreateCard<T>(Player owner) where T : CardModel, new() => new T { Owner = owner };
    }

    public static class ModelDb
    {
        public static T Card<T>() where T : CardModel, new() => new();
    }

    public class EventOption
    {
        public Func<Task> Action;
        public EventOption(Func<Task> action) => Action = action;
    }
}

namespace MegaCrit.Sts2.Core.Models.Cards
{
    public class IronWave : CardModel
    {
        public IronWave() => Name = "IronWave";
    }
}

namespace MegaCrit.Sts2.Core.Localization
{
    public class LocString
    {
        public string Table { get; }
        public string Key { get; }
        public LocString(string table, string key) => (Table, Key) = (table, key);
    }
}

namespace MegaCrit.Sts2.Core.Localization.DynamicVars
{
    public class DynamicVar { }
    public class StringVar : DynamicVar
    {
        public string StringValue = "";
        public string Key { get; }
        public StringVar(string key) => Key = key;
    }
}

namespace MegaCrit.Sts2.Core.CardSelection
{
    public class CardSelectorPrefs
    {
        public bool Cancelable;
        public LocString Prompt { get; }
        public int Count { get; }
        public CardSelectorPrefs(LocString prompt, int count) => (Prompt, Count) = (prompt, count);
    }
}

namespace MegaCrit.Sts2.Core.Commands
{
    public static class CardSelectCmd
    {
        public static List<CardModel> LastCandidates = new();
        public static CardModel? LastSelected;
        public static int RemovalCalls;

        public static void Reset()
        {
            LastCandidates.Clear();
            LastSelected = null;
            RemovalCalls = 0;
        }

        public static async Task<IEnumerable<CardModel>> FromDeckGeneric(Player player,
            CardSelectorPrefs prefs, Func<CardModel, bool>? filter = null,
            Func<CardModel, int>? sortingOrder = null)
        {
            if (prefs.Count != 1)
                throw new InvalidOperationException("探针仅实现单卡选择契约");
            ProbeState.Trace.Add("select");
            IEnumerable<CardModel> candidates = player.Deck.Where(card => filter == null || filter(card));
            if (sortingOrder != null)
                candidates = candidates.OrderBy(sortingOrder);
            LastCandidates = candidates.ToList();
            LastSelected = LastCandidates.FirstOrDefault();
            // Deterministically select the first eligible card, instead of driving an actual UI.
            await Task.Yield();
            if (LastSelected == null)
                return Array.Empty<CardModel>();
            ProbeState.AfterSelection?.Invoke(LastSelected);
            return new[] { LastSelected };
        }

        public static Task<IEnumerable<CardModel>> FromDeckForRemoval(Player player,
            CardSelectorPrefs prefs, Func<CardModel, bool>? filter = null)
        {
            RemovalCalls++;
            return FromDeckGeneric(player, prefs, card => card.IsRemovable && (filter == null || filter(card)));
        }
    }

    public static class CardPileCmd
    {
        public static async Task Add(CardModel card, PileType pile)
        {
            if (pile != PileType.Deck)
                throw new InvalidOperationException("探针仅实现牌堆收牌");
            await Task.Yield();
            card.Owner.Deck.Add(card);
            ProbeState.Added.Add(card);
            ProbeState.Trace.Add("add");
        }

        public static async Task RemoveFromDeck(IReadOnlyList<CardModel> cards)
        {
            await Task.Yield();
            ProbeState.Removed.AddRange(cards);
            // No implicit IsRemovable protection: production must select and filter correctly.
            foreach (var card in cards)
                card.Owner.Deck.Remove(card);
            ProbeState.Trace.Add("remove");
        }
    }
}

namespace Spire1.Spire1Code.Cards
{
    // Satisfy the production file's existing namespace import without copying game content.
    internal class ProbeNamespace { }
}

namespace Spire1.Spire1Code.Events
{
    public abstract class Spire1Event
    {
        public Player Owner = new();
        public Dictionary<string, DynamicVar> DynamicVars = new();
        protected virtual string ShippedPortrait => "";
        protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();
        public virtual void CalculateVars() { }
        protected abstract IReadOnlyList<EventOption> GenerateInitialOptions();
        protected EventOption Option(Func<Task> action, string? page = null) => new(action);
        protected string PageDescription(string page) => page;
        protected void SetEventState(string page, IReadOnlyList<EventOption> options) { }
        protected void SetEventFinished(string page)
        {
            ProbeState.FinishedPage = page;
            ProbeState.Trace.Add("finish");
        }
    }
}
