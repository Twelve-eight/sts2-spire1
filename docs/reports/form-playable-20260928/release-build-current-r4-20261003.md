# 当前源码驱动 Release 重建与门禁 - 2026-10-03

## 结论

当前工作树修正缺少的 `Spire1.Spire1Code.Run` 命名空间导入后，源码驱动 Release 重建成功。构建输出、PCK 资产结构、绑定到 payload 的 AssemblyRef/manifest/TypeDef 门禁全部通过；这份字节随后通过真实隔离三形态烟测和当前字节交叉挂载矩阵。

## 修正

- 文件：`mod/Spire1Code/Patches/Spire1LargeCapsuleGatePatch.cs`
- 修正：补入 `using Spire1.Spire1Code.Run;`，使该文件引用的 `Spire1CardsGateSnapshot` 与实际定义一致。
- 首次重建在 2026-10-03 09:22 左右以 4 个 `CS0103` 失败；失败原因只涉及该缺失导入，未部署到游戏。

## 构建证据

- 构建脚本：`G:\omp works\Sts\sts2-spire1\tools\release\Build-Spire1Release.ps1`
- 输出目录：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\release-r4-current-20261003\`
- 配置：`Release`
- 部署参数：`CopyToModsFolderOnBuild=false`
- 编译结果：0 errors / 60 warnings
- DLL：788480 bytes，SHA256 `4F49BA0D2BE134C6EFD149E96E4B94387E0AA873DFFE40AA7368FC41EBBD88C2`
- PCK：19669354 bytes，SHA256 `70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79`
- manifest：548 bytes，SHA256 `CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305`
- PCK verifier：`PCK_VERIFY_PASS`，1464 entries，744 source files
- 结构门禁：AssemblyRef 15，TypeDef 978；forbidden AssemblyRef、manifest consistency、forbidden TypeDef 全部 PASS
- 绑定 payload 的门禁：`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\release-r4-current-20261003\evidence\release-gates-bound-r1.json`

## 边界

这份报告只证明源码可编译、发布资产结构正确、二进制依赖边界正确；可见 UI、视觉动画、长战斗、存档、重连、多人同步和完整平衡仍需单独验收。
