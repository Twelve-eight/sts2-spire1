using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using Spire1.Spire1Code.Extensions;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 - Cultist Headpiece (event face relic; StS1 class <c>com.megacrit.cardcrawl.relics.CultistMask</c>,
/// id "CultistMask", RelicTier.SPECIAL, LandingSound.CLINK).
/// <para>
/// Purely cosmetic joke relic - it has NO gameplay effect. The decompiled class declares only ID and
/// no numeric field at all; <c>atBattleStart()</c> is its only hook and it queues no power, damage,
/// block or Gold: flash(), RelicAboveCreatureAction, SFXAction("VO_CULTIST_1A"), then
/// TalkAction(true, "@CAW!@ NL @CAAAW@", 1.0f, 2.0f). That is why the StS1 dump lists no numbers for
/// it and why this port adds no stats.
/// </para>
/// <para>
/// The presentation is the host's own cultist cast, not StS1's audio asset. StS1's voice line
/// "VO_CULTIST_1A" is not shipped with this mod, so the relic reuses the shipped Calcified Cultist
/// INCANTATION sound and its shipped banter line through SfxCmd/TalkCmd: this is a host presentation
/// adaptation, not a byte-for-byte reproduction of the StS1 sample, and the bubble text is the host's
/// cultist line rather than a copy of StS1's "@CAW!@ NL @CAAAW@" literal.
/// </para>
/// </summary>
public class CultistHeadpiece : Spire1Relic
{
    // Both literals are the host's own presentation boundary for the Calcified Cultist's INCANTATION
    // move: the event path is the private const CalcifiedCultist._buffSfx (CalcifiedCultist.cs:24), and
    // the LocString is the same (table, key) pair CalcifiedCultist builds into its static
    // _cawCawDialogue (CalcifiedCultist.cs:22). CalcifiedCultist plays that sound at
    // CalcifiedCultist.cs:79 and that line with VfxColor.Purple / VfxDuration.Standard at
    // CalcifiedCultist.cs:81; this relic reuses the same three values.
    private const string CultistBuffSfx = "event:/sfx/enemy/enemy_attacks/cultists/cultists_buff_calcified";

    private static readonly LocString CawCawBanter = new LocString("monsters", "CALCIFIED_CULTIST.moves.INCANTATION.banter");

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    // StS1's sprite files use the StS1 class/id name (CultistMask -> cultist_mask*), not this model's
    // entry (cultist_headpiece*), so the three sprite slots point at the shipped StS1-named art.
    public override string PackedIconPath => "cultist_mask.png".RelicImagePath();

    protected override string PackedIconOutlinePath => "cultist_mask_outline.png".RelicImagePath();

    protected override string BigIconPath => "cultist_mask.png".BigRelicImagePath();

    // Localization is JSON-only on purpose: this relic does not override Localization.
    //
    // Carrier order in the shipped engine: OneTimeInitialization.ExecuteEssential calls
    // LocManager.Initialize() before ModelDb.Init() (OneTimeInitialization.cs:79-81), so the mod's
    // JSON tables are already merged when BaseLib's ModelLocPatch (postfix on ModelDb.Init) runs.
    // That patch writes every ILocalizationProvider pair into the table of the language that is
    // active at startup (BaseLib 3.4.8 ModelLocPatch.cs:34-57: LocManager.Instance.GetTable -> LocTable._translations),
    // and LocTable.GetRawText reads that dictionary first (LocTable.cs:47-56). A C# pair here is a
    // single (English) string, so it would replace the zhs JSON text until the next SetLanguage
    // reload (NLanguageDropdown.cs:136). The two carriers must not disagree: the text lives only in
    // localization/eng/relics.json and localization/zhs/relics.json, keys
    // SPIRE1-CULTIST_HEADPIECE.title/.description/.flavor. Unsupported languages fall back to the
    // merged eng table (LocManager.cs:411-413).
    //
    // The caw line is not a mod key: CawCawBanter reads the host's own "monsters" table entry, so
    // both languages come from the shipped tables and no new JSON key is needed.

    // StS1 atBattleStart order: flash(), RelicAboveCreatureAction, SFXAction("VO_CULTIST_1A"), then
    // TalkAction(true, "@CAW!@ NL @CAAAW@", 1.0f, 2.0f).
    //
    // BeforeCombatStart is this mod's combat-start hook (MarkOfPain precedent). This port keeps the
    // StS1 order for the parts it has a real host carrier for: Flash(), then the cultist buff sound,
    // then the owner's speech bubble. RelicAboveCreatureAction has no ported host equivalent here and
    // is not synthesized. The effect stays cosmetic: no power, Gold, block, damage or energy.
    public override Task BeforeCombatStart()
    {
        Flash();
        SfxCmd.Play(CultistBuffSfx);
        TalkCmd.Play(CawCawBanter, Owner.Creature, VfxColor.Purple, VfxDuration.Standard);
        return Task.CompletedTask;
    }
}
