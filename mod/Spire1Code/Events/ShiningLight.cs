using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 — Shining Light. Entering the light costs 20% of Max HP and upgrades two random upgradeable
/// cards (only one if the deck holds a single upgradeable card). The choice is locked when nothing
/// can be upgraded.
/// </summary>
public class ShiningLight : Spire1Event
{
    private const float _hpLossPercent = 0.2f;

    private const float _a15HpLossPercent = 0.3f;

    private const int _upgradeCount = 2;

    public override ActModel[] Acts => Act1;

    protected override string ShippedPortrait => "colossal_flower";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("Damage", 0)];

    public override void CalculateVars()
    {
        // StS1: damage = MathUtils.round(maxHealth * 0.2f), i.e. floor(x + 0.5); 0.3f at Ascension 15+.
        float percent = Owner.RunState.AscensionLevel >= 15 ? _a15HpLossPercent : _hpLossPercent;
        DynamicVars["Damage"].BaseValue = (int)(Owner.Creature.MaxHp * percent + 0.5f);
    }

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        bool canEnter = PileType.Deck.GetPile(Owner).Cards.Any(c => c.IsUpgradable);
        return
        [
            canEnter ? Option(Enter).ThatDoesDamage(DynamicVars["Damage"].BaseValue) : LockedOption("LOCKED_ENTER"),
            Option(Leave),
        ];
    }

    private async Task Enter()
    {
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature,
            DynamicVars["Damage"].BaseValue, ValueProp.Unblockable | ValueProp.Unpowered, null, null, null);

        // StS1: collect upgradeable cards, shuffle with a Random seeded from miscRng, upgrade up to two.
        var upgradeable = PileType.Deck.GetPile(Owner).Cards.Where(c => c.IsUpgradable).ToList();
        Rng.Shuffle(upgradeable);
        foreach (var card in upgradeable.Take(_upgradeCount))
        {
            CardCmd.Upgrade(card);
        }
        SetEventFinished(PageDescription("ENTERED"));
    }

    private async Task Leave()
    {
        SetEventFinished(PageDescription("LEFT"));
    }
}
