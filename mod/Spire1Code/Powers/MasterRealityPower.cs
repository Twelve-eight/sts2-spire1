using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Spire1.Spire1Code.Config;

namespace Spire1.Spire1Code.Powers;

/// <summary>
/// StS1 Watcher - Master Reality. Whenever a card is created during combat, Upgrade it.
/// AfterCardEnteredCombat fires for every card that lands in a combat pile without coming from another pile, i.e.
/// exactly the cards that were created mid-combat (cards moved from the deck at combat start have an old pile and
/// never reach here). Clones and dupes are skipped: those are copies of a card that already exists, and vanilla
/// only upgrades freshly created cards.
/// </summary>
public sealed class MasterRealityPower : Spire1Power
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override List<(string, string)>? Localization =>
        new PowerLoc(
            "Master Reality",
            "#Whenever a card is created during combat, Upgrade it.",
            "Whenever a card is created during combat, Upgrade it.");

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card.Owner != Owner.Player || card.IsClone || card.IsDupe || !card.IsUpgradable)
            return Task.CompletedTask;
        if (!Spire1Config.IsEnabled(Spire1Config.Spire1ContentGroup.Powers))
        {
            // C12 r5 stale cleanup: wind the disabled instance down at its own next relevant hook.
            return PowerCmd.Remove(this);
        }
        Flash();
        CardCmd.Upgrade(card, CardPreviewStyle.None);
        return Task.CompletedTask;
    }
}
