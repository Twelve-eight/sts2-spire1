# scan-hooks.mjs 用法与复现说明, 2026-09-24

本页说明 sts1-kb 目录下 scan-hooks.mjs 的用法, 参数默认值, 本机复现命令, 失败语义与仍然未验证的边界. 本页不声称修复了知识库根目录 monsters-scan.json 的来源链, 也不声称完成了全知识库的语义验收.

## 1. 脚本做什么

- 在给定的 class 根下取一个包目录, 递归收集其中的 .class 文件 (排除文件名含 $ 的内部类), 对每个类执行 javap -p, 再用固定的 PATTERNS 正则判断该类是否声明了对应成员.
- 输出一个 { name, hooks } 记录数组的 JSON. name 是相对 class 根的 POSIX 风格路径, hooks 是命中的键名数组, 键序与 PATTERNS 的定义顺序一致.
- 判据只是 javap -p 打印的声明成员文本. 脚本不解析继承来的行为, 不判断游戏内可达性, 不读本地化文本, 不判断卡池或怪物注册链.
- 标准 Node ESM, 只用 Node 标准库, 无第三方依赖. 第 1 行是可选的 shebang, 直接 node 也可运行.

## 2. 参数与默认值

用法: node scan-hooks.mjs [package] [options]

- package: 单个安全目录名, 默认 powers. 必须是 class 根下的单层目录名; 绝对路径, 路径分隔符, 单点与双点, 上级穿越, 控制字符, 首尾空白, 尾随空格或点, 以及 Windows 保留设备名 (CON, PRN, AUX, NUL, COM1-COM9, LPT1-LPT9, CONIN$, CONOUT$, 含带扩展名的形式) 都会被拒绝.
- --base <dir>: class 根, 即包含各包目录的 com/megacrit/cardcrawl 目录. 默认按脚本自身位置解析 <脚本目录>/.tmp-javap/cls/com/megacrit/cardcrawl, 不指向任何旧的工作区根路径.
- --output <file>: 输出 JSON 路径. 默认 <脚本目录>/.tmp-javap/<package>-scan.json. 输出目录必须已存在, 脚本不创建目录.
- --javap <file>: javap 可执行文件路径.
- --help: 打印用法并以 0 退出.

javap 的解析顺序固定为: 显式 --javap, 其次 JAVA_HOME/bin, 最后 PATH. 选项值中的控制字符与全空白值会被直接拒绝.

## 3. 本机中央验证必须显式指定 JDK 21

本机环境与脚本默认解析并不一致 (只读探测结果, 不是运行结果):

- 进程环境变量 JAVA_HOME 为 C:\Program Files\Zulu\zulu-21\, 且其 bin\javap.exe 存在.
- PATH 上解析到的 javap 属于 JDK 25 (G:\zulu25.36.15-ca-jdk25.0.4-win_x64\bin\javap.exe).

因此若不显式传 --javap, 取决于 JAVA_HOME 是否生效, 可能落到 JDK 25 的 javap. 本机中央验证要求显式指定 JDK 21 的 javap:

    C:/Program Files/Zulu/zulu-21/bin/javap.exe

该显式指定的要求只固定本轮验证的可执行文件来源, 不代表已用 JDK 21 跑过完整扫描, 也不代表产物内容已核对.

## 4. 复现命令 (本机)

前置条件: 目标 class 树已存在于 class 根下. 本轮只读探测到 G:/omp works/Sts/sts2-spire1/research/sts1-kb/.tmp-javap/cls/com/megacrit/cardcrawl 存在, 且 powers 子树下存在 .class 文件.

    cd "G:/omp works/Sts/sts2-spire1/research/sts1-kb"
    node "G:/omp works/Sts/sts2-spire1/research/sts1-kb/scan-hooks.mjs" powers \
      --javap "C:/Program Files/Zulu/zulu-21/bin/javap.exe" \
      --output "G:/omp works/Sts/sts2-spire1/research/sts1-kb/.tmp-javap/powers-scan.json"

复现要点:

- 先确认输出目录存在, 否则脚本报输出目录不可读并以非零退出, 不会写任何文件.
- 审计类复现建议把脚本复制到经授权的全新 G: 隔离目录, 并显式给出 --base 与 --output, 不覆盖当前快照或既有证据.
- 成功时 stdout 先打印 scan-hooks: wrote <path> 与 total <N> (excluded inner classes containing $: <M>), 再为每个 PATTERNS 键打印一行 ``## <key> <count> : <name1,name2,...>``.

## 5. 失败语义与写出事务

- 下列情况都以非零退出并打印首个失败位置与原因: class 根或包目录缺失或不是目录; 包目录不在 class 根之下; 输入树内出现符号链接或 reparse point; 可达的 .class 集为空; javap 非零退出, 被信号终止, 超时, 超出输出上限或输出为空.
- javap 通过 execFileSync 以参数数组调用 (不经过 shell 拼接), 单个类设有有界超时与 maxBuffer, 失败信息包含该类路径.
- 全部 class 成功之后才写目标: 输出目录内唯一的临时文件, 以独占方式创建, fsync 后原子改名为目标. 任一失败时既有输出文件字节保持不变; 清理只针对本进程自己创建的临时文件, 不递归删除任何目录.
- 因此失败运行不会留下看似有效的空 hooks 记录, 也不会把 javap 错误写成正常结果.

退出码约定: 用法与包名等参数错误为 2; 路径, javap, 空 class 集与写出失败为 1.

## 6. 未验证范围

本节区分已落盘的源码结论与尚未取得的运行证据. 本页由实现会话书写, 该会话未运行 javap, 未运行生成器, 未做构建, lint, 测试或游戏验证; 下述运行行为均留给中央验证.

- 未验证真实 JDK 21 全树扫描的实际产物内容与耗时.
- 未验证失败时既有输出字节保持不变 (需要合成 class 或失败 javap 夹具).
- 未验证路径含空格或非 ASCII 字符时的实际运行.
- 未验证符号链接与上级穿越在真实文件系统上的拒绝行为 (仅为代码层拒绝).
- 未验证 Windows 保留设备名的实际拒绝触发 (仅为代码层拒绝).
- 未验证 PATTERNS 命中结果与旧版脚本逐条一致的运行对比; 本轮只做了 PATTERNS 块的逐字节比较.
- 未验证 class 根之外的 .tmp-javap 目录中是否存在其它既有 *-scan.json 产物.
- 未验证知识库根目录 monsters-scan.json 的来源链; 该文件不是本脚本的产物, 本轮不改动它, 也不声称已修复其来源关系.
- 未完成全知识库的语义验收: 本脚本只报告声明成员, 不构成继承行为, 可达性或数值正确性的独立核验.

## 7. 相关文件

- 脚本: G:/omp works/Sts/sts2-spire1/research/sts1-kb/scan-hooks.mjs
- 行为契约: G:/omp works/Sts/sts2-spire1/docs/DEVELOP-hook-scan-20260924.md
- 重验说明 (来源与差异边界): G:/omp works/Sts/sts2-spire1/research/sts1-kb/REVALIDATION-2026-09-23.md
- 目录总览与重新生成入口: G:/omp works/Sts/sts2-spire1/research/sts1-kb/README.md
