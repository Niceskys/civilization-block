---
description: 对《文明块》任务执行只读预检，确认模型声明、Git状态、范围、Skill和风险，不修改文件
agent: wmb-executor
---

对下面的任务执行严格的只读预检：

$ARGUMENTS

要求：

1. 阅读根目录`AGENTS.md`。
2. 加载`wmb-task-contract`。
3. 不修改任何文件。
4. 不创建任何文件。
5. 不执行安装、联网、提交、推送、切分支或清理命令。
6. 执行：
   - `git status --short`
   - `git branch --show-current`
   - `git rev-parse HEAD`
7. 检查任务是否包含：
   - 指定模型；
   - 任务编号；
   - 单一目标；
   - 必读文件；
   - 允许修改文件；
   - 禁止范围；
   - 验收标准；
   - 验证命令；
   - 失败停止条件。
8. 识别任务所需Skill。
9. 识别潜在连锁影响。
10. 判断是否可以进入实施阶段。

严格按以下格式输出：

```text
PREFLIGHT_RESULT=PASS/BLOCKED

TASK_DECLARED_MODEL=
MODEL_RUNTIME_VERIFICATION=USER_UI_REQUIRED
TASK_ID=
BRANCH=
BASE_COMMIT=
WORKTREE_CLEAN=YES/NO

REQUIRED_SKILLS
- ...

ALLOWED_FILES
- ...

FORBIDDEN_SCOPE
- ...

POTENTIAL_CONSEQUENCES
- ...

MISSING_FIELDS
- ...

BLOCKERS
- ...

IMPLEMENTATION_RECOMMENDATION
- 可以进入实施 / 不得进入实施
```

预检结束后停止，不执行任务正文。