# Spire1 Forms Beta r2 发布包复核 - 2026-10-03

## 范围

本报告绑定公开 Beta r2 包、未压缩 staging、最终 Release gate JSON 和当前隔离运行使用的同一 Spire1.dll 字节。只记录静态包门禁与文件身份,不把包复核外推为完整游戏验收。

## 已确认

### 1. 包身份和结构

- 包路径: `G:\omp works\Sts\sts2-spire1\dist\Spire1-Forms-Beta-20261003-r2.zip`
- 文件长度: `28413851` bytes.
- ZIP SHA256: `B83B91E8F540D58305AC92AD2055D9A921001E9E43BE09384758DB565B0AEE7E`.
- 包内仅有四项:
  - `mods/Spire1/Spire1.dll`
  - `mods/Spire1/Spire1.pck`
  - `mods/Spire1/Spire1.json`
  - `README-安装说明.txt`
- 包内不含 `Spire1.pdb`, `Spire1.deps.json`, 日志、配置或本机临时路径文件。
- staging 路径: `G:\omp works\.tmp\Spire1-beta-20261003-r2\`.

### 2. payload 字节身份

| 文件 | 长度 | SHA256 |
|---|---:|---|
| `mods/Spire1/Spire1.dll` | 781312 | `51224C20B51EC0F550AEADD9E749B01B19D5F13DE74DD44D5EC8E190436A7AA7` |
| `mods/Spire1/Spire1.pck` | 28866294 | `CF37054F2926F5CE92BF267D48CEB5CADD003F85AE73A0AF09F7B0A9931C623E` |
| `mods/Spire1/Spire1.json` | 548 | `CDBD57D54374285503538D866551B897938A5D540C2285CE5068019565BB9305` |

README 的 staging SHA256 为 `3C03273FE7966284A3299E83AD105536F91B9DED261852D9448688CE87631121`,长度 `1762`。

### 3. manifest 和依赖边界

`Spire1.json` 声明:

- `version`: `1.2.3`
- `min_game_version`: `0.111.0`
- 唯一 manifest 依赖: `BaseLib >= 3.4.5`
- `has_dll`: `true`
- `has_pck`: `true`
- `affects_gameplay`: `true`

Watcher、AutoAnthony、AutoAnthonyWatcher 没有被写成 manifest 硬前置。它们只由运行时可选桥接探测。

### 4. 当前 r2 DLL 的结构门禁

门禁命令输出和绑定 JSON:

`G:\omp works\.tmp\form-playable-20260928-01a0e7ad\beta-r2-release-gates-bound-20261003.json`

- 命令退出码: `0`.
- `passed`: `true`.
- AssemblyRef: `15`.
- TypeDef: `972`.
- `assemblyref-forbidden`: PASS.
- `manifest-consistency`: PASS;二进制 mod 引用只有 `BaseLib`,manifest 也只声明 `BaseLib`.
- `typedef-forbidden`: PASS.
- 该 gate 绑定的 DLL 路径为 staging 中的 `Spire1.dll`,其 SHA256 与上表 `51224C20...` 一致。

### 5. README 边界声明

README 已明确:

- BaseLib 3.4.5 或更高版本是必需项。
- Watcher、AutoAnthony、AutoAnthonyWatcher 是可选运行时桥接。
- 视觉 UI、长战斗平衡、存档重载、重连和多人行为仍是 Beta 未完全验收范围。
- 公开包有意排除调试符号。

## 进行中

- Beta r2 已有当前 DLL 的真实隔离三形态运行证据,见 `form-native-smoke-r25-current-20261003.md`。
- 朋友安装仍应由用户在目标测试副本或朋友环境中完成一次可见 UI 验收;本报告不替代该验收。

## 未知

- 可见 UI 和实际视觉呈现。
- 长战斗、战中存档/读档、重连、多人同步、性能和完整数值平衡。
- 不同 Mod 组合和不同 Mod 加载顺序下的全部用户环境差异。

## 复核命令

```powershell
Get-FileHash -LiteralPath 'G:\omp works\Sts\sts2-spire1\dist\Spire1-Forms-Beta-20261003-r2.zip' -Algorithm SHA256
Add-Type -AssemblyName System.IO.Compression.FileSystem
$z=[IO.Compression.ZipFile]::OpenRead('G:\omp works\Sts\sts2-spire1\dist\Spire1-Forms-Beta-20261003-r2.zip')
$z.Entries | Select-Object FullName,Length,CompressedLength
$z.Dispose()
```