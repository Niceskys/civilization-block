---
description: 按任务书验证《文明块》当前修改，只运行授权测试并报告结果，不自动修复
agent: wmb-executor
---

验证当前任务修改。

任务或验证范围：

$ARGUMENTS

要求：

1. 阅读根目录`AGENTS.md`。
2. 加载`wmb-validation-evidence`。
3. 根据任务类型加载必要的Runtime或文档治理Skill。
4. 不修改任何文件。
5. 不自动修复失败。
6. 不安装依赖。
7. 不联网。
8. 不提交或推送。
9. 只运行任务书明确要求的验证命令。
10. 命令未明确时，不自行猜测破坏性命令；输出`BLOCKED`。
11. 每条命令记录实际命令、退出码和关键输出。
12. 最后执行：
    - `git diff --name-status`
    - `git diff --stat`
    - `git diff --check`
    - `git status --short`
13. 检查是否出现任务外文件。

输出格式：

```text
VERIFY_RESULT=PASS/FAIL/BLOCKED

VALIDATION_COMMANDS

1.
COMMAND=
EXIT_CODE=
RESULT=
KEY_OUTPUT=

2.
COMMAND=
EXIT_CODE=
RESULT=
KEY_OUTPUT=

SCOPE_CHECK

- 任务外文件：
- 意外新增：
- 意外删除：
- diff检查：

GIT_EVIDENCE

git diff --name-status:
...

git diff --stat:
...

git diff --check:
...

git status --short:
...

BLOCKED
- ...

PLAN
- 等待核心设计者复核，不继续下一任务。
```

验证结束后停止，不进行修复。