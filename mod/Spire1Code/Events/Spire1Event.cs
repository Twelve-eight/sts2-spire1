using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// Base class for every ported vanilla Slay the Spire 1 "?" room event.
/// <para>
/// Act routing: StS1's four regions map 1:1 onto the four shipped StS2 acts, so a ported event
/// spawns in the same relative part of the run as it did in StS1:
/// Exordium -&gt; <see cref="Overgrowth"/>, The City -&gt; <see cref="Underdocks"/>,
/// The Beyond -&gt; <see cref="Hive"/>, and StS1's shrines (which could appear in any act)
/// stay "shared" by leaving <see cref="Acts"/> empty, exactly as
/// <see cref="CustomEventModel.Acts"/> documents.
/// </para>
/// <para>
/// Art: StS1 event portraits are not redistributable, and this mod ships no event art, so each
/// event points <see cref="CustomInitialPortraitPath"/> at a thematically matching portrait that
/// StS2 already ships under <c>res://images/events/</c>. That keeps the room looking finished
/// instead of showing a placeholder box, and adds no assets to the repo.
/// </para>
/// </summary>
public abstract class Spire1Event : CustomEventModel
{
    /// <summary>
    /// File-name stem of a portrait that ships with StS2 under <c>res://images/events/</c>,
    /// for example <c>lost_wisp</c>. Verified to exist in <c>SlayTheSpire2.pck</c>.
    /// </summary>
    protected abstract string ShippedPortrait { get; }

    // Cached per instance: the game reads this getter when preloading assets and again when the
    // room builds its visuals, and interpolating the path each time would allocate a new string.
    private string? _portraitPath;

    public override string? CustomInitialPortraitPath =>
        _portraitPath ??= $"res://images/events/{ShippedPortrait}.png";

    // Act arrays are cached because `Acts` is hot: BaseLib reads it once per event in the
    // `CustomEventModel` constructor and then again for every custom event each time an act
    // enumerates its event list (`ContentPatches.AddCustomEvents`). An expression-bodied property
    // would allocate a fresh single-element array on every one of those reads.
    //
    // Resolving a shipped act through `ModelDb` this early is safe, and deliberately so:
    // `ModelDb.Init` constructs models in `AllAbstractModelSubtypes` order, which enumerates
    // `AbstractModelSubtypes.All` (base game) BEFORE appending `ReflectionHelper.GetSubtypesInMods`
    // (us), so all four shipped acts already exist in `_contentById` by the time our event
    // constructors run. If a future BaseLib version reorders that, these lookups would throw at
    // load; that is the one assumption this file makes about model construction order.
    private static ActModel[]? _act1;
    private static ActModel[]? _act2;
    private static ActModel[]? _act3;

    /// <summary>StS1 Exordium events.</summary>
    protected static ActModel[] Act1 => _act1 ??= [ModelDb.Act<Overgrowth>()];

    /// <summary>StS1 The City events.</summary>
    protected static ActModel[] Act2 => _act2 ??= [ModelDb.Act<Underdocks>()];

    /// <summary>StS1 The Beyond events.</summary>
    protected static ActModel[] Act3 => _act3 ??= [ModelDb.Act<Hive>()];
}
