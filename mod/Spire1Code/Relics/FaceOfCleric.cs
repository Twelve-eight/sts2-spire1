using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace Spire1.Spire1Code.Relics;

/// <summary>
/// StS1 - Face Of Cleric (event face relic; StS1 class <c>com.megacrit.cardcrawl.relics.FaceOfCleric</c>,
/// RelicTier.SPECIAL, LandingSound.CLINK). At the end of every combat won, Max HP goes up by 1.
/// </summary>
public class FaceOfCleric : Spire1Relic
{
    /// <summary>StS1 <c>onVictory()</c>: the amount is the iconst_1 literal in
    /// <c>AbstractDungeon.player.increaseMaxHp(1, true)</c>; the class declares no field.</summary>
    private const decimal _maxHpPerVictory = 1m;

    public override RelicRarity Rarity => RelicRarity.Event;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    // Localization is JSON-only on purpose: this relic does not override Localization, so the text
    // lives only in localization/{eng,zhs}/relics.json (SPIRE1-FACE_OF_CLERIC.title/.description/.flavor).
    // A C# RelicLoc is a single (English) string and BaseLib's ModelLocPatch postfix on ModelDb.Init
    // writes it into the table of the language active at startup (3.4.8 Patches/Localization/ModelLocPatch.cs:34-57);
    // that would replace the zhs JSON text until the next SetLanguage reload.

    // onVictory() is the only StS1 hook and it fires only on a won combat (AbstractPlayer.onVictory is
    // guarded by !isDying). AfterCombatVictory is the exact StS2 analogue; AfterCombatEnd also fires
    // after a lost fight and must NOT be used here.
    //
    // StS1's increaseMaxHp(1, true) bumps maxHealth and then unconditionally heals the same amount -
    // its "heal" boolean is never even read. CreatureCmd.GainMaxHp already raises max HP and heals
    // the gained amount, so adding a second heal would double the StS1 effect.
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        Flash();
        await CreatureCmd.GainMaxHp(base.Owner.Creature, _maxHpPerVictory);
    }
}
