# GSE 原生存储隔离 - 2026-10-01

## 已确认

- 夹具目录: `G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-gse-storage-r8-20261001-01`.
- 使用隔离复制的 `steam_api64.dll`, SHA256 为 `FA16B86EF97FBD248031722ECAE4898987C703AB54F848D227B99850D1A0B3EC`,并由真实 GSE API 导出调用 `SteamAPI_Init`, `SteamAPI_SteamUser_v023`, `SteamAPI_SteamRemoteStorage_v016`.
- `GseSavePath` 指向本轮 G: 目录, `account=76561199520000001`,初始 RemoteStorage file count 为0.写入和读回 marker 成功,唯一实际文件位于 `gse\2963800\remote\form-storage-isolation-20261001.txt`.
- 夹具进程 exitCode=0, timedOut=false, priority=BelowNormal,没有启动游戏 EXE,没有写 Steam 安装、共享 `mod_configs` 或 C:.

## 未知

- 这只证明 GSE 原生存储路径和账号设置的隔离,不证明 Godot 游戏、ModManager、Forms 加载、真实战斗、视觉、存档迁移或多人.
