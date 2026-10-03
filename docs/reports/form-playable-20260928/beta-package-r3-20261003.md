# Spire1 Forms Beta r3 发布包复核 - 2026-10-03

## 包身份

- 包：`G:\omp works\Sts\sts2-spire1\dist\Spire1-Forms-Beta-20261003-r3.zip`
- 长度：19372105 bytes
- SHA256：`D2C161A52DAA3F9694D38088C8CC03A07860AAC9B54D2831968987E6B45EEFD5`
- 包内仅有四项：3 个 payload 文件和 `README-安装说明.txt`
- 不含 PDB、deps、日志、配置或本机临时路径

## payload

- `mods/Spire1/Spire1.dll`：788480 bytes，SHA256 `4F49BA0D2BE134C6EFD149E96E4B94387E0AA873DFFE40AA7368FC41EBBD88C2`
- `mods/Spire1/Spire1.pck`：19669354 bytes，SHA256 `70CCBB4D1A2DD1439152F030E40B4C1A8BCA775547EB4F8273C956F62BB51C79`
- `mods/Spire1/Spire1.json`：548 bytes，SHA256 `CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305`

## 依赖门禁

- 二进制 AssemblyRef 只有 `BaseLib`。
- manifest 只声明 `BaseLib >= 3.4.5`。
- Watcher、AutoAnthony、AutoAnthonyWatcher 均未写成硬前置。
- 绑定到 payload 的 AssemblyRef、manifest consistency、TypeDef 门禁全部 PASS。

## 运行证据

该包的确切 DLL 已完成当前 Release 字节三形态真实烟测和 9-case 可选 Mod 交叉启动矩阵。报告分别为：

- `form-native-smoke-r5-current-20261003.md`
- `partial-mod-launch-matrix-current-release-r32-20261003.md`

## 未知

朋友环境的可见 UI、视觉动画、长战斗、存档/读档、重连、多人同步、性能和平衡仍需要真人或后续专项验收。
