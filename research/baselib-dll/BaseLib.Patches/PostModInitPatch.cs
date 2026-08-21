using System;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using BaseLib.Patches.Features;
using BaseLib.Patches.Saves;
using BaseLib.Patches.Utils;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using SmartFormat;
using SmartFormat.Core.Extensions;

namespace BaseLib.Patches;

[HarmonyPatch]
internal class PostModInitPatch
{
	private static bool _earlyInit;

	private static bool _lateInit;

	public static bool CanModifyGameplay { get; private set; }

	[HarmonyPatch(typeof(LocManager), "Initialize")]
	[HarmonyPrefix]
	private static void EarlyPostInit()
	{
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Expected O, but got Unknown
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Expected O, but got Unknown
		if (_earlyInit)
		{
			return;
		}
		_earlyInit = true;
		BaseLibMain.Logger.Info("Performing early post-mod init", 1);
		foreach (Mod loadedMod in ModManager.GetLoadedMods())
		{
			ModManifest manifest = loadedMod.manifest;
			if (manifest != null && manifest.affectsGameplay && BetaMainCompatibility._ModManifest.HasDependency(loadedMod.manifest, "BaseLib"))
			{
				BaseLibMain.Logger.Info("Mod " + loadedMod.manifest.id + " that modifies gameplay has BaseLib dependency; gameplay modification enabled.", 1);
				CanModifyGameplay = true;
				break;
			}
		}
		if (CanModifyGameplay)
		{
			CardModifier.RegisterSave();
		}
		CustomMessageWrapper.Initialize();
		CustomTargetedMessageWrapper.Initialize();
		Harmony harmony = new Harmony("PostModInit");
		AddActContent.Patch(harmony);
		ModInterop modInterop = new ModInterop();
		Type[] modTypes = ReflectionHelper.ModTypes;
		foreach (Type type in modTypes)
		{
			modInterop.ProcessType(harmony, type);
			if (type.IsAssignableTo(typeof(IAutoRegisterFormatSpecifier)) && (object)type != null && !type.IsAbstract && !type.IsInterface)
			{
				try
				{
					Smart.Default.AddExtensions((IFormatter[])(object)new IFormatter[1] { (IFormatter)AccessToolsExtensions.CreateInstance(type) });
					BaseLibMain.Logger.Info("Added custom format specifier " + type.Name, 1);
				}
				catch (Exception value)
				{
					BaseLibMain.Logger.Error($"Exception occurred adding format specifier {type}; {value}", 1);
				}
			}
		}
	}

	[HarmonyPatch(typeof(ModelDb), "InitIds")]
	[HarmonyPrefix]
	private static void LatePostInit()
	{
		if (_lateInit)
		{
			return;
		}
		_lateInit = true;
		BaseLibMain.Logger.Info("Performing late post-mod init", 1);
		Type[] modTypes = ReflectionHelper.ModTypes;
		foreach (Type type in modTypes)
		{
			bool flag = false;
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (PropertyInfo propertyInfo in properties)
			{
				if (((MemberInfo)propertyInfo).GetCustomAttribute<SavedPropertyAttribute>() != null && !(propertyInfo.DeclaringType == null))
				{
					if (!SavePatchUtils.IsStoreTypeBaseSupported(propertyInfo.PropertyType))
					{
						BaseLibMain.Logger.Warn($"SavedProperty does not support values of type {propertyInfo.PropertyType}; change {type.Name}.{propertyInfo.Name} to a SavedSpireField for BaseLib to save it.", 1);
					}
					else if (!SavePatchUtils.IsHolderTypeBaseSupported(propertyInfo.DeclaringType))
					{
						string value = (ExtendedSaveTypes.IsSaveHolderSupported(type) ? "change to a SavedSpireField for BaseLib to save it." : "this type is currently also unsupported by BaseLib for saved values.");
						BaseLibMain.Logger.Warn($"SavedProperty {propertyInfo.Name} will not work on type {type.Name}; {value}", 1);
					}
					else
					{
						flag = true;
					}
				}
			}
			FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			for (int j = 0; j < fields.Length; j++)
			{
				CheckSpecialSpireField(fields[j]);
			}
			if (flag)
			{
				if (SavedPropertiesTypeCache._cache.Count == 0)
				{
					BaseLibMain.Logger.Warn("Adding saved properties too early; type cache is still empty.", 1);
				}
				SavedPropertiesTypeCache.InjectTypeIntoCache(type);
			}
		}
		SavedSpireFieldPatch.AddFieldsSorted();
	}

	private static void CheckSpecialSpireField(FieldInfo field)
	{
		Type fieldType = field.FieldType;
		if (fieldType.IsGenericType)
		{
			Type genericTypeDefinition = fieldType.GetGenericTypeDefinition();
			if (!(genericTypeDefinition != typeof(SavedSpireField<, >)) || !(genericTypeDefinition != typeof(AddedNode<, >)))
			{
				field.GetValue(null);
			}
		}
	}
}
