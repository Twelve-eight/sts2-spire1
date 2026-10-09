# 子代理任务请求

你是只读审查员,范围: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Cards\Spire1Card.cs`、`Config\Spire1Config.cs`、`Run\FormNativeSmokeRunner.cs`,以及权威反编译 `G:\omp works\Sts\sts2-spire1\.tmp\dllsrc` 与 `research\_decomp`. 用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api via gateway`, 推理 `xhigh`. 只准使用当前 harness 的原生子代理设施,不得更换模型,不得启动其它代理运行时,不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\canonical-listener-contract-scout-20261003.md` (只可写这一个文件;不改产品代码/构建/部署/游戏/共享配置,不写 C:).

## 增量落盘

1. 拿到第一条可用结论后立即追加写入报告文件,然后才做下一步.
2. 每完成一个检查面写一次盘;结论未定时也写入已排除的可能性与证据.
3. 报告固定三段: `## 已确认`、`## 进行中`、`## 未知`.

## 任务

审查 `Spire1DeckGrantGuard` 将 canonical `Spire1.Spire1Code.Cards.Strike` 返回给 `ModHelper.SubscribeForRunStateHooks` 的契约. 查明 canonical listener 是否会被 `HookPlayerChoiceContext.GetOwner` 拒绝,以及是否存在不使用 canonical model 仍能阻止 `CardPileCmd.Add(..., PileType.Deck)` 的最小边界方案. 对照引擎反编译的订阅、IterateHookListeners、Hook.ShouldAddToDeck、Hook.AfterDeath 和 HookPlayerChoiceContext.GetOwner. 输出 P0/P1/P2 风险与最小修复建议,不修改源码、不构建、不运行游戏.

## 输出每项包含

优先级,绝对路径与准确行号,触发条件,权威契约,当前控制流,可复现命令,最小修复范围,尚缺的实机证据. 有证据就停,不凑数量,不把源码推理称为实机复现.

## 语言

只允许中文,英文,法文,德文,俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.
