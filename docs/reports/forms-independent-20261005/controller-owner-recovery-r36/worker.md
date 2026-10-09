# r36 worker 报告

## 已确认

- 已读取 `worker.request.md` 与 `.tooling/subagent-report-protocol.md`。
- r34 原脚本：`G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r34.ps1`
- r34 SHA256：`DC61A5C7F79A190E6132362387597925B2F39F7343CC3CD9F8690FA2243B04A3`，与请求冻结值 `DC61A5C7F79A190E6132362387597925B2F39F7343CC3CD9F8690FA2243B04A3` 一致；字节数 `68221`，行数 `793`，行尾为 LF 且文件末尾无额外换行。
- 已定位并逐字核对 r34 行 628-632（1-based）为旧 before+afterShutdown 共用循环：
```text
  foreach($pair in @(@('ownerCountsBefore',$ownersBefore),@('ownerCountsAfterShutdown',$ownersAfter))){
   if($pair[1]['Forms'] -ne 0){throw ($pair[0]+'.Forms must be exactly 0')}
   if($pair[1]['Forms.FormStanceMode.Watcher'] -ne 0){throw ($pair[0]+'.Forms.FormStanceMode.Watcher must be exactly 0')}
   if($pair[1]['Forms.FormStanceSafety'] -ne 1){throw ($pair[0]+'.Forms.FormStanceSafety must be exactly 1')}
  }
```
- 该循环错误地再次要求 before Forms=0 / Bridge=0 / Safety=1，与行 622 已要求的 before Safety=2 及行 613 的 before old owner 非负契约直接矛盾。
- 新脚本：`G:\omp works\.tmp\forms-independent-20261005\run-isolated-smoke-r36.ps1`
- 新脚本 SHA256：`09115BA296E8C5008BEF4373D3B605DAED1BFAB02DCB3FF7ED67F7AE2BFF3375`，字节数：`67805`，行数：`788`。
- 唯一 delta：按原始字节删除 r34 行 628-632 的精确区间，共删除 `416` 字节；r36 其余字节与 r34 完全一致。
- 独立重建校验：r36 行 1-627 与 r34 行 1-627 逐行逐字相同，r36 行 628-788 与 r34 行 633-793 逐行逐字相同；按原始字节重拼与 r36 文件逐字节一致。
- 初次行数组写出曾把 LF 规范化为 CRLF 并补末尾换行，已被逐字节校验拒绝；随后改为原始字节删除区间重建，最终 r36 才通过上述校验。未保留错误中间产物，未改其它源。
- 保留面：行 613 before old owner 非负、行 622 before Safety=2、afterReinit 两 old owner 等于 before 且 Safety=2、afterShutdown 严格 0/0/2；三组 Play proof 各 1 与 identity 稳定性比较未删除；旧 Remove proof、final 故障与状态字段、5 个旧模式、launch/隔离/日志/共享配置/保留 mod 主体未改。
- 新脚本未被执行；未运行 Parser。

## 进行中

- 无。唯一机械窄修与静态完整性校验已完成。

## 未知

- 未构建、lint、Parser、测试、运行游戏、Git；未验证真实 runtime JSON、真实退出码、真实 Harmony 输出或实机行为。
- 新脚本的运行时语法与执行结果尚未由本 worker 验证；该边界留给 hub/监督按后续授权处理。