你是 实现者, 范围: G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke.
用户本轮唯一指定模型 `global:deepseek-v4.1-flash`, 路由 `wb2api`. 只准使用当前 harness 的原生子代理设施,
不得更换模型, 不得启动其它代理运行时(omp/codex 等), 不得再委派.

## 唯一可写路径

报告文件: `G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\native-tool-r6\worker.md` (只可写这一个文件; 不改产品代码/构建/部署/游戏/共享配置, 不写 C:).

## 增量落盘 (硬要求, 违反则结果不作交付证据)

1. 拿到第一条可用结论(发现, 证据, 文件与行号, 复现命令, 失败原因)后 **立即** 追加写入报告文件, 然后才做下一步.
2. 每完成一个检查面(一个文件 / 一个不变量 / 一个项目面)写一次盘; 结论未定时也要写入已排除的可能性与证据.
3. 报告文件固定三段: `## 已确认`(有证据), `## 进行中`(半成品, 需复核), `## 未知`(未覆盖).
4. 不要攒到最后一次性输出; 最终回复允许只是摘要, 并给出报告绝对路径.

## 输出每项包含

优先级, 绝对路径与准确行号, 触发条件, 宣称或权威契约, 当前控制流, 可复现命令, 最小修复范围, 尚缺的实机证据.
最多 8 项, 有证据就停; 不凑数量, 不把源码推理称为实机复现.

## 语言

只允许中文, 英文, 法文, 德文, 俄文与 ASCII 标点. 未知多语言原文只引本地路径与行号.


## 实现白名单与本批规格
以上模板的只读限制仅对监督者生效. 实现者可写 G:\omp works\Sts\sts2-forms\tests\FormsNativeSmoke 下的代码/项目/manifest/中文README, 加唯一报告. 不改任何生产代码和共享文档. 不构建lint测试部署, 不运行游戏, 不执行git, 不写C:或Steam或共享mod_configs. 不再委派, 不调codex exec/omp, 唯一global:deepseek-v4.1-flash/wb2api/xhigh.
1. 把已验证的旧原生烟测载体从 G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Run\FormNativeSmokeRunner.cs 和 Patches\FormNativeSmokePatch.cs 迁移成独立测试mod FormsNativeSmoke. 只做namespace/usings/MainFile适配, 保留原三姿态场景, 固定种子, 真实Watcher牌, 真实actions, 超时/退出/证据机制. 生产Forms.dll/Spire1.dll不得编入任何测试源码. 测试工程不得引用Spire1.
2. 使用Godot.NET.Sdk/4.5.1/net9.0, 0Harmony/sts2引用同生产路径, Publicizer仅sts2. Forms.dll通过必填FormsDllPath引用既有已构建生产字节, Private=false. 不用ProjectReference递归构建生产. 必要BaseLib包3.4.5, 无自动部署/Publish targets. 输出由hub传到G:隔离目录. 参考生产Forms.csproj/Sts2PathDiscovery.props/Directory.Build.props. manifest has_pck=false, has_dll=true, dependencies BaseLib/Forms, 明确test-only. 自有Initializer装NGame._Ready补丁并验证失败不吞.
3. 若不增加明显复杂度, 单独 LifecycleSmokeRunner 实现 --forms-lifecycle-smoke: 在Godot主线程已ModelDb就绪后核对bridge Bound; 重复Initialize/TryBind不叠Forms两个Harmony owner的patch; 从已加载Watcher的Location读字节Assembly.Load(byte[])造同simple-name不同身份, 等实际主线程pump帧消费, 核对不再Available, 明确Terminal/需要重启; 最后调用Shutdown两次, 核对两owner无残留patch/引用. 只称同名多程序集隔离复现, 不冒充真正卸载热替换. 不执行ModelDb.Init第二次以免改变引擎缓存; 未可安全实现即flag, 不写stub.
4. 测试只能在hub显式参数启用时运行. 报告环境保持SPIRE1_FORM_SMOKE_REPORT兼容旧控制器, 输出路径验证G:隔离根, 不能落共享存档. 运行脚本由hub集中处理, 本写集不创建自动启动任务.
5. 新README先写测试契约与边界, 旧test-only carrier保留原行为不可为了变绿删除Blasphemy即死或力量费用规则. 第一结论立即落盘, 完成写CODE_COMPLETE, 列修改文件和编译/实机未知. 任务限10分钟, 超时落剩余接口供hub拆分, 不攒长推理.