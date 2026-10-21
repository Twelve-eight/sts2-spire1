# Summary / 变更摘要

<!-- What changes, and why. Link the issue: "Fixes #123". -->
<!-- 说明改了什么、为什么改；关联 issue。 -->

## Type of change / 变更类型

- [ ] Bug fix / 修复
- [ ] New content (card, relic, potion, event) / 新增内容
- [ ] Faithfulness fix to vanilla StS1 / 还原一代原版
- [ ] Localization / 本地化
- [ ] Multiplayer or mod compatibility / 联机或 mod 兼容
- [ ] Build, tooling or documentation / 构建、工具或文档
- [ ] Release PR (version bump + Workshop payload) / 发布 PR

## Verification / 验证

<!-- Paste what you actually ran. Do not check a box you did not run. -->
<!-- 只勾选真正跑过的项。 -->

- [ ] `dotnet build mod/Spire1.csproj -c Release -p:Sts2Path=... -p:CopyToModsFolderOnBuild=false` — exit 0, no new warnings
- [ ] Release gates: `tools/build-gates/run-release-gates.ps1` — `assemblyref,manifest,typedef` all pass
- [ ] Localization: entries exist in **both** `localization/eng` and `localization/zhs`; STS001 clean
- [ ] In-game smoke test / 实机冒烟 — log attached below

Game version: `0.111.x`  
Spire1 version: `1.2.x`  
BaseLib version: `3.4.5` (source: Workshop / GitHub)

<details>
<summary>Log excerpt / 日志片段</summary>

```
paste %APPDATA%\SlayTheSpire2\logs\godot.log lines here
```

</details>

## Checklist / 检查清单

- [ ] One logical change per PR; no unrelated refactors or formatting churn
- [ ] No compile-time `Reference` to optional mods (AutoAnthony, AFTP, RitsuLib, ...); reflection/string bridges only
- [ ] No `Spire1Code/Forms/**` or form-native smoke types added back to the assembly
- [ ] Content IDs unchanged for shipped classes (old-save compatibility), or the save impact is explained
- [ ] No `[R]`/`[G]`/`[B]`/`[W]`, `#r`/`#y`, or literal ` NL ` in localization
- [ ] Manifest version untouched (unless this is a release PR)
- [ ] `workshop/workshop_upload.vdf` untouched (unless this is a release PR)
- [ ] Docs updated in the same PR when behaviour or contracts changed
- [ ] AI assistance disclosed below (project policy) / 已披露 AI 辅助

## Notes for reviewers / 审查提示

<!-- Root cause, engine evidence, known gaps, or what you could not verify. -->
<!-- 根因、引擎证据、已知缺口、未能验证的部分。 -->
