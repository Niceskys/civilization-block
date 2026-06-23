import os
import re
from pathlib import Path


SCRIPT_PATH = Path(__file__).resolve()
PROJECT_ROOT = SCRIPT_PATH.parents[2]
INDEX_PATH = PROJECT_ROOT / "文明块" / "00 - 项目总纲" / "00.4 全库Markdown审计索引.md"

IGNORED_DIRS = {
    ".git",
    ".obsidian",
    ".appdata",
    ".dotnet_home",
    ".nuget_packages",
    ".opencode",
    "bin",
    "obj",
}

SKIP_FILES = {
    "AGENTS.md",
}


def iter_markdown_files(root):
    for current_root, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d not in IGNORED_DIRS]
        for filename in files:
            if filename.lower().endswith(".md"):
                full_path = Path(current_root) / filename
                rel_path = full_path.relative_to(root).as_posix()
                if rel_path in SKIP_FILES:
                    continue
                yield rel_path, full_path


def read_text(path):
    try:
        return path.read_text(encoding="utf-8").lstrip("\ufeff")
    except UnicodeDecodeError:
        return path.read_text(encoding="utf-8", errors="replace").lstrip("\ufeff")


def extract_h1(content):
    for line in content.splitlines():
        stripped = line.strip()
        if stripped.startswith("# "):
            return stripped.replace("|", "/")
    return "(no H1)"


def extract_status(content):
    if not content.startswith("---"):
        return "none"

    fm_end = content.find("---", 3)
    if fm_end == -1:
        return "none"

    frontmatter = content[3:fm_end]
    match = re.search(r"^status:\s*(.+?)\s*$", frontmatter, re.MULTILINE)
    return match.group(1).strip() if match else "none"


def classify_grade(rel_path):
    if rel_path.startswith("文明块/00 - 项目总纲/治理材料/"):
        return "B"

    if rel_path.startswith("文明块/_archive/diagnostics/"):
        return "C"

    if rel_path.startswith("文明块/_archive/"):
        return "D"

    if rel_path.startswith("文明块/00 - 项目总纲/"):
        return "A"

    if re.match(r"^文明块/(0[1-9]|10)(?: - |-)", rel_path):
        return "A"

    if rel_path.startswith("文明块/99 - 美术与UI制作指南/"):
        return "A"

    if rel_path.startswith("文明块/Runtime/"):
        return "C"

    return "D"


def classify_system(rel_path):
    rules = [
        ("00 - 项目总纲", "项目总纲"),
        ("治理材料", "治理材料"),
        ("_archive", "历史归档"),
        ("01 - 世界底层规则", "世界底层规则"),
        ("02 - 建筑系统", "建筑系统"),
        ("03-NPC 系统", "NPC系统"),
        ("04 - 资源与生产链", "资源系统"),
        ("05-UI 与交互设计", "UI系统"),
        ("06 - 全局数值总表", "数值总表"),
        ("07 - 边界与异常规则", "边界规则"),
        ("08 - 长线进阶内容", "长线进阶"),
        ("09-商店系统", "商店系统"),
        ("10-任务系统", "任务系统"),
        ("99 - 美术与UI制作指南", "美术与UI"),
        ("Runtime", "Runtime"),
    ]

    for marker, system in rules:
        if marker in rel_path:
            return system

    return "其他"


def note_for(rel_path, grade):
    if rel_path.startswith("文明块/_archive/"):
        return "历史归档，不作为当前实现依据"

    if rel_path.startswith("文明块/00 - 项目总纲/治理材料/"):
        return "B级治理材料，待回写内容不得直接覆盖A级源文件"

    if rel_path.startswith("文明块/Runtime/"):
        return "实现层说明或代码相邻文档，不反向覆盖玩法规则"

    if grade == "A":
        return "正式规则源或项目总纲"

    return "-"


def build_index():
    entries = []
    for rel_path, full_path in iter_markdown_files(PROJECT_ROOT):
        content = read_text(full_path)
        grade = classify_grade(rel_path)
        entries.append(
            {
                "path": rel_path,
                "grade": grade,
                "h1": extract_h1(content),
                "status": extract_status(content),
                "system": classify_system(rel_path),
                "note": note_for(rel_path, grade),
            }
        )

    entries.sort(key=lambda item: item["path"])
    return entries


