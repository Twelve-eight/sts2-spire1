using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Orbs;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Defect - Chill (Uncommon Skill, 0 cost). javap <c>cards/blue/Chill.txt</c>:
/// <c>::&lt;init&gt;</c> @33-61 sets <c>exhaust = true</c> and
/// <c>baseMagicNumber = magicNumber = 1</c>; <c>::upgrade</c> @7-27 sets <c>isInnate = true</c>
/// and leaves <c>exhaust = true</c>.
/// <para>
/// <c>::use</c> counts, in one pass before anything is queued (@0-46), every monster whose
/// <c>isDeadOrEscaped()</c> is false, then queues exactly <c>count * magicNumber</c>
/// <c>ChannelAction(Frost)</c> at the bottom of the action queue (@49-84). The count is a
/// snapshot: a monster added or removed by an earlier Frost channel never changes how many orbs
/// this play generates.
/// </para>
/// </summary>
[Pool(typeof(Spire1LegacyPool))]
public class Chill() : Spire1Card(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("MagicNumber", 1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
    {
        var enemies = CombatState?.Enemies;
        if (enemies == null)
            return;

        // AbstractMonster.isDeadOrEscaped() = isDying || halfDead || isEscaping, mapped from
        // public host state only: a dying or half-dead monster is CurrentHp <= 0
        // (dllsrc Creature.cs:207), and an escaping monster has already left CombatState.Enemies
        // (CreatureCmd.Escape -> CombatState.CreatureEscaped -> RemoveCreature,
        // dllsrc CreatureCmd.cs:620-635 / CombatState.cs:267-278). Creature.IsHittable is NOT
        // that gate: Hook.ShouldAllowHitting also hides alive monsters (Creature.cs:286-298),
        // and StS1 keeps counting those.
        int enemyCount = 0;
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive)
                enemyCount++;
        }

        // One real Frost channel per counted enemy-magicNumber pair, awaited in order: the same
        // ChannelAction(Frost) sequence the legacy addToBot loop produces, so FrostOrb.Passive,
        // Evoke and the Block/history entries are produced by the host orb queue itself.
        int channels = enemyCount * DynamicVars["MagicNumber"].IntValue;
        for (int i = 0; i < channels; i++)
            await OrbCmd.Channel<FrostOrb>(choiceContext, Owner);
    }

    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
