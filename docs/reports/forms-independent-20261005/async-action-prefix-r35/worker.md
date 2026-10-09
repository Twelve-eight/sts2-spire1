# r35 P1 返工实现

## 已确认

- 2026-10-05（Asia/Shanghai）：已读取唯一请求 `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\async-action-prefix-r35\worker.request.md`、协议模板与 r33 监督报告。
- 唯一代码文件 `G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs` 编辑前 SHA256 = `B722AD5770D8D4DA02EB51EC5D0DA395157A728AD38873B71DA2A330B41AFAB7`，与请求中 r33 原 hash 一致；编辑前无 BOM。
- 编辑后 SHA256 = `5088D2E6E906CF7B22EF0F95B42A60EFBC09170E871E419A89801E391986C929`；编辑后仍无 BOM。
- 只改 `PlayPrefix` 的 Task 边界，`RemovePrefix`、owner/HarmonyId、两个 target 解析、原子安装/rollback、`IsInstalled`/`ProofHoldsLocked` 均未改。
- `PlayPrefix` 新签名：`private static bool PlayPrefix(PlayCardAction __instance, ref Task __result)`。
- 正常路径仍调用原 `FormStanceMode.ThrowIfSelectedFormsActionUnavailable(__instance.Player.Creature.CombatState)`，未命中时 `return true`，原 Bound/未选/nonlive 行为不变。
- 不可用或该检测发生异常时，`catch (Exception exception)` 将同一异常对象交给 `Task.FromException(exception)`，赋给 `ref Task __result`，并 `return false` 跳过原方法体；不再同步 throw，异常未被包装、未被吞掉、未被批准或忽略。
- 该实现使真实 `GameAction.Execute()` 继续走 `TaskHelper.RunSafely(ExecuteAction())` 返回的 faulted Task，从而让原 await/try/finally/Finished/AfterFinished/PopAction 路径有机会按引擎语义结算；不产生 UI/支付/历史/marker 副作用，不返回 Cancelled/Pending。
- `using System.Threading.Tasks;` 已加入，`Task` 类型精确对应 `PlayCardAction.ExecuteAction()` 返回类型。

### 准确 diff（由编辑后文件重建原文后对比）

```diff
--- a/G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs
+++ b/G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceSafetyGuard.cs
@@
 using System;
 using System.Reflection;
+using System.Threading.Tasks;
 using HarmonyLib;
@@ -182,8 +183,20 @@
     /// native behavior. Only a selected Forms run in the current live combat whose bridge is
-    /// unavailable throws the same explicit restart exception; it is never canceled or treated
-    /// as pending.
+    /// unavailable faults the returned action Task with the same explicit restart exception;
+    /// it is never canceled or treated as pending.
     /// </summary>
-    private static bool PlayPrefix(PlayCardAction __instance)
-    {
-        FormStanceMode.ThrowIfSelectedFormsActionUnavailable(__instance.Player.Creature.CombatState);
-        return true;
-    }
+    private static bool PlayPrefix(PlayCardAction __instance, ref Task __result)
+    {
+        try
+        {
+            FormStanceMode.ThrowIfSelectedFormsActionUnavailable(__instance.Player.Creature.CombatState);
+        }
+        catch (Exception exception)
+        {
+            // Convert the same explicit rejection into a faulted action Task instead of a
+            // synchronous prefix throw; GameAction.Execute can then settle Finished and
+            // AfterFinished. This is not approval or suppression: the same exception object
+            // remains the action fault and is neither wrapped nor swallowed.
+            __result = Task.FromException(exception);
+            return false;
+        }
+        return true;
+    }
```

- 编辑后 `PlayPrefix` 位于第 186-202 行；新 using 位于第 3 行。

## 进行中

- 无。机械实现已完成，状态 `CODE_COMPLETE`。

## 未知

- 未构建、未 lint、未 Parser、未测试、未实机、未 Git；按请求验证由 hub 集中执行。
- 尚未由 hub 完成本批真实 wait 后通知 r33 同批监督审查。