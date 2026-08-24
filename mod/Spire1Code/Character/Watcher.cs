using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Cards;
using Spire1.Spire1Code.Relics;

namespace Spire1.Spire1Code.Character;

/// <summary>
/// "StS1 - Watcher" — the vanilla Slay the Spire 1 Watcher as an additive StS2 character.
/// FLAG: StS2 has no Watcher visual, so <see cref="PlaceholderCharacterModel"/> uses
/// PlaceholderID = "regent" (the StS2 Regent) as a DOCUMENTED SUBSTITUTE visual —
/// the Watcher herself does not exist in the StS2 roster.
/// FLAG: no Calm/Wrath/Divinity/Mantra stance API exists in StS2 v0.111.0; stance
/// cards (Eruption, Vigilance) implement only their damage/Block and never fake stances.
/// ID = SPIRE1-WATCHER.
/// </summary>
public class Watcher : PlaceholderCharacterModel
{
    public const string CharacterId = "Watcher";

    /// <summary>StS1 Watcher purple (card-back / name color).</summary>
    public static readonly Color Color = new("C973F6");

    /// <summary>Substitute visual: StS2 Regent (no Watcher visual exists in StS2).</summary>
    public override string PlaceholderID => "regent";
    // ARCHIVED by default (Spire1Config.EnableSts1Watcher=false): AFTP ships a finished
    // Watcher; ours keeps two compromises (no stance API, borrowed Regent visual).
    // Models stay registered for old-save compat; character hidden from select + random pool.
    public override bool HideFromVanillaCharacterSelect => !Spire1.Spire1Code.Config.Spire1Config.EnableSts1Watcher;

    public override bool AllowInVanillaRandomCharacterSelect => Spire1.Spire1Code.Config.Spire1Config.EnableSts1Watcher;

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 72; // vanilla StS1 Watcher

    // Vanilla starter deck: 4 Strike, 4 Defend, 1 Eruption, 1 Vigilance.
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeWatcher>(), ModelDb.Card<StrikeWatcher>(), ModelDb.Card<StrikeWatcher>(), ModelDb.Card<StrikeWatcher>(),
        ModelDb.Card<DefendWatcher>(), ModelDb.Card<DefendWatcher>(), ModelDb.Card<DefendWatcher>(), ModelDb.Card<DefendWatcher>(),
        ModelDb.Card<Eruption>(),
        ModelDb.Card<Vigilance>(),
    ];

    // Starting relic: Pure Water (2 Energy at the start of each combat) — mod class, ID SPIRE1-PURE_WATER.
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<PureWater>()];

    public override CardPoolModel CardPool => ModelDb.CardPool<WatcherCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<WatcherRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<WatcherPotionPool>();
}
