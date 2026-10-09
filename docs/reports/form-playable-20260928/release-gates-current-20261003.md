
# Spire1 Release 依赖门禁 - 2026-10-03

目标 DLL:

`G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll`

原始机器结果:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\central-release-gates-current-20261003.json`

- `assemblyref-forbidden`: PASS. AssemblyRef 15 个,未发现 `AutoAnthony`、`AutoAnthonyWatcher`、`Watcher`、`DirectConnectIP`、`ActsFromThePast`。
- `manifest-consistency`: PASS. 发布 DLL 的 mod AssemblyRef 只有 `BaseLib`; `Spire1.json` 的依赖声明与之相同。
- `typedef-forbidden`: PASS. TypeDef 964 个,未发现 Forms、Experimental、held-back 或 Debug 禁止类型。

结论: 最新 Release 产物没有把 Watcher、AutoAnthony、AutoAnthonyWatcher 或其它可选 mod 写成 AssemblyRef 硬前置;运行时可选桥接保持反射路径。
