你是只读审查员,范围: G:\omp works\.tmp\spire1-beta-r4-clean\mod\Spire1Code\Run\FormNativeSmokeRunner.cs 和 G:\omp works\.tmp\spire1-beta-r4-clean\mod\Spire1Code\Patches\Sts1EventToggleFilterPatch.cs,并以 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs 的最近修改作为对照.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`,路由 `wb2api via local gateway`. 只准使用当前 harness 的原生子代理设施,不得更换模型,不得启动其它代理运行时(omp/codex exec等),不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\r4-clean-source-review-20261003.md` (只可写这一个文件;不改产品代码/构建/部署/游戏/共享配置,不写 C:).

## 增量落盘 (硬要求)

1. 拿到第一条可用结论后立即追加写入报告文件,然后才做下一步.
2. 每完成一个检查面写一次盘;结论未定时也写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据),`## 进行中`(半成品,需复核),`## 未知`(未覆盖).
4. 不要攒到最后一次性输出;最终回复允许只是摘要并给出报告绝对路径.

## 检查目标

- 只读比较 clean worktree 的两个白名单文件与当前工作树版本和 a6e46e5 基线.
- 检查 turns smoke 的 Divinity null target, status=blocked/partial 语义,主线程 gate,异常与清理边界,以及最后的 `WATCHER_BLASPHEMY` 拼写修复.
- 检查 Sts1EventToggleFilterPatch 的 fallback 重命名是否只解决编译遮蔽,没有引入语义回归.
- 检查 clean worktree 是否仍有 Watcher/AutoAnthony/其它可选 mod 的二进制硬引用或 manifest 硬前置风险. 只报告有证据的发现.
- 不要把静态推理写成实机通过;不构建,不运行游戏.

每项输出含:优先级,绝对路径与准确行号,触发条件,权威契约或宣称,当前控制流,可复现命令,最小修复范围,尚缺的实机证据.最多 8 项,有证据就停,不凑数量.

## 语言

只允许中文,英文,法文,德文,俄文与 ASCII 标点.未知多语言原文只引本地路径与行号.
