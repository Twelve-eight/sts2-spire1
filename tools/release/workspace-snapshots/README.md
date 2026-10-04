# 工作区发布脚本精确备份

工作区根 .tooling 不是 Git 仓. 本目录保存本轮有修改的 G:\omp works\.tooling\refresh-workshop-payloads.ps1 的精确字节, 以便 GitHub 备份与恢复. 不直接执行本快照: 正式受控入口仍是 G:\omp works\.tooling\workshop-push-all.ps1, Spire1 的单项 wrapper 转发到它.

恢复前读取 G:\omp works\START-HERE.md 与 G:\omp works\docs\WORKSPACE-PROJECTS.md, 核对当前契约和现行入口, 比较 SHA256. 若根脚本已有后续改动, 不盲目覆盖. 快照不能代替真实全量 VerifyOnly/GuardsOnly.

本轮快照 SHA256: 35E412AAD8C84CF465A3D5E17E0B2F53D6ADEFEBBDBAE4F782968F30B402A1BD. 旧版复现与修复后负例在 G:\omp works\.tmp\workshop-prep-20261004-central; 摘要和监督报告在 G:\omp works\Sts\sts2-spire1\docs\reports\workshop-prep-20261004.

本子目录 .gitattributes 对 *.ps1 设置 -text, Git提交/恢复不得正规化这些精确脚本字节. 验收同时检查工作树与Git blob的SHA256等于正式脚本.
