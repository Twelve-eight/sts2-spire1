using BaseLib.Abstracts;
using Godot;

namespace Spire1.Spire1Code.Character;

/// <summary>
/// Sink pool for StS1 cards that StS2 already ships under the same name with identical
/// behavior (see .tmp/duplicate-cards-report.md, group A + the six Strike/Defend variants).
/// They must stay registered as models (saved runs reference their SPIRE1-* ids, and
/// BaseLib's CustomCardModel ctor throws without a Pool attribute), but they must NOT be
/// offered as rewards: a StS1 character drawing "Survivor" should get the real StS2 card.
/// No character references this pool and IsShared is false, so it never surfaces in any
/// reward screen, shop, or card library — the cards are effectively retired while their
/// model ids remain loadable for old saves.
/// </summary>
public class Spire1LegacyPool : CustomCardPoolModel
{
    public override string Title => "Spire1Legacy"; // not a display name

    public override bool IsColorless => true;

    public override string EnergyColorName => "colorless";

    public override string CardFrameMaterialPath => "card_frame_colorless";

    public override Color DeckEntryCardColor => new("8A8A8A");
}
