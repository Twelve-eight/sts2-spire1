using BaseLib.Abstracts;
using Godot;

namespace Spire1.Spire1Code.Character;

public class WatcherRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Watcher.Color;

    // Reuse StS2 Regent's energy icon (matching PlaceholderID="regent"); custom charui/*.png render gray.
    public override string EnergyColorName => "regent";
}
