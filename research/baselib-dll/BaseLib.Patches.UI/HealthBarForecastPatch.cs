using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Hooks;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Patches.UI;

[HarmonyPatch]
public static class HealthBarForecastPatch
{
	private sealed class HealthBarForecastUiState(Control rightContainer, Control leftContainer, NinePatchRect rightTemplate, NinePatchRect leftTemplate, List<NinePatchRect> rightSegments)
	{
		public Control RightContainer { get; } = rightContainer;

		public Control LeftContainer { get; } = leftContainer;

		public NinePatchRect RightTemplate { get; } = rightTemplate;

		public NinePatchRect LeftTemplate { get; } = leftTemplate;

		public List<NinePatchRect> RightSegments { get; } = rightSegments;

		public List<NinePatchRect> LeftSegments { get; } = new List<NinePatchRect>();

		public HealthBarForecastRenderResult LastRender { get; set; } = HealthBarForecastRenderResult.Empty;
	}

	private readonly record struct CustomSegment(int Amount, Color Color, HealthBarForecastDirection Direction, int Order, long SequenceOrder, Material? OverlayMaterial, Color? OverlaySelfModulate);

	private readonly record struct LethalCandidate(int Amount, Color Color, int Order, long SequenceOrder);

	private readonly record struct HealthBarForecastRenderResult(bool HasRightForecast, float RightForecastEdgeOffsetRight, Color? LethalRightColor, Color? LethalLeftColor)
	{
		public static HealthBarForecastRenderResult Empty => new HealthBarForecastRenderResult(HasRightForecast: false, 0f, null, null);
	}

	private static readonly SpireField<NHealthBar, HealthBarForecastUiState?> UiStates = new SpireField<NHealthBar, HealthBarForecastUiState>(() => (HealthBarForecastUiState?)null);

	private static readonly Color DoomLethalTextColor = new Color("FB8DFF");

	private static readonly Color DoomLethalOutlineColor = new Color("2D1263");

	[HarmonyPatch(typeof(NHealthBar), "RefreshForeground")]
	[HarmonyPostfix]
	private static void RefreshForegroundPostfix(NHealthBar __instance)
	{
		RefreshForegroundOverlay(__instance);
	}

	[HarmonyPatch(typeof(NHealthBar), "RefreshMiddleground")]
	[HarmonyPostfix]
	private static void RefreshMiddlegroundPostfix(NHealthBar __instance)
	{
		RefreshMiddlegroundOverlay(__instance);
	}

	[HarmonyPatch(typeof(NHealthBar), "RefreshText")]
	[HarmonyPostfix]
	private static void RefreshTextPostfix(NHealthBar __instance)
	{
		RefreshTextOverlay(__instance);
	}

