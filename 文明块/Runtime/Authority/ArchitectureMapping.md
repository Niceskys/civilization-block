# 文明块 Runtime 权威架构对照

## 目标链路

`Client -> Command -> Simulation -> Event -> View`

UI 或 View 层只允许：

- 读取 `GameState` 的只读投影或订阅 `EventStream`。
- 构造 `CommandEnvelope` 并提交给 `Simulation.ExecuteCommand`。

UI 或 View 层禁止：

- 直接修改 `GameState`、`BuildingRuntimeState`、`ResourceState`。
- 绕过 `CommandBus` 调用系统内部方法。

## 新旧结构对照

| 原有散落规则 | 新模块 |
| --- | --- |
| Runtime 架构蓝图中的 Simulation Kernel | `Simulation.cs` |
| Command Bus 单一入口 | `CommandBus.cs`、`SimulationProtocol.cs` |
| Event Stream 单一输出 | `EventStream.cs`、`GameState.Events` |
| 稳定 namespace ID | `StableId.cs` |
| 建筑状态、施工任务、首建加速存档字段 | `GameState.cs` |
| 建筑放置、施工推进、取消施工、首建加速预约/释放/消耗 | `BuildingSystem.cs` |
| NPC岗位分配、原子换岗、解除与失效清理 | `WorkerAssignmentSystem.cs` |
| 确定性三维占用格展开、边界与重叠校验 | `SpatialOccupancy.cs` |
| 确定性支撑图、接触比例与整数载荷传播 | `StructuralSupport.cs` |
| 存档序列化与读档修复 | `SaveSystem.cs` |
| 建筑静态数值来源 | `DefinitionRegistry.cs` |
| 核心内容模块与统一系统装配 | `RuntimeComposition.cs` |
| 单机/服务器/远端统一会话入口 | `GameSession.cs` |
| 远端快照与事件游标同步 | `IGameSessionTransport`、`LoopbackGameSessionTransport` |
| 联机前协议与模组一致性校验 | `ModCompatibility.cs`、`ModRegistry.CreateCompatibilityProfile` |

`SpatialOccupancy`是无状态纯规则组件，只接受整数边界、放置快照和既有占用集合。施工任务与完工建筑共用该组件；UI、模组和网络适配层不得复制或改写碰撞算法。`BuildingSystem`已开放有界`Solid`矩形占地；`Attachment/Connector`与新承重图仍未开放。

`StructuralSupport`是唯一结构承重规则组件：最低接触比例50%，载荷使用1000倍整数单位，共同支撑按接触格比例和稳定ID分配。施工任务有重量但不能作为支持者。`BuildingSystem`与`StateDiagnostics`已切换到该组件，旧按层汇总承重算法已删除；UI、模组和网络层不得复制承重计算。

`RuntimeComposition`是正式单机和服务器的唯一核心装配入口，固定按建筑、岗位、持续生产、批次生产、物流顺序注册五个系统。业务启动代码不得自行调用`AddSystem`拼装另一套运行时。`DefinitionRegistry`注册时深复制可变定义，拒绝重复ID，并在组成模拟时严格校验引用后封存；模拟启动后，核心代码、模组和外部引用均不得修改定义。建筑成本、配方输入输出、持续生产输入输出、管道成本必须引用已注册资源，生产定义必须引用已注册建筑。`IDefinitionModule`是后续核心内容与模组内容的注册边界，模块ID和定义ID冲突必须明确失败，禁止静默覆盖。

`CoreContentDefinitionModule`正式注册4.1的16种资源、管道，以及首批逐栋迁移的房屋、农田、水井、树场。四种初始建筑使用已裁决的标准`1x1x1`占地、派生重量、20单位本地缓存及6.1施工/结构数值；树场应急炭化是首个正式核心配方。农田、水井、树场基础产出由独立`ContinuousProductionSystem`结算，不与手动批次混用。其余13类普通建筑仍需在Phase E逐栋确认后接入。

## Runtime 统一权威边界映射

本节把 `00.5 Runtime架构蓝图_v1.md` 的 `Runtime 统一权威边界` 映射到当前 Runtime 文档中的既有文件、类和职责。本节只做映射说明，不新增 Runtime 系统，不修改代码，不修改存档结构，不修改 StateHash 算法，不修改协议版本。

### Definition 映射

Definition 映射到 `DefinitionRegistry`、核心 Definition 模块、资源定义、建筑定义、配方定义、管道定义和后续 `IDefinitionModule` 注册边界。

Definition 进入 Runtime 后只读。`DefinitionRegistry` 注册时深复制可变定义，拒绝重复 ID，并在组成模拟时严格校验引用后封存。

