---
description: 调用只读Reviewer审查《文明块》当前差异，检查越界、架构、规则、测试和证据
agent: wmb-reviewer
subtask: true
---

对当前工作区差异执行独立只读审查。

任务计划书或审查要求：

$ARGUMENTS

必须：

1. 阅读根目录`AGENTS.md`。
2. 加载：
   - `wmb-task-contract`
   - `wmb-validation-evidence`
3. 根据修改内容加载：
   - `wmb-runtime-authority`
   - 或`wmb-document-governance`
4. 不修改任何文件。
5. 不自动修复。
6. 不提交或推送。
7. 阅读当前`git diff`和任务回执。
8. 对照允许文件、禁止文件和验收标准。
9. 检查：
   - 范围越界；
   - 架构漂移；
   - 权威规则错误；
   - 边界条件遗漏；
   - 测试不足；
   - 回归风险；
   - 证据与声明不一致；
   - 任务外文件；
   - 新增依赖；
   - 密钥或缓存污染。
10. 缺少任务书或测试证据时写`REVIEW_INCOMPLETE`，不得猜测通过。

输出格式：

```text
REVIEW_RESULT=PASS/PASS_WITH_WARNINGS/REQUEST_CHANGES/REVIEW_INCOMPLETE

CRITICAL
- ...

HIGH
- ...

MEDIUM
- ...

LOW
- ...

TASK_SCOPE
- ...

ARCHITECTURE
- ...

RULE_AUTHORITY
- ...

TEST_COVERAGE
- ...

EVIDENCE
- ...

UNVERIFIED
- ...

FINAL_RECOMMENDATION
- ...
```

没有问题的分类写“无”。

审查结束后停止，不替执行者继续修改。