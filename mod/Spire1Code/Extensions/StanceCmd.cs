using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Spire1.Spire1Code.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Spire1.Spire1Code.Extensions;

public static class StanceCmd
{
    /// <summary>Vanilla StS1: every 10 Mantra converts into one entry to Divinity.</summary>
    private const int _mantraThreshold = 10;

    public static bool IsIn<TStance>(Player player) where TStance : StancePower
    {
        return player.Creature.GetPower<TStance>() != null;
    }

    public static StancePower? Current(Player player)
    {
        return player.Creature.Powers.OfType<StancePower>().FirstOrDefault();
    }

    public static async Task Enter<TStance>(PlayerChoiceContext ctx, Player player, CardModel? source)
        where TStance : StancePower
    {
        StancePower? current = Current(player);
        if (current is TStance)
        {
            return;
        }

        if (current != null)
        {
            await PowerCmd.Remove(current);
        }

        TStance? entered = await PowerCmd.Apply<TStance>(ctx, player.Creature, 1m, player.Creature, source);
        if (entered == null)
        {
            return;
        }

        if (entered is DivinityPower)
        {
            await PlayerCmd.GainEnergy(3m, player);
        }

        await Dispatch(player, ctx, current, entered);
    }

    public static async Task Exit(PlayerChoiceContext ctx, Player player, CardModel? source)
    {
        StancePower? current = Current(player);
        if (current == null)
        {
            return;
        }

        await PowerCmd.Remove(current);
        await Dispatch(player, ctx, current, null);
    }

    public static async Task GainMantra(PlayerChoiceContext ctx, Player player, decimal amount, CardModel? source)
    {
        MantraPower? mantra = await PowerCmd.Apply<MantraPower>(
            ctx,
            player.Creature,
            amount,
            player.Creature,
            source);
        if (mantra == null)
        {
            return;
        }

        // Vanilla: every 10 Mantra enters Divinity and the remainder carries over, so this must be a
        // loop (Prostrate+ can push past 20 at once). It MUST NOT be an unguarded `while`, though:
        // PowerCmd.ModifyAmount silently returns without touching the amount when the combat is
        // ending or the owner has no CombatState (PowerCmd.cs:221-231), and hooks may rewrite the
        // offset (:239-240), so the naive form can spin forever and hang the game. Bail out the
        // moment an iteration fails to reduce the counter.
        while (mantra.Amount >= _mantraThreshold)
        {
            int before = mantra.Amount;
            await PowerCmd.ModifyAmount(ctx, mantra, -_mantraThreshold, player.Creature, source);
            await Enter<DivinityPower>(ctx, player, source);
            if (mantra.Amount >= before)
            {
                break;
            }
        }
    }

    private static async Task Dispatch(
        Player player,
        PlayerChoiceContext ctx,
        StancePower? from,
        StancePower? to)
    {
        List<IOnStanceChanged> listeners = new();
        foreach (IOnStanceChanged listener in player.Creature.Powers.OfType<IOnStanceChanged>())
        {
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        if (player.PlayerCombatState != null)
        {
            foreach (CardPile pile in player.PlayerCombatState.AllPiles)
            {
                foreach (CardModel card in pile.Cards)
                {
                    if (card is IOnStanceChanged listener && !listeners.Contains(listener))
                    {
                        listeners.Add(listener);
                    }
                }
            }
        }

        foreach (IOnStanceChanged listener in listeners)
        {
            await listener.OnStanceChanged(ctx, from, to);
        }
    }
}
