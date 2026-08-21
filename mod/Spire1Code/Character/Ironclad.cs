using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Character;

/// <summary>
/// "StS1 - Ironclad" — the vanilla Slay the Spire 1 Ironclad as an additive StS2 character.
/// Uses base-game ironclad visuals via <see cref="PlaceholderCharacterModel"/> (PlaceholderID = "ironclad"),
/// so no custom art is required. ID = SPIRE1-IRONCLAD.
/// </summary>
public class Ironclad : PlaceholderCharacterModel
{
    public const string CharacterId = "Ironclad";

    /// <summary>StS1 Ironclad red (card-back / name color).</summary>
    public static readonly Color Color = new("cc4444");

    public override string PlaceholderID => "ironclad";

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Neutral;
    public override int StartingHp => 80; // vanilla StS1 Ironclad

    // Vanilla starter deck: 5 Strike, 4 Defend, 1 Bash. StS2 ships identical Strike_Ironclad /
    // Defend_Ironclad / Bash models (see .tmp/duplicate-cards-report.md), so the deck uses the
    // base-game cards — fully qualified, because Spire1.Spire1Code.Cards defines retired
    // same-named mod copies (Strike/Defend/Bash) that now live in Spire1LegacyPool.
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.StrikeIronclad>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendIronclad>(), ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.DefendIronclad>(),
        ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.Bash>(),
    ];

    // Starting relic: Burning Blood (heal 6 HP after combat) — mod class, ID SPIRE1-BURNING_BLOOD.
    public override IReadOnlyList<RelicModel> StartingRelics => [ ModelDb.Relic<BurningBlood>() ];

    public override CardPoolModel CardPool => ModelDb.CardPool<Spire1CardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<Spire1RelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<Spire1PotionPool>();
}
