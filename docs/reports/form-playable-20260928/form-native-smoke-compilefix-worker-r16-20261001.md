# r16 compilefix 增量报告

## 已确认

- 中央 Release 构建日志 `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-build-r15-20261001.log` 指向 `FormNativeSmokeRunner.cs:1353` 的空参数 lambda 语法错误，并伴随 `CS1525`、`CS1026`、`CS1002`、`CS1513` 连锁诊断。
- 当前源码 1352-1357 行确实为 `()` 后直接换行到 `{`，缺少 C# lambda 箭头；目标方法签名为 `InvokeOnMainThreadWithTimeoutAsync<T>(Func<T> operation, string description, List<DetachedOperation> detachedOperations)`。
- 因此该位置的最小修复目标是将 `()` 改为 `() =>`，保持现有退出逻辑和调用参数不变。

## 进行中

- 尚未修改产品文件；下一步仅对上述单一语法位置做最小修复，并静态扫描同文件是否存在同类空参数 lambda 缺少 `=>` 的明显模式。

## 未知

- 本轮尚未构建、测试、部署或启动游戏；中央构建修复后的结果和真实战斗行为均未验证。

## 已确认（完成）

- 仅修改白名单产品文件 `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs`。
- 将 1353 行从 `()` 最小改为 `() =>`；`game.GetTree().Quit(exitCode);`、`return true;`、描述字符串和 `detachedOperations` 参数均未改动。
- 对同文件执行了空参数后直接花括号的静态模式扫描，命中两处均为合法的 `new()` 对象初始化器（约第 40 行）和无参数方法声明（约第 1048 行），不是缺失 lambda 箭头；除目标位置外未发现同类明显语法错误。
- `git diff --check` 对该产品文件无输出，未发现空白错误。
- 中央构建日志仅作为本轮修复依据；本轮没有重新执行中央构建。

## 进行中

- 无。按请求已完成最小语法修复和静态检查。

## 未知

- 未重新构建，因此 `CS1525`、`CS1026`、`CS1002`、`CS1513` 是否在中央 Release 构建中消失仍未由本轮验证。
- 未运行测试、未部署、未启动游戏；真实战斗 smoke、六形态状态、视觉加载和退出时序均未验证。
- 未修改共享 `mod_configs`、Steam 安装或其它产品文件。
