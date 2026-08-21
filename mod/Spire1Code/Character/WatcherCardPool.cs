using BaseLib.Abstracts;
using Godot;

namespace Spire1.Spire1Code.Character;

public class WatcherCardPool : CustomCardPoolModel
{
    public override string Title => Watcher.CharacterId; //This is not a display name.

    // Reuse StS2 Regent's energy icon (matching PlaceholderID="regent"), since no Watcher
    // energy icon exists in StS2 and custom charui/*.png render gray.
    public override string EnergyColorName => "regent";

    // FLAG: "card_frame_purple" is NOT a shipped StS2 material (shipped: blue, pink, orange,
    // colorless, curse, quest, red, green — see AssetSets + base-game pools), so per pool
    // conventions we use "card_frame_red" and tint it purple via the HSV shader below.
    public override string CardFrameMaterialPath => "card_frame_red";

    // Watcher purple applied as HSV tint onto the red frame (same mechanism Spire1CardPool
    // documents for coloring card backs).
    public override Color ShaderColor => Watcher.Color;

    //Color of small card icons
    public override Color DeckEntryCardColor => new("C973F6");

    public override bool IsColorless => false;
}
