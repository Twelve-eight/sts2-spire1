// These collaborators supply only the carrier's external selection/marker identity inputs.
// They do not implement the Watcher bridge, native marker lifecycle, or hook scheduling.
namespace Forms.FormsCode;

public enum FormStanceKind { None, Calm, Wrath, Divinity }

public static class FormStanceMode
{
    public static bool IsEnabled(Player? player) => player != null;
    public static string StanceName(FormStanceKind kind) => kind.ToString();
}

public static class FormStanceWatcherBridge
{
    public static FormStanceKind KindOfMarker(Type type)
        => type == typeof(ProbeDivinityMarker) ? FormStanceKind.Divinity : FormStanceKind.None;

    public static Task Exit(Player player)
        => Task.FromException(new NotSupportedException("Native Watcher Exit is outside the carrier probe. Remove the fixture marker before removing its carrier."));
}

public sealed class ProbeDivinityMarker : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
}
