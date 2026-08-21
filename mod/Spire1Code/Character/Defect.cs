using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Character;

/// <summary>
/// "StS1 - Defect" — the vanilla Slay the Spire 1 Defect as an additive StS2 character.
/// Uses base-game Defect visuals via <see cref="PlaceholderCharacterModel"/> (PlaceholderID = "defect"),
/// so no custom art is required. ID = SPIRE1-DEFECT.
/// </summary>
public class Defect : PlaceholderCharacterModel
{
    public const string CharacterId = "Defect";

    /// <summary>StS1 Defect blue (card-back / name color), matching StS2's StsColors.blue.</summary>
    public static readonly Color Color = new("87CEEB");

    public override string PlaceholderID => "defect";

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 75; // vanilla StS1 Defect

    // Vanilla starter deck: 4 Strike, 4 Defend, 1 Zap, 1 Dualcast.
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeDefect>(), ModelDb.Card<StrikeDefect>(), ModelDb.Card<StrikeDefect>(), ModelDb.Card<StrikeDefect>(),
        ModelDb.Card<DefendDefect>(), ModelDb.Card<DefendDefect>(), ModelDb.Card<DefendDefect>(), ModelDb.Card<DefendDefect>(),
        ModelDb.Card<Zap>(),
        ModelDb.Card<Dualcast>(),
    ];

    // Starting relic: Cracked Core (channel 1 Lightning at start of combat) — mod class, ID SPIRE1-CRACKED_CORE.
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<CrackedCore>()];

    public override CardPoolModel CardPool => ModelDb.CardPool<DefectCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<DefectRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<DefectPotionPool>();

    // Vanilla StS1 Defect: 3 orb slots.
    public override int BaseOrbSlotCount => 3;
}
