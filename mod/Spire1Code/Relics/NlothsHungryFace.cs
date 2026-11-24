using System;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using Spire1.Spire1Code.Patches;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 - N'loth's Hungry Face (event face relic; StS1 class
/// <c>com.megacrit.cardcrawl.relics.NlothsMask</c>, RelicTier.SPECIAL, LandingSound.FLAT).
/// <para>
/// StS1 ships it armed with <c>counter = 1</c> and consumes that single charge in
/// <c>onChestOpenAfter(boolean bossChest)</c>: <c>if (!bossChest &amp;&amp; counter &gt; 0) { counter--;
/// removeOneRelicFromRewards(); if (counter == 0) setCounter(-2); }</c>. So the next non-Boss chest is
/// empty and the relic spends itself.
/// </para>
/// </summary>
public class NlothsHungryFace : Spire1Relic
{
    /// <summary>StS1 <c>counter = 1</c>: exactly one chest is emptied.</summary>
    private int _charges = 1;

    /// <summary>
    /// Persisted identity of the treasure room whose owner relic slot was suppressed. The key is derived from
    /// RunState's saved act/map/history data; AbstractRoom.Id is deliberately not used because the engine documents
    /// that it is not stable across save/load.
    /// </summary>
    private string _consumedTreasureRoomKey = string.Empty;

    public override RelicRarity Rarity => RelicRarity.Event;
    public override string PackedIconPath => "nloths_mask.png".RelicImagePath();

    protected override string PackedIconOutlinePath => "nloths_mask_outline.png".RelicImagePath();

    protected override string BigIconPath => "nloths_mask.png".BigRelicImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /// <summary>
    /// The saved charge is changed only through this property so mutable checks, status refresh, and counter display
    /// notifications all use the engine's normal model contract.
    /// </summary>
    [SavedProperty]
    public int Charges
    {
        get => _charges;
        set
        {
            AssertMutable();
            bool changed = _charges != value;
            _charges = value;
            Status = IsUsedUp ? RelicStatus.Disabled : RelicStatus.Normal;
            if (changed)
            {
                InvokeDisplayAmountChanged();
            }
        }
    }

    /// <summary>
    /// Saved room identity used to make repeated relic-selection notifications for one chest idempotent. An empty
    /// value means that no room identity has been consumed yet.
    /// </summary>
    [SavedProperty]
    public string ConsumedTreasureRoomKey
    {
        get => _consumedTreasureRoomKey;
        set
        {
            AssertMutable();
            _consumedTreasureRoomKey = value ?? string.Empty;
        }
    }

    public override bool ShowCounter => Charges > 0;

    public override int DisplayAmount => Charges;

    public override bool IsUsedUp => Charges <= 0;

    /// <summary>
    /// The engine asks this predicate from two different consumers. The Harmony scope set around
    /// TreasureRoomRelicSynchronizer.BeginRelicPicking identifies the relic-generation query only; the later
    /// OneOffSynchronizer gold query is outside that scope and therefore remains true, preserving the original gold
    /// RNG and reward consumer. The aggregate Hook still short-circuits other listeners, so this method never turns
    /// another relic's veto back into true.
    /// </summary>
    public override bool ShouldGenerateTreasure(Player player)
    {
        if (!NlothsHungryFaceTreasurePatch.IsRelicPicking
            || player.NetId != base.Owner.NetId
            || player.RunState.CurrentRoom is not TreasureRoom
            || !TryGetTreasureRoomKey(player.RunState, out string roomKey))
        {
            return true;
        }

        if (string.Equals(ConsumedTreasureRoomKey, roomKey, StringComparison.Ordinal))
        {
            return false;
        }

        if (Charges <= 0)
        {
            // A spent relic from an earlier room must not carry a room flag into this room. Clearing through the
            // saved property also makes the next save reflect that the old room is no longer the active scope.
            if (ConsumedTreasureRoomKey.Length != 0)
            {
                ConsumedTreasureRoomKey = string.Empty;
            }
            return true;
        }

        Charges = 0;
        ConsumedTreasureRoomKey = roomKey;
        Flash();
        return false;
    }

    /// <summary>
    /// Builds a stable current-room identity from fields the 0.113 run save actually carries. No fallback key is
    /// emitted when the coordinate or history entry is absent, avoiding collisions in debug/partial-load states.
    /// </summary>
    private static bool TryGetTreasureRoomKey(IRunState runState, out string roomKey)
    {
        roomKey = string.Empty;
        if (runState.CurrentRoom is not TreasureRoom || !runState.CurrentMapCoord.HasValue)
        {
            return false;
        }

        var historyEntry = runState.CurrentMapPointHistoryEntry;
        if (historyEntry is null || historyEntry.Rooms.Count == 0)
        {
            return false;
        }

        var coord = runState.CurrentMapCoord.Value;
        roomKey = $"{runState.CurrentActIndex}:{coord.col}:{coord.row}:{(int)historyEntry.MapPointType}:{historyEntry.Rooms.Count - 1}";
        return true;
    }
}