Definition 不保存当前资源数量、建筑实例、NPC 状态、生产进度、物流任务或 UI 状态。

UI / View / 模组外部引用不得修改 Definition 来影响当前玩法结果；需要新增内容时必须通过受控 Definition 注册流程，不得静默覆盖既有定义。

### State 映射

State 映射到 `GameState` 及其子状态。

权威 State 包括资源、建筑、施工、NPC、岗位、批次生产、持续生产、物流、连接设施、结构状态、时间、难度、诊断所需状态、远端同步所需游标与快照一致性字段等已接入内容。

State 只能由 `Simulation` 通过 Command 执行或 Tick 推进修改。

UI、View、Diagnostics、日志、传输层和表现层不得直接修改权威 State；Diagnostics 只能读取 Runtime 结果并给出诊断，不得成为规则来源。

### View 映射

View 映射到只读投影、`EventStream`、`StateDiagnostics`、UI 消费端和远端隔离快照。

UI / View 可以读取 Runtime 输出、订阅事件、读取诊断结果，也可以构造 `CommandEnvelope` 并提交给命令入口。

UI 不得复制生产、库存、物流、承重、光照、燃料或消耗算法；这些结算必须由 Runtime 权威系统完成。

UI 不得维护第二套诊断优先级，不得解析自然语言 Message 作为规则来源；冲突类型、拒绝原因、诊断状态和同步结果应来自机器可读状态或事件字段。

### Command 映射

Command 映射到 `CommandBus`、`SimulationProtocol`、`CommandEnvelope` 与 `Simulation.ExecuteCommand`。

玩家操作和外部状态修改请求必须通过 Command，包括建造、取消、拆除、岗位分配、生产切换、物流、管道、时间控制以及未来联机输入。

Command 必须校验、原子执行，并返回结果与事件。失败 Command 不得留下部分权威 State 修改。

不得为某个系统建立第二套命令入口，不得绕过 `CommandBus` 或 `Simulation.ExecuteCommand` 直接调用系统内部方法修改 `GameState`。

### Event 映射

Event 映射到 `EventStream` 和 `GameState.Events`。

Event 是事实输出，不是修改入口。UI、日志、诊断、同步和测试可以读取 Event，但不得通过 Event 订阅直接修改权威 State。

如果 Event 触发后续玩法变化，必须回到 Command，或由 Simulation 内部明确规则在 Tick / Execute 流程中处理。

`CommandResult.Events`只包含该命令产生的事件；积压事件通过远端 `EventStream` 同步，不能混入命令结果。

### Simulation 映射

Simulation 映射到 `Simulation`、`ISimulationSystem` 和 `RuntimeComposition`。

`RuntimeComposition.CreateSimulation` 是 Local / Server 共同装配入口。单机和服务器必须复用同一个 `CreateSimulation`，不能拥有两套玩法实现。

系统只能在 Command 执行或 Tick 中修改 `GameState`。新增玩法系统时实现 `ISimulationSystem`，并在系统内注册自己的 `ICommandHandler`。

不得创建第二套 Runtime 入口、第二套资源库存、第二套事件系统，或与 `Simulation` / `RuntimeComposition` 平行的总管理器。

### Save 映射

Save 映射到 `SaveSystem` 和 `GameState.SaveVersion`。

Save 只保存权威 State，不保存纯 UI 状态。资源、建筑、施工、岗位、生产、物流、连接设施、垃圾、肥料、太阳灯运行时状态、时间、难度和必要同步字段等影响玩法结果的内容应纳入权威存档边界。

面板位置、hover、动画、音效、摄像机、纯表现状态和不影响玩法结果的本地偏好不得混入权威存档 State。

新增 State 字段必须评估 `SaveVersion`、迁移规则和兼容测试。本次只补强映射边界，不修改存档结构。

### StateHash 映射

StateHash 映射到当前 StateHash / 快照一致性检查机制。

StateHash 只覆盖影响玩法结果的权威状态，包括资源、建筑、施工、岗位、生产、物流、连接设施、太阳灯燃料覆盖、时间、难度、结构状态和同步一致性所需字段。

StateHash 不覆盖 UI 面板、hover、动画、音效、摄像机、粒子、纯表现状态或不影响玩法结果的本地偏好。

本次只补强 StateHash 映射边界，不修改 StateHash 算法。

### ServerAuthority 映射

ServerAuthority 映射到 `LocalGameSession`、`ServerGameSession`、`RemoteGameSession`、`IGameSessionTransport`、`LoopbackGameSessionTransport` 和 snapshot sync。

Local 模式中，本地 `Simulation` 是权威。

