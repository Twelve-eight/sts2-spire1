using MegaCrit.Sts2.Core.Rooms;
using Spire1.Spire1Code.Monsters;

namespace Spire1.Spire1Code.Encounters;

/// <summary>
/// StS1 "Sentry and Sphere" strong encounter (<c>MonsterHelper.getEncounter("Sentry and Sphere")</c>,
/// bytecode <c>new MonsterGroup(new Sentry(-305f, 30f), new SphericGuardian())</c> — the Act-1
/// <see cref="Sentry"/> paired with the Act-2 <see cref="SphericGuardian"/>.
/// </summary>
public sealed class SentryAndSphereEncounter : Spire1Encounter
{
    public SentryAndSphereEncounter() : base(RoomType.Monster) { }

    public override RoomType RoomType => RoomType.Monster;

    public override IReadOnlyList<int> HomeActs => [2];

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Sentry>(),
        ModelDb.Monster<SphericGuardian>(),
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters() =>
    [
        (ModelDb.Monster<Sentry>().ToMutable(), null),
        (ModelDb.Monster<SphericGuardian>().ToMutable(), null),
    ];

    public override List<(string, string)>? Localization => [("title", "Sentry and Sphere")];
}