	private static void RefreshForegroundOverlay(NHealthBar healthBar)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_020f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d4: Unknown result type (might be due to invalid IL or missing references)
		Creature creature = healthBar._creature;
		if (creature.CurrentHp <= 0 || HpDisplayExtensions.IsInfinite(creature.HpDisplay))
		{
			HideAllCustomSegments(healthBar);
			return;
		}
		CustomSegment[] customSegments = GetCustomSegments(creature);
		if (customSegments.Length == 0)
		{
			HideAllCustomSegments(healthBar);
		}
		else
		{
			if (!EnsureUiState(healthBar))
			{
				return;
			}
			HealthBarForecastUiState healthBarForecastUiState = UiStates[healthBar];
			if (healthBarForecastUiState == null)
			{
				return;
			}
			EnsureOverlayOrder(healthBar, healthBarForecastUiState);
			float maxFgWidth = GetMaxFgWidth(healthBar);
			Control hpForeground = healthBar._hpForeground;
			int num = Math.Clamp(HpFromOffsetRight(healthBar, hpForeground.OffsetRight), 0, creature.CurrentHp);
			int num2 = ((((CanvasItem)hpForeground).Visible || num < creature.CurrentHp) ? num : 0);
			CustomSegment[] array = (from segment in customSegments
				where segment.Direction == HealthBarForecastDirection.FromRight
				orderby segment.Order, segment.SequenceOrder
				select segment).ToArray();
			int num3 = num2;
			float offsetRight = hpForeground.OffsetRight;
			Color? lethalRightColor = null;
			int num4 = 0;
			CustomSegment[] array2 = array;
			for (int num5 = 0; num5 < array2.Length; num5++)
			{
				CustomSegment customSegment = array2[num5];
				if (num3 <= 0)
				{
					break;
				}
				int num6 = Math.Min(customSegment.Amount, num3);
				if (num6 > 0)
				{
					EnsureSegmentCount(healthBarForecastUiState.RightSegments, healthBarForecastUiState.RightContainer, num4 + 1, healthBarForecastUiState.RightTemplate);
					NinePatchRect val = healthBarForecastUiState.RightSegments[num4];
					int amount = num3;
					num3 -= num6;
					float fgWidth = GetFgWidth(healthBar, num3);
					float fgWidth2 = GetFgWidth(healthBar, amount);
					((CanvasItem)val).Visible = true;
					ApplyForecastSegmentAppearance(val, customSegment.Color, customSegment.OverlayMaterial, customSegment.OverlaySelfModulate);
					((Control)val).OffsetLeft = ((num3 > 0) ? Math.Max(0f, fgWidth - (float)val.PatchMarginLeft) : 0f);
					((Control)val).OffsetRight = fgWidth2 - maxFgWidth;
					if (num4 == 0)
					{
						offsetRight = ((Control)val).OffsetRight;
					}
					if (num3 <= 0)
					{
						lethalRightColor = customSegment.Color;
					}
					num4++;
				}
			}
			HideSegments(healthBarForecastUiState.RightSegments, num4);
			if (num4 > 0)
			{
				if (num3 > 0)
				{
					((CanvasItem)hpForeground).Visible = true;
					hpForeground.OffsetRight = GetFgWidth(healthBar, num3) - maxFgWidth;
				}
				else
				{
					((CanvasItem)hpForeground).Visible = false;
				}
				Control doomForeground = healthBar._doomForeground;
				if (((CanvasItem)doomForeground).Visible)
				{
					if (num3 > 0)
					{
						doomForeground.OffsetRight = Math.Min(doomForeground.OffsetRight, hpForeground.OffsetRight);
					}
					else
					{
						((CanvasItem)doomForeground).Visible = false;
					}
				}
			}
			if (num3 <= 0)
			{
				HideSegments(healthBarForecastUiState.LeftSegments);
				healthBarForecastUiState.LastRender = new HealthBarForecastRenderResult(HasRightForecast: true, offsetRight, lethalRightColor, null);
				return;
			}
			CustomSegment[] array3 = (from segment in customSegments
				where segment.Direction == HealthBarForecastDirection.FromLeft
				orderby segment.Order, segment.SequenceOrder
				select segment).ToArray();
			int num7 = 0;
			int num8 = 0;
			array2 = array3;
			for (int num5 = 0; num5 < array2.Length; num5++)
			{
				CustomSegment customSegment2 = array2[num5];
				if (num7 >= num3)
				{
					break;
				}
				int num9 = num7;
				num7 = Math.Min(num3, num7 + customSegment2.Amount);
				if (num7 > num9)
				{
					EnsureSegmentCount(healthBarForecastUiState.LeftSegments, healthBarForecastUiState.LeftContainer, num8 + 1, healthBarForecastUiState.LeftTemplate);
					NinePatchRect val2 = healthBarForecastUiState.LeftSegments[num8];
					float fgWidth3 = GetFgWidth(healthBar, num9);
					float fgWidth4 = GetFgWidth(healthBar, num7);
					((CanvasItem)val2).Visible = true;
					ApplyForecastSegmentAppearance(val2, customSegment2.Color, customSegment2.OverlayMaterial, customSegment2.OverlaySelfModulate);
					((Control)val2).OffsetLeft = ((num9 > 0) ? Math.Max(0f, fgWidth3 - (float)val2.PatchMarginLeft) : 0f);
					float num10 = Math.Min(0f, fgWidth4 - maxFgWidth + (float)val2.PatchMarginRight);
					if (num4 > 0)
					{
						num10 = Math.Min(num10, offsetRight);
					}
					((Control)val2).OffsetRight = num10;
					num8++;
				}
			}
			HideSegments(healthBarForecastUiState.LeftSegments, num8);
			Color? lethalLeftColor = ResolveLeftLethalColor(creature, num3, array3);
			healthBarForecastUiState.LastRender = new HealthBarForecastRenderResult(num4 > 0, offsetRight, lethalRightColor, lethalLeftColor);
		}
	}

	private static void RefreshMiddlegroundOverlay(NHealthBar healthBar)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		HealthBarForecastUiState healthBarForecastUiState = UiStates[healthBar];
		if (healthBarForecastUiState == null || !healthBarForecastUiState.LastRender.HasRightForecast)
		{
			return;
		}
		Creature creature = healthBar._creature;
		if (creature.CurrentHp > 0 && !HpDisplayExtensions.IsInfinite(creature.HpDisplay))
		{
			Control hpMiddleground = healthBar._hpMiddleground;
			float rightForecastEdgeOffsetRight = healthBarForecastUiState.LastRender.RightForecastEdgeOffsetRight;
			bool flag = rightForecastEdgeOffsetRight >= hpMiddleground.OffsetRight;
			hpMiddleground.OffsetRight += 1f;
			Tween middlegroundTween = healthBar._middlegroundTween;
			if (middlegroundTween != null)
			{
				middlegroundTween.Kill();
			}
			Tween val = ((Node)healthBar).CreateTween();
			val.TweenProperty((GodotObject)(object)hpMiddleground, NodePath.op_Implicit("offset_right"), Variant.op_Implicit(rightForecastEdgeOffsetRight - 2f), 1.0).SetDelay(flag ? 0.0 : 1.0).SetEase((EaseType)1)
				.SetTrans((TransitionType)5);
			healthBar._middlegroundTween = val;
		}
	}

	private static void RefreshTextOverlay(NHealthBar healthBar)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		HealthBarForecastUiState healthBarForecastUiState = UiStates[healthBar];
		if (healthBarForecastUiState == null)
		{
			return;
		}
		Creature creature = healthBar._creature;
		if (creature.CurrentHp <= 0 || HpDisplayExtensions.IsInfinite(creature.HpDisplay))
		{
			return;
		}
		Color? val = healthBarForecastUiState.LastRender.LethalRightColor ?? healthBarForecastUiState.LastRender.LethalLeftColor;
		MegaLabel hpLabel = healthBar._hpLabel;
		if (!val.HasValue)
		{
			if (IsDoomLethalAfterRight(healthBar, creature))
			{
				((Control)hpLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), DoomLethalTextColor);
				((Control)hpLabel).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), DoomLethalOutlineColor);
			}
		}
		else
		{
			((Control)hpLabel).AddThemeColorOverride(StringName.op_Implicit("font_color"), val.Value);
			((Control)hpLabel).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), DarkenForOutline(val.Value));
		}
	}

	private static CustomSegment[] GetCustomSegments(Creature creature)
	{
		return (from registered in HealthBarForecastRegistry.GetSegments(creature)
			select new CustomSegment(registered.Segment.Amount, registered.Segment.Color, registered.Segment.Direction, registered.Segment.Order, registered.SequenceOrder, registered.Segment.OverlayMaterial, registered.Segment.OverlaySelfModulate) into segment
			where segment.Amount > 0
			select segment).ToArray();
	}

	private static void HideAllCustomSegments(NHealthBar healthBar)
	{
		HealthBarForecastUiState healthBarForecastUiState = UiStates[healthBar];
		if (healthBarForecastUiState != null)
		{
			HideSegments(healthBarForecastUiState.RightSegments);
			HideSegments(healthBarForecastUiState.LeftSegments);
			healthBarForecastUiState.LastRender = HealthBarForecastRenderResult.Empty;
		}
	}

	private static bool EnsureUiState(NHealthBar healthBar)
	{
		if (UiStates[healthBar] != null)
		{
			return true;
		}
		Control poisonForeground = healthBar._poisonForeground;
		NinePatchRect val = (NinePatchRect)(object)((poisonForeground is NinePatchRect) ? poisonForeground : null);
		if (val == null)
		{
			return false;
		}
		Control doomForeground = healthBar._doomForeground;
		NinePatchRect val2 = (NinePatchRect)(object)((doomForeground is NinePatchRect) ? doomForeground : null);
		if (val2 == null)
		{
			return false;
		}
		Node parent = ((Node)val).GetParent();
		Control val3 = (Control)(object)((parent is Control) ? parent : null);
		if (val3 == null)
		{
			return false;
		}
		Control val4 = CreateContainer("BaseLibForecastRightContainer");
		Control val5 = CreateContainer("BaseLibForecastLeftContainer");
		((Node)val3).AddChild((Node)(object)val4, false, (InternalMode)0);
		((Node)val3).AddChild((Node)(object)val5, false, (InternalMode)0);
		UiStates[healthBar] = new HealthBarForecastUiState(val4, val5, CreateSegmentTemplate(val, "BaseLibForecastRightTemplate"), CreateSegmentTemplate(val2, "BaseLibForecastLeftTemplate"), new List<NinePatchRect>());
		return true;
	}

	private static Control CreateContainer(string name)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		Control val = new Control
		{
			Name = StringName.op_Implicit(name),
			MouseFilter = (MouseFilterEnum)2
		};
		val.SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		return val;
	}

	private static NinePatchRect CreateSegmentTemplate(NinePatchRect template, string name)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		NinePatchRect val = (NinePatchRect)((Node)template).Duplicate(15);
		((Node)val).Name = StringName.op_Implicit(name);
		((CanvasItem)val).Visible = false;
		((CanvasItem)val).SelfModulate = Colors.White;
		((CanvasItem)val).Material = null;
		return val;
	}

	private static void EnsureOverlayOrder(NHealthBar healthBar, HealthBarForecastUiState state)
	{
		Control poisonForeground = healthBar._poisonForeground;
		if (poisonForeground == null)
		{
			return;
		}
		Control hpForeground = healthBar._hpForeground;
		if (hpForeground == null)
		{
			return;
		}
		Control doomForeground = healthBar._doomForeground;
		if (doomForeground != null)
		{
			Node parent = ((Node)poisonForeground).GetParent();
			Control val = (Control)(object)((parent is Control) ? parent : null);
			if (val != null)
			{
				int index = ((Node)poisonForeground).GetIndex(false);
				int index2 = ((Node)hpForeground).GetIndex(false);
				int num = Math.Clamp(index + 1, 0, index2);
				((Node)val).MoveChild((Node)(object)state.RightContainer, num);
				int index3 = ((Node)doomForeground).GetIndex(false);
				int childCount = ((Node)val).GetChildCount(false);
				int num2 = Math.Clamp(index3 + 1, 0, Math.Max(0, childCount - 1));
				((Node)val).MoveChild((Node)(object)state.LeftContainer, num2);
			}
		}
	}

	private static void EnsureSegmentCount(List<NinePatchRect> segments, Control container, int requiredCount, NinePatchRect template)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		while (segments.Count < requiredCount)
		{
			NinePatchRect val = (NinePatchRect)((Node)template).Duplicate(15);
			((Node)val).Name = StringName.op_Implicit($"BaseLibForecastSegment{segments.Count}");
			((CanvasItem)val).Visible = false;
			((Node)container).AddChild((Node)(object)val, false, (InternalMode)0);
			segments.Add(val);
		}
	}

	private static void HideSegments(IEnumerable<NinePatchRect> segments, int startIndex = 0)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		foreach (NinePatchRect segment in segments)
		{
			if (num++ >= startIndex)
			{
				((CanvasItem)segment).Visible = false;
				((CanvasItem)segment).Material = null;
				((CanvasItem)segment).SelfModulate = Colors.White;
			}
		}
	}

	private static void ApplyForecastSegmentAppearance(NinePatchRect node, Color color, Material? overlayMaterial, Color? overlaySelfModulate)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		((CanvasItem)node).Material = overlayMaterial;
		((CanvasItem)node).SelfModulate = overlaySelfModulate ?? color;
	}

	private static float GetMaxFgWidth(NHealthBar healthBar)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		float expectedMaxFgWidth = healthBar._expectedMaxFgWidth;
		if (!(expectedMaxFgWidth > 0f))
		{
			return healthBar._hpForegroundContainer.Size.X;
		}
		return expectedMaxFgWidth;
	}

	private static float GetFgWidth(NHealthBar healthBar, int amount)
	{
		Creature creature = healthBar._creature;
		if (creature.MaxHp <= 0 || amount <= 0)
		{
			return 0f;
		}
		return Math.Max((float)amount / (float)creature.MaxHp * GetMaxFgWidth(healthBar), (creature.CurrentHp > 0) ? 12f : 0f);
	}

	private static int HpFromOffsetRight(NHealthBar healthBar, float offsetRight)
	{
		Creature creature = healthBar._creature;
		if (creature.MaxHp <= 0)
		{
			return 0;
		}
		float maxFgWidth = GetMaxFgWidth(healthBar);
		if (maxFgWidth <= 0f)
		{
			return 0;
		}
		return (int)Math.Round(Math.Clamp(offsetRight + maxFgWidth, 0f, maxFgWidth) / maxFgWidth * (float)creature.MaxHp);
	}

	private static Color DarkenForOutline(Color color)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		return new Color(Math.Clamp(color.R * 0.3f, 0f, 1f), Math.Clamp(color.G * 0.3f, 0f, 1f), Math.Clamp(color.B * 0.3f, 0f, 1f), 1f);
	}

	private static bool IsDoomLethalAfterRight(NHealthBar healthBar, Creature creature)
	{
		int powerAmount = creature.GetPowerAmount<DoomPower>();
		if (powerAmount <= 0)
		{
			return false;
		}
		int num = Math.Clamp(HpFromOffsetRight(healthBar, healthBar._hpForeground.OffsetRight), 0, creature.CurrentHp);
		if (num > 0)
		{
			return powerAmount >= num;
		}
		return false;
	}

	private static Color? ResolveLeftLethalColor(Creature creature, int remainingHp, IReadOnlyList<CustomSegment> leftSegments)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		if (remainingHp <= 0)
		{
			return null;
		}
		List<LethalCandidate> list = new List<LethalCandidate>();
		foreach (CustomSegment leftSegment in leftSegments)
		{
			if (leftSegment.Amount > 0)
			{
				list.Add(new LethalCandidate(leftSegment.Amount, leftSegment.Color, leftSegment.Order, leftSegment.SequenceOrder));
			}
		}
		int powerAmount = creature.GetPowerAmount<DoomPower>();
		if (powerAmount > 0)
		{
			list.Add(new LethalCandidate(powerAmount, DoomLethalTextColor, 0, -2305843009213693952L));
		}
		if (list.Count == 0)
		{
			return null;
		}
		IOrderedEnumerable<LethalCandidate> orderedEnumerable = from candidate in list
			orderby candidate.Order, candidate.SequenceOrder
			select candidate;
		int num = 0;
		foreach (LethalCandidate item in orderedEnumerable)
		{
			num = Math.Min(remainingHp, num + item.Amount);
			if (num >= remainingHp)
			{
				return item.Color;
			}
		}
		return null;
	}
}