Server 模式中，服务器 `Simulation` 是权威，负责校验授权、执行 Command、推进 Tick、持有权威 State，并输出 Event / Snapshot / StateHash。

Remote 模式中，客户端只提交 Command、接收 Event / Snapshot / StateHash，不推进权威 Simulation，不直接写权威 State。

单机与服务器必须共用 `Simulation / RuntimeComposition` 权威规则。不得创建第二套服务器专用资源库存、建筑系统、事件系统或总管理器。

## 会话权威边界

| 模式 | 执行命令 | 推进模拟 | 持有权威状态 |
| --- | --- | --- | --- |
| `Local` | 本地`Simulation` | 是 | 是 |
| `Server` | 校验玩家授权后交给同一`Simulation` | 是 | 是 |
| `Remote` | 经`IGameSessionTransport`提交 | 否，只同步 | 否，仅持有隔离快照 |

- 单机和服务器不得拥有两套玩法实现；二者只在会话权限与传输方式上不同。
- 单机使用`RuntimeComposition.CreateLocalSession`，服务器使用`RuntimeComposition.CreateServerSession`；两者必须复用同一个`CreateSimulation`。
- 远端调用`Tick`只拉取服务器快照和新事件，不得推进服务器`SimulationTick`。
- 传输快照必须与服务器对象隔离；客户端误改本地快照不得污染权威状态。
- 事件使用单调游标同步，同一游标后的事件按服务器顺序发送，已确认事件不得重放。
- `CommandResult.Events`只包含该命令产生的事件；积压事件通过远端`EventStream`同步，不能混入命令结果。
- `ServerGameSession`只接受已授权玩家的命令；撤销授权后，命令不得修改状态或进入命令历史。

`LoopbackGameSessionTransport`用于在没有真实网络层时验证完整协议。未来网络传输实现`IGameSessionTransport`即可替换，不得绕过`ServerGameSession`直接调用玩法系统。

## 联机兼容握手

- 每条远程连接必须先调用`Handshake`；握手通过前禁止同步快照或提交命令。
- 服务器和客户端必须使用完全相同的协议版本、游戏版本、模组ID、模组版本、校验和、加载顺序与依赖声明。
- `CompatibilityIssueCode`为稳定的机器可读分类；界面和日志应按分类显示具体不一致项，不能只报告“连接失败”。
- `ModRegistry`在注册manifest时深复制可变集合，并在建立连接档案时再次生成只读快照，外部后续修改不得改变握手结果。
- 握手失败后传输状态为`Disconnected`，不得接触服务器快照或命令执行入口。

## 状态哈希与快照修复

- 远端在同步或提交命令时上报当前`StateHash`；命令路径必须与执行命令前的服务器状态比较，不能把命令造成的合法变化误判为漂移。
- 服务器响应携带权威快照哈希，并明确标记客户端状态是否需要修复。
- 远端收到快照后必须重新计算哈希；若传输内容与声明哈希不一致，只允许额外请求一次权威快照。
- 一次修复后仍不一致时抛出`StateSynchronizationException`，不得继续使用无法验证的状态。
- `LastSynchronizationRepaired`和`StateRepairCount`用于日志、诊断与未来断线重连策略，不参与玩法规则。

## 最小断线重连

- 传输只有在`Disconnected`状态下才能进入`Reconnecting`并重新执行兼容握手；已连接会话不得重复重连。
- 重连沿用断线前的事件游标，只补发游标之后的事件；重连完成后的普通同步不得重放这些事件。
- 重连上报断线前快照哈希，服务器状态已变化时复用权威快照修复流程。
- 玩家最后接受的命令序列保存在服务器`GameState`中；重连不得清空、回退或另建客户端权威序列。
- 当前切片不缓存离线命令，也不实现公网传输、超时退避或自动重试；这些由未来真实网络适配层负责。

## 存档兼容策略

- `GameState.SaveVersion` 当前为2.9；1.0至2.8存档加载后迁移为三维放置schema 1、默认普通难度，并为旧建筑补充正常结构状态、空的实际支付成本快照、NPC岗位、批次生产、持续生产、物流、连接设施、共享容量、垃圾、肥料与太阳灯运行时状态。
- 实例和施工任务保存锚点、基础层、旋转、放置尺寸与schema快照；旧定义与旧Plot默认保持`1x1x1`。
- 旧`PlotId + Layer`从Plot坐标生成锚点；缺失地皮、未知schema、非法旋转、越界或重叠占地必须明确拒绝。
- 建筑完工复制施工任务的放置快照，不能根据可能已变化的Definition重新推导。
- 已消耗的首建加速字段以 `Consumed` 为最终权威。
- 读档时如果发现“已预约但施工任务不存在”，且未消耗，则自动恢复为未预约。
- 所有持久对象使用 `namespace:type:id` 字符串 ID，不依赖场景路径、内存引用或数字自增外泄。

