using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using Spire1.Spire1Code.Character;

namespace Spire1.Spire1Code.Patches;

/// <summary>
/// 归档角色的内容从卡牌总览（Card Library）隐藏。
/// <para>
/// 背景：观者已归档（用户裁定硬隐藏），但模型仍注册在 ModelDb——这是刻意的：
/// 注销会改变 ModelID 序列化映射并破坏引用观者牌的旧存档。副作用是总览里能翻到
/// 借用储君占位美术的观者牌。引擎在 NCardLibraryGrid._Ready 用
/// <c>CardModel.ShouldShowInCardLibrary</code> 过滤入册卡（dllsrc CardModel.cs:826-829，
/// 构造参数注入的自动属性），本补丁在该 getter 上按"归属归档池"拦截。
/// </para>
/// <para>
/// 通用机制：新增归档角色时把其 CardPool 类型加进 <see cref="CharacterArchive.ArchivedPools"/>
/// 即可全量生效，无需逐卡标记。
/// </para>
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_ShouldShowInCardLibrary")]
internal static class ArchivedCharacterGatePatch
{
    [HarmonyPrefix]
    private static bool HideArchived(CardModel __instance, ref bool __result)
    {
        if (!CharacterArchive.ArchivedModelTypes.Contains(__instance.GetType()))
        {
            return true; // 非归档内容走原逻辑
        }

        __result = false;
        return false;
    }
}

/// <summary>归档角色登记处与其模型类型的启动期扫描。</summary>
internal static class CharacterArchive
{
    /// <summary>
    /// 归档角色的卡池类型。追加条目即让该角色全部卡牌退出总览；
    /// 角色本体需同时保持 <c>HideFromVanillaCharacterSelect => true</c>。
    /// </summary>
    private static readonly HashSet<Type> ArchivedPools = new() { typeof(WatcherCardPool) };

    /// <summary>归属于任何归档池的本程序集模型类型（启动期扫描一次）。</summary>
    public static readonly HashSet<Type> ArchivedModelTypes = Scan();

    private static HashSet<Type> Scan()
    {
        var result = new HashSet<Type>();

        // 用 CustomAttributeData 反射读 [Pool(...)] 的构造实参——不实例化特性、
        // 不依赖 BaseLib 特性类的公开成员形态（单 Type 或 Type[] 两种 ctor 都覆盖）。
        foreach (Type type in typeof(MainFile).Assembly.GetTypes())
        {
            foreach (CustomAttributeData cad in type.GetCustomAttributesData())
            {
                if (cad.AttributeType.Name != "PoolAttribute")
                {
                    continue;
                }

                foreach (CustomAttributeNamedArgument? _ in cad.NamedArguments) { /* none expected */ }

                bool matches = false;
                foreach (CustomAttributeTypedArgument arg in cad.ConstructorArguments)
                {
                    if (arg.Value is Type single && ArchivedPools.Contains(single))
                    {
                        matches = true;
                        break;
                    }
                    if (arg.Value is System.Collections.IEnumerable coll
                        && arg.ArgumentType == typeof(Type[]))
                    {
                        foreach (object item in coll)
                        {
                            if (item is CustomAttributeTypedArgument ta
                                && ta.Value is Type pt
                                && ArchivedPools.Contains(pt))
                            {
                                matches = true;
                                break;
                            }
                        }
                    }
                }

                if (matches)
                {
                    result.Add(type);
                    break;
                }
            }
        }

        MainFile.Logger.Info(
            $"[Spire1] character archive: {result.Count} model type(s) hidden from card library " +
            $"(pools: {string.Join(",", ArchivedPools.Select(p => p.Name))})");
        return result;
    }
}
