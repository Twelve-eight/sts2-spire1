using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace Spire1.Spire1Code.Events;

/// <summary>
/// StS1 Beyond event — Sensory Stone. Touching the tesseract recalls a random memory (one of four) and
/// offers up to three colorless card rewards at an HP cost (5 / 10 HP as HP_LOSS damage).
/// The colorless card rewards are FLAGGED: StS1 grants random COLORLESS card rewards
/// (RewardItem(CardColor.COLORLESS), one per option tier); no StS1 colorless cards exist in the mod, so
/// the rewards are not granted — only the memory text and the HP loss are implemented.
/// </summary>
public class SensoryStone : Spire1Event
{
    protected override string ShippedPortrait => "sapphire_seed";

    public override ActModel[] Acts => Act3;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // StS1 INTRO: "[Interact]" only.
        return [Option(Interact)];
    }

    private async Task Interact()
    {
        SetEventState(PageDescription("CHOICE"), new List<EventOption>
        {
            Option(RecallOne, "CHOICE"),
            Option(RecallTwo, "CHOICE").ThatDoesDamage(5m),
            Option(RecallThree, "CHOICE").ThatDoesDamage(10m),
        });
    }

    private async Task RecallOne()
    {
        // FLAGGED: no colorless card reward granted (1 card in StS1).
        await ShowRandomMemory();
    }

    private async Task RecallTwo()
    {
        // FLAGGED: no colorless card reward granted (2 cards in StS1).
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, 5m, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        await ShowRandomMemory();
    }

    private async Task RecallThree()
    {
        // FLAGGED: no colorless card reward granted (3 cards in StS1).
        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature, 10m, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        await ShowRandomMemory();
    }

    private Task ShowRandomMemory()
    {
        // StS1: getRandomMemory() shuffles the four memory texts and shows one.
        int memory = Rng.NextInt(0, 4) + 1;
        SetEventFinished(PageDescription($"MEMORY_{memory}"));
        return Task.CompletedTask;
    }
}
