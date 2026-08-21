using BaseLib.Abstracts;
using Godot;

namespace Spire1.Spire1Code.Character;

public class DefectPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Defect.Color;

    // Reuse StS2 Defect's energy icon instead of mod charui/*.png (which render gray).
    public override string EnergyColorName => "defect";
}
