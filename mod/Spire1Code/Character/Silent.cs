using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Character;

/// <summary>
/// "StS1 - Silent" — the vanilla Slay the Spire 1 Silent as an additive StS2 character.
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
    // base-game models directly — fully qualified, because Spire1.Spire1Code.Cards (imported
    // below) defines retired same-named mod copies that now live in Spire1LegacyPool.
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeSilent>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendSilent>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Neutralize>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Survivor>(),
    ];

    // Starting relic: Ring of the Snake (draw 2 at start of each combat) — mod class, ID SPIRE1-RING_OF_THE_SNAKE.
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<RingOfTheSnake>()];

    public override CardPoolModel CardPool => ModelDb.CardPool<SilentCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<SilentRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<SilentPotionPool>();
}
