using BaseLib.Abstracts;

namespace Spire1.Spire1Code.Acts;

/// <summary>
/// Marker base for the four StS1 acts (Exordium / The City / The Beyond / The Ending).
/// <para>
/// Exists so <c>Spire1Encounter.IsValidForAct</c> can gate on "any StS1 act" without naming
/// each one, and so StS1 encounters never leak into a vanilla StS2 run.
/// </para>
/// <para>
/// <c>actNumber = -1</c> is BaseLib's documented opt-out from natural act spawning
/// ("Set to -1 to prevent your act from spawning naturally"), which is exactly what the M3
/// dungeon selector needs: every StS1 act is registered but unreachable until the selector
/// places it. Note <c>CustomActModel</c> switches on <c>Index</c> for <c>AllAncients</c> and
/// throws on a non-basegame index, so subclasses MUST override <c>AllAncients</c> and
/// <c>BaseNumberOfRooms</c>.
/// </para>
/// </summary>
public abstract class Spire1Act : CustomActModel
{
    protected Spire1Act() : base(-1)
    {
    }

    /// <summary>StS1 act ordinal (1-4). Encounters declare which ordinals they belong to.</summary>
    public abstract int Sts1ActNumber { get; }
}
