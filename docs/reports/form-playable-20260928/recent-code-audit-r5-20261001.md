# 最近落盘代码独立审核报告

日期: 2026-10-01
状态: 进行中, 增量落盘.
范围: 请求文件指定的 SerpentFormPower, AutoAnthonyCompatBridge, FormEffectsProbe, 相关直接调用者和既有报告.
证据类别: 只读源码和既有报告. 未运行 build/lint/test/probe/deploy/game; 未更改产品代码, 安装或共享配置; 不提交或推送. 唯一可写文件为本报告.
模型与路由: 请求指定 gpt-6-astra-ar, gateway -> agentrouter -> gpt-6-astra. 本审查未委派, 未切换模型或启动其它代理运行时; 请求文字不作为实际路由元数据证明.

## 已确认

### [P1] 当前默认 FormEffectsProbe 缺少 Serpent 新增的战斗结束协作者契约

- 绝对路径与行号: `G:\omp works\Sts\sts2-spire1\mod\Spire1Code\Forms\SerpentFormPower.cs:12-13,174-186`; `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\ContractStubs.cs:19,219,309-324`; `G:\omp works\Sts\sts2-spire1\tools\form-effects-probe\FormEffectsProbe.csproj:7,12,15-17,29`.
- 触发条件: 按当前项目默认 FormsSourceRoot 编译 FormEffectsProbe, 使用本次落盘 SerpentFormPower.
- 当前控制流: 项目直接链接生产 SerpentFormPower; 它新增 `AfterCombatEnd(CombatRoom)` override 并引用 `MegaCrit.Sts2.Core.Rooms`. stub 中 PowerModel 没有该虚方法, CustomPowerModel 仅继承该 stub; 此文件没有 Rooms 命名空间或 CombatRoom 定义, 项目也没有真实游戏程序集引用. 因而当前协作者定义不足以编译此新生产源码. 这是源码级编译阻断, 不是本轮编译器诊断; 历史探针成功不能覆盖本快照.
- 最小修复范围: 只在 ContractStubs 补齐与真实引擎一致的 Rooms.CombatRoom 以及 PowerModel.AfterCombatEnd 签名, 并在 SerpentScenarios 中补入清理场景; 不应从探针排除生产 Serpent 文件来绕过阻断.
- 尚缺证据: 新快照的真实探针构建诊断, 修复后的探针结果及生产编译; 完整实机战斗结束流程仍未验证.

## 进行中

- 正在检查 Serpent 的共享 pending, Apply 拒绝/异常, listener 快照, bridge 生命周期和直接调用者.
- AutoAnthony getter 声明身份与缓存机制尚待本轮逐项交叉核对.

## 未知

- 未执行任何构建, 探针或实机行为. 不把本报告推理或历史报告当作本轮运行证据.
- 本会话实际模型和 provider 路由未取得独立元数据证明.