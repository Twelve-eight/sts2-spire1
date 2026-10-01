using System.Reflection;

namespace FormEffectsProbe;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        string? filter = null;
        bool listOnly = false;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--list") listOnly = true;
            else if (args[i] == "--filter" && i + 1 < args.Length) filter = args[++i];
            else
            {
                Console.WriteLine("ERROR usage: FormEffectsProbe [--filter text] [--list]");
                return 2;
            }
        }
        string source = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "FormsSourceRoot").Value ?? "unspecified";
        Console.WriteLine("SOURCE " + source);
        Console.WriteLine("BOUNDARY production form hooks linked; command spies and narrow power lifecycle collaborators only");
        Console.WriteLine("BOUNDARY scripted RNG is not MegaRandom; no game bridge, UI, multiplayer, save or full scheduler validation");
        var suite = new ProbeSuite();
        VoidScenarios.Register(suite);
        SerpentScenarios.Register(suite);
        DemonScenarios.Register(suite);
        EchoScenarios.Register(suite);
        CelestialReaperScenarios.Register(suite);
        CloneScenarios.Register(suite);
        TransactionScenarios.Register(suite);
        return await suite.Run(filter, listOnly);
    }
}
