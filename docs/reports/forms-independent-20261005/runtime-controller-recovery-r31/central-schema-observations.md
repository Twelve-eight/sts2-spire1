# 中央静态schema疑点, 需r31独立核对, 非最终结论

- r28 Test-RuntimeSafetyEvidence 的 ownerCountsBefore 要求Forms/Watcher均0, 但实际来源是在Shutdown前采样, 这可能错误拒绝全部真报告; after必须0, before应按真实owner数/guard身份契约.
- r28 当前所有mode都会要求3 command probes, 而 runtime-unselected实际只有strikeProbe与tranquilityProbe, 三command分支须仅safety.
- r29 final会保留全部scenario raw而非仅late raw; r28对同一整份JSON调用Test-RuntimeFaults final false并强制raw为空, 可能错误拒绝真实expected command故障. 必须全raw/expected严格关联, 不按全部raw为空冒充late未知判定.
- r28脚本中的Property会管道展开empty typed数组, 新模式应采用Evidence-Property保形状, 包括entryCarrierTypes/entryEffectTypes等.

源码准确来源为 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke\RuntimeSafetySmokeRunner.cs, 当前r29 hash48499CDCBFC2ECD427118BE1EC2B6105CF57F19E42BBE9B1CF63BA09259EAB9C. 不更改此源. 本文件只供静态返工线索, 必须独立确认, 不继承旧错误模型审核.