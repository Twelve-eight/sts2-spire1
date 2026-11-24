using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

// Compile-time signatures only, like TransactionPatchAdapter's Harmony attributes.
// This executable observes command requests, not host IL, commits or hook dispatch.
// Accidental invocation fails explicitly; real continuation validation uses the host-DLL carrier.
namespace FormEffectsProbe
{
    internal static class CelestialMetadata
    {
        internal static Exception Unsupported() => new NotSupportedException(
            "Host commit/hook metadata is not executable in the narrow form-effects probe; use the real-host Celestial consumer.");
    }
}
namespace MegaCrit.Sts2.Core.Entities.Players
{
    public sealed partial class PlayerCombatState
    {
        public void GainEnergy(decimal amount) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public IReadOnlyList<CardPile> AllPiles => throw FormEffectsProbe.CelestialMetadata.Unsupported();
    }
}
namespace MegaCrit.Sts2.Core.Entities.Cards
{
    public enum CardPilePosition { Top, Bottom, Random }
    public sealed partial class CardPile
    {
        public void AddInternal(CardModel card, int index, bool silent) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public IReadOnlyList<CardModel> Cards => throw FormEffectsProbe.CelestialMetadata.Unsupported();
    }
}
namespace MegaCrit.Sts2.Core.Models
{
    public sealed partial class CardModel
    {
        public void RemoveFromCurrentPile(bool silent) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public void InvokeDrawn() => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public bool HasBeenRemovedFromState => throw FormEffectsProbe.CelestialMetadata.Unsupported();
    }
    public abstract class AbstractModel
    {
        public Task AfterModifyingEnergyGain() => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public Task AfterPreventingDraw() => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public Task AfterCardEnteredCombat(CardModel card) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public Task AfterCardChangedPiles(CardModel card, PileType oldPile, AbstractModel? clonedBy) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public Task AfterCardChangedPilesLate(CardModel card, PileType oldPile, AbstractModel? clonedBy) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public Task AfterCardDrawnEarly(PlayerChoiceContext context, CardModel card, bool fromHandDraw) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public Task AfterCardDrawn(PlayerChoiceContext context, CardModel card, bool fromHandDraw) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public Task AfterShuffle(PlayerChoiceContext context, Player player) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public decimal ModifyEnergyGain(Player player, decimal amount) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public bool ShouldDraw(Player player, bool fromHandDraw) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public void ModifyShuffleOrder(Player player, List<CardModel> cards, bool initial) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
    }
}
namespace MegaCrit.Sts2.Core.Commands
{
    public static partial class CardPileCmd
    {
        public static Task ShuffleIfNecessary(PlayerChoiceContext context, Player player) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public static Task Shuffle(PlayerChoiceContext context, Player player) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public static Task Add(IEnumerable<CardModel> cards, CardPile pile, CardPilePosition position, AbstractModel? source, bool a, bool b) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
    }
}
namespace MegaCrit.Sts2.Core.Hooks
{
    public static class Hook
    {
        public static Task BeforePowerAmountChanged() => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public static Task AfterModifyingEnergyGain(ICombatState combat, IEnumerable<AbstractModel> models) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public static Task AfterPreventingDraw(ICombatState combat, AbstractModel model) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public static Task AfterCardEnteredCombat(ICombatState combat, CardModel card) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public static Task AfterCardChangedPiles(MegaCrit.Sts2.Core.Runs.IRunState run, ICombatState combat, CardModel card, PileType oldPile, AbstractModel? source) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public static Task AfterCardDrawn(ICombatState combat, PlayerChoiceContext context, CardModel card, bool fromHandDraw) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public static Task AfterShuffle(ICombatState combat, PlayerChoiceContext context, Player player) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public static decimal ModifyEnergyGain() => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public static bool ShouldDraw() => throw FormEffectsProbe.CelestialMetadata.Unsupported();
        public static void ModifyShuffleOrder() => throw FormEffectsProbe.CelestialMetadata.Unsupported();
    }
}
namespace MegaCrit.Sts2.Core.Combat
{
    public sealed class CombatStateTracker
    {
        public void Subscribe(CardModel card) => throw FormEffectsProbe.CelestialMetadata.Unsupported();
    }
}
