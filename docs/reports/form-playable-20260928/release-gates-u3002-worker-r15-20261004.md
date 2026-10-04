# release-gates U+3002 最小返工报告 (worker r15, 2026-10-04)

## 已确认

基线 (编辑前, 只读采集):
- [P0] 目标脚本绝对路径: G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1
- [P0] 编辑前字节长度: 4989 bytes. 编辑前 BOM: 无, 首三字节 23-20-72 (ASCII '# r'). 编辑前换行: CRLF=96, 纯 LF=0.
- [P0] 编辑前 U+3002 计数: 9 个, 行号 L1, L5, L15, L17, L64, L66, L67, L71, L78 (均为注释行行尾句号).
- [P0] 编辑前身份回写实现未改动: 基线采集为只读, 未写入任何字节; 该文件相对 HEAD 已带有前一轮未提交的身份回写块, 本轮未触碰该控制流.
- [P0] 触发条件: 任何读取/执行/发布门禁链路加载该脚本时都会遇到这 9 个 U+3002; 该字符为 CJK 表意句号, 非 ASCII 标点, 违反 AGENTS.md Sec 5 语言卫生约束.
- [P0] 契约: 只把 U+3002 替换为 ASCII '.' 或删除; 不改运行语义; 不改身份回写控制流; 保持 no BOM 与 CRLF.
- [P0] 当前控制流 (编辑前): 参数解析 -> dotnet build (可 -SkipBuild 跳过) -> 三道结构门禁执行 -> 读取 DLL 单次快照 -> 保留门禁 JSON 原字段并在根对象结束符前插入 dllSha256/dllLength -> 按门禁退出码透传 (exit 0/2/3) 或回写失败时改判 (原 0 改 exit 1). 本轮不得改动该控制流.
- [P0] 最小修复范围: 仅该脚本的 9 个 U+3002. 不构建, 不运行门禁, 不部署, 不启动游戏, 不触碰 Steam 安装与共享 mod_configs, 不写 C:.
- [P0] 尚缺的实机证据: 未构建, 未运行门禁, 未部署; 修复后的脚本执行行为与身份回写端到端结果尚未验证.
- [P0] 基线复现命令 (PowerShell, 编辑前已实际执行):
  $p='G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'
  $b=[IO.File]::ReadAllBytes($p); $t=[Text.Encoding]::UTF8.GetString($b)
  "BYTES=$($b.Length)"; "BOM=$([BitConverter]::ToString($b[0..2]))"
  "CRLF=$(([regex]::Matches($t,"`r`n")).Count)"; "LF=$(([regex]::Matches($t,"(?<!`r)`n")).Count)"
  $i=0; foreach($l in ($t -split "`n")){$i++; if($l.Contains([char]0x3002)){"L$i : $l"}}
  "U3002COUNT=$(([regex]::Matches($t,[char]0x3002)).Count)"
  实测输出: BYTES=4989 / BOM=23-20-72 / CRLF=96 / LF=0 / 9 行命中 (L1,L5,L15,L17,L64,L66,L67,L71,L78) / U3002COUNT=9.

修复 (本轮已完成, 2026-10-04):
- [P0] 修复方法: 对目标脚本做逐字节过滤, 遇到 UTF-8 三字节序列 E3 80 82 (U+3002) 即替换为单字节 0x2E (ASCII '.'); 其余字节原样保留; 该方式不改编码, 不重写换行, 不添加 BOM. 过滤计数: REPLACED=9.
- [P0] 9 处旧 -> 新 (仅行尾字符变化, 其余文本逐字符一致; 下表 "U+3002" 即 CJK 句号原字符):
  - L1: "供 CI / 发布脚本调用" + U+3002 -> "供 CI / 发布脚本调用" + '.'
  - L5: "作为本脚本退出码" + U+3002 -> "作为本脚本退出码" + '.'
  - L15: "身份字段追加失败等)" + U+3002 -> "身份字段追加失败等)" + '.'
  - L17: "PowerShell 5.1 兼容" + U+3002 -> "PowerShell 5.1 兼容" + '.'
  - L64: "(Int64 字节)" + U+3002 -> "(Int64 字节)" + '.'
  - L66: "原退出码透传" + U+3002 -> "原退出码透传" + '.'
  - L67: "并在 stderr 报错" + U+3002 -> "并在 stderr 报错" + '.'
  - L71: "都取自这份字节" + U+3002 -> "都取自这份字节" + '.'
  - L78: "(不对 JSON 自身取哈希)" + U+3002 -> "(不对 JSON 自身取哈希)" + '.'
- [P0] 修复后字节长度: 4971 bytes (4989 - 9 x 2). BOM: 无, 首三字节仍为 23-20-72. 换行: CRLF=96, 纯 LF=0, 末行仍以 CRLF 结束. U+3002 计数: 0. SHA256: 19DE80674200285C7E56B8D06F6C33BFD36FF58FD48CA50174B1D8FAC2F31965.
- [P0] 最小差异证明: 将当前文件 9 处行尾 '.' 还原为 U+3002 得重建基线, 该重建为 4989 bytes / U+3002=9 / CRLF=96 / LF=0 / 无 BOM, 9 处行号与行内容与编辑前采集逐行一致 (重建 SHA256=61901BCDC4D9B7DE13114C0A08EF22845F17FF865BE21A9DB9768590105E4739); 对重建基线再次执行同一字节过滤后与当前文件逐字节相等 (FILTERED_EQUALS_CURRENT=True). 结论: 相对编辑前基线的差异仅为这 9 处字符替换.
- [P0] 9 处替换后字节偏移 (末字节均为 0x2E): L1 @86, L5 @326, L15 @1037, L17 @1066, L64 @2960, L66 @3161, L67 @3289, L71 @3560, L78 @3991.
- [P0] 身份回写控制流未改动: 回写代码块 (L63-L96) 中所有可执行语句与编辑前逐字节一致, 未新增/删除/重排任何语句, 未改动任何条件/退出码/异常处理; 块内仅注释行 L64/L66/L67/L71/L78 的行尾字符由 U+3002 变为 ASCII '.', 属本轮允许的字符替换范围.
- [P0] 修复后可复现字节检查命令 (PowerShell, 已实际执行):
  $p='G:\omp works\Sts\sts2-spire1\tools\build-gates\run-release-gates.ps1'
  $b=[IO.File]::ReadAllBytes($p); $t=[Text.Encoding]::UTF8.GetString($b)
  "BYTES=$($b.Length)"; "BOM=$([BitConverter]::ToString($b[0..2]))"
  "CRLF=$(([regex]::Matches($t,"`r`n")).Count)"; "LF=$(([regex]::Matches($t,"(?<!`r)`n")).Count)"
  "U3002COUNT=$(([regex]::Matches($t,[char]0x3002)).Count)"
  实测输出: BYTES=4971 / BOM=23-20-72 / CRLF=96 / LF=0 / U3002COUNT=0.
- [P0] 提交状态: 该文件在工作区相对 HEAD 本就带有前一轮未提交改动; 本轮 9 处替换同样留在工作区, 未 git commit, 未 push; 集成与提交由协调方决定.
- [P0] 尚缺的实机证据 (修复后): 未构建, 未运行发布门禁, 未部署, 未启动游戏; Windows PowerShell 5.1 实际运行结果与身份回写端到端结果均未验证, 不得声称通过.

## 进行中

- 无. 本轮规定的最小返工 (9 处 U+3002 替换为 ASCII '.') 已完成并落盘; 无剩余动作. 提交/推送/构建/门禁运行均在本轮范围外, 未执行.

## 未知

- [P2] 未知: 本文件范围外的其他脚本或文档是否仍含 U+3002; 本轮不修改, 需清理时应另开任务.
- [P2] 未知: 修复后脚本在 Windows PowerShell 5.1 下的实际执行结果; 未运行, 不声称通过.
- [P2] 未知: 前一轮未提交的身份回写块相对 HEAD 的最终集成状态; 本轮未改动, 未提交.