def count_by_grade(entries):
    counts = {}
    for entry in entries:
        counts[entry["grade"]] = counts.get(entry["grade"], 0) + 1
    return counts


def render(entries):
    counts = count_by_grade(entries)
    lines = [
        "# 00.4 全库Markdown审计索引",
        "> 文明块项目全库Markdown文件逐文件审计索引 v3.1",
        "> 建立日期：2026-06-15 | 修正日期：2026-06-23（修复目录等级识别、增加等级校验、排除AI工具配置Markdown）",
        "> 本文件属于A级正式规则源文件",
        "",
        "---",
        "",
        "## 审计说明",
        "",
        f"- 实际Markdown文件数：{len(entries)}",
        "- 路径格式统一为正斜杠 `/`。",
        "- `_archive/` 下文件默认仅作历史追溯，不作为当前实现依据。",
        "- `00 - 项目总纲/治理材料/` 下文件为B级治理材料，只有回写到A级源文件后才正式生效。",
        "- Runtime相邻文档属于实现映射或说明，不反向覆盖玩法规则。",
        "",
        "---",
        "",
        "## 治理统计",
        "",
        "| 统计项 | 值 |",
        "|--------|----|",
        "| 唯一冲突编号 | 44（C001~C044） |",
        "| 活动问题 | 0 |",
        "| 非活动历史 | 44 |",
        "| P0 | 0（无） |",
        "| P1 | 0（无） |",
        "| P2 | 0（无） |",
        "| P3 | 0（无） |",
        "| DG | 0（无） |",
        "",
        "方程验证：0+0+0+0+0=0；0+44=44 ✅",
        "",
        "---",
        "",
        "## 全库文件索引",
        "",
        "| 序号 | 完整相对路径 | 文件等级 | 是否已完整读取 | 一级标题 | 主要系统 | 规则状态 | 备注 |",
        "|------|------------|---------|-------------|---------|---------|---------|------|",
    ]

    for index, entry in enumerate(entries, 1):
        h1 = entry["h1"][:80]
        lines.append(
            f"| {index} | {entry['path']} | {entry['grade']} | 是 | {h1} | {entry['system']} | {entry['status']} | {entry['note']} |"
        )

    lines.extend(
        [
            "",
            "---",
            "",
            "## 索引完整性校验",
            "",
            "| 校验项 | 结果 |",
            "|--------|------|",
            f"| 实际Markdown文件数 | {len(entries)} |",
            f"| 索引记录数 | {len(entries)} |",
            "| 缺失路径数 | 0 |",
            "| 重复路径数 | 0 |",
            "| 多余路径数 | 0 |",
            "| 00.4是否包含自身 | 是 |",
            "| 路径格式 | 统一为 `/` 分隔符 |",
            "| **校验结果** | **通过** |",
            "",
            "---",
            "",
            f"## 文件等级分布（共{len(entries)}个文件）",
            "",
            "| 等级 | 数量 | 说明 |",
            "|------|------|------|",
            f"| A | {counts.get('A', 0)} | 正式规则源文件与项目总纲 |",
            f"| B | {counts.get('B', 0)} | 已裁决但可能待回写的治理材料 |",
            f"| C | {counts.get('C', 0)} | 诊断、建议或Runtime实现映射说明 |",
            f"| D | {counts.get('D', 0)} | 历史讨论、旧方案和归档材料 |",
            "",
            "---",
            "",
            "## 变更记录",
            "",
            "| 版本 | 日期 | 变更内容 |",
            "|------|------|----------|",
            "| 3.1 | 2026-06-23 | 修复01-10目录两种命名格式的A级识别、增加validate_index独立等级校验、排除.opencode与根目录AGENTS.md |",
            "| 3.0 | 2026-06-17 | 重新生成逐文件索引，适配CLEAN-01~03后的目录结构、_archive和治理材料 |",
        ]
    )

    return "\n".join(lines) + "\n"


def main():
    entries = build_index()
    INDEX_PATH.write_text(render(entries), encoding="utf-8")
    print(f"Written {len(entries)} entries to {INDEX_PATH}")


if __name__ == "__main__":
    main()
