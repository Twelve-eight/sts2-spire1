using BaseLib.Abstracts;
using Spire1.Spire1Code.Extensions;
using Godot;

namespace Spire1.Spire1Code.Character;

public class Spire1PotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Ironclad.Color;
    

    public override string EnergyColorName => "ironclad";
}