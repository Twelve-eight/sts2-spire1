using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Monsters;

/// <summary>
/// StS1 The City — BronzeOrb (<c>com.megacrit.cardcrawl.monsters.city.BronzeOrb</c>).
/// 官方中文名：铜球（<c>.tmp/m25-zhs-names.json</c>）。
/// <para>
/// Bytecode (<c>city_BronzeOrb.txt</c>): HP 52-58, A9 54-60; BEAM_DMG 8 flat; SUPPORT_BEAM
/// grants the BronzeAutomaton GainBlock(12); STASIS (once per combat, roll &gt;= 25) is
/// ApplyStasisAction — seal one random card from the player's hand until this orb dies.
/// getMove: <c>!usedStasis &amp;&amp; r&gt;=25</c> → STASIS (STRONG_DEBUFF);
/// <c>r&gt;=70 &amp;&amp; !lastTwo(SUPPORT)</c> → SUPPORT (DEFEND);
/// <c>!lastTwo(BEAM)</c> → BEAM (ATTACK 8); else SUPPORT. The first move is rolled normally,
/// so the machine's initial state is the branch itself (SlaverBlue precedent). takeTurn BEAM:
/// DamageAction(damage[0]); SUPPORT: GainBlockAction(getMonster("BronzeAutomaton"), 12);
/// STASIS: ApplyStasisAction(this).
/// </para>
/// <para>
/// Ticket note (bytecode audit): this m25 dump contains NO orb-death boss buff — BronzeOrb has
/// no <c>die()</c> override at all and <c>BronzeAutomaton.die()</c> only suicides the survivors,
/// so "killing an orb strengthens the boss" is not a mechanic in this build; the orb supports
/// the boss with SUPPORT_BEAM block instead. Stasis is modelled in-code without a power file:
/// the sealed card is pulled out of combat piles via CardPileCmd.RemoveFromCombat (it keeps its
/// deck ownership, so an orb that survives to combat end returns the card exactly like vanilla's
/// limbo does between rooms), and the AfterDeath hook puts it back into the owner's hand when
/// the orb dies. The floating sealed-card display around the orb is cosmetic and is not ported.
/// </para>
/// <para>
/// Ascension mapping: vanilla A9 HP tier → <see cref="AscensionLevel.ToughEnemies"/>; there is
/// no damage/block bump to map. Donor: <c>zapbot</c> — the shipped small floating robot; closest
 /// spherical drone among the shipped scenes, and its scene ships the engine-default track set
