namespace BaseLib.Config
{
    public abstract class SimpleModConfig { }
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class ConfigHoverTipsByDefaultAttribute : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class ConfigIgnoreAttribute : System.Attribute { }
}
