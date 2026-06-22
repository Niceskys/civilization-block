---
name: wmb-validation-evidence
description: 完成《文明块》代码或文档任务时使用，规范真实测试、退出码、Git差异、事实标签、失败处理和可审计回执
compatibility: opencode
metadata:
  project: civilization-block
  category: validation
  language: zh-CN
---

# 《文明块》验证与证据Skill

## 适用范围

所有产生文件修改的任务必须加载。

只读审查也可加载，用于判断证据是否完整。

---

## 一、验证原则

验证必须来自真实命令输出。

禁止：

- 写“理论上通过”；
- 写“应该没有问题”；
- 伪造退出码；
- 根据代码外观声称测试已通过；
- 省略失败输出；
- 只运行最小测试却声称全项目通过；
- 删除测试；
- 降低断言；
- 修改无关生产代码掩盖失败。

---

## 二、命令记录

每个验证步骤必须记录：

```text
COMMAND=<实际命令>
EXIT_CODE=<退出码>
RESULT=PASS/FAIL/BLOCKED
KEY_OUTPUT=<关键输出>
```

工具没有提供退出码时明确写：

```text
EXIT_CODE=UNAVAILABLE
```

不得猜测为0。

---

## 三、基础Git证据

修改任务完成后运行：

```powershell
git diff --name-status
git diff --stat
git diff --check
git status --short
```

任务书要求时运行：

```powershell
git diff --unified=5 -- <指定文件>
```

必须确认：

- 修改文件全部在允许范围；
- 没有意外新增文件；
- 没有意外删除文件；
- 没有尾随空格错误；
- 没有未说明的二进制文件；
- 没有密钥；
- 没有构建缓存；
- 没有包缓存。

---

## 四、任务测试层级

### 局部验证

针对本次修改的最小直接测试。

### 系统验证

针对被修改系统的完整测试。

### 项目验证

仓库提供时运行全项目验证。

不得用局部验证替代任务书要求的项目验证。

---

## 五、测试覆盖

代码任务至少考虑：

- 成功路径；
- 失败路径；
- 边界条件；
- 重复调用；
- 空值或非法输入；
- 不变量；
- 回归风险。

仅在任务适用时考虑：

- 保存加载；
- 确定性；
- StateHash；
- 并发；
- 网络；
- 模组；
- 迁移。

不要为了凑数量编写与目标无关的测试。

---

## 六、失败处理

同一原因失败后：

1. 阅读真实错误；
2. 判断是否在任务范围内；
3. 只允许一次基于证据的修正重试；
4. 再次失败则停止；
5. 保留现场；
6. 输出`BLOCKED`。

禁止：

- 原样无限重试；
- 随机改代码；
- 安装包；
- 换技术方案；
- 修改任务外文件；
- 降低验证标准；
- 清理用户工作区。

---

## 七、事实分类

### FACT

必须有文件内容或命令输出支持。

### CHANGE

必须能在当前Git差异中看到。

### ASSUMPTION

未被直接验证的推断。

### BLOCKED

明确说明阻塞原因、最后命令和现场状态。

### PLAN

只表示后续建议，不代表已执行。

---

## 八、结果等级

### PASS

所有必需验收和验证通过。

### PASS_WITH_WARNINGS

必需验收通过，但存在不阻塞的明确风险。

### PARTIAL

完成部分目标，但不满足完整验收。

### FAIL

实施结果错误或验证失败。

### BLOCKED

因权限、范围、环境、冲突或必要信息缺失而停止。

不得把PARTIAL或BLOCKED描述成完成。

---

## 九、标准回执

```text
FACT

- 当前分支：
- 基线提交：
- 工作区初始状态：
- 任务声明模型：
- 模型运行身份验证：
- 实际工具：

ROOT_CAUSE

- ...

CHANGE

- 文件：
  - 修改：

CONSEQUENCE_CHECK

- 是否改变玩法：
- 是否改变Runtime：
- 是否改变存档：
- 是否改变网络：
- 是否改变模组：
- 是否新增依赖：
- 是否新增文件：
- 是否修改任务外文件：
- 是否提交：
- 是否推送：

VALIDATION

- COMMAND:
- EXIT_CODE:
- RESULT:
- KEY_OUTPUT:

GIT_EVIDENCE

- git diff --name-status:
- git diff --stat:
- git diff --check:
- git status --short:

ASSUMPTION

- ...

BLOCKED

- ...

PLAN

- 等待核心设计者复核，不继续下一任务。
```

没有内容的分类写“无”，不得删除分类。