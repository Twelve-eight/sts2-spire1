# Form effects probe r12 中央运行报告

- 日期: 2026-10-04 Asia/Shanghai
- 项目: G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj
- 生产 Forms 来源: G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms

## 构建

- 命令: dotnet build G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj -c Release -m:1 -nr:false -p:UseSharedCompilation=false --nologo
- 结果: 0 errors, 3 warnings, DLL 生成成功.

## 运行

- 命令: dotnet run --project G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj -c Release --no-build
- 结果: TOTAL 133 PASS 133 FAIL 0 SELECTED 133.
- 覆盖边界: 生产 form hooks 已链接, 但探针只使用 command spies 和窄 power lifecycle collaborators; 不包含 game bridge, UI, multiplayer, save 或 full scheduler.

## 日志

- 构建日志: G:\omp works\.tmp\form-effects-probe-r12-20261004.log
- 运行日志: G:\omp works\.tmp\form-effects-probe-r12-20261004-run.log

## 未验证边界

- 探针通过不替代当前 Release 隔离游戏 smoke, 也不证明真实 Harmony 动态目标在完整游戏运行时命中.
