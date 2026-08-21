using BaseLib.Abstracts;
using Godot;

namespace Spire1.Spire1Code.Character;

public class DefectCardPool : CustomCardPoolModel
{
    public override string Title => Defect.CharacterId; //This is not a display name.

    // Reuse StS2 Defect's energy icon (res://images/packed/sprite_fonts/defect_energy_icon.png)
    // and its blue card frame, matching PlaceholderID="defect". No custom charui paths.
    public override string EnergyColorName => "defect";
    public override string CardFrameMaterialPath => "card_frame_blue";

    //Color of small card icons (native StS2 Defect card pool color)
    public override Color DeckEntryCardColor => new("3EB3ED");

    public override bool IsColorless => false;
}
