using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Spire1.Spire1Code.Diagnostics;

/// <summary>
/// Console entry point for the ordered pool census (SP1-1, 2026-09-15).
///
/// The engine DevConsole auto-instantiates every AbstractConsoleCmd subclass found in
/// loaded mod assemblies (DevConsole ctor via ReflectionHelper.GetSubtypesInMods) and
/// registers DebugOnly commands whenever debug commands are allowed - which includes
/// every modded session (NDevConsole.Create: ModManager.IsRunningModded()). So this
/// command is available in normal play without any build flag, and it is the on-demand
/// trigger that needs no config: typing "poolcensus" runs PoolCensus.Run("console").
///
/// The command is inherently post-load: the console does not exist until the game has
/// finished starting up, so the pool reads it performs can never freeze a later-loaded
/// mod out of a pool (the SP1-1 hazard lives only at initializer time).
///
/// Must keep a public parameterless constructor and a trivial constructor body - the
/// engine instantiates every command class once at console creation WITHOUT per-type
/// error isolation, so a throwing ctor would break the whole console.
/// </summary>
public class PoolCensusConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "poolcensus";

    public override string Args => "";

    public override string Description =>
        "Ordered card-pool census for the Spire1 pools (model id, rarity, multiplicity, injection origin). "
        + "Post-load diagnostic only: reading a pool freezes it (ModHelper.ConcatModelsFromMods) for later-loaded mods.";

    public override bool IsNetworked => false;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        return new CmdResult(success: true, PoolCensus.Run("console"));
    }
}
