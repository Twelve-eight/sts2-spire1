你是Spire1存档保护与资源归属窄返工实现者. 产品可写:
G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\FormsCompatibilityBridge.cs;
G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\FormsMissingModifierSaveGuardPatch.cs (新文件);
G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs (仅已指出错误注释);
G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\eng\powers.json;
G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\zhs\powers.json;
G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\eng\modifiers.json;
G:\omp works\Sts\sts2-spire1\mod\Spire1\localization\zhs\modifiers.json.
唯一报告 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\spire1-worker.md. 不写Forms, 不改Release脚本/csproj/gate-config. 本批有独立监督.
先读 dsv41f-round3\spire1-supervisor.md 的最终P1/P2/P3. 只修:
1. 缺Forms读旧档时引擎会先丢失原始modifier身份. 对ModifierModel.FromSerializable加真实签名前置保护, raw SerializableModifier.Id.Entry精确等于SPIRE1-FORM_STANCE_MODIFIER才检查可用, Forms不存在/签名或运行不可用明确抛错, 不允许变DeprecatedModifier后普通规则. 其它vanilla/mod modifier/deprecated旧内容不受影响. 核对权威本机源码 G:\omp works\Sts\sts2-spire1\.tmp\dllsrc\MegaCrit.Sts2.Core.Models\ModifierModel.cs 和 ..\MegaCrit.Sts2.Core.Saves.Runs\SerializableModifier.cs 及 SaveUtil.cs/RunState.cs, 别猜Harmony参数名/类型. 不引用Forms/Watcher. Spire1自身MainFile会扫描本程序集HarmonyPatch, 无需改MainFile. 明确保证范围: 两mod均缺失时这段保护没有代码可执行, 不能宣称那种环境也被本mod保护.
2. 已选身份命中后Forms.IsSelected返回false时显式抛异常, 不静默回普通规则. 属性读取异常不能以false吞掉确认的形态选择. 修改相关误导注释, PowersGate注释只修程序集身份/签名无关的已知表述, 不改其行为.
3. 从Spire1上述四JSON只移除与新Forms生产文件对应的9power x3 keys与modifier x2 keys. 保留所有其它键/值/相对顺序及普通姿态共享png, 不新增zhs译名. modifiers.json若只剩空对象可保留{}, 优先最小diff. 可用脚本读取既有JSON并统计键/哈希, 不在模型上下文打印未知多语言文本, 不整体重格式化造成无关diff. 不改任何其他assets.
4. 报告列出去除精确键集合, 非Forms键值相等的静态核对, rawID保护完整控制流/异常边界, 缺全模组场景未知. 不构建测试. 不碰另一发布会话staging.
用户唯一模型 global:deepseek-v4.1-flash, 路由 wb2api, reasoning xhigh. 原生Codex, 禁止再委派和其它模型/fallback/codex exec/omp. 不写C:, 不构建lint测试部署, 不运行游戏, 不写Steam/共享mod_configs/canonical/Workshop, 不改其它发布会话脚本, 不执行git操作. 主会话集中构建和实机. 第一可用结论立即写唯一报告, 每面追加, 固定 ## 已确认 / ## 进行中 / ## 未知. 完成写CODE_COMPLETE/修改文件与未验证面, 立即结束实现回合. 任务限为下述小写集, 超过10分钟即落盘精确剩余接口供hub拆分. 不做无关重构.
契约 G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md. 前r3编译两错误已由r4修复, 中央Forms r4真实Release0errors/1warning且Forms.dll/PCK已经生成, 保留这些修法. 两侧PE门禁已通过, 不新增硬引用.
# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-spire1\mod.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\spire1-worker.md` (产品/文档仅可写上方白名单; 报告只写此文件; 不构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 8 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.

---

## 主会话收割方法

```powershell
Get-ChildItem -LiteralPath '<报告目录>' -File | Sort-Object LastWriteTime |
  Select-Object Name,Length,LastWriteTime
```

- 子代理超时/中断/额度耗尽时: 先读报告文件, 把 `已确认` 当作可用证据, `进行中` 当作半成品复核, 不得当作无产出.
- 收到第一批落盘结果后即向用户汇报一次, 后续增量补充.