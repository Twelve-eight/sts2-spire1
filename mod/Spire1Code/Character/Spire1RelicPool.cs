using BaseLib.Abstracts;
using Spire1.Spire1Code.Extensions;
using Godot;

namespace Spire1.Spire1Code.Character;

public class Spire1RelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Ironclad.Color;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}