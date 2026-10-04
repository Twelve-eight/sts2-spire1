# Release gates current r15 2026-10-04

## 已确认

- [P0] 中央 Release 构建脚本: G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1.
- [P0] 输出目录: G:\omp works\.tmp\spire1-release-r15-20261004-central.
- [P0] Release build: 0 errors, 64 warnings, exit 0. 构建命令由 Build-Spire1Release.ps1 内部执行,不写 Steam 安装.
- [P0] PCK verify: PCK_VERIFY_PASS, entries=1464, sourceFiles=744, sha256=70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79.
- [P0] 最终 payload 文件:
  - Spire1.dll length=900608, sha256=8C7CA3DB1AE21FACB4A982287535ED89E25EBB3C369F5C346C68373FC4962F06.
  - Spire1.pck length=19669354, sha256=70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79.
  - Spire1.json length=548, sha256=CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305.
- [P0] 真实 PASS 门禁: G:\omp works\.tmp\spire1-release-r15-20261004-central\evidence\release-gates.json, exit=0. AssemblyRef, manifest consistency, TypeDef 全部 PASS. JSON 中 dllSha256=8C7CA3DB1AE21FACB4A982287535ED89E25EBB3C369F5C346C68373FC4962F06, dllLength=900608.
- [P0] 真实 FAIL 门禁: G:\omp works\.tmp\release-gates-central-r15-20261004\fail-r1.json, 使用隔离临时 gate-config 将 Spire1.Spire1Code.MainFile 加入 forbidden TypeDef, exit=2, passed=false, 身份字段与 DLL 实际 hash 和 length 一致.
- [P0] 缺失 DLL 错误路径修复: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 L35 使用 Console.Error.WriteLine, L36 可达且中央实测 exit=3. JSON sentinel 在该路径保持不变.
- [P0] 脚本语言和字节门禁: check-agent-text.mjs exit=0, PS 5.1 Parser parseErrors=0, no BOM, CRLF=96, LF-only=0, C0=0, U+3002=0.

## 进行中

- [P0] Workshop Promote 已执行成功. 脚本为 `G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1 -RepoRoot G:\omp works\Sts\sts2-spire1 -OutputRoot G:\omp works\.tmp\spire1-release-r15-promote-20261004 -Configuration Release -SkipBuild -Promote`.
- [P0] Workshop 目录 `G:\omp works\Sts\sts2-spire1\workshop\content\Spire1` 现在只含 `Spire1.dll`,`Spire1.json`,`Spire1.pck`; 三个文件 hash 与 promote payload 一致.

## 未知

- [P2] 未验证可见 UI 和视觉资源.
- [P2] 未验证完整长战斗,战中存档重载,重连,多人同步,性能和完整平衡.
- [P2] AutoAnthony 自身仍输出 Expected 65 complete v111 Colorless cards, found 76. 这是第三方资源版本问题,不是 Spire1 门禁失败.

## 可复现命令

```powershell
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1 -Dll G:\omp works\Sts\sts2-spire1\mod\.godot\mono\temp\bin\Release\Spire1.dll -Gates assemblyref,manifest,typedef -Json G:\omp works\.tmp\release-gates-central-r15-20261004\pass.json -SkipBuild
```

结论: 当前源码驱动 Release payload 在结构门禁,身份绑定,隔离真实运行和 Workshop 精简 promote 方面通过,等待版本控制收口.