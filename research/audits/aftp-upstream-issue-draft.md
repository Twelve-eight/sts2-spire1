# Draft upstream issue — Cany0udance/ActsFromThePast

> 供用户审阅后以本人账号提交（gh keyring token 失效待重配，且按惯例由人工发送）。
> 语言卫生：对外 issue 用英文。

**Title:** Rest-site rooms can black-screen on entry: custom `*_rest_site.tscn` backgrounds are a hard dependency of `NRestSiteRoom._Ready`

## Body

**Summary**
Entering a rest site in any AFTP act can hard-lock the room on a black screen. The engine's
`NRestSiteRoom._Ready` does:

```csharp
Control control = _runState.Act.CreateRestSiteBackground();        // ActModel.cs L251-253
BgContainer.AddChildSafely(control);
_restSiteLighting = control.GetNode<Control>("%RestSiteLighting"); // throws if missing (L321-324)
```

`CreateRestSiteBackground()` instantiates `Act.RestSiteBackgroundPath`, and all three AFTP acts
override it with custom scenes (`overgrowth_rest_site.tscn`, `hive_rest_site.tscn`,
`glory_rest_site.tscn`). If that scene fails to load/instantiate (an asset-preload race in
multiplayer being the likely trigger), or lacks the scene-unique `%RestSiteLighting` node, the
exception aborts `_Ready` and the room UI never initializes: the player sees a permanent black
screen on entering the campfire.

**Observed behavior** (multiplayer, public-beta 0.111.0, lobby of 3):
- Selecting a rest-site node → black screen, game soft-locks.
- Force-quitting and reloading skips the *enter* transition (the run resumes inside the campfire),
  which is why the bug looks intermittent.
- If the previous checkpoint was an event, reload replays the event and re-selecting the campfire
  re-locks — effectively ending the run.

**Suggested fixes** (any one suffices):
1. In `NRestSiteRoom._Ready`, replace `GetNode` with `GetNodeOrNull` plus a fallback lighting node.
2. Wrap `CreateRestSiteBackground()` in try/catch and substitute a plain dark background when
   instantiation fails, logging the exception.
3. Audit the three custom tscn files to guarantee `%RestSiteLighting` exists.

We verified the failure contract locally by reproducing the same crash class against a scene
missing `%RestSiteLighting`. Happy to share logs and divergence bundles.

— 附注（中文，不随 issue 发出）：我方 mod 已上线通用救援层（Finalizer 兜底背景 + Postfix 注入灯光节点），
上游不修我们也不再被卡死；此 issue 是推动正解落地。