建造、拆除、岗位、生产、物流和连接设施命令与拒绝结果的会话协议当前为21；空间与结构错误码、拆除退款明细、岗位、持续/批次生产、运输、管道生命周期、NPC、垃圾、肥料和太阳灯燃料覆盖状态必须从服务器经传输层原样到达远端，客户端不得自行修改权威状态、重新计算产出或解析自然语言原因来判断冲突类型。

`GameTimeDifficulty.cs`定义权威Tick、暂停/倍速换算、昼夜阶段纯派生规则及结构事故难度策略。`DayNightCycle`只根据`SimulationTick`派生白天/夜晚，不保存第二套昼夜状态；当前仅由农田光照门控读取以裁决夜晚停产，不直接驱动NPC、怪物或UI行为。难度配置进入`GameState`、存档和StateHash；服务器推进Tick，客户端墙钟不参与宽限或崩塌裁决。

`BuildingSystem`拥有拆除与结构事故状态机。建筑结构状态只允许`normal`、`grace`、`disabled`；截止Tick和下一次崩塌Tick进入存档与StateHash。失稳建筑继续占用空间，但不提供承重；补建支撑完工后统一重算并恢复。自动崩塌按最高占用层降序、同层稳定ID升序执行，客户端只能消费事件并显示倒计时。

施工任务的`PaidBuildCost`保存实际扣除资源，完工时原样复制到建筑实例。正常拆除按该快照计算75%向下取整返还，并在事件中区分应返还、实际入库和容量溢出；坍塌不退款。`BuildingOperationalRules`是工人分配、生产和库存转运判断建筑能否运行的统一入口；这些系统接入后不得自行解释结构状态。

`WorkerAssignmentSystem`拥有岗位唯一权威状态。NPC分配到建筑定义声明的稳定槽位，空槽按编号升序选择；换岗在同一命令内先解除旧岗位再建立新岗位。死亡、永久离场、昏迷及建筑拆除、失稳、停用、摧毁会确定性解除岗位；睡眠、距离和不可取消任务锁不删除已有岗位，其中任务锁只阻止新分配或换岗。职业适配、工作行为和产出尚未在本切片实现。

`ProductionSystem`拥有最小生产批次权威状态。配方、单批/连续模式、输入锁、工作量、单批产物缓冲和建筑局部库存进入存档与StateHash。输入按全局库存后建筑本地库存的顺序原子锁定，完成前不扣除；有效岗位按每名工人每Tick一点工作量推进。工人或建筑条件失效时保留进度和锁，取消时完整解锁。产物先放建筑本地库存，再放全局库存，只有整批可容纳时才转移；活动批次、待转移产物或非空本地库存阻止普通拆除。职业倍率、物流耗时、多生产槽和表现动画不属于本切片。

`ContinuousProductionSystem`拥有农田、水井和树场的基础持续产出。进度使用整数分子累计，日产量不会因浮点或Tick切分漂移；农田每个有效运行日预付1水并保存剩余覆盖Tick，停工不消耗覆盖；农田光照裁决会读取昼夜、遮挡和太阳灯完整覆盖，太阳灯被使用时按1燃料/游戏日预付燃料覆盖Tick并写入`GameState.Sunlamps`。整数产物先进入本地库存再进入全局库存；满仓时最多保留1单位待转移并停止推进，待转移产物阻止普通拆除。进度、灌溉覆盖、待转移量、光照暂停状态和太阳灯覆盖状态进入存档2.9与StateHash，服务器是唯一结算方。

`LogisticsSystem`拥有显式运输任务与正式管道连接设施。管道先经过施工生命周期，完工后生成仅允许“下层到上层”、仅允许所选资源的权威路线；每根管道最多维持一个自动运输任务，并按配置批量复用源锁和目标预约机制。临时失稳或停用会暂停施工、取消在途任务并保留管道，端点实际摧毁会清理施工、管道、路线及锁预约。路线、连接设施、施工、任务、锁、预约和序列进入存档1.9与StateHash；命令由协议10同步。当前仍未实现道路寻路、搬运工、复杂优先级、吞吐升级和表现动画。

## 扩展方式

新增玩法系统时实现 `ISimulationSystem`，并在系统内注册自己的 `ICommandHandler`。系统只能在 `Execute` 或 `Tick` 中修改 `GameState`，且必须返回 `GameEvent` 通知外部。
