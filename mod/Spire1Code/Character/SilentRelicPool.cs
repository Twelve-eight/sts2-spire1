using BaseLib.Abstracts;
using Godot;

namespace Spire1.Spire1Code.Character;

public class SilentRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Silent.Color;

    // Reuse StS2 Silent's energy icon instead of mod charui/*.png (which render gray).
    public override string EnergyColorName => "silent";
}
