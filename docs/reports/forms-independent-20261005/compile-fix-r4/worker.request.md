你是窄编译返工实现者. 唯一产品可写文件 G:\omp works\Sts\sts2-forms\mod\FormsCode\MainFile.cs 和 G:\omp works\Sts\sts2-forms\mod\FormsCode\FormStanceWatcherBridge.cs. 唯一报告 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\compile-fix-r4\worker.md.
主会话真实Release诊断构建exit1, 日志 G:\omp works\.tmp\forms-independent-20261005\forms-build-r3.log, 2个确定编译错误: FormStanceWatcherBridge.cs:75 CS0246 Node缺using或限定; MainFile.cs:25 CS0133 ResPath常量表达式中使用了非const ModId.
仅最小修复这两处以及同一Godot命名导入引起的类型限定, 不重构生命周期/六效果, 不改变公开interop签名. 优先明确Godot命名限定以避免Logger重歧义, ModId与ResPath必须仍固定Forms资源身份. 静态扫同文件的未限定Godot符号, 避免只修首处Node后留下同一类编译错误. 不构建. 预计小改, 直接写限定文件, 立即写报告后最终交付.
本批配有监督, 由hub实际wait completed后激活. 原round3监督将对冻结快照审查, 不认可新文件直到本批监督通过.
仅global:deepseek-v4.1-flash/wb2api/xhigh, 原生Codex, 不再委派, 不启动codex exec或omp, 不换模型或fallback. 不写C:, 不构建lint测试部署, 不运行游戏, 不改Steam/共享配置/canonical/Workshop. 报告按 ## 已确认 / ## 进行中 / ## 未知, 第一结论立即写, 每面追加. 最终 CODE_COMPLETE 或监督 SUPERVISION_PASS/NEEDS_REWORK, 明确未构建与未实机. 不执行git操作.
# 子代理派发与增量汇报模板 (AGENTS.md Sec 8b, 2026-09-22)

派发前: 逐字复制下面模板, 只替换 `<...>` 占位符, 并把内容写成本地请求文件后交给子代理
(不要直接把大段中文多语言内容贴进模型请求; 引本地路径).

---

你是 实现者, 范围: G:\omp works\Sts\sts2-forms\mod\FormsCode.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\compile-fix-r4\worker.md` (产品仅可写上方2个白名单文件; 报告只写此文件; 不构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 4 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

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