/// (idle_loop/cast/attack/hurt/die) so the default animator works untouched (byrdpip precedent).
/// </para>
/// </summary>
public sealed class BronzeOrb : Spire1Monster
{
    // setHp(52, 58); ascension >= 9 -> setHp(54, 60)
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 54, 52);

    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 60, 58);

    // BEAM_DMG = 8 (no ascension variant)
    private const int BeamDamage = 8;

    // SUPPORT_BEAM: GainBlockAction(BronzeAutomaton, 12) (no ascension variant)
    private const int SupportBlock = 12;

    protected override string DonorId => "zapbot";

    // Vanilla field: usedStasis; plus our stasis seal state (vanilla keeps it inside StasisPower).
    private bool _usedStasis;

    private CardModel? _sealedCard;

    private Player? _sealedOwner;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState beam = new("BEAM_MOVE", BeamMove, new SingleAttackIntent(BeamDamage));
        MoveState support = new("SUPPORT_BEAM_MOVE", SupportBeamMove, new DefendIntent());
        MoveState stasis = new("STASIS_MOVE", StasisMove, new DebuffIntent());

        ConditionalBranchState branch = new("BRONZE_ORB_BRANCH");
        beam.FollowUpState = branch;
        support.FollowUpState = branch;
        stasis.FollowUpState = branch;

        // Predicate order reproduces vanilla priority: once-only STASIS, then the two roll bands.
        branch.AddState(stasis, () => !_usedStasis && RollHundred() >= 25);
        branch.AddState(support, () => RollHundred() >= 70 && !LastTwoWere(support));
        branch.AddState(beam, () => !LastTwoWere(beam));
        branch.AddState(support, () => true);
        return new MonsterMoveStateMachine([beam, support, stasis, branch], branch);
    }

    private async Task BeamMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Attack", 0.4f);
        await DamageCmd.Attack(BeamDamage).FromMonster(this)
            .WithHitFx("vfx/vfx_attack_lightning")
            .Execute(null);
    }

    private async Task SupportBeamMove(IReadOnlyList<Creature> targets)
    {
        // takeTurn SUPPORT_BEAM: GainBlockAction(monsters.getMonster("BronzeAutomaton"), 12) —
        // the block goes to the boss, not to the orb itself. Vanilla passes a null target when
        // the automaton is gone; GainBlock on a dead creature is a no-op in StS2 as well.
        _ = targets;
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        Creature? automaton = CombatState.Enemies
            .FirstOrDefault(c => c.Monster is BronzeAutomaton && !c.IsDead);
        if (automaton != null)
        {
            await CreatureCmd.GainBlock(automaton, SupportBlock, ValueProp.Move, null);
        }
    }

    /// <summary>
    /// takeTurn STASIS: ApplyStasisAction(this) seals one random hand card until death.
    /// Multiplayer takes one card from one randomly chosen player's hand (vanilla is
    /// single-player); the run's CombatCardSelection rng picks the card (AllOutAttack idiom).
    /// </summary>
    private async Task StasisMove(IReadOnlyList<Creature> targets)
    {
        _usedStasis = true;
        List<Player> players = targets
            .Select(t => t.Player)
            .Where(p => p != null && p.RunState.CurrentRoom is MegaCrit.Sts2.Core.Rooms.CombatRoom)
            .Distinct()
            .ToList()!;
        if (players.Count == 0)
        {
            return;
        }
        Player owner = players[base.Rng.NextInt(players.Count)];
        IReadOnlyList<CardModel> hand = PileType.Hand.GetPile(owner).Cards;
        if (hand.Count == 0)
        {
            return;
        }
        _sealedOwner = owner;
        _sealedCard = CombatState.RunState.Rng.CombatCardSelection.NextItem(hand.ToList());
        if (_sealedCard != null)
        {
            await CardPileCmd.RemoveFromCombat(_sealedCard);
        }
    }

    /// <summary>
    /// Stasis release: when the sealing orb dies, the sealed card returns to its owner's hand
    /// (vanilla StasisPower removal path). If combat already ended, RemoveFromCombat left the
    /// card in the run deck, so nothing needs to happen.
    /// </summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (creature != base.Creature || _sealedCard == null || _sealedOwner == null)
        {
            return;
        }
        CardModel card = _sealedCard;
        Player owner = _sealedOwner;
        _sealedCard = null;
        _sealedOwner = null;
        if (owner.RunState.CurrentRoom is MegaCrit.Sts2.Core.Rooms.CombatRoom)
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    // One stable 0-99 draw per move selection (vanilla passes one aiRng roll through getMove).
    private int? _roll;

    private int _rollTurn = -1;

    private int RollHundred()
    {
        int turn = base.Creature?.CombatState?.RoundNumber ?? 0;
        if (_roll == null || _rollTurn != turn)
        {
            _roll = base.Rng.NextInt(100);
            _rollTurn = turn;
        }
        return _roll.Value;
    }

    private bool LastTwoWere(MonsterState state)
    {
        List<MonsterState> log = base.MoveStateMachine.StateLog;
        return log.Count >= 2 && ReferenceEquals(log[^1], state) && ReferenceEquals(log[^2], state);
    }

    public override List<(string, string)>? Localization =>
        new MonsterLoc("Bronze Orb",
        [
            ("BEAM_MOVE", "Beam"),
            ("SUPPORT_BEAM_MOVE", "Support Beam"),
            ("STASIS_MOVE", "Stasis"),
        ]);
}
