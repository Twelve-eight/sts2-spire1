using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 - Ssserpent Head (event face relic; StS1 class
/// <c>com.megacrit.cardcrawl.relics.SsserpentHead</c>, RelicTier.SPECIAL, LandingSound.CLINK).
/// <para>
/// StS1: <c>private static final int GOLD_AMT = 50</c> and <c>onEnterRoom(AbstractRoom room)</c> pays
/// <c>flash(); player.gainGold(50)</c> whenever <c>room instanceof EventRoom</c>. No floor, act or
/// once-per-run gate and no counter.
/// </para>
/// </summary>
public class SsserpentHead : Spire1Relic
{
    /// <summary>StS1 <c>GOLD_AMT = 50</c>; the description splices the same bipush 50 literal.</summary>
    private const decimal _goldAmount = 50m;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    // Localization is JSON-only on purpose: this relic does not override Localization, so the text
    // lives only in localization/{eng,zhs}/relics.json (SPIRE1-SSSERPENT_HEAD.title/.description/.flavor).
    // A C# RelicLoc is a single (English) string and BaseLib's ModelLocPatch postfix on ModelDb.Init
    // writes it into the table of the language active at startup (3.4.8 Patches/Localization/ModelLocPatch.cs:34-57);
    // that would replace the zhs JSON text until the next SetLanguage reload.

    // The faithful test is the map point, not the room type. StS1 only ever saw an EventRoom at
    // relic.onEnterRoom time because every "?" node is constructed as one before its event roll, so it
    // paid on every "?" node. StS2 resolves the room type up front (RunManager.RollRoomTypeFor sends
    // MapPointType.Unknown through the Unknown odds into Event / Monster / Treasure / Shop), so
    // `room is EventRoom` would silently skip every "?" node that rolled into a fight, shop or chest.
    // MapPointType.Ancient resolves to RoomType.Event too; StS1 has no Ancient point type, so excluding
    // it (Unknown-only) is the vanilla-faithful choice.
    //
    // The RunState.BaseRoom guard is MawBank's: it limits the payout to one per map point instead of
    // once per sub-room on the room stack.
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (base.Owner.RunState.BaseRoom != room)
            return;
        if (base.Owner.RunState.CurrentMapPoint?.PointType != MapPointType.Unknown)
            return;

        Flash();
        await PlayerCmd.GainGold(_goldAmount, base.Owner);
    }
}
