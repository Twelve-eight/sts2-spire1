# forms-independent 20261005 test-docs-hotfix-r9 worker

状态: CODE_COMPLETE

## 已确认

1. [P0] 测试 API 类型错误已窄修到 staging, 原测试保持只读.
   - 原文件: `G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\MainFile.cs:120`
   - 触发条件: 编译 `FormsSaveGuardSmoke` 工程.
   - 宣称与权威契约: `Harmony.GetPatchInfo(MethodBase)` 返回 `HarmonyLib.Patches`, 不是 `HarmonyLib.PatchInfo`.
   - 当前控制流: 原行 `HarmonyLib.PatchInfo? info = ready == null ? null : Harmony.GetPatchInfo(ready);`, 随后读取 `info.Postfixes`; 编译期即失败.
   - 权威签名证据: `G:\omp works\Sts\sts2-spire1\.nuget\packages\lib.harmony\2.4.2\lib\net9.0\0Harmony.dll` (SHA256 `A849B726E1F9248D71AABBED8114DEAF79BEB7ACC25E8344FF92A27AD8AC87AB`) 反射实测返回 `HarmonyLib.Patches`.
   - 最小修复范围: 仅该变量声明改 `var` 或实际返回类型, 不动 owner 校验与业务.
   - 原 SHA256 (写入前): `5E095A7978DA85FE14ACF9093F638FB2329D9FA60232417D949066D99EBB5438` (5882 bytes).
   - 原 SHA256 (写入后重核): `5E095A7978DA85FE14ACF9093F638FB2329D9FA60232417D949066D99EBB5438` (未漂移).
   - staging: `G:\omp works\.tmp\forms-independent-20261005\r9-hotfix-staging\MainFile.cs`
   - staging SHA256: `1A7B38DC7950D2A4D8C5ABCAEC3E17F38FDDBA6373DCCB0B240EC347D4D06D9A` (5864 bytes).
   - 一行 diff: `-            HarmonyLib.PatchInfo? info = ready == null ? null : Harmony.GetPatchInfo(ready);` / `+            var info = ready == null ? null : Harmony.GetPatchInfo(ready);`
   - 字节级替换偏移: 4311; 仅替换 21 字节为 8 字节, 其余字节顺序与行尾 (LF) / 无 BOM 保持; 全文件仅此 1 行差异.
   - 可复现命令: `Compare-Object (Get-Content -LiteralPath 'G:\omp works\Sts\sts2-forms\tests\FormsSaveGuardSmoke\MainFile.cs') (Get-Content -LiteralPath 'G:\omp works\.tmp\forms-independent-20261005\r9-hotfix-staging\MainFile.cs')`
   - 尚缺实机证据: 未构建, 未测试, 未部署, 未运行游戏 (按约束); 集成后由 hub 编译与实机验证.

2. [P1] README 已创建, 覆盖独立 mod 安装与验证边界.
   - 路径: `G:\omp works\Sts\sts2-forms\README.md` (SHA256 `F792327B6D3FA376B78B339717183CBF053615E99B7E77DDEC7325F4C8FD2CE0`, 2412 bytes, UTF-8 无 BOM, LF).
   - 已写明: 独立 Forms; 运行组合 BaseLib + 兼容 Watcher + Forms, 无 Watcher 禁入口; 不是依赖 Spire1 的插件; 可与配套拆分版 Spire1 同挂, 但含 Forms 类型的旧 Spire1 存在同 10 个 ID 重复定义风险, 未写"已兼容"; canonical/Workshop 旧包未覆盖, 同用 Spire1 必须拿本轮验证的拆分版配套; 不分发 test carrier, 玩家包仅 `Forms.dll` / `Forms.pck` / `Forms.json`, 无需共享 config 开关; 入口是自定义 run modifier, 不默认所有普通局开启; 中文名沿用现有文案; 旧 CustomID 保留不等于旧档/多人通过; 重复初始化/晚加载幂等与真替换 fail-closed + 重启分层; UI/长战斗/跨进程读档/多人/性能未知; 验证数字以 `DEVLOG.md` 最新中央段与证据 JSON 为准.
   - 尚缺实机证据: README 是文档, 不构成运行验证; 未构建, 未运行游戏.

3. [P1] 项目级短技能已创建, 仅 `SKILL.md`.
   - 路径: `G:\omp works\Sts\sts2-forms\.agents\skills\verify-sts2-forms\SKILL.md` (SHA256 `40C803A3CDF42C7AA33D921D88C0277B8C05B88CE4DB441A4E26DAA06F29EF86`, 2617 bytes, UTF-8 无 BOM, LF, frontmatter 仅 `name` / `description`).
   - 已记录本轮真实可复用方法: 隔离 Output/obj 与缓存全 G; 显式禁 auto-copy; PE `AssemblyRef` + `TypeDef` 与 10 个 ID 精确集合而非字符串包含; PCK 表与 MD5/本地化源字节归属; Godot 主线程 pump/代数关闭/真替换需重启; `GetPatchInfo` 返回 `Patches` 非 `PatchInfo`; 生产包不编测试; 新字节实机绑定; 验证脚本显式 `exit 0` 避免旧 `LASTEXITCODE`.
   - 权限边界: 明确不授权未来自动启动可见游戏, 写 Steam, 改共享 `mod_configs`, 自动换模型/路由或启用未指定计费路由; 并区分源码/编译/隔离原生/未验证.
   - 引用: 当前 `DEVELOP.md`, `DEVLOG.md`, 两个 test 工程与中央证据路径.
   - 未新建 UI 元数据/脚本/占位 (技能目录仅 `SKILL.md` 一个文件).
   - 尚缺实机证据: skill 是文档, 不构成运行验证.

4. [P1] 本轮白名单遵守与范围核对.
   - 仅写 4 个允许路径: staging `MainFile.cs`, `README.md`, `SKILL.md`, 唯一报告 `worker.md`; 未改任何实际测试/生产源码, 共享 `DEVLOG`, 发布脚本, 未构建/lint/测试/部署/运行游戏/执行 git/写 C:/ 再委派.
   - 未触碰原测试文件 (哈希两度一致), 未触碰 canonical Release, Workshop, Steam 副本或共享 `mod_configs`.

## 进行中

- 无; 本轮实现范围已收敛.

## 未知

- 未编译, 未测试, 未部署, 未运行游戏 (按任务约束).
- 集成 staging 后的 `FormsSaveGuardSmoke` 编译结果与实机 JSON/退出码未知.
- UI 视觉, 长战斗, 跨进程读档, 多人, 性能, 真正热替换/热卸载均未验证.
- 原文件在 hub 集成时是否仍保持上述 SHA256 需由 hub 复核.

## CODE_COMPLETE

- 唯一报告: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\test-docs-hotfix-r9\worker.md`
- 交付文件与 SHA256:
  - `G:\omp works\.tmp\forms-independent-20261005\r9-hotfix-staging\MainFile.cs` = `1A7B38DC7950D2A4D8C5ABCAEC3E17F38FDDBA6373DCCB0B240EC347D4D06D9A`
  - `G:\omp works\Sts\sts2-forms\README.md` = `F792327B6D3FA376B78B339717183CBF053615E99B7E77DDEC7325F4C8FD2CE0`
  - `G:\omp works\Sts\sts2-forms\.agents\skills\verify-sts2-forms\SKILL.md` = `40C803A3CDF42C7AA33D921D88C0277B8C05B88CE4DB441A4E26DAA06F29EF86`