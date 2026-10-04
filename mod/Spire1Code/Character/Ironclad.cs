using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Character;

/// <summary>
/// "StS1 - Ironclad" - the vanilla Slay the Spire 1 Ironclad as an additive StS2 character.
/// Uses base-game ironclad visuals via <see cref="PlaceholderCharacterModel"/> (PlaceholderID = "ironclad"),
/// so no custom art is required. ID = SPIRE1-IRONCLAD.
/// </summary>
public class Ironclad : PlaceholderCharacterModel
{
    public const string CharacterId = "Ironclad";

    /// <summary>StS1 Ironclad red (card-back / name color).</summary>
    public static readonly Color Color = new("cc4444");

    public override bool HideFromVanillaCharacterSelect => !Config.CharacterGate.IroncladEnabled;

    public override bool AllowInVanillaRandomCharacterSelect => Config.CharacterGate.IroncladEnabled;

    public override string PlaceholderID => "ironclad";

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 80; // vanilla StS1 Ironclad

    // Vanilla starter deck: 5 Strike, 4 Defend, 1 Bash. StS2 ships identical Strike_Ironclad /
    // Defend_Ironclad / Bash models (see .tmp/duplicate-cards-report.md), so the deck uses the
    // base-game cards - fully qualified, because Spire1.Spire1Code.Cards defines retired
    // same-named mod copies (Strike/Defend/Bash) that now live in Spire1LegacyPool.
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendIronclad>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Bash>(),
    ];

    // Starting relic: Burning Blood (heal 6 HP after combat) - mod class, ID SPIRE1-BURNING_BLOOD.
    //
    // C02 gate (2026-10-02). Timing evidence: Player.CreateForNewRun is evaluated as an ARGUMENT
    // to RunState.CreateForNewRun (NGame.cs:1140/1158, NCharacterSelectScreen.cs:745), so this
    // getter runs BEFORE Spire1ContentSnapshotPatch.LatchOnNewRun (a postfix on
    // RunState.CreateForNewRun) updates Spire1RunContent.ContentActiveThisRun. The per-run latch
    // therefore still holds the PREVIOUS run's value here; Spire1Config.IsEnabled(Relics) would be
    // stale and would ignore the user's RegisterContentNextRun choice for the run being created.
    // The correct source is the pending setting: characters gate (master + group) + the relics
    // group + RegisterContentNextRun. Closing any of them returns the ENGINE Burning Blood, so
    // character select (StartingRelics[0] - NCharacterSelectScreen.cs:848/881) and the vanilla
    // start-of-run grant stay valid.
    public override IReadOnlyList<RelicModel> StartingRelics =>
        Config.CharacterGate.IroncladEnabled
        && Config.Spire1Config.EnableSts1Relics
        && Config.Spire1Config.RegisterContentNextRun
            ? [ModelDb.Relic<BurningBlood>()]
            : [ModelDb.Relic<MegaCrit.Sts2.Core.Models.Relics.BurningBlood>()];

    // C02 gate note: the pool GETTERS stay stable on purpose. ModelDb caches AllCardPools /
    // AllRelicPools / AllPotionPools at Preload and CardModel.Pool / RelicModel.Pool /
    // PotionModel.Pool resolve through those caches; swapping the getter would drop our pools from
    // the cache whenever a switch is off at launch and make old saves with SPIRE1-* ids throw
    // "not in any card pool". The content gate lives on the pools themselves instead
    // (GetUnlockedCards / GetUnlockedRelics / GetUnlockedPotions in Character/Spire1*Pool.cs),
    // which is the same query entry every reward/shop/event consumer uses.
    public override CardPoolModel CardPool => ModelDb.CardPool<Spire1CardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<Spire1RelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<Spire1PotionPool>();
}