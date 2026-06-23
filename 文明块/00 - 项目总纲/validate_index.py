import os
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


def iter_actual_markdown(root):
    """Walk the repository and return a set of relative paths to all .md files."""
    paths = set()
    for current_root, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d not in IGNORED_DIRS]
        for filename in files:
            if filename.lower().endswith(".md"):
                full_path = Path(current_root) / filename
                rel_path = full_path.relative_to(root).as_posix()
                if rel_path in SKIP_FILES:
                    continue
                paths.add(rel_path)
    return paths


def read_index_entries():
    """Parse the index table and return a list of (path, grade) tuples."""
    if not INDEX_PATH.exists():
        print(f"ERROR: Index file not found: {INDEX_PATH}")
        print("Please run generate_index.py first to create the index.")
        raise SystemExit(1)

    entries = []
    in_table = False
    for line in INDEX_PATH.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        if stripped.startswith("| 序号 | 完整相对路径"):
            in_table = True
            continue

        if in_table and (not stripped.startswith("|") or stripped.startswith("---")):
            break

        if not in_table or "------" in stripped:
            continue

        parts = [part.strip() for part in stripped.split("|")]
        if len(parts) < 5:
            continue

        seq = parts[1]
        rel_path = parts[2].replace("\\", "/")
        grade = parts[3]
        if seq.isdigit() and rel_path.endswith(".md"):
            entries.append((rel_path, grade))

    if not in_table:
        print('ERROR: Could not find table header "| 序号 | 完整相对路径" in index file.')
        print("The index file may be malformed or the header format has changed.")
        raise SystemExit(1)

    return entries


def find_duplicate_paths(paths):
    """Return a set of paths that appear more than once in the given list."""
    seen = set()
    duplicates = set()
    for path in paths:
        if path in seen:
            duplicates.add(path)
        seen.add(path)
    return duplicates


A_GRADE_SYSTEM_DIRS = {
    "01 - 世界底层规则",
    "02 - 建筑系统",
    "03-NPC 系统",
    "04 - 资源与生产链",
    "05-UI 与交互设计",
    "06 - 全局数值总表",
    "07 - 边界与异常规则",
    "08 - 长线进阶内容",
    "09-商店系统",
    "10-任务系统",
    "99 - 美术与UI制作指南",
}


def classify_grade_expected(rel_path):
    """Independently determine the expected grade for a markdown file.

    Uses an exact whitelist of system directory names rather than a regex,
    so the validator does not share the generator's classification bugs.
    """
    if rel_path.startswith("文明块/00 - 项目总纲/治理材料/"):
        return "B"
    if rel_path.startswith("文明块/_archive/diagnostics/"):
        return "C"
    if rel_path.startswith("文明块/_archive/"):
        return "D"
    if rel_path.startswith("文明块/00 - 项目总纲/"):
        return "A"

    # Extract the first directory segment after 文明块/
    if rel_path.startswith("文明块/"):
        rest = rel_path[len("文明块/"):]
        first_segment = rest.split("/")[0] if "/" in rest else rest
        if first_segment in A_GRADE_SYSTEM_DIRS:
            return "A"

    if rel_path.startswith("文明块/Runtime/"):
        return "C"
    return "D"


def check_status_for_building_details():
    """Check that all files in 单个建筑详情 have a status field in frontmatter."""
    building_dir = PROJECT_ROOT / "文明块" / "02 - 建筑系统" / "单个建筑详情"
    if not building_dir.exists():
        return []

    missing = []
    for path in sorted(building_dir.glob("*.md")):
        content = path.read_text(encoding="utf-8", errors="replace").lstrip("\ufeff")
        if not content.startswith("---") or "status:" not in content[:500]:
            missing.append(path.name)
    return missing


def main():
    """Validate the index against actual markdown files in the repository."""
    VALID_GRADES = {"A", "B", "C", "D"}

    actual = iter_actual_markdown(PROJECT_ROOT)
    indexed_entries = read_index_entries()
    indexed_paths = [entry[0] for entry in indexed_entries]
    indexed_set = set(indexed_paths)
    duplicates = find_duplicate_paths(indexed_paths)

    missing = actual - indexed_set
    extra = indexed_set - actual
    self_path = "文明块/00 - 项目总纲/00.4 全库Markdown审计索引.md"
    self_in = self_path in indexed_set

    print(f"ACTUAL_MD_FILES={len(actual)}")
    print(f"INDEXED_PATHS={len(indexed_entries)}")
    print(f"MISSING={len(missing)} extra={len(extra)} duplicate={len(duplicates)}")

    if missing:
        for path in sorted(missing):
            print(f"  MISSING: {path}")

    if extra:
        for path in sorted(extra):
            print(f"  EXTRA: {path}")

    if duplicates:
        for path in sorted(duplicates):
            print(f"  DUPLICATE: {path}")

    print(f"00.4_INCLUDES_SELF={self_in}")

    no_status = check_status_for_building_details()
    print("\nBUILDING_DETAIL_STATUS_CHECK:")
    print(f"  Files without status: {len(no_status)}")
    for filename in no_status:
        print(f"    {filename}")

    # --- Grade Validation ---
    illegal_grades = []
    grade_mismatches = []
    for path, grade in indexed_entries:
        if grade not in VALID_GRADES:
            illegal_grades.append((path, grade))
        else:
            expected = classify_grade_expected(path)
            if grade != expected:
                grade_mismatches.append((path, grade, expected))

    print(f"\nILLEGAL_GRADE={len(illegal_grades)}")
    for path, grade in illegal_grades:
        print(f"  ILLEGAL: {path} grade='{grade}'")

    print(f"GRADE_MISMATCH={len(grade_mismatches)}")
    for path, grade, expected in grade_mismatches:
        print(f"  MISMATCH: {path} index={grade} expected={expected}")

    base_passed = not missing and not extra and not duplicates and self_in
    grade_passed = not illegal_grades and not grade_mismatches
    passed = base_passed and grade_passed

    print("\n=== INDEX CHECK COMPLETE ===")
    print(f"FINAL_RESULT={'PASS' if passed else 'FAIL'}")

    if not passed:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
