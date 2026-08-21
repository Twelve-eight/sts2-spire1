using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 — Cultist Headpiece (event relic, from FaceTrader). Purely cosmetic joke relic: the player caws in a
/// speech bubble at the start of every combat. It has NO gameplay effect.
///
/// StS1 (face-relics-and-madness.json "CultistMask"): atBattleStart() queues flash, a
/// RelicAboveCreatureAction, the voice line "VO_CULTIST_1A" and a TalkAction whose text is the relic's own
/// DESCRIPTIONS[1] "@CAW!@ NL @CAAAW@". The class declares no gameplay constant at all.
///
/// StS2 port: the engine has no atBattleStart relic hook; the core doc-comment (AbstractModel.cs:1147-1148)
/// names AfterRoomEntered with a `room is CombatRoom` check as the hook for "start of combat" effects that
/// must run before the first turn — exactly StS1's atBattleStart timing. BeforeSideTurnStart would place the
/// caw after the first turn's energy/draw setup for no benefit, and needs a TurnNumber guard to avoid re-cawing
/// every turn. We do not ship StS1's audio or its loc string; the bubble reuses StS2's own shipped cultist caw
/// line (CalcifiedCultist.cs:22, played at CalcifiedCultist.cs:81), so only the game's own assets are touched.
/// No SfxCmd is played because StS2 ships no player-voiced caw — the cultist "buff" sfx is a chant, not a caw.
/// </summary>
public class CultistMask : Spire1Relic
{
    /// <summary>
    /// StS2's own shipped cultist caw, resolved through the game's "monsters" loc table. This is the line the
    /// CalcifiedCultist speaks for its Incantation move (CalcifiedCultist.cs:22,81); referencing it by key
    /// means we never ship StS1's "VO_CULTIST_1A" audio nor its "@CAW!@ NL @CAAAW@" text.
    /// </summary>
    private static readonly LocString _cawLine =
        new LocString("monsters", "CALCIFIED_CULTIST.moves.INCANTATION.banter");

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override List<(string, string)>? Localization =>
        new RelicLoc(
            "Cultist Headpiece",
            "#You feel more talkative.",
            "Part of the Flock!");

    // StS1 atBattleStart() -> flash + speech bubble. AfterRoomEntered (AbstractModel.cs:1153) with a
    // CombatRoom check is the engine-sanctioned stand-in for "start of combat" effects
    // (doc-comment at AbstractModel.cs:1147-1148). TalkCmd.Play is null-safe for the speaker (TalkCmd.cs:22)
    // and attaches the bubble through a null-conditional vfx container (TalkCmd.cs:34), so this can never
    // crash a room transition.
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
            return Task.CompletedTask;

        Flash();
        TalkCmd.Play(_cawLine, Owner.Creature, VfxColor.Purple, VfxDuration.Standard);
        return Task.CompletedTask;
    }
}
