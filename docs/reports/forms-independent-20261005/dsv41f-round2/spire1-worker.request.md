你是实现者,范围: G:\omp works\Sts\sts2-spire1\mod 以及必要的同仓文档报告. 用户本轮唯一指定模型 global:deepseek-v4.1-flash, 路由 wb2api. 只准使用当前 harness 的原生子代理设施, 不得更换模型, 不得启动其它代理运行时, 不得再委派.

## 唯一可写路径

只可写以下产品文件和报告:

- G:\omp works\Sts\sts2-spire1\mod\Spire1.csproj
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Extensions\StanceCmd.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Patches\Spire1PowersGatePatch.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Interop\FormsCompatibilityBridge.cs
- G:\omp works\Sts\sts2-spire1\mod\Spire1Code\MainFile.cs, 仅在确有必要时
- G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\dsv41f-round2\spire1-worker.md

不写 Forms 项目. 不构建, 不部署, 不启动游戏, 不改共享配置, 不写 C:.

## 增量落盘硬要求

拿到第一条可用结论后立即追加报告, 每完成一个检查面再追加一次. 报告固定为 ## 已确认, ## 进行中, ## 未知. 最终回复给出报告绝对路径和修改文件清单.

## 输入和契约

先读:

- G:\omp works\Sts\sts2-spire1\docs\DEVELOP-forms-independent-20261005.md
- G:\omp works\Sts\sts2-spire1\docs\HANDOFF-forms-current-20261005.md
- G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\build-split-audit.md
- G:\omp works\Sts\sts2-spire1\docs\reports\forms-independent-20261005\coupling-audit.md
- 当前目标文件和全部源码调用者

## 实现范围

把 Spire1 对 Forms 的编译期硬引用移除, 并让 Spire1 在 Forms 不存在时仍能正常加载:

1. Spire1.csproj 默认移除 Spire1Code\Forms\**. 同时移除 FormNativeSmokeRunner.cs 和 FormNativeSmokePatch.cs, 因为它们不得留在生产 Spire1.dll.
2. 从 StanceCmd.cs 移除 using 和 Forms 类型引用. 保留普通 Calm/Wrath/Divinity 逻辑. 增加对 Forms.FormsCode.Interop.FormsRuntimeEntryPoint 的可选反射桥, 只在程序集存在并通过签名校验时转发; 不得出现 Forms AssemblyRef. 缺 Forms 时普通姿态路径不抛异常.
3. 新建 FormsCompatibilityBridge.cs, 封装程序集查找, 方法签名验证, disabled reason 和线程安全静态状态. 只按字符串和反射, 不使用 typeof(Forms...).
4. 从 Spire1PowersGatePatch.cs 删除 Forms using, FormsNamespace 和 FormStanceMode 直接访问. 独立 Forms power 不应进入 Spire1 powers gate. 保持 Spire1 自己 namespace 的 gate 行为不变. 如果需要让 Spire1 自己的 mode 查询可选, 通过 FormsCompatibilityBridge, 但不要增加独立硬前置.
5. MainFile.cs 只在必要时处理 Forms smoke 或跨程序集扫描. 不让 Spire1 扫描不存在的 Forms. 不引入 Forms manifest 依赖.
6. 不删除普通 Spire1 形态基类,卡牌或 IOnStanceChanged; Forms 缺失时普通 Watcher 相关 Spire1 行为保持原语义.
7. 不构建和不测试. 发现 Forms entrypoint 签名需要调整时, 记录准确原因, 但不要写 stub 或编译时引用.

## 禁止

- 不改 FormsCode 或 G:\omp works\Sts\sts2-forms.
- 不把 Forms 目录内容复制回 Spire1.
- 不用 namespace 字符串判断 Forms.
- 不引入任何新的 manifest dependency.
- 不碰其他会话发布脚本和 Workshop staging.

## 输出要求

报告必须区分源码已改, 进行中和未知. 最终回复只摘要, 给出报告路径和修改文件清单.

