using BaseLib.Abstracts;
using Godot;

namespace Spire1.Spire1Code.Character;

public class SilentCardPool : CustomCardPoolModel
{
    public override string Title => Silent.CharacterId; //This is not a display name.

    // Reuse StS2 Silent's energy icon (res://images/packed/sprite_fonts/silent_energy_icon.png)
    // and its green card frame, matching PlaceholderID="silent". No custom charui paths.
    public override string EnergyColorName => "silent";
    public override string CardFrameMaterialPath => "card_frame_green";

    //Color of small card icons
    public override Color DeckEntryCardColor => new("5EBD00");

    public override bool IsColorless => false;
}
