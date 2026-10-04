using System.Runtime.CompilerServices;
namespace Godot
{
    public static class GD
    {
        public static readonly List<string> Messages = new();
        public static void Print(string message) => Messages.Add(message);
        public static void Print(params object[] items) => Messages.Add(string.Concat(items));
    }
    public static class ResourceLoader
    {
        public static bool Present;
        public static int Checks;
        public static bool Exists(string path) { Checks++; return Present; }
    }
}
namespace MegaCrit.Sts2.Core.Models
{
    public class CardModel
    {
        public string PortraitPath { [MethodImpl(MethodImplOptions.NoInlining)] get => "base:" + GetType().Name; }
    }
    public class SnakeBite : CardModel { }
    public class SNAKEBITE : CardModel { }
}
namespace MegaCrit.Sts2.Core.Nodes.Screens.MainMenu
{
    public class NMainMenu { public void _Ready() { } }
}
namespace MegaCrit.Sts2.Core.Modding
{
    public sealed class ModInitializerAttribute : Attribute { public ModInitializerAttribute(string name) { } }
}