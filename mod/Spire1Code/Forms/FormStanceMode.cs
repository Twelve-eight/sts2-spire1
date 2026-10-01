using System;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Powers;

namespace Spire1.Spire1Code.Forms;

public enum FormStanceKind
{
    None,
    Calm,
    Wrath,
    Divinity
}

/// <summary>Run-local selection. Bridge availability is capability, never the run's rules switch.</summary>
public static class FormStanceMode
{
    public static bool IsSelected(Player? player)
        => player?.RunState?.Modifiers.Any(modifier => modifier is FormStanceModifier) == true;

    // A selected save must fail explicitly if its optional dependency can no longer be bound.
    public static bool IsEnabled(Player? player)
    {
        if (!IsSelected(player))
            return false;
        RequireAvailable();
        return true;
    }

    public static void RequireAvailable()
    {
        if (!FormStanceWatcherBridge.IsAvailable)
            throw new InvalidOperationException("Spire1 Forms unavailable: " + FormStanceWatcherBridge.UnavailableReason);
    }

    public static FormStanceKind KindOf(Type type)
    {
        if (type == typeof(CalmPower) || type == typeof(VoidSerpentStancePower))
            return FormStanceKind.Calm;
        if (type == typeof(WrathPower) || type == typeof(DemonReaperStancePower))
            return FormStanceKind.Wrath;
        if (type == typeof(DivinityPower) || type == typeof(EchoCelestialStancePower))
            return FormStanceKind.Divinity;
        return FormStanceKind.None;
    }

    public static string StanceName(FormStanceKind kind) => kind switch
    {
        FormStanceKind.Calm => "Calm",
        FormStanceKind.Wrath => "Wrath",
        FormStanceKind.Divinity => "Divinity",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    // These canonical values are read-only semantic notification arguments, never applied powers.
    internal static StancePower? LogicalStance(FormStanceKind kind) => kind switch
    {
        FormStanceKind.Calm => ModelDb.Power<VoidSerpentStancePower>(),
        FormStanceKind.Wrath => ModelDb.Power<DemonReaperStancePower>(),
        FormStanceKind.Divinity => ModelDb.Power<EchoCelestialStancePower>(),
        _ => null
    };

    internal static WatcherFormStancePower CreateTracking(FormStanceKind kind, PowerModel nativeMarker)
    {
        var canonical = LogicalStance(kind) as WatcherFormStancePower
            ?? throw new ArgumentOutOfRangeException(nameof(kind));
        var tracking = (WatcherFormStancePower)canonical.ToMutable();
        tracking.BindNativeMarker(nativeMarker);
        return tracking;
    }
}
