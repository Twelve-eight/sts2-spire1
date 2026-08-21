using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Helpers;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// Base for every ported vanilla StS1 monster.
/// <para>
/// Visuals borrow one of the 121 shipped StS2 creature scenes rather than shipping art —
/// the same redirection <c>PlaceholderCharacterModel</c> already uses for our characters,
/// which is proven in-game. <see cref="MonsterModel.VisualsPath"/> is
/// <c>creature_visuals/&lt;Id.Entry.ToLowerInvariant()&gt;</c>, and BaseLib's
/// <c>VisualsPath</c> patch substitutes <see cref="CustomMonsterModel.CustomVisualPath"/>
/// when it is non-null, so pointing at a shipped scene id is all that is required.
/// </para>
/// <para>
/// Localization is supplied in code. <see cref="CustomMonsterModel"/> is the one BaseLib
/// content base class that does NOT declare <see cref="ILocalizationProvider"/>, so we add
/// it here; <c>ModelLocPatch</c> only tests <c>is ILocalizationProvider</c>, and category
/// <c>MonsterModel</c> already maps to the <c>monsters</c> table, so <c>LocTable</c> stays
/// null. Build the value with BaseLib's <c>MonsterLoc</c> record, which keys move titles as
/// <c>moves.&lt;STATE_ID&gt;.title</c>.
/// </para>
/// </summary>
public abstract class Spire1Monster : CustomMonsterModel, ILocalizationProvider
{
    /// <summary>
    /// The shipped StS2 monster whose <c>creature_visuals</c> scene this monster borrows,
    /// spelled as it appears on disk: snake_case of the shipped class name
    /// (e.g. <c>DampCultist</c> ships as <c>damp_cultist</c>).
    /// </summary>
    protected abstract string DonorId { get; }

    public override string? CustomVisualPath => SceneHelper.GetScenePath("creature_visuals/" + DonorId);

    /// <summary>
    /// Vanilla monster sfx paths are derived from the model Id, so they cannot resolve for a
    /// modded id. Default to silence instead of a broken FMOD event. To add sound, copy the
    /// event string verbatim out of the donor's shipped model source and re-enable this —
    /// the paths are not mechanically derivable (<c>DampCultist</c> dies to
    /// <c>.../cultists/cultists_die_damp</c>, not <c>.../damp_cultist/...</c>).
    /// </summary>
    public override bool HasDeathSfx => false;

    /// <summary>Monster name plus one entry per move title. Required for every monster.</summary>
    public abstract List<(string, string)>? Localization { get; }
}
