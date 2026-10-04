using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Character;

/// <summary>
/// "StS1 - Silent" - the vanilla Slay the Spire 1 Silent as an additive StS2 character.
/// Uses base-game Silent visuals via <see cref="PlaceholderCharacterModel"/> (PlaceholderID = "silent"),
/// so no custom art is required. ID = SPIRE1-SILENT.
/// </summary>
public class Silent : PlaceholderCharacterModel
{
    public const string CharacterId = "Silent";

    /// <summary>StS1 Silent green (card-back / name color).</summary>
    public static readonly Color Color = new("5EBD00");

    public override bool HideFromVanillaCharacterSelect => !Config.CharacterGate.SilentEnabled;

    public override bool AllowInVanillaRandomCharacterSelect => Config.CharacterGate.SilentEnabled;

    public override string PlaceholderID => "silent";

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 70; // vanilla StS1 Silent

    // Vanilla starter deck: 5 Strike, 5 Defend, 1 Neutralize, 1 Survivor. StS2 already ships
    // identical versions of all four (see .tmp/duplicate-cards-report.md), so the deck uses the
    // base-game models directly - fully qualified, because Spire1.Spire1Code.Cards (imported
    // below) defines retired same-named mod copies that now live in Spire1LegacyPool.
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Neutralize>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Survivor>(),
    ];

    // Starting relic: Ring of the Snake (draw 2 at start of each combat) - mod class, ID SPIRE1-RING_OF_THE_SNAKE.
    //
    // C02 gate (2026-10-02). Timing evidence: Player.CreateForNewRun is evaluated as an ARGUMENT
    // to RunState.CreateForNewRun (NGame.cs:1140/1158, NCharacterSelectScreen.cs:745), so this
    // getter runs BEFORE Spire1ContentSnapshotPatch.LatchOnNewRun (a postfix on
    // RunState.CreateForNewRun) updates Spire1RunContent.ContentActiveThisRun. The per-run latch
    // therefore still holds the PREVIOUS run's value here; Spire1Config.IsEnabled(Relics) would be
    // stale and would ignore the user's RegisterContentNextRun choice for the run being created.
    // The correct source is the pending setting: characters gate (master + group) + the relics
    // group + RegisterContentNextRun. Closing any of them returns the ENGINE Ring of the Snake,
    // so character select (StartingRelics[0] - NCharacterSelectScreen.cs:848/881) and the vanilla
    // start-of-run grant stay valid.
    public override IReadOnlyList<RelicModel> StartingRelics =>
        Config.CharacterGate.SilentEnabled
        && Config.Spire1Config.EnableSts1Relics
        && Config.Spire1Config.RegisterContentNextRun
            ? [ModelDb.Relic<RingOfTheSnake>()]
            : [ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.RingOfTheSnake>()];

    // C02 gate note: the pool GETTERS stay stable on purpose. ModelDb caches AllCardPools /
    // AllRelicPools / AllPotionPools at Preload and CardModel.Pool / RelicModel.Pool /
    // PotionModel.Pool resolve through those caches; swapping the getter would drop our pools from
    // the cache whenever a switch is off at launch and make old saves with SPIRE1-* ids throw
    // "not in any card pool". The content gate lives on the pools themselves instead
    // (GetUnlockedCards / GetUnlockedRelics / GetUnlockedPotions in Character/Spire1*Pool.cs),
    // which is the same query entry every reward/shop/event consumer uses.
    public override CardPoolModel CardPool => ModelDb.CardPool<SilentCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<SilentRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<SilentPotionPool>();
}