---
description: 收集《文明块》当前任务的Git差异、验证状态和FACT/CHANGE证据，不修改文件
agent: wmb-executor
---

为当前任务收集完整证据。

任务信息：

$ARGUMENTS

要求：

1. 加载`wmb-validation-evidence`。
2. 全程只读。
3. 不修改文件。
4. 不运行自动修复。
5. 不提交、不推送、不切分支。
6. 执行：
   - `git branch --show-current`
   - `git rev-parse HEAD`
   - `git diff --name-status`
   - `git diff --stat`
   - `git diff --check`
   - `git status --short`
7. 根据当前会话中的真实命令输出整理验证证据。
8. 没有实际执行记录的命令必须标记为`UNVERIFIED`。
9. 不得根据代码外观声称测试通过。
10. 检查任务外文件、密钥、缓存、构建产物和意外删除。

输出：

```text
FACT

- 当前分支：
- 基线提交：
- 当前工作区：
- 任务声明模型：
- 模型运行身份验证：USER_UI_REQUIRED

CHANGE

- 修改文件：
- 新增文件：
- 删除文件：
- 重命名文件：

VALIDATION

- 已真实执行：
- 未验证：
- 失败：

GIT_EVIDENCE

git diff --name-status:
...

git diff --stat:
...

git diff --check:
...

git status --short:
...

SCOPE_VIOLATIONS

- ...

ASSUMPTION

- ...

BLOCKED

- ...

PLAN

- 等待核心设计者复核，不继续下一任务。
```

收集完成后停止。