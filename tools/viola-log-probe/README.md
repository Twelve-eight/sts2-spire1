# Viola 日志变换中央探针

使用真实测试副本 0Harmony.dll 和实际兼容源码. Fixture 默认从本机已落盘的第三方反编译文件重新编译, 保留程序集/类型身份与原方法内容; 可通过 FixtureSource 显式指定该只读输入. 不复制第三方源码进 Git, 不加载原始游戏 DLL, 不调用真实 Godot 资源系统.

先构建 Contracts, 再分别构建 Fixture 和 Host, 将 ContractsDll 指向第一步的 DLL. 每个项目的 OutputPath, BaseIntermediateOutputPath 和 IntermediateOutputPath 必须分别放在 G: 独占目录. 最后运行 Host/ViolaProbe.dll, 第一个参数为生成的 ViolaSnakeBite.dll.

探针先在真实 Harmony 中包装卡图 getter, 再安装兼容层, 检查现存调用链的输出, 资源检查次数与日志次数. 另验证缺 mod, 幂等, 外部日志, 调用计数漂移, 前缀, 元数据保留和实际动态方法的参数求值. 静态 labels/blocks 用例不执行带不完整异常区段的合成 IL. 所有清理仅撤销本进程的自有 owner.

该结果不等于原安装 DLL 字节或游戏内视觉验收. 未解除实验编译门禁, 未部署, 未操作 Steam/共享配置或服务.
复现入口为 Run-Probe.ps1, 传入 -Source 原始反编译文件与 -OutputDirectory 工作区 .tmp 下的独占路径. 脚本只移除编译器禁止显式声明的模块 RefSafetyRules(11) 属性, C# 编译器自行生成该元数据;方法正文与 debug 属性不变, 原始输入不改写. 预处理前后指纹和精确规则均落盘. 首次原样编译的 CS8335 是夹具重建限制, 不是兼容补丁缺陷.
