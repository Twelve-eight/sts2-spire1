using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — N'loth's Hungry Face (event relic, from FaceTrader). The next non-Boss chest you open is "empty":
/// the first RELIC reward is removed from the chest (plus its Sapphire Key link), but the chest still pays
/// its gold.
///
/// StS1 (face-relics-and-madness.json "NlothsMask"): the constructor arms counter = 1; onChestOpenAfter
/// (!bossChest &amp;&amp; counter &gt; 0) decrements it, calls AbstractRoom.removeOneRelicFromRewards() and then
/// setCounter(-2), the standard "spent relic" sentinel (grayscale + used-up description). The gold is added to
/// the room BEFORE the relic loop in AbstractChest.open and is never removed, so "empty" still pays gold.
///
/// StS2 port: AbstractModel.ShouldGenerateTreasure(Player) (AbstractModel.cs:2325) is the only gate that
/// reaches the treasure room's relic generation; Hook.ShouldGenerateTreasure (Hook.cs:2325-2334) is veto-style
/// (any listener returning false wins), and shipped SilverCrucible already suppresses a treasure room through
/// it (SilverCrucible.cs:140-147). The "non-Boss" clause is automatic: RunManager.CreateRoom maps
/// RoomType.Boss to a CombatRoom (RunManager.cs:947-954), and ShouldGenerateTreasure is reached only from
/// TreasureRoom paths (TreasureRoomRelicSynchronizer.cs:105, OneOffSynchronizer.cs:129).
///
/// Call order keeps the gold faithful: TreasureRoom.EnterInternal runs BeginRelicPicking at room entry
/// (TreasureRoom.cs:47), which calls ShouldGenerateTreasure FIRST (TreasureRoomRelicSynchronizer.cs:105) and
/// consumes the charge there, returning false so no relic is generated. The chest is opened later by the
/// player (NTreasureRoom.cs:283-298); its DoTreasureRoomRewards (OneOffSynchronizer.cs:129) then sees the
/// spent charge, returns true and pays the gold (PlayerCmd.GainGold at OneOffSynchronizer.cs:138) — exactly
/// StS1's "gold yes, relic no". The charge is spent inside ShouldGenerateTreasure itself because
/// AfterRoomEntered fires BEFORE BeginRelicPicking (TreasureRoom.cs:44 then :47) and AbstractModel has no
/// room-exit hook to spend after the chest opens.
/// </summary>
public class NlothsMask : Spire1Relic
{
    private bool _hasConsumedTreasure;

    public override RelicRarity Rarity => RelicRarity.Event;

    /// <summary>Greyed-out "used up" presentation, exactly like shipped MawBank (MawBank.cs:23-41).</summary>
    public override bool IsUsedUp => HasConsumedTreasure;

    /// <summary>StS1 shows the single charge (counter = 1) until the mask is spent; usedUp clears the counter.</summary>
    public override bool ShowCounter => !HasConsumedTreasure;

    public override int DisplayAmount => 1;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "N'loth's Hungry Face",
            "#The next non-Boss chest you open is empty.",
            "You feel hungry.");

    /// <summary>
    /// Persists across save/load like StS1's counter. The AssertMutable() setter turns any accidental write on
    /// the canonical model into an exception instead of silently corrupting state shared by every player's
    /// clone (AbstractModel.MutableClone is MemberwiseClone, AbstractModel.cs:159-187).
    /// </summary>
    [SavedProperty]
    public bool HasConsumedTreasure
    {
        get => _hasConsumedTreasure;
        set
        {
            AssertMutable();
            _hasConsumedTreasure = value;
            if (IsUsedUp)
                Status = RelicStatus.Disabled;
            InvokeDisplayAmountChanged();
        }
    }

    // Veto-style gate (Hook.cs:2325-2334: any listener returning false wins). The charge is spent on the
    // FIRST invocation for the owner — the room-entry relic pick (TreasureRoomRelicSynchronizer.cs:105,
    // driven by TreasureRoom.cs:47); the later chest-open gold call (OneOffSynchronizer.cs:129) then passes,
    // so the chest still pays gold and only the relic is suppressed.
    public override bool ShouldGenerateTreasure(Player player)
    {
        if (player != Owner)
            return true;
        if (HasConsumedTreasure)
            return true;

        HasConsumedTreasure = true;
        Flash();
        return false;
    }
}
