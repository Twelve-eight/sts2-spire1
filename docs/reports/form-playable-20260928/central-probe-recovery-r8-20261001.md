# 中央探针恢复 - 2026-10-01

## 已确认

- r7 三名代理关闭时原生状态均为 running.关闭前唯一报告均不存在,未通过实现完成或监督门禁.不采信这些代理为已完成.
- 本轮最后一个实现切片已超过十分钟仍无工具或报告产出.按 AGENTS.md Sec 11 的超时接管规则,主会话接管仅探针协作者及最短回归.不改生产实现,不把接管当作 Astra 监督通过.
- 改前真实编译错误已保留在 central-r6-20261001-01/probe-before-build.log.真实 AbstractModel.cs:520-522 确认 public virtual Task AfterCombatEnd(CombatRoom room) 默认返回 Task.CompletedTask.

## 进行中

- ContractStubs.cs 补编译协作者,SerpentScenarios.cs 补两个直接调用生产 hook 的场景.所有命令仅由主会话执行.

## 未知

- 当前源码的探针结果尚待中央运行.真实战斗,Watcher 桥接,存档,多人和视觉仍未验收.此报告不宣称生产代码监督已通过.

- 探针两文件已落盘.仅补真实签名和两个回归,没有复制生产 pending 算法.生产 SerpentFormPower.cs 尚未修改.


### 12:14 中央隔离复现

- 新协作者快照编译: 0 warnings,0 errors,exitCode=0.
- serpent-before.log 的权威输出: TOTAL 27 PASS 26 FAIL 1 SELECTED 27.
- 已复现: serpent.removed_source_and_bridge_duplicates_hit_once_and_clean_up 断言 expected=0,actual=1.伤害命令1次,退出能量1次,但群蛇 completion bridge 仍挂载.
- 源码原因: SerpentFormPower.AfterCardPlayed 在共享 tracker.Remove 返回 false 时直接 return,绕过 finally 清理.旧已移除 listener 先消费唯一 pending 后,新 bridge 回调变成重复事件并不再清理空桥.这属于链接生产 hook 的隔离复现,不是实机战斗复现.
- serpent.removed_source_live_listener_finishes_pending 已 PASS,表明只投递当前 listener 的正常承接路径在此窄协作者下成立.
- 最小返工范围: 仅 SerpentFormPower.AfterCardPlayed,确保重复回调也执行空桥清理,保留消费先于 await,恰好一次伤害与退出能量.不修改 pending 数据结构或 Watcher 桥接.
