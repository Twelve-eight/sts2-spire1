using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Ssserpent Head (event relic, from FaceTrader). Whenever you enter a "?" room, gain 50 Gold.
///
/// StS1 (face-relics-and-madness.json "SsserpentHead"): GOLD_AMT = 50; onEnterRoom(room) pays 50 gold when
/// `room instanceof EventRoom`. That test is only true because the relic loop in
/// AbstractDungeon.nextRoomTransition runs BEFORE EventRoom.onPlayerEntry rolls the event — every "?" node IS
/// an EventRoom at that moment, so the gold pays on EVERY "?" node regardless of what it later resolves into
/// (event, fight, shop or treasure).
///
/// StS2 port: StS2 resolves the room type up front (RunManager.RollRoomTypeFor at RunManager.cs:976-994), so
/// `room is EventRoom` would silently skip every "?" that rolled into a fight, shop or chest (the divergence
/// is documented at Rooms/RoomType.cs:19-27). The faithful test is the MAP POINT:
/// CurrentMapPoint.PointType == MapPointType.Unknown (IRunState.CurrentMapPoint at IRunState.cs:59;
/// MapPoint.PointType at MapPoint.cs:22; MapPointType.Unknown at MapPointType.cs:10). MapPointType.Ancient
/// (MapPointType.cs) resolves to RoomType.Event (RunManager.cs:992) but StS1 has no Ancient point type, so it
/// is deliberately excluded.
///
/// The BaseRoom == room guard (shipped MawBank.cs:43-50) limits the payout to one per map point instead of
/// once per sub-room on the room stack: an event that pushes a combat re-fires AfterRoomEntered for the
/// CombatRoom, but BaseRoom is still the EventRoom.
/// </summary>
public class SsserpentHead : Spire1Relic
{
    /// <summary>StS1 <c>SsserpentHead.GOLD_AMT = 50</c> (bipush 50 in onEnterRoom).</summary>
    private const decimal _goldAmount = 50m;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Ssserpent Head",
            "#Whenever you enter a ? room, gain 50 *Gold*.",
            "The most fulfilling of lives is that in which you can buy anything!");

    // AfterRoomEntered (AbstractModel.cs:1153); MawBank is the template for the whole shape
    // (MawBank.cs:43-50). CurrentMapPoint is already the new point here: EnterMapPointInternal appends it to
    // the map point history (RunManager.cs:922) before EnterRoom pushes the room and fires the hook.
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        // One payout per map point, exactly like MawBank: sub-rooms pushed on the stack (an event starting a
        // combat) re-fire this hook but must not re-pay.
        if (Owner.RunState.BaseRoom != room)
            return;
        // StS1 pays on every "?" node; in StS2 the map point is the only faithful signal (see class doc).
        if (Owner.RunState.CurrentMapPoint?.PointType != MapPointType.Unknown)
            return;

        Flash();
        await PlayerCmd.GainGold(_goldAmount, Owner);
    }
}
