# A 阶段: 原生用户目录预检

状态: CODE_COMPLETE, 仅源码交付. 2026-09-30 未运行打包/构建/测试/游戏/git. 不包含 B 阶段的 C# 模组, 不修改生产形态代码.

## 四个交付文件

- G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\project.godot: 独立项目, 专用用户目录名 FormNativeUserdataProbe, 无 main_scene/autoload/插件, 关闭文件日志和音频输入, 配置 Dummy.
- G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\userdata_probe.gd: 直接继承 SceneTree, 输出真实引擎/目录/驱动 JSONL, 每条刷新, 最后只退出自己的 SceneTree.
- G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\pack-userdata-probe.mjs: 只用 Node 内建模块, 固定收录上述两个资源, 不启动任何程序, 不创建目录, 不覆盖已有 PCK.
- G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\README.md: 主会话的后续执行说明, 本次没有执行下列命令.

## 格式和安全边界

- PCK 依据 godotengine/godot 的 4.5.1-stable 官方源码: core/io/file_access_pack.h:39-51, core/io/pck_packer.cpp:92-123,148-199,204-256, core/io/file_access_pack.cpp:260-325. 使用 V3, magic 0x43504447, PACK_REL_FILEBASE=2, 32 字节文件区对齐, 相对文件偏移, 4 字节对齐 UTF-8 路径, 原始长度与真实 MD5. 不是猜测旧版 PCK 布局.
- 原生目录线索: G:\omp works\.tmp\form-playable-20260928-01a0e7ad\godot-os-windows-4.5.1.cpp:2396-2417,2487-2488. APPDATA -> get_data_path -> get_user_data_dir 只是官方源码证据, 当前 EXE 的行为须实际预检.
- 为避免误传共享 G:\appdata, 本载体比原请求更严格: APPDATA/LOCALAPPDATA/TEMP/TMP 和所有报告/PCK 必须在 G:\omp works\.tmp\form-playable-20260928-01a0e7ad 的子目录中. 根路径由调用者显式传入, 不提供宽泛默认值.
- 拒绝相对路径, UNC, 路径穿越, ADS, 设备名, 短路径别名, 已有链接/junction/reparse point. 父目录须已存在, 报告和 PCK 必须用新文件名. 不向 user:// 写探测文件, 不创建或改动游戏配置.
- 目录链接检查不是原子防护, 不认证恶意并发替换. 主会话须独占本轮新建普通目录. GDScript 也不能追溯阻止其执行前的原生写入, 所以不能省略启动前环境隔离.

## 主会话最短先跑顺序

以下都是后续主会话的操作, 不是已执行记录. 本阶段不需要构建或安装依赖.

### 1. 准备新隔离目录和只读 EXE 副本

本例统一使用 G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-preflight-20260930-01. 主会话先确认它是独占的新普通目录, 创建其 appdata, localappdata, temp, exe 子目录. 不通过链接指向其它位置.

主会话把 E:\Slay the Spire 2\SlayTheSpire2.exe 的只读副本和必要原生运行依赖放入该 exe 子目录, 记录源/副本 SHA256. 不在 E: 原安装或 Steam 安装执行. 不带游戏 PCK, project.godot, override.cfg, mods 或任何游戏 autoload. 本工具不自动复制或启动 EXE. 如果副本缺运行依赖, 保留失败信息, 不退回原安装启动.

### 2. 单独打包两个预检资源

仅在上述父目录已存在后由主会话执行:

```powershell
& 'G:\nodejs\node.exe' 'G:\omp works\Sts\sts2-spire1\tools\form-native-smoke\pack-userdata-probe.mjs' --output 'G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-preflight-20260930-01\userdata-probe.pck'
```

Node 路径仅做过只读发现, 本会话未启动它. 打包成功只输出 PACKED_UNVERIFIED, 文件长度/偏移/MD5/SHA256 来自生成字节, 不等于实际引擎已接受. 重跑须使用新 PCK 文件名, 不删除或覆盖旧证据来制造成功.

### 3. 在子进程创建前设置环境

只设置新子进程的环境, 不用 setx, 不改用户级配置或既有游戏环境:

```text
APPDATA=G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-preflight-20260930-01\appdata
LOCALAPPDATA=G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-preflight-20260930-01\localappdata
TEMP=G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-preflight-20260930-01\temp
TMP=G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-preflight-20260930-01\temp
```

工作目录为上述 exe 子目录. stdout/stderr 分别重定向到同一 native-preflight-20260930-01 目录下的新日志. 如运行依赖需要其它缓存目录, 主会话也必须提前重定向到 G:, 不能把此脚本当作全部原生写入的沙箱.

### 4. 仅通过无窗口低优先级启动器执行

下面给出精确 EXE 和参数, 只交给主会话现有启动器, 不直接粘贴到交互终端执行. 启动器必须使用 Hidden, 低优先级, 有界超时和独立的进程句柄. 若使用 Start-Process, 必须带 -WindowStyle Hidden. 不操作前台, 不枚举/停止用户其它进程. 不传 --autoslay, --bootstrap, --editor 或任何游戏参数.

```text
"G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-preflight-20260930-01\exe\SlayTheSpire2.exe" --headless --audio-driver Dummy --main-pack "G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-preflight-20260930-01\userdata-probe.pck" --script res://userdata_probe.gd --disable-crash-handler -- --expected-appdata-root="G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-preflight-20260930-01\appdata" --report="G:\omp works\.tmp\form-playable-20260928-01a0e7ad\native-preflight-20260930-01\userdata-probe-01.jsonl"
```

两个自定义参数必须在最后的 -- 之后且使用 --key=value. GDScript 核对实际 DisplayServer=headless 和 AudioServer=Dummy, 不只信任参数. 报告安全性检查失败时不创建不安全文件, 只有重定向 stdout 中的 FAIL JSON.

### 5. 主会话判定和停止边界

- 必须同时满足: 子进程退出码 0, 本次新 JSONL 文件具有 observed 及最终 result/PASS, errors 为空, 报告内 PID 和路径对应本次启动, 实际 user_data_dir 为指定 APPDATA 的子路径. 不能只看进程正常退出或只看旧报告中的 PASS.
- 退出码 2 表示参数/目录/隔离/驱动/项目身份检查失败; 3 表示报告 IO 失败. 解析错误, 崩溃, 超时或没有完整报告也都算失败, 即使原生进程意外返回 0. Godot 启动横幅可能混入 stdout, 优先读取单独 JSONL.
- 不安全目录不能被创建报告来验证失败; 测错误参数时仍保持子进程环境完全在 G:, 不把 APPDATA 改回 C: 或共享用户目录.
- A 成功只证明该 EXE 副本在此预检项目及环境下的目录与无窗口/Dummy 驱动结果. 不证明游戏启动/模组配置/存档/形态/多人/重连/视觉效果, 不自动放行 B. 保留 EXE 和 PCK 身份, 再由主会话决定下一阶段.

## 实现报告

G:\omp works\Sts\sts2-spire1\docs\reports\form-playable-20260928\native-worker.md
