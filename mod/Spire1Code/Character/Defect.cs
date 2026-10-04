using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Character;

/// <summary>
/// "StS1 - Defect" - the vanilla Slay the Spire 1 Defect as an additive StS2 character.
/// Uses base-game Defect visuals via <see cref="PlaceholderCharacterModel"/> (PlaceholderID = "defect"),
/// so no custom art is required. ID = SPIRE1-DEFECT.
/// </summary>
public class Defect : PlaceholderCharacterModel
{
    public const string CharacterId = "Defect";

    /// <summary>StS1 Defect blue (card-back / name color), matching StS2's StsColors.blue.</summary>
    public static readonly Color Color = new("87CEEB");

    public override bool HideFromVanillaCharacterSelect => !Config.CharacterGate.DefectEnabled;

    public override bool AllowInVanillaRandomCharacterSelect => Config.CharacterGate.DefectEnabled;

    public override string PlaceholderID => "defect";

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 75; // vanilla StS1 Defect

    // Vanilla starter deck: 4 Strike, 4 Defend, 1 Zap, 1 Dualcast. StS2 ships identical
    // versions of all four (see .tmp/duplicate-cards-report.md), so the deck uses the base-game
    // models - fully qualified, because Spire1.Spire1Code.Cards defines retired same-named mod
    // copies that now live in Spire1LegacyPool.
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeDefect>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeDefect>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeDefect>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeDefect>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendDefect>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendDefect>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendDefect>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendDefect>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Zap>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Dualcast>(),
    ];

    // Starting relic: Cracked Core (channel 1 Lightning at start of combat) - mod class, ID SPIRE1-CRACKED_CORE.
    //
    // C02 gate (2026-10-02). Timing evidence: Player.CreateForNewRun is evaluated as an ARGUMENT
    // to RunState.CreateForNewRun (NGame.cs:1140/1158, NCharacterSelectScreen.cs:745), so this
    // getter runs BEFORE Spire1ContentSnapshotPatch.LatchOnNewRun (a postfix on
    // RunState.CreateForNewRun) updates Spire1RunContent.ContentActiveThisRun. The per-run latch
    // therefore still holds the PREVIOUS run's value here; Spire1Config.IsEnabled(Relics) would be
    // stale and would ignore the user's RegisterContentNextRun choice for the run being created.
    // The correct source is the pending setting: characters gate (master + group) + the relics
    // group + RegisterContentNextRun. Closing any of them returns the ENGINE Cracked Core, so
    // character select (StartingRelics[0] - NCharacterSelectScreen.cs:848/881) and the vanilla
    // start-of-run grant stay valid.
    public override IReadOnlyList<RelicModel> StartingRelics =>
        Config.CharacterGate.DefectEnabled
        && Config.Spire1Config.EnableSts1Relics
        && Config.Spire1Config.RegisterContentNextRun
            ? [ModelDb.Relic<CrackedCore>()]
            : [ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.CrackedCore>()];

    // C02 gate note: the pool GETTERS stay stable on purpose. ModelDb caches AllCardPools /
    // AllRelicPools / AllPotionPools at Preload and CardModel.Pool / RelicModel.Pool /
    // PotionModel.Pool resolve through those caches; swapping the getter would drop our pools from
    // the cache whenever a switch is off at launch and make old saves with SPIRE1-* ids throw
    // "not in any card pool". The content gate lives on the pools themselves instead
    // (GetUnlockedCards / GetUnlockedRelics / GetUnlockedPotions in Character/Spire1*Pool.cs),
    // which is the same query entry every reward/shop/event consumer uses.
    public override CardPoolModel CardPool => ModelDb.CardPool<DefectCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<DefectRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<DefectPotionPool>();

    // Vanilla StS1 Defect: 3 orb slots.
    public override int BaseOrbSlotCount => 3;
}