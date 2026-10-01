using Godot;

namespace Spire1.Spire1Code.Extensions;

//Mostly utilities to get asset paths.
public static class StringExtensions
{
    public static string ImagePath(this string path)
    {
        return Path.Join(MainFile.ResPath, "images", path).Replace('\\', '/');
    }

    public static string CardImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "card_portraits", path).Replace('\\', '/');
        if (ResourceLoader.Exists(path)) return path;
        
        MainFile.Logger.Info("Could not find card image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "card_portraits", "card.png").Replace('\\', '/');
    }

    public static string BigCardImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "card_portraits", "big", path).Replace('\\', '/');
        if (ResourceLoader.Exists(path)) return path;
        
        MainFile.Logger.Info("Could not find big card image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "card_portraits", "big", "card.png").Replace('\\', '/');
    }

    public static string PowerImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "powers", path).Replace('\\', '/');
        if (ResourceLoader.Exists(path)) return path;
        
        MainFile.Logger.Info("Could not find power image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "powers", "power.png").Replace('\\', '/');
    }

    public static string BigPowerImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "powers", "big", path).Replace('\\', '/');
        if (ResourceLoader.Exists(path)) return path;
        
        MainFile.Logger.Info("Could not find big power image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "powers", "big", "power.png").Replace('\\', '/');
    }

    public static string RelicImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "relics", path).Replace('\\', '/');
        if (ResourceLoader.Exists(path)) return path;
        
        MainFile.Logger.Info("Could not find relic image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "relics", "relic.png").Replace('\\', '/');
    }

    public static string BigRelicImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "relics", "big", path).Replace('\\', '/');
        if (ResourceLoader.Exists(path)) return path;
        
        MainFile.Logger.Info("Could not find big relic image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "relics", "big", "relic.png").Replace('\\', '/');
    }

    public static string PotionImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "potions", path).Replace('\\', '/');
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find potion image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "potions", "potion.png").Replace('\\', '/');
    }

    public static string PotionOutlineImagePath(this string path)
    {
        path = Path.Join(MainFile.ResPath, "images", "potions", path).Replace('\\', '/');
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find potion image path: " + path);
        return Path.Join(MainFile.ResPath, "images", "potions", "outline", "potion.png").Replace('\\', '/');
    }

    public static string CharacterUiPath(this string path)
    {
        return Path.Join(MainFile.ResPath, "images", "charui", path).Replace('\\', '/');
    }
}