using BaseLib.Diagnostics;
using Godot;

namespace BaseLib.Config;

[ConfigHoverTipsByDefault]
internal class BaseLibConfig : SimpleModConfig
{
	public static bool ShowModConfigInMainMenu { get; set; } = true;

	[ConfigSection("LogSection")]
	public static bool OpenLogWindowOnStartup { get; set; } = false;

	public static bool OpenLogWindowOnError { get; set; } = false;

	[ConfigSlider(128.0, 2048.0, 64.0)]
	public static int LimitedLogSize { get; set; } = 256;

	[ConfigSlider(8.0, 48.0, 1.0)]
	public static int LogFontSize { get; set; } = 14;

	[ConfigSection("GeneralSettings")]
	[ConfigSlider(1.0, 64.0, 1.0)]
	public static int SfxPlayerLimit { get; set; } = 16;

	[ConfigSection("HarmonyDumpSection")]
	[ConfigTextInput(MaxLength = 1024)]
	public static string HarmonyPatchDumpOutputPath { get; set; } = "";

	public static bool HarmonyPatchDumpOnFirstMainMenu { get; set; }

	[ConfigHideInUI]
	public static int LastLogLevel { get; set; } = 3;

	[ConfigHideInUI]
	public static bool LogUseRegex { get; set; } = false;

	[ConfigHideInUI]
	public static bool LogInvertFilter { get; set; } = false;

	[ConfigHideInUI]
	public static string LogLastFilter { get; set; } = "";

	[ConfigHideInUI]
	public static int LogLastSizeX { get; set; } = 0;

	[ConfigHideInUI]
	public static int LogLastSizeY { get; set; } = 0;

	[ConfigHideInUI]
	public static int LogLastPosX { get; set; } = int.MinValue;

	[ConfigHideInUI]
	public static int LogLastPosY { get; set; } = int.MinValue;

	[ConfigHideInUI]
	public static string LastModConfigModId { get; set; } = "";

	[ConfigButton("HarmonyDumpBrowse")]
	public static void HarmonyDumpBrowseForOutput(ModConfig config)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected O, but got Unknown
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Expected O, but got Unknown
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (((val != null) ? val.Root : null) == null)
		{
			BaseLibMain.Logger.Warn("[HarmonyDump] Cannot open file dialog: SceneTree root is not available.", 1);
			return;
		}
		FileDialog dialog = new FileDialog
		{
			Title = ModConfig.GetBaseLibLabelText("HarmonyDumpBrowseTitle"),
			FileMode = (FileModeEnum)4,
			Access = (AccessEnum)2,
			CurrentFile = "baselib_harmony_patch_dump.log"
		};
		dialog.AddFilter("*.log", "Log");
		dialog.AddFilter("*.txt", "Text");
		dialog.FileSelected += (FileSelectedEventHandler)delegate(string path)
		{
			HarmonyPatchDumpOutputPath = path;
			config.Save();
			config.ConfigReloaded();
			((Node)dialog).QueueFree();
		};
		((AcceptDialog)dialog).Canceled += ((Node)dialog).QueueFree;
		((Node)val.Root).AddChild((Node)(object)dialog, false, (InternalMode)0);
		((Window)dialog).PopupCenteredRatio(0.55f);
	}

	[ConfigButton("HarmonyDumpNow")]
	public static void HarmonyDumpWriteNow(ModConfig _)
	{
		HarmonyPatchDumpCoordinator.TryManualDumpFromSettings();
	}
}
