你是 实现者, 范围: G:\omp works\Sts\sts2-spire1\mod\Spire1Code.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\early-save-guard-r8\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 6 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.


## 实现白名单
以上模板的只读限制仅适用于监督. 实现者只可改 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs, Patches\FormsMissingModifierSaveGuardPatch.cs, Patches\Spire1PowersGatePatch.cs, 加唯一报告. 不改其它生产/共享文档/测试/发布脚本/csproj. 不构建lint测试部署不运行游戏不执行git不写C:不再委派. 唯一global:deepseek-v4.1-flash/wb2api/xhigh.
前置已完成r5 raw ID prefix与FormsCompatibilityBridge, 本轮只闭合r5监督发现的两个窄面. 必须先读 G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\runtime-rework-r5\spire1-supervisor.md (收敛两项) 和独立化契约 docs\DEVELOP-forms-independent-20261005.md.
1. 采用行为修复而非仅声明未知: guard是独立save安全面, 不能因Spire1PowersGate.ContentUnavailableActive而漏装. 在最早Initializer安全阶段显式安装并通过真实Harmony.GetPatchInfo验证 ModifierModel.FromSerializable(SerializableModifier) 的精确guard prefix已存在/owner/priority. 不读取Spire1Config或optionalmod具体类型, 不扫描ModelDb, 不改其它内容熔断语义. 若阶段2/3中途进入不可用, guard仍保持已安装. 可在guard类内新增幂等EnsureInstalled(Harmony)来集中安装/验证, MainFile最早入口调用; Phase3反射扫描须精确跳过该类, 避免与显式路径重复. 保护失败须明确日志/异常说明未闭合, 不伪称注册熔断即可保护FormsrawID. 必要无法保证之处先flag, 不写stub.
2. 重复Initialize/正常扫描/不可用分支不能叠相同owner+prefix条目. 保留其他补丁独立安装和既有_phase3Completed/_phase3ScanCompleted语义, 不改powersgate本体. 对所有非Forms raw modifier保持原引擎解析. guard自己的rawID/Forms unavailable失败逻辑不得放宽.
3. 修 Patches\Spire1PowersGatePatch.cs:48 的同一处残留误导注释, 与696-702/IsFormsType真实行为一致: 只看程序集simple name+noncollectible身份, 不看Forms bridge IsAvailable或签名. 纯注释, 不改方法体.
4. 第一结论立即报告, 每检查面一次, 完成CODE_COMPLETE列具体文件/精确hash/未构建未实机面. 任务限10分钟, 不做其它长篇审查. 监督r5关于Forms门禁FormsKeysInSpire1字段不能作为Spire1 PCK实测, 本轮不重复该声明; 主会话将对新Spire1字节另做中央门禁.