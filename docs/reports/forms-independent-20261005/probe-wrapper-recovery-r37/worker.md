# r37 worker report

## 已确认

- 2026-10-05: 请求文件已读取. 唯一代码路径 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs, 唯一报告路径 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\probe-wrapper-recovery-r37\worker.md.
- 源文件修改前 SHA256 实测为 45C4E599F66AFDB3044D8BBFCBB52B5EDE4615E7990E5EB109BA073AF8FEB946, 与请求一致. 修改后 SHA256 为 C638B984846E91B269018332CCFEB60A4F69C309C3BAA48DDA5CE0D6468A9F92. 文件长度 97976 -> 97897 bytes, 行数 1583 -> 1580, LF 1583 -> 1580, CRLF 7 -> 7, 无 BOM.
- 已定位并删除三处覆盖, 原行号为 397, 411, 424:
  - result["removeProbe"] = removeProbe.ToJson();
  - result["damageProbe"] = damageProbe.ToJson();
  - result["powerAmountProbe"] = amountProbe.ToJson();
- 三处之前均先把 RuntimeSafetyProbeJson 的结果存入 result; RuntimeSafetyFinalizeProbe 的实参正是该 result 字典. 删除后 result 中的原始 wrapper Dictionary 不再被 ToJson 替换.
- RuntimeSafetyProbeEvidence 局部变量 removeProbe, damageProbe, amountProbe 保留, 对应 if (!*.Passed) 检查保留.
- 精确 delta 为三行删除, 无其它代码行改动. 修改后全文检索 result["removeProbe"], result["damageProbe"], result["powerAmountProbe"] 的 ToJson 覆盖结果为 NO_TOJSON_OVERWRITES.
- 修改后行号: removeProbe 检查在 396-398; damageProbe 检查在 409-411; amountProbe 检查在 421-423. 修改后文件头仍为 75 73 69 6E 67 20 53 79 ("using Sy"), 无 BOM.

## 进行中

- 无. 三处最小删除已完成.

## 未知

- 未构建, 未 lint, 未 parser, 未测试, 未运行游戏, 未触碰 Git. 按请求这些验证全部跳过.
- 未发现其它需要恢复的 wrapper 字段; 未扩大范围.
