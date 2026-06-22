---
name: wmb-runtime-authority
description: 修改《文明块》C# Runtime、测试、系统Tick、Command、Event、Definition、State或View时使用，保护唯一权威状态和未来单机服务器模组边界
compatibility: opencode
metadata:
  project: civilization-block
  category: runtime-architecture
  language: zh-CN
---

# 《文明块》Runtime权威Skill

## 适用范围

涉及以下任一内容时必须加载：

- `文明块/Runtime/`
- `文明块/Runtime.Tests/`
- Command；
- Event；
- Simulation；
- GameState；
- Definition；
- ViewState；
- Tick；
- Save；
- Session；
- Mod；
- Server；
- 三维占地；
- NPC行为；
- 生产、库存、建筑和运输系统。

---

## 一、必读材料

按任务需要读取：

- `文明块/00 - 项目总纲/00.5 Runtime架构蓝图_v1.md`
- `文明块/00 - 项目总纲/00.9 DeepSeek代码协作规范_v1.md`
- 当前任务相关生产代码；
- 当前任务相关测试；
- 涉及三维建筑时：
  - `文明块/00 - 项目总纲/00.24 三维占地与房间模块迁移设计_v1.md`

不得只根据文件名猜测架构。

---

## 二、权威原则

### 唯一状态权威

运行状态必须只有一套正式权威。

不得创建：

- 第二套资源库存；
- 第二套建筑注册表；
- 第二套NPC状态；
- 第二套时间状态；
- 第二套事件流；
- 第二套总入口；
- 用于“临时方便”的平行状态。

缓存和View必须可从权威状态重新生成。

### Command入口

玩家或外部请求修改状态时，应遵守正式命令链路：

```text
CommandEnvelope
→ CommandBus
→ Validate
→ Execute
→ GameEvent[]
→ EventStream
```

不得由：

- UI；
- 网络适配器；
- 调试面板；
- 模组；
- 临时脚本

直接修改权威状态。

### System Tick

系统Tick可以按照正式顺序推进状态，但不得擅自改变：

- Tick顺序；
- 系统注册顺序；
- 随机数来源；
- 时间推进语义；
- 同帧结算顺序。

---

## 三、Definition、State、View分离

### Definition

静态定义，例如：

- 建筑成本；
- 资源属性；
- 配方；
- 图标ID；
- 占地尺寸；
- 解锁条件。

### State

运行状态，例如：

- 当前库存；
- 建筑实例；
- NPC状态；
- 施工进度；
- 当前时间；
- 当前任务进度。

### View

表现和读取结果，例如：

- UI显示；
- 模型状态；
- 高亮；
- 提示；
- 诊断摘要；
- 当前选中对象。

禁止把UI状态写入权威State。

禁止让View反向成为规则源。

---

## 四、稳定ID

持久ID必须符合项目现有稳定格式。

示例：

```text
building:core:farm
item:core:wood
command:core:000001
event:core:000001
```

不得使用：

- 中文显示名称；
- 文件路径；
- 裸自增数字；
- 随机字符串；
- 内存地址；
- 当前时间戳

作为长期持久ID。

不得擅自迁移已有ID。

---

## 五、装配与入口

正式单机、服务器和测试装配应遵守项目现有装配规则。

不得：

- 新建`GameManager`；
- 新建`WorldManager`；
- 新建`GlobalManager`；
- 创建新的平行Simulation；
- 在正式路径中绕过`RuntimeComposition`；
- 为单机和服务器分别复制玩法逻辑。

专项单元测试可以按现有测试风格构造最小对象，但不能反向改变正式装配。

---

## 六、存档与兼容性

除非任务书明确授权，不得：

- 修改存档字段；
- 改变序列化名称；
- 删除旧字段；
- 改变默认修复逻辑；
- 改变版本号；
- 新增迁移；
- 改变StateHash；
- 改变网络握手；
- 改变模组兼容协议。

发现必须调整存档时，停止并请求独立架构任务。

---

## 七、代码风格

遵循现有代码，而不是引入个人偏好。

默认要求：

- 不新增第三方包；
- 不使用反射；
- 不使用动态代码生成；
- 不引入全局单例；
- 不引入无必要异步；
- 不引入无必要线程；
- 不添加大而泛化的抽象层；
- 参数进行合法性检查；
- 失败返回明确原因；
- 复杂规则写简短解释；
- 不为每行添加废话注释；
- 不大范围格式化。

---

## 八、测试要求

Runtime任务默认运行：

```powershell
powershell -ExecutionPolicy Bypass -File tools\test-runtime.ps1
```

任务书提供其他命令时一并执行。

测试至少检查：

- 成功路径；
- 失败路径；
- 边界条件；
- 状态不变量；
- 重复调用；
- 适用时的保存加载；
- 适用时的确定性；
- 适用时的StateHash一致性。

不得通过修改测试预期来接受错误行为，除非任务本身就是正式规则变更。

---

## 九、Runtime高风险文件

修改以下内容必须由任务书逐个点名：

- `GameState.cs`
- `Simulation.cs`
- `CommandBus.cs`
- `EventStream.cs`
- `SaveSystem.cs`
- `StableId.cs`
- `ModRegistry.cs`
- Runtime正式装配入口
- 网络和Session权威代码

没有逐个授权时，读取可以，修改不可以。

---

## 十、完成前检查

逐项回答：

```text
是否新增第二权威状态：
是否绕过CommandBus：
是否混合Definition/State/View：
是否改变Tick顺序：
是否改变存档：
是否改变网络协议：
是否改变模组协议：
是否新增依赖：
是否新增正式入口：
是否修改任务外Runtime文件：
```

任一项为“是”且任务书未明确授权时，任务失败。