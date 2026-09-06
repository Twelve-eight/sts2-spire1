using Spire1.Spire1Code.Character;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Spire1.Spire1Code.Cards;

/// <summary>
/// StS1 Colorless — The Bomb (Rare Skill). 2 cost. At the end of 3 turns, deal 40 damage
/// to ALL enemies (50 upgraded: upgradeMagicNumber(10), javap-colorless/TheBomb.txt L57-67).
/// R6 (2026-09-06): own class required — RARITY DRIFT: the shipped StS2 TheBomb is
/// CardRarity.Uncommon (dllsrc TheBomb.cs L24) while StS1 is RARE (jar ctor CardRarity.RARE,
/// TheBomb.txt L19). Injecting the shipped card would leak StS2 rarity odds into the StS1
/// colorless pool. All numeric fields match (Turns 3, BombDamage 40, +10 upgrade; engine
/// OnUpgrade L33-36 upgrades BombDamage by 10), so this class reuses the shipped
/// TheBombPower unchanged — only the card wrapper is ours.
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public class TheBomb() : Spire1Card(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private const string TurnsKey = "Turns";
    private const string BombDamageKey = "BombDamage";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new IntVar(TurnsKey, 3), new IntVar(BombDamageKey, 40)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay play)
        => (await PowerCmd.Apply<TheBombPower>(choiceContext, Owner.Creature,
            DynamicVars[TurnsKey].BaseValue, Owner.Creature, this))
            .SetDamage(DynamicVars[BombDamageKey].BaseValue);

    protected override void OnUpgrade() => DynamicVars[BombDamageKey].UpgradeValueBy(10m);
